namespace CommRouter;

partial class frmMain
{
    private System.ComponentModel.IContainer components = null!;

    // ─── Controls ─────────────────────────────────────────────────────────────

    private TabControl tabMain = null!;
    private TabPage tabMatrix = null!;
    private TabPage tabListeners = null!;
    private TabPage tabReceivers = null!;
    private TabPage tabLog = null!;

    // Matrix tab
    private DataGridView dgvMatrix = null!;
    private Label lblMatrixHint = null!;

    // Listeners tab
    private ListView lvListeners = null!;
    private Button btnAddListener = null!;
    private Button btnEditListener = null!;
    private Button btnDeleteListener = null!;

    // Receivers tab
    private ListView lvReceivers = null!;
    private Button btnAddReceiver = null!;
    private Button btnEditReceiver = null!;
    private Button btnDeleteReceiver = null!;

    // Log tab
    private RichTextBox txtLog = null!;
    private Button btnClearLog = null!;

    // Status bar
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;
    private ToolStripStatusLabel lblInfo = null!;
    private ToolStripStatusLabel lblHubStatus = null!;

    // Toolbar
    private ToolStrip toolStrip = null!;
    private ToolStripButton btnStart = null!;
    private ToolStripButton btnStop = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        // ── Form ──────────────────────────────────────────────────────────────
        Text = "ComRouter";
        Size = new Size(1100, 720);
        MinimumSize = new Size(800, 560);
        StartPosition = FormStartPosition.CenterScreen;

        // ── ToolStrip ─────────────────────────────────────────────────────────
        toolStrip = new ToolStrip { Dock = DockStyle.Top };
        btnStart = new ToolStripButton("▶ Start") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        btnStop  = new ToolStripButton("■ Stop")  { DisplayStyle = ToolStripItemDisplayStyle.Text };
        btnStart.Click += btnStart_Click;
        btnStop.Click  += btnStop_Click;
        toolStrip.Items.AddRange([btnStart, new ToolStripSeparator(), btnStop]);

        // ── TabControl ────────────────────────────────────────────────────────
        tabMain = new TabControl { Dock = DockStyle.Fill };

        tabMatrix    = new TabPage("Matrice");
        tabListeners = new TabPage("Listeners");
        tabReceivers = new TabPage("Receivers");
        tabLog       = new TabPage("Log");
        tabMain.TabPages.AddRange([tabMatrix, tabListeners, tabReceivers, tabLog]);

        // ── Matrix tab ────────────────────────────────────────────────────────
        lblMatrixHint = new Label
        {
            Text = "Doppio click su una cella per creare/modificare un match.",
            Dock = DockStyle.Bottom,
            Height = 20,
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 8),
        };

        dgvMatrix = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            RowTemplate = { Height = 40 },
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.None,
        };
        dgvMatrix.CellDoubleClick += dgvMatrix_CellDoubleClick;
        tabMatrix.Controls.AddRange([dgvMatrix, lblMatrixHint]);

        // ── Listeners tab ─────────────────────────────────────────────────────
        lvListeners = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
        };
        lvListeners.Columns.AddRange([
            new ColumnHeader { Text = "Nome",         Width = 160 },
            new ColumnHeader { Text = "Tipo",         Width = 120 },
            new ColumnHeader { Text = "Stato",        Width = 90  },
            new ColumnHeader { Text = "Configurazione", Width = 360 },
        ]);

        var pnlListenerBtns = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(4) };
        btnAddListener    = new Button { Text = "+ Aggiungi", Width = 90, Height = 26 };
        btnEditListener   = new Button { Text = "✏ Modifica", Width = 90, Height = 26 };
        btnDeleteListener = new Button { Text = "🗑 Elimina",  Width = 90, Height = 26 };
        btnAddListener.Click    += btnAddListener_Click;
        btnEditListener.Click   += btnEditListener_Click;
        btnDeleteListener.Click += btnDeleteListener_Click;
        pnlListenerBtns.Controls.AddRange([btnAddListener, btnEditListener, btnDeleteListener]);
        tabListeners.Controls.AddRange([lvListeners, pnlListenerBtns]);

        // ── Receivers tab ─────────────────────────────────────────────────────
        lvReceivers = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
        };
        lvReceivers.Columns.AddRange([
            new ColumnHeader { Text = "Nome",          Width = 160 },
            new ColumnHeader { Text = "Tipo",          Width = 120 },
            new ColumnHeader { Text = "Configurazione", Width = 460 },
        ]);

        var pnlReceiverBtns = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(4) };
        btnAddReceiver    = new Button { Text = "+ Aggiungi", Width = 90, Height = 26 };
        btnEditReceiver   = new Button { Text = "✏ Modifica", Width = 90, Height = 26 };
        btnDeleteReceiver = new Button { Text = "🗑 Elimina",  Width = 90, Height = 26 };
        btnAddReceiver.Click    += btnAddReceiver_Click;
        btnEditReceiver.Click   += btnEditReceiver_Click;
        btnDeleteReceiver.Click += btnDeleteReceiver_Click;
        pnlReceiverBtns.Controls.AddRange([btnAddReceiver, btnEditReceiver, btnDeleteReceiver]);
        tabReceivers.Controls.AddRange([lvReceivers, pnlReceiverBtns]);

        // ── Log tab ───────────────────────────────────────────────────────────
        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 8.5f),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };
        btnClearLog = new Button { Text = "Pulisci", Dock = DockStyle.Bottom, Height = 26 };
        btnClearLog.Click += btnClearLog_Click;
        tabLog.Controls.AddRange([txtLog, btnClearLog]);

        // ── StatusStrip ───────────────────────────────────────────────────────
        statusStrip = new StatusStrip();
        lblStatus    = new ToolStripStatusLabel("○ Stopped") { ForeColor = Color.Red };
        lblInfo      = new ToolStripStatusLabel("") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        lblHubStatus = new ToolStripStatusLabel("○ Hub") { ForeColor = Color.Red, Alignment = ToolStripItemAlignment.Right };
        statusStrip.Items.AddRange([lblStatus, lblInfo, lblHubStatus]);

        // ── Assemble ──────────────────────────────────────────────────────────
        Controls.AddRange([tabMain, toolStrip, statusStrip]);

        ResumeLayout(false);
    }
}
