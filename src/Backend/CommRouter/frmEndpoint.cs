using CommRouter.Core;
using CommRouter.Core.Plugins.Abstract;
using CommRouter.Interfaces;
using CommRouter.Interfaces.Dto;
using CommRouter.Panels;

namespace CommRouter;

/// <summary>
/// Dialog per creare o modificare un Listener o Receiver.
/// Mostra dinamicamente il pannello di configurazione specifico del tipo selezionato.
/// </summary>
public partial class frmEndpoint : Form
{
    // Result properties — used by frmMain after OK
    public EndpointResult? Result { get; private set; }

    private readonly object? _existing;   // ListenerDto | ReceiverDto | null
    private readonly List<PluginTypeDto> _types;
    private readonly bool _isListener;

    // Controls
    private TextBox _txtName = null!;
    private ComboBox _cboType = null!;
    private Panel _pnlConfig = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    // Current config panel (RS232Panel / TcpIpPanel / ProcessPanel / null)
    private Control? _configPanel;

    // Temp instance used to back the config panel
    private IConfigurable? _configInstance;

    public frmEndpoint(object? existing, List<PluginTypeDto> types, bool isListener)
    {
        _existing = existing;
        _types = types;
        _isListener = isListener;
        InitializeComponent();
        LoadValues();
    }

    private void LoadValues()
    {
        Text = _isListener
            ? (_existing == null ? "Nuovo Listener" : "Modifica Listener")
            : (_existing == null ? "Nuovo Receiver" : "Modifica Receiver");

        foreach (var t in _types)
            _cboType.Items.Add(new TypeItem(t));

        if (_existing is ListenerDto ld)
        {
            _txtName.Text = ld.Name;
            SelectType(ld.TypeName);
            ApplyExistingConfig(ld.Config);
        }
        else if (_existing is ReceiverDto rd)
        {
            _txtName.Text = rd.Name;
            SelectType(rd.TypeName);
            ApplyExistingConfig(rd.Config);
        }

        // Lock type change on edit
        _cboType.Enabled = _existing == null;
    }

    private void SelectType(string typeName)
    {
        for (int i = 0; i < _cboType.Items.Count; i++)
        {
            if (_cboType.Items[i] is TypeItem ti && ti.Dto.TypeName == typeName)
            {
                _cboType.SelectedIndex = i;
                return;
            }
        }
    }

    private void ApplyExistingConfig(IReadOnlyDictionary<string, string> config)
    {
        _configInstance?.SetConfig(config);
    }

    private void CboType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_cboType.SelectedItem is not TypeItem ti) return;

        // Instantiate a temp object to back the config panel
        try
        {
            _configInstance = PluginLoader.CreateInstance(ti.Dto.AssemblyName, ti.Dto.TypeName) as IConfigurable;
        }
        catch
        {
            _configInstance = null;
        }

        // Apply existing config if available
        if (_existing is ListenerDto ld) _configInstance?.SetConfig(ld.Config);
        else if (_existing is ReceiverDto rd) _configInstance?.SetConfig(rd.Config);

        // Swap config panel
        _pnlConfig.Controls.Clear();
        _configPanel = _configInstance != null ? ControlPanelFactory.CreatePanel(_configInstance) : null;
        if (_configPanel != null)
        {
            _configPanel.Dock = DockStyle.Fill;
            _pnlConfig.Controls.Add(_configPanel);
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (_cboType.SelectedItem is not TypeItem ti)
        {
            MessageBox.Show("Seleziona un tipo.", "ComRouter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var config = _configInstance?.GetConfig().ToDictionary() ?? new Dictionary<string, string>();

        Result = new EndpointResult
        {
            Name = _txtName.Text.Trim(),
            TypeName = ti.Dto.TypeName,
            AssemblyName = ti.Dto.AssemblyName,
            Config = config,
        };
        DialogResult = DialogResult.OK;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class TypeItem(PluginTypeDto dto)
    {
        public PluginTypeDto Dto { get; } = dto;
        public override string ToString() => Dto.DisplayName;
    }
}

/// <summary>Result DTO produced by frmEndpoint, used to call the REST API.</summary>
public sealed class EndpointResult
{
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public string AssemblyName { get; set; } = "";
    public Dictionary<string, string> Config { get; set; } = [];
}
