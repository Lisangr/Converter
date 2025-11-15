namespace Converter.WinForms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private Button btnAddFiles = null!;
    private Button btnStart = null!;
    private TextBox txtOutput = null!;
    private Button btnBrowseOutput = null!;
    private ComboBox cmbProfiles = null!;
    private ListView lvQueue = null!;
    private ImageList thumbnails = null!;
    private ContextMenuStrip queueMenu = null!;
    private ToolStripMenuItem cancelMenuItem = null!;
    private ToolStripMenuItem removeMenuItem = null!;
    private Label lblOutput = null!;
    private Label lblProfiles = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        btnAddFiles = new Button();
        btnStart = new Button();
        txtOutput = new TextBox();
        btnBrowseOutput = new Button();
        cmbProfiles = new ComboBox();
        lvQueue = new ListView();
        thumbnails = new ImageList(components);
        queueMenu = new ContextMenuStrip(components);
        cancelMenuItem = new ToolStripMenuItem();
        removeMenuItem = new ToolStripMenuItem();
        lblOutput = new Label();
        lblProfiles = new Label();
        SuspendLayout();
        // 
        // btnAddFiles
        // 
        btnAddFiles.Location = new Point(12, 12);
        btnAddFiles.Name = "btnAddFiles";
        btnAddFiles.Size = new Size(120, 32);
        btnAddFiles.Text = "Add Files";
        btnAddFiles.UseVisualStyleBackColor = true;
        btnAddFiles.Click += (_, _) => OnAddFilesClicked();
        // 
        // btnStart
        // 
        btnStart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnStart.Location = new Point(672, 12);
        btnStart.Name = "btnStart";
        btnStart.Size = new Size(120, 32);
        btnStart.Text = "Start";
        btnStart.UseVisualStyleBackColor = true;
        btnStart.Click += (_, _) => OnStartClicked();
        // 
        // lblProfiles
        // 
        lblProfiles.Location = new Point(12, 56);
        lblProfiles.AutoSize = true;
        lblProfiles.Text = "Preset";
        // 
        // cmbProfiles
        // 
        cmbProfiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cmbProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbProfiles.Location = new Point(12, 76);
        cmbProfiles.Name = "cmbProfiles";
        cmbProfiles.Size = new Size(780, 28);
        // 
        // lblOutput
        // 
        lblOutput.Location = new Point(12, 112);
        lblOutput.AutoSize = true;
        lblOutput.Text = "Output";
        // 
        // txtOutput
        // 
        txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtOutput.Location = new Point(12, 132);
        txtOutput.Name = "txtOutput";
        txtOutput.Size = new Size(672, 27);
        // 
        // btnBrowseOutput
        // 
        btnBrowseOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseOutput.Location = new Point(690, 132);
        btnBrowseOutput.Name = "btnBrowseOutput";
        btnBrowseOutput.Size = new Size(102, 27);
        btnBrowseOutput.Text = "Browse";
        btnBrowseOutput.UseVisualStyleBackColor = true;
        btnBrowseOutput.Click += (_, _) => OnBrowseClicked();
        // 
        // lvQueue
        // 
        lvQueue.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lvQueue.FullRowSelect = true;
        lvQueue.Location = new Point(12, 175);
        lvQueue.Name = "lvQueue";
        lvQueue.Size = new Size(780, 363);
        lvQueue.View = View.Details;
        lvQueue.SmallImageList = thumbnails;
        lvQueue.Columns.Add("Input", 250);
        lvQueue.Columns.Add("Output", 250);
        lvQueue.Columns.Add("Status", 120);
        lvQueue.Columns.Add("Progress", 120);
        lvQueue.ContextMenuStrip = queueMenu;
        // 
        // queueMenu
        // 
        queueMenu.Items.AddRange(new ToolStripItem[] { cancelMenuItem, removeMenuItem });
        cancelMenuItem.Text = "Cancel";
        cancelMenuItem.Click += (_, _) => OnCancelSelected();
        removeMenuItem.Text = "Remove";
        removeMenuItem.Click += (_, _) => OnRemoveSelected();
        // 
        // thumbnails
        // 
        thumbnails.ColorDepth = ColorDepth.Depth32Bit;
        thumbnails.ImageSize = new Size(64, 36);
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(804, 550);
        Controls.Add(lvQueue);
        Controls.Add(btnBrowseOutput);
        Controls.Add(txtOutput);
        Controls.Add(lblOutput);
        Controls.Add(cmbProfiles);
        Controls.Add(lblProfiles);
        Controls.Add(btnStart);
        Controls.Add(btnAddFiles);
        Name = "MainForm";
        Text = "Converter";
        ResumeLayout(false);
        PerformLayout();
    }
}
