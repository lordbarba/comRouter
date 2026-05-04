using CommRouter.Core.Plugins.Abstract;

namespace CommRouter.Panels;

/// <summary>Configuration panel for TCP/IP listener and receiver.</summary>
public sealed class TcpIpPanel : UserControl
{
    private readonly TcpIpBase _target;

    private TextBox _txtIp = null!;
    private TextBox _txtPort = null!;
    private Button _btnApply = null!;

    public TcpIpPanel(TcpIpBase target)
    {
        _target = target;
        BuildUI();
        LoadValues();
    }

    private void BuildUI()
    {
        int row = 4;
        const int labelW = 90, ctrlW = 160, h = 24, margin = 4;

        Controls.Add(MakeLabel("IP Address:", 4, row));
        _txtIp = new TextBox { Location = new Point(labelW + 8, row), Width = ctrlW };
        Controls.Add(_txtIp);
        row += h + margin;

        Controls.Add(MakeLabel("Port:", 4, row));
        _txtPort = new TextBox { Location = new Point(labelW + 8, row), Width = ctrlW };
        Controls.Add(_txtPort);
        row += h + margin + 4;

        _btnApply = new Button { Text = "Apply", Location = new Point(labelW + 8, row), Size = new Size(ctrlW, 26) };
        _btnApply.Click += BtnApply_Click;
        Controls.Add(_btnApply);

        Size = new Size(labelW + ctrlW + 24, row + 36);
    }

    private void LoadValues()
    {
        _txtIp.Text = _target.IpAddress;
        _txtPort.Text = _target.Port.ToString();
    }

    private void BtnApply_Click(object? sender, EventArgs e)
    {
        _target.IpAddress = _txtIp.Text.Trim();
        if (int.TryParse(_txtPort.Text.Trim(), out int p)) _target.Port = p;
    }

    private static Label MakeLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y + 4), Size = new Size(90, 20), AutoSize = false };
}
