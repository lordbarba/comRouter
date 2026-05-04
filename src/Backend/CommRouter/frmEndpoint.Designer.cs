namespace CommRouter;

partial class frmEndpoint
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

        Size = new Size(460, 400);
        MinimumSize = new Size(420, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        int pad = 10;

        // Name
        var lblName = new Label { Text = "Nome:", Location = new Point(pad, pad + 3), AutoSize = true };
        _txtName = new TextBox { Location = new Point(80, pad), Width = 330 };

        // Type selector
        var lblType = new Label { Text = "Tipo:", Location = new Point(pad, 40 + 3), AutoSize = true };
        _cboType = new ComboBox
        {
            Location = new Point(80, 40),
            Width = 330,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cboType.SelectedIndexChanged += CboType_SelectedIndexChanged;

        // Config panel area
        var lblConfig = new Label { Text = "Configurazione:", Location = new Point(pad, 76), AutoSize = true };
        _pnlConfig = new Panel
        {
            Location = new Point(pad, 96),
            Size = new Size(420, 220),
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true,
        };

        // Footer buttons
        _btnOk     = new Button { Text = "Salva",   Location = new Point(254, 330), Width = 80, Height = 28 };
        _btnCancel = new Button { Text = "Annulla", Location = new Point(344, 330), Width = 80, Height = 28 };
        _btnOk.Click     += BtnOk_Click;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.AddRange([
            lblName, _txtName,
            lblType, _cboType,
            lblConfig, _pnlConfig,
            _btnOk, _btnCancel,
        ]);

        ResumeLayout(false);
    }
}
