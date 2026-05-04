using CommRouter.Interfaces.Dto;

namespace CommRouter;

/// <summary>Result prodotto da frmMatch — contiene solo i campi editabili.</summary>
public sealed record MatchFormResult(string Name, bool Enabled, List<string> ListenerCommands, List<string> ReceiverCommands);

/// <summary>Dialog per creare o modificare un Match.</summary>
public partial class frmMatch : Form
{
    public MatchFormResult? Result { get; private set; }
    public bool DeleteRequested { get; private set; }

    private readonly ListenerDto _listener;
    private readonly ReceiverDto _receiver;
    private readonly MatchDto? _existing;

    // Controls
    private TextBox _txtName = null!;
    private CheckBox _chkEnabled = null!;
    private ListBox _lstListenerCmds = null!;
    private ListBox _lstReceiverCmds = null!;
    private TextBox _txtListenerCmd = null!;
    private TextBox _txtReceiverCmd = null!;
    private Button _btnAddListenerCmd = null!;
    private Button _btnRemoveListenerCmd = null!;
    private Button _btnAddReceiverCmd = null!;
    private Button _btnRemoveReceiverCmd = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;
    private Button _btnDelete = null!;

    public frmMatch(ListenerDto listener, ReceiverDto receiver, MatchDto? existing)
    {
        _listener = listener;
        _receiver = receiver;
        _existing = existing;
        InitializeComponent();
        LoadValues();
    }

    private void LoadValues()
    {
        _txtName.Text = _existing?.Name ?? $"{_listener.Name} → {_receiver.Name}";
        _chkEnabled.Checked = _existing?.Enabled ?? true;

        if (_existing != null)
        {
            foreach (var cmd in _existing.ListenerCommands)
                _lstListenerCmds.Items.Add(cmd);
            foreach (var cmd in _existing.ReceiverCommands)
                _lstReceiverCmds.Items.Add(cmd);
        }

        _btnDelete.Visible = _existing != null;
    }

    private void BtnAddListenerCmd_Click(object? sender, EventArgs e)
    {
        var v = _txtListenerCmd.Text.Trim();
        if (!string.IsNullOrEmpty(v)) { _lstListenerCmds.Items.Add(v); _txtListenerCmd.Clear(); }
    }

    private void BtnRemoveListenerCmd_Click(object? sender, EventArgs e)
    {
        if (_lstListenerCmds.SelectedIndex >= 0)
            _lstListenerCmds.Items.RemoveAt(_lstListenerCmds.SelectedIndex);
    }

    private void BtnAddReceiverCmd_Click(object? sender, EventArgs e)
    {
        var v = _txtReceiverCmd.Text.Trim();
        if (!string.IsNullOrEmpty(v)) { _lstReceiverCmds.Items.Add(v); _txtReceiverCmd.Clear(); }
    }

    private void BtnRemoveReceiverCmd_Click(object? sender, EventArgs e)
    {
        if (_lstReceiverCmds.SelectedIndex >= 0)
            _lstReceiverCmds.Items.RemoveAt(_lstReceiverCmds.SelectedIndex);
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        Result = new MatchFormResult(
            Name: _txtName.Text.Trim(),
            Enabled: _chkEnabled.Checked,
            ListenerCommands: _lstListenerCmds.Items.Cast<string>().ToList(),
            ReceiverCommands: _lstReceiverCmds.Items.Cast<string>().ToList()
        );
        DialogResult = DialogResult.OK;
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show($"Eliminare il match '{_txtName.Text}'?", "Conferma",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        DeleteRequested = true;
        DialogResult = DialogResult.OK;
    }
}
