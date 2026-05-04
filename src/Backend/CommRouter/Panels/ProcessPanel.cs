using CommRouter.Core.Plugins;

namespace CommRouter.Panels;

/// <summary>Configuration panel for ProcessReceiver.</summary>
public sealed class ProcessPanel : UserControl
{
    private readonly ProcessReceiver _target;

    private TextBox _txtPath = null!;
    private TextBox _txtArgs = null!;
    private Button _btnBrowse = null!;
    private Button _btnApply = null!;

    public ProcessPanel(ProcessReceiver target)
    {
        _target = target;
        BuildUI();
        LoadValues();
    }

    private void BuildUI()
    {
        int row = 4;
        const int labelW = 90, ctrlW = 200, h = 24, margin = 4;

        Controls.Add(MakeLabel("Process:", 4, row));
        _txtPath = new TextBox { Location = new Point(labelW + 8, row), Width = ctrlW - 32 };
        Controls.Add(_txtPath);
        _btnBrowse = new Button { Text = "...", Location = new Point(labelW + 8 + ctrlW - 28, row), Size = new Size(28, h) };
        _btnBrowse.Click += BtnBrowse_Click;
        Controls.Add(_btnBrowse);
        row += h + margin;

        Controls.Add(MakeLabel("Arguments:", 4, row));
        _txtArgs = new TextBox { Location = new Point(labelW + 8, row), Width = ctrlW };
        Controls.Add(_txtArgs);
        row += h + margin + 4;

        _btnApply = new Button { Text = "Apply", Location = new Point(labelW + 8, row), Size = new Size(ctrlW, 26) };
        _btnApply.Click += BtnApply_Click;
        Controls.Add(_btnApply);

        Size = new Size(labelW + ctrlW + 24, row + 36);
    }

    private void LoadValues()
    {
        _txtPath.Text = _target.ProcessPath;
        _txtArgs.Text = _target.Arguments;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select executable",
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
        };
        if (!string.IsNullOrEmpty(_txtPath.Text)) ofd.FileName = _txtPath.Text;
        if (ofd.ShowDialog() == DialogResult.OK) _txtPath.Text = ofd.FileName;
    }

    private void BtnApply_Click(object? sender, EventArgs e)
    {
        _target.ProcessPath = _txtPath.Text.Trim();
        _target.Arguments = _txtArgs.Text.Trim();
    }

    private static Label MakeLabel(string text, int x, int y) =>
        new() { Text = text, Location = new Point(x, y + 4), Size = new Size(90, 20), AutoSize = false };
}
