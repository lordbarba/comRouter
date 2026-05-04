namespace CommRouter;

partial class frmMatch
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        Text = _existing != null ? $"Modifica Match — {_existing.Name}" : "Nuovo Match";
        Size = new Size(620, 480);
        MinimumSize = new Size(560, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        int pad = 10;

        // ── Name + enabled ────────────────────────────────────────────────────
        var lblName = new Label { Text = "Nome:", Location = new Point(pad, pad + 3), AutoSize = true };
        _txtName = new TextBox { Location = new Point(80, pad), Width = 300 };

        _chkEnabled = new CheckBox { Text = "Abilitato", Location = new Point(395, pad + 2), AutoSize = true, Checked = true };

        // ── Commands panel ────────────────────────────────────────────────────
        int cmdTop = 44;
        int halfW  = 270;

        // Listener commands
        var lblLCmd = new Label
        {
            Text = $"Comandi Listener ({_listener.Name}):",
            Location = new Point(pad, cmdTop),
            AutoSize = true,
        };
        _lstListenerCmds = new ListBox
        {
            Location = new Point(pad, cmdTop + 20),
            Size = new Size(halfW, 200),
            Font = new Font("Consolas", 8.5f),
        };
        var pnlLCmdInput = new Panel { Location = new Point(pad, cmdTop + 224), Width = halfW, Height = 26 };
        _txtListenerCmd = new TextBox { Dock = DockStyle.Fill, Width = halfW - 36 };
        _btnAddListenerCmd = new Button { Text = "+", Dock = DockStyle.Right, Width = 30 };
        _btnRemoveListenerCmd = new Button { Text = "−", Location = new Point(pad, cmdTop + 254), Width = halfW, Height = 24 };
        _btnAddListenerCmd.Click    += BtnAddListenerCmd_Click;
        _btnRemoveListenerCmd.Click += BtnRemoveListenerCmd_Click;
        pnlLCmdInput.Controls.AddRange([_txtListenerCmd, _btnAddListenerCmd]);

        // Receiver commands
        int rx = pad + halfW + 10;
        var lblRCmd = new Label
        {
            Text = $"Comandi Receiver ({_receiver.Name}):",
            Location = new Point(rx, cmdTop),
            AutoSize = true,
        };
        _lstReceiverCmds = new ListBox
        {
            Location = new Point(rx, cmdTop + 20),
            Size = new Size(halfW, 200),
            Font = new Font("Consolas", 8.5f),
        };
        var pnlRCmdInput = new Panel { Location = new Point(rx, cmdTop + 224), Width = halfW, Height = 26 };
        _txtReceiverCmd = new TextBox { Dock = DockStyle.Fill };
        _btnAddReceiverCmd = new Button { Text = "+", Dock = DockStyle.Right, Width = 30 };
        _btnRemoveReceiverCmd = new Button { Text = "−", Location = new Point(rx, cmdTop + 254), Width = halfW, Height = 24 };
        _btnAddReceiverCmd.Click    += BtnAddReceiverCmd_Click;
        _btnRemoveReceiverCmd.Click += BtnRemoveReceiverCmd_Click;
        pnlRCmdInput.Controls.AddRange([_txtReceiverCmd, _btnAddReceiverCmd]);

        // ── Footer buttons ────────────────────────────────────────────────────
        int btnTop = cmdTop + 286;
        _btnDelete = new Button { Text = "Elimina", Location = new Point(pad, btnTop), Width = 80, Height = 28, BackColor = Color.MistyRose };
        _btnOk     = new Button { Text = "Salva",   Location = new Point(420, btnTop), Width = 80, Height = 28 };
        _btnCancel = new Button { Text = "Annulla", Location = new Point(508, btnTop), Width = 80, Height = 28 };

        _btnDelete.Click += BtnDelete_Click;
        _btnOk.Click     += BtnOk_Click;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.AddRange([
            lblName, _txtName, _chkEnabled,
            lblLCmd, _lstListenerCmds, pnlLCmdInput, _btnRemoveListenerCmd,
            lblRCmd, _lstReceiverCmds, pnlRCmdInput, _btnRemoveReceiverCmd,
            _btnDelete, _btnOk, _btnCancel,
        ]);

        ResumeLayout(false);
    }
}
