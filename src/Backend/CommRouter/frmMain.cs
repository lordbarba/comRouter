using CommRouter.Interfaces.Dto;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

namespace CommRouter;

/// <summary>
/// Client WinForms per ComRouter. Comunica con il WebServer tramite REST API e SignalR.
/// La logica di business risiede interamente nel WebServer — qui solo presentazione.
/// </summary>
public partial class frmMain : Form
{
    // ─── HTTP + SignalR ───────────────────────────────────────────────────────

    private readonly HttpClient _http;
    private HubConnection? _hub;

    // ─── State cache ──────────────────────────────────────────────────────────

    private List<ListenerDto> _listeners = [];
    private List<ReceiverDto> _receivers = [];
    private List<MatchDto> _matches = [];
    private RouterStatusDto? _status;

    public frmMain()
    {
        InitializeComponent();
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5025") };
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await ConnectHubAsync();
        await RefreshAllAsync();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_hub != null)
            await _hub.DisposeAsync();
    }

    // ─── SignalR ──────────────────────────────────────────────────────────────

    private async Task ConnectHubAsync()
    {
        _hub = new HubConnectionBuilder()
            .WithUrl("http://localhost:5025/hubs/router")
            .WithAutomaticReconnect()
            .Build();

        _hub.On("StateChanged", async () =>
        {
            await RefreshAllAsync();
        });

        _hub.On<LogEntryDto>("LogEntry", entry =>
        {
            AppendLog(entry);
        });

        _hub.Reconnecting += _ => { SetHubStatus(false); return Task.CompletedTask; };
        _hub.Reconnected += _ => { SetHubStatus(true); return Task.CompletedTask; };
        _hub.Closed += _ => { SetHubStatus(false); return Task.CompletedTask; };

        try
        {
            await _hub.StartAsync();
            SetHubStatus(true);
        }
        catch
        {
            SetHubStatus(false);
        }
    }

    private void SetHubStatus(bool connected)
    {
        if (InvokeRequired) { Invoke(() => SetHubStatus(connected)); return; }
        lblHubStatus.Text = connected ? "● Hub" : "○ Hub";
        lblHubStatus.ForeColor = connected ? Color.Green : Color.Red;
    }

    // ─── Data refresh ─────────────────────────────────────────────────────────

    private async Task RefreshAllAsync()
    {
        try
        {
            var tasks = new Task[]
            {
                Task.Run(async () => _listeners = await _http.GetFromJsonAsync<List<ListenerDto>>("/api/listeners") ?? []),
                Task.Run(async () => _receivers = await _http.GetFromJsonAsync<List<ReceiverDto>>("/api/receivers") ?? []),
                Task.Run(async () => _matches   = await _http.GetFromJsonAsync<List<MatchDto>>("/api/matches") ?? []),
                Task.Run(async () => _status    = await _http.GetFromJsonAsync<RouterStatusDto>("/api/router/status")),
            };
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            AppendLog(new LogEntryDto(DateTime.Now.ToString("o"), "error", $"Refresh error: {ex.Message}"));
        }

        if (InvokeRequired) { Invoke(RebuildUI); return; }
        RebuildUI();
    }

    // ─── UI rebuild ───────────────────────────────────────────────────────────

    private void RebuildUI()
    {
        UpdateStatusBar();
        RebuildListenersTab();
        RebuildReceiversTab();
        RebuildMatrixTab();
    }

    private void UpdateStatusBar()
    {
        bool running = _status?.IsRunning ?? false;
        lblStatus.Text = running ? "● Running" : "○ Stopped";
        lblStatus.ForeColor = running ? Color.Green : Color.Red;
        btnStart.Enabled = !running;
        btnStop.Enabled = running;
        lblInfo.Text = $"Listeners: {_status?.ListenersCount ?? 0}  Receivers: {_status?.ReceiversCount ?? 0}  Matches: {_status?.MatchesCount ?? 0}";
    }

    // ─── Listeners tab ────────────────────────────────────────────────────────

    private void RebuildListenersTab()
    {
        lvListeners.BeginUpdate();
        lvListeners.Items.Clear();
        foreach (var l in _listeners)
        {
            var item = new ListViewItem(l.Name);
            item.SubItems.Add(l.TypeName.Split('.').Last());
            item.SubItems.Add(l.IsListening ? "In ascolto" : "Fermo");
            item.SubItems.Add(string.Join(", ", l.Config.Select(kv => $"{kv.Key}={kv.Value}")));
            item.ForeColor = l.IsListening ? Color.DarkGreen : Color.Gray;
            item.Tag = l;
            lvListeners.Items.Add(item);
        }
        lvListeners.EndUpdate();
    }

    // ─── Receivers tab ────────────────────────────────────────────────────────

    private void RebuildReceiversTab()
    {
        lvReceivers.BeginUpdate();
        lvReceivers.Items.Clear();
        foreach (var r in _receivers)
        {
            var item = new ListViewItem(r.Name);
            item.SubItems.Add(r.TypeName.Split('.').Last());
            item.SubItems.Add(string.Join(", ", r.Config.Select(kv => $"{kv.Key}={kv.Value}")));
            item.Tag = r;
            lvReceivers.Items.Add(item);
        }
        lvReceivers.EndUpdate();
    }

    // ─── Matrix tab ───────────────────────────────────────────────────────────

    private void RebuildMatrixTab()
    {
        dgvMatrix.Rows.Clear();
        dgvMatrix.Columns.Clear();

        if (_listeners.Count == 0 || _receivers.Count == 0) return;

        // Header column: listener names
        dgvMatrix.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Listener \\ Receiver",
            Name = "col_listener",
            Width = 140,
            ReadOnly = true,
        });

        // One column per receiver
        foreach (var r in _receivers)
        {
            dgvMatrix.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = r.Name,
                Name = $"col_{r.Id}",
                Width = 120,
                Tag = r,
            });
        }

        // One row per listener
        foreach (var l in _listeners)
        {
            var row = new DataGridViewRow();
            row.Tag = l;

            // First cell: listener name
            var headerCell = new DataGridViewTextBoxCell { Value = l.Name };
            row.Cells.Add(headerCell);

            // Cell per receiver
            foreach (var r in _receivers)
            {
                var match = _matches.FirstOrDefault(m => m.ListenerId == l.Id && m.ReceiverId == r.Id);
                var cell = new DataGridViewTextBoxCell
                {
                    Value = match?.Name ?? "",
                    Tag = match,
                    Style =
                    {
                        BackColor = match == null ? Color.WhiteSmoke
                                  : match.Enabled ? Color.FromArgb(220, 255, 220)
                                  : Color.FromArgb(255, 220, 220),
                    },
                };
                row.Cells.Add(cell);
            }

            dgvMatrix.Rows.Add(row);
        }
    }

    // ─── Matrix double-click → open match dialog ──────────────────────────────

    private async void dgvMatrix_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex <= 0) return; // skip header row/col

        var listener = _listeners[e.RowIndex];
        var receiver = (ReceiverDto)dgvMatrix.Columns[e.ColumnIndex].Tag!;
        var existing = _matches.FirstOrDefault(m => m.ListenerId == listener.Id && m.ReceiverId == receiver.Id);

        using var dlg = new frmMatch(listener, receiver, existing);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            if (dlg.DeleteRequested && existing != null)
            {
                await _http.DeleteAsync($"/api/matches/{existing.Id}");
            }
            else if (existing != null)
            {
                var req = new UpdateMatchRequest(dlg.Result!.Name, dlg.Result.Enabled, dlg.Result.ListenerCommands, dlg.Result.ReceiverCommands);
                await _http.PutAsJsonAsync($"/api/matches/{existing.Id}", req);
            }
            else
            {
                var req = new CreateMatchRequest(dlg.Result!.Name, listener.Id, receiver.Id, dlg.Result.Enabled, dlg.Result.ListenerCommands, dlg.Result.ReceiverCommands);
                await _http.PostAsJsonAsync("/api/matches", req);
            }
            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore: {ex.Message}", "ComRouter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─── Listeners CRUD ───────────────────────────────────────────────────────

    private async void btnAddListener_Click(object sender, EventArgs e)
    {
        var types = await _http.GetFromJsonAsync<List<PluginTypeDto>>("/api/types/listeners") ?? [];
        using var dlg = new frmEndpoint(null, types, isListener: true);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _http.PostAsJsonAsync("/api/listeners", dlg.Result);
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    private async void btnEditListener_Click(object sender, EventArgs e)
    {
        if (lvListeners.SelectedItems.Count == 0) return;
        var l = (ListenerDto)lvListeners.SelectedItems[0].Tag!;
        var types = await _http.GetFromJsonAsync<List<PluginTypeDto>>("/api/types/listeners") ?? [];
        using var dlg = new frmEndpoint(l, types, isListener: true);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _http.PutAsJsonAsync($"/api/listeners/{l.Id}", new { dlg.Result!.Name, dlg.Result.Config });
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    private async void btnDeleteListener_Click(object sender, EventArgs e)
    {
        if (lvListeners.SelectedItems.Count == 0) return;
        var l = (ListenerDto)lvListeners.SelectedItems[0].Tag!;
        if (MessageBox.Show($"Eliminare il listener '{l.Name}'?", "Conferma", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            await _http.DeleteAsync($"/api/listeners/{l.Id}");
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    // ─── Receivers CRUD ───────────────────────────────────────────────────────

    private async void btnAddReceiver_Click(object sender, EventArgs e)
    {
        var types = await _http.GetFromJsonAsync<List<PluginTypeDto>>("/api/types/receivers") ?? [];
        using var dlg = new frmEndpoint(null, types, isListener: false);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _http.PostAsJsonAsync("/api/receivers", dlg.Result);
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    private async void btnEditReceiver_Click(object sender, EventArgs e)
    {
        if (lvReceivers.SelectedItems.Count == 0) return;
        var r = (ReceiverDto)lvReceivers.SelectedItems[0].Tag!;
        var types = await _http.GetFromJsonAsync<List<PluginTypeDto>>("/api/types/receivers") ?? [];
        using var dlg = new frmEndpoint(r, types, isListener: false);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            await _http.PutAsJsonAsync($"/api/receivers/{r.Id}", new { dlg.Result!.Name, dlg.Result.Config });
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    private async void btnDeleteReceiver_Click(object sender, EventArgs e)
    {
        if (lvReceivers.SelectedItems.Count == 0) return;
        var r = (ReceiverDto)lvReceivers.SelectedItems[0].Tag!;
        if (MessageBox.Show($"Eliminare il receiver '{r.Name}'?", "Conferma", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            await _http.DeleteAsync($"/api/receivers/{r.Id}");
            await RefreshAllAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    // ─── Router start/stop ────────────────────────────────────────────────────

    private async void btnStart_Click(object sender, EventArgs e)
    {
        try { await _http.PostAsync("/api/router/start", null); await RefreshAllAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    private async void btnStop_Click(object sender, EventArgs e)
    {
        try { await _http.PostAsync("/api/router/stop", null); await RefreshAllAsync(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Errore"); }
    }

    // ─── Log panel ────────────────────────────────────────────────────────────

    private void AppendLog(LogEntryDto entry)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(entry)); return; }

        var time = DateTime.TryParse(entry.Timestamp, out var dt) ? dt.ToLocalTime().ToString("HH:mm:ss") : "??:??:??";
        var line = $"[{time}] [{entry.Level.ToUpperInvariant()}] {entry.Message}";

        txtLog.AppendText(line + Environment.NewLine);
        // Limit log size
        if (txtLog.Lines.Length > 500)
        {
            var lines = txtLog.Lines.Skip(100).ToArray();
            txtLog.Lines = lines;
        }
    }

    private void btnClearLog_Click(object sender, EventArgs e) => txtLog.Clear();
}
