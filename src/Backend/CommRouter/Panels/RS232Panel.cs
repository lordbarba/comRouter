using System.IO.Ports;
using CommRouter.Core.Plugins.Abstract;

namespace CommRouter.Panels;

/// <summary>Configuration panel for RS232 listener and receiver.</summary>
public sealed class RS232Panel : UserControl
{
    private readonly RS232Base _target;

    private ComboBox _cboCom = null!;
    private ComboBox _cboBaud = null!;
    private ComboBox _cboDataBits = null!;
    private ComboBox _cboParity = null!;
    private ComboBox _cboStopBits = null!;
    private ComboBox _cboHandshake = null!;
    private Button _btnApply = null!;

    public RS232Panel(RS232Base target)
    {
        _target = target;
        BuildUI();
        LoadValues();
    }

    private void BuildUI()
    {
        int row = 4;
        const int labelW = 90, ctrlW = 150, h = 24, margin = 4;

        Controls.Add(MakeLabel("COM Port:", 4, row));
        _cboCom = MakeCombo(labelW + 8, row, ctrlW, SerialPort.GetPortNames());
        row += h + margin;

        Controls.Add(MakeLabel("Baud Rate:", 4, row));
        _cboBaud = MakeCombo(labelW + 8, row, ctrlW, ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200"]);
        row += h + margin;

        Controls.Add(MakeLabel("Data Bits:", 4, row));
        _cboDataBits = MakeCombo(labelW + 8, row, ctrlW, ["5", "6", "7", "8"]);
        row += h + margin;

        Controls.Add(MakeLabel("Parity:", 4, row));
        _cboParity = MakeCombo(labelW + 8, row, ctrlW, Enum.GetNames<Parity>());
        row += h + margin;

        Controls.Add(MakeLabel("Stop Bits:", 4, row));
        _cboStopBits = MakeCombo(labelW + 8, row, ctrlW, Enum.GetNames<StopBits>());
        row += h + margin;

        Controls.Add(MakeLabel("Handshake:", 4, row));
        _cboHandshake = MakeCombo(labelW + 8, row, ctrlW, Enum.GetNames<Handshake>());
        row += h + margin + 4;

        _btnApply = new Button { Text = "Apply", Location = new Point(labelW + 8, row), Size = new Size(ctrlW, 26) };
        _btnApply.Click += BtnApply_Click;
        Controls.Add(_btnApply);

        Size = new Size(labelW + ctrlW + 24, row + 36);
    }

    private void LoadValues()
    {
        SelectOrFirst(_cboCom, _target.PortName);
        SelectOrFirst(_cboBaud, _target.BaudRate.ToString());
        SelectOrFirst(_cboDataBits, _target.DataBits.ToString());
        SelectOrFirst(_cboParity, _target.Parity.ToString());
        SelectOrFirst(_cboStopBits, _target.StopBits.ToString());
        SelectOrFirst(_cboHandshake, _target.Handshake.ToString());
    }

    private void BtnApply_Click(object? sender, EventArgs e)
    {
        if (_cboCom.SelectedItem is string com) _target.PortName = com;
        if (_cboBaud.SelectedItem is string baud && int.TryParse(baud, out int bv)) _target.BaudRate = bv;
        if (_cboDataBits.SelectedItem is string db && int.TryParse(db, out int dbv)) _target.DataBits = dbv;
        if (_cboParity.SelectedItem is string parity && Enum.TryParse<Parity>(parity, out var pv)) _target.Parity = pv;
        if (_cboStopBits.SelectedItem is string sb && Enum.TryParse<StopBits>(sb, out var sv)) _target.StopBits = sv;
        if (_cboHandshake.SelectedItem is string hs && Enum.TryParse<Handshake>(hs, out var hv)) _target.Handshake = hv;
    }

    private Label MakeLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y + 4), Size = new Size(90, 20), AutoSize = false };

    private ComboBox MakeCombo(int x, int y, int w, IEnumerable<string> items)
    {
        var cbo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x, y), Width = w };
        cbo.Items.AddRange(items.Cast<object>().ToArray());
        Controls.Add(cbo);
        return cbo;
    }

    private static void SelectOrFirst(ComboBox cbo, string value)
    {
        int idx = cbo.Items.IndexOf(value);
        cbo.SelectedIndex = idx >= 0 ? idx : (cbo.Items.Count > 0 ? 0 : -1);
    }
}
