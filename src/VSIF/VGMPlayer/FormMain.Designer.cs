
namespace zanac.VGMPlayer
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && (currentSong != null))
            {
                currentSong.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fILEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addPlaylistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadPlaylistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsThePlaylistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.eXITToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cLOUDToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vgmripsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.oPTIONSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aBOUTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanelButton = new System.Windows.Forms.TableLayoutPanel();
            this.buttonPrev = new System.Windows.Forms.Button();
            this.imageListSmall = new System.Windows.Forms.ImageList(this.components);
            this.buttonFast = new System.Windows.Forms.Button();
            this.buttonSlow = new System.Windows.Forms.Button();
            this.buttonFreeze = new System.Windows.Forms.Button();
            this.buttonPlay = new System.Windows.Forms.Button();
            this.imageListBig = new System.Windows.Forms.ImageList(this.components);
            this.labelSpeed = new System.Windows.Forms.Label();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonNext = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonEject = new System.Windows.Forms.Button();
            this.labelElapsed = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.label19 = new System.Windows.Forms.Label();
            this.contextMenuStripList = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.playToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.explorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelLoad = new System.Windows.Forms.Label();
            this.progressBarLoad = new System.Windows.Forms.ProgressBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label10 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxTitle = new System.Windows.Forms.TextBox();
            this.textBoxUseSystem = new System.Windows.Forms.TextBox();
            this.textBoxSongSystem = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.saveFileDialogM3U = new System.Windows.Forms.SaveFileDialog();
            this.checkBoxLoop = new System.Windows.Forms.CheckBox();
            this.numericUpDownLooped = new System.Windows.Forms.NumericUpDown();
            this.checkBoxLoopTimes = new System.Windows.Forms.CheckBox();
            this.dateTimePickerLoopTimes = new System.Windows.Forms.DateTimePicker();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBoxCnvClk = new System.Windows.Forms.CheckBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.checkBoxClip = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanelPort = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxPCE = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxSCC = new System.Windows.Forms.ComboBox();
            this.comboBoxSccSlot = new System.Windows.Forms.ComboBox();
            this.numericUpDownClkWidthDCSG = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.comboBoxPortYm2413 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxDCSG = new System.Windows.Forms.ComboBox();
            this.comboBoxPortSN76489 = new System.Windows.Forms.ComboBox();
            this.comboBoxPortSCC = new System.Windows.Forms.ComboBox();
            this.numericUpDownOPLL = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownSCC = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.comboBoxY8910 = new System.Windows.Forms.ComboBox();
            this.numericUpDownY8910 = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortY8910 = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.checkBoxConnDCSG = new System.Windows.Forms.CheckBox();
            this.checkBoxConnOPLL = new System.Windows.Forms.CheckBox();
            this.checkBoxConnSCC = new System.Windows.Forms.CheckBox();
            this.checkBoxConnY8910 = new System.Windows.Forms.CheckBox();
            this.checkBoxConnOPN2 = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxOPN2 = new System.Windows.Forms.ComboBox();
            this.numericUpDownOPN2 = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortYM2612 = new System.Windows.Forms.ComboBox();
            this.comboBoxSccType = new System.Windows.Forms.ComboBox();
            this.checkBoxConnOPM = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.numericUpDownOPM = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortOPM = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxOPM = new System.Windows.Forms.ComboBox();
            this.comboBoxOpmSlot = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxOPLL = new System.Windows.Forms.ComboBox();
            this.comboBoxOpllSlot = new System.Windows.Forms.ComboBox();
            this.checkBoxConnOPL3 = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.comboBoxOPL3 = new System.Windows.Forms.ComboBox();
            this.numericUpDownOPL3 = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortOPL3 = new System.Windows.Forms.ComboBox();
            this.checkBoxConnOPNA = new System.Windows.Forms.CheckBox();
            this.label15 = new System.Windows.Forms.Label();
            this.comboBoxOPNA = new System.Windows.Forms.ComboBox();
            this.numericUpDownOPNA = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortOPNA = new System.Windows.Forms.ComboBox();
            this.checkBoxConnY8950 = new System.Windows.Forms.CheckBox();
            this.label16 = new System.Windows.Forms.Label();
            this.numericUpDownY8950 = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortY8950 = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxY8950 = new System.Windows.Forms.ComboBox();
            this.comboBoxY8950Slot = new System.Windows.Forms.ComboBox();
            this.checkBoxConnOPN = new System.Windows.Forms.CheckBox();
            this.label13 = new System.Windows.Forms.Label();
            this.comboBoxOPN = new System.Windows.Forms.ComboBox();
            this.numericUpDownOPN = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortOPN = new System.Windows.Forms.ComboBox();
            this.label17 = new System.Windows.Forms.Label();
            this.numericUpDownOPN2Div = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownDCSGDiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownOPLLDiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownSCCDiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownPSGDiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownOPMDiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownOPL3Div = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownOPNADiv = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown8950Div = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownOPNDiv = new System.Windows.Forms.NumericUpDown();
            this.checkBoxConnNES = new System.Windows.Forms.CheckBox();
            this.label20 = new System.Windows.Forms.Label();
            this.comboBoxNES = new System.Windows.Forms.ComboBox();
            this.numericUpDownNES = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortNES = new System.Windows.Forms.ComboBox();
            this.numericUpDownNESDiv = new System.Windows.Forms.NumericUpDown();
            this.checkBoxConnMCD = new System.Windows.Forms.CheckBox();
            this.label21 = new System.Windows.Forms.Label();
            this.comboBoxMCD = new System.Windows.Forms.ComboBox();
            this.numericUpDownMCD = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortMCD = new System.Windows.Forms.ComboBox();
            this.numericUpDownMCDDiv = new System.Windows.Forms.NumericUpDown();
            this.checkBoxConnSAA = new System.Windows.Forms.CheckBox();
            this.label22 = new System.Windows.Forms.Label();
            this.comboBoxSAA = new System.Windows.Forms.ComboBox();
            this.numericUpDownSAA = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortSAA = new System.Windows.Forms.ComboBox();
            this.numericUpDownSAADiv = new System.Windows.Forms.NumericUpDown();
            this.checkBoxConnPCE = new System.Windows.Forms.CheckBox();
            this.label23 = new System.Windows.Forms.Label();
            this.comboBoxPortPCE = new System.Windows.Forms.ComboBox();
            this.checkBoxConnOPNB = new System.Windows.Forms.CheckBox();
            this.comboBoxOPNB = new System.Windows.Forms.ComboBox();
            this.comboBoxPortOPNB = new System.Windows.Forms.ComboBox();
            this.comboBoxOPNBType = new System.Windows.Forms.ComboBox();
            this.checkBoxConnPWM = new System.Windows.Forms.CheckBox();
            this.label24 = new System.Windows.Forms.Label();
            this.comboBoxPWM = new System.Windows.Forms.ComboBox();
            this.numericUpDownPWM = new System.Windows.Forms.NumericUpDown();
            this.comboBoxPortPWM = new System.Windows.Forms.ComboBox();
            this.numericUpDownPWMDiv = new System.Windows.Forms.NumericUpDown();
            this.comboBoxWaitAlg = new System.Windows.Forms.ComboBox();
            this.listViewList = new ListViewInsertionDrag.DraggableListView();
            this.columnHeaderFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.tableLayoutPanelButton.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.contextMenuStripList.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLooped)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.tableLayoutPanelPort.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownClkWidthDCSG)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPLL)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSCC)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownY8910)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPM)).BeginInit();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPL3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownY8950)).BeginInit();
            this.tableLayoutPanel7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN2Div)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDCSGDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPLLDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSCCDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPSGDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPMDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPL3Div)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNADiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown8950Div)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownNES)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownNESDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMCD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMCDDiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSAA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSAADiv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPWM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPWMDiv)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fILEToolStripMenuItem,
            this.cLOUDToolStripMenuItem,
            this.oPTIONSToolStripMenuItem,
            this.aBOUTToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(759, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fILEToolStripMenuItem
            // 
            this.fILEToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addPlaylistToolStripMenuItem,
            this.loadPlaylistToolStripMenuItem,
            this.saveAsThePlaylistToolStripMenuItem,
            this.toolStripSeparator1,
            this.eXITToolStripMenuItem});
            this.fILEToolStripMenuItem.Name = "fILEToolStripMenuItem";
            this.fILEToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.fILEToolStripMenuItem.Text = "&FILE";
            // 
            // addPlaylistToolStripMenuItem
            // 
            this.addPlaylistToolStripMenuItem.Name = "addPlaylistToolStripMenuItem";
            this.addPlaylistToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.addPlaylistToolStripMenuItem.Size = new System.Drawing.Size(285, 22);
            this.addPlaylistToolStripMenuItem.Text = "&Add music files...";
            this.addPlaylistToolStripMenuItem.Click += new System.EventHandler(this.addPlaylistToolStripMenuItem_Click);
            // 
            // loadPlaylistToolStripMenuItem
            // 
            this.loadPlaylistToolStripMenuItem.Name = "loadPlaylistToolStripMenuItem";
            this.loadPlaylistToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.L)));
            this.loadPlaylistToolStripMenuItem.Size = new System.Drawing.Size(285, 22);
            this.loadPlaylistToolStripMenuItem.Text = "&Load and play music files...";
            this.loadPlaylistToolStripMenuItem.Click += new System.EventHandler(this.loadPlaylistToolStripMenuItem_Click);
            // 
            // saveAsThePlaylistToolStripMenuItem
            // 
            this.saveAsThePlaylistToolStripMenuItem.Name = "saveAsThePlaylistToolStripMenuItem";
            this.saveAsThePlaylistToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveAsThePlaylistToolStripMenuItem.Size = new System.Drawing.Size(285, 22);
            this.saveAsThePlaylistToolStripMenuItem.Text = "&Save playlist...";
            this.saveAsThePlaylistToolStripMenuItem.Click += new System.EventHandler(this.saveAsThePlaylistToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(282, 6);
            // 
            // eXITToolStripMenuItem
            // 
            this.eXITToolStripMenuItem.Name = "eXITToolStripMenuItem";
            this.eXITToolStripMenuItem.Size = new System.Drawing.Size(285, 22);
            this.eXITToolStripMenuItem.Text = "&Exit";
            // 
            // cLOUDToolStripMenuItem
            // 
            this.cLOUDToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.vgmripsToolStripMenuItem});
            this.cLOUDToolStripMenuItem.Name = "cLOUDToolStripMenuItem";
            this.cLOUDToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.cLOUDToolStripMenuItem.Text = "&CLOUD";
            // 
            // vgmripsToolStripMenuItem
            // 
            this.vgmripsToolStripMenuItem.Name = "vgmripsToolStripMenuItem";
            this.vgmripsToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.vgmripsToolStripMenuItem.Text = "&vgmrips";
            this.vgmripsToolStripMenuItem.Click += new System.EventHandler(this.vgmripsToolStripMenuItem_Click);
            // 
            // oPTIONSToolStripMenuItem
            // 
            this.oPTIONSToolStripMenuItem.Name = "oPTIONSToolStripMenuItem";
            this.oPTIONSToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
            this.oPTIONSToolStripMenuItem.Text = "&OPTIONS";
            this.oPTIONSToolStripMenuItem.Click += new System.EventHandler(this.oPTIONSToolStripMenuItem_Click);
            // 
            // aBOUTToolStripMenuItem
            // 
            this.aBOUTToolStripMenuItem.Name = "aBOUTToolStripMenuItem";
            this.aBOUTToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.aBOUTToolStripMenuItem.Text = "&ABOUT";
            this.aBOUTToolStripMenuItem.Click += new System.EventHandler(this.aBOUTToolStripMenuItem_Click);
            // 
            // tableLayoutPanelButton
            // 
            this.tableLayoutPanelButton.ColumnCount = 12;
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 59F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.Controls.Add(this.buttonPrev, 0, 0);
            this.tableLayoutPanelButton.Controls.Add(this.checkBoxLoop, 7, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonFast, 6, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonSlow, 5, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonFreeze, 4, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonPlay, 1, 0);
            this.tableLayoutPanelButton.Controls.Add(this.labelSpeed, 5, 1);
            this.tableLayoutPanelButton.Controls.Add(this.numericUpDownLooped, 7, 1);
            this.tableLayoutPanelButton.Controls.Add(this.buttonStop, 3, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonNext, 2, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonClear, 11, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonEject, 10, 0);
            this.tableLayoutPanelButton.Controls.Add(this.checkBoxLoopTimes, 8, 0);
            this.tableLayoutPanelButton.Controls.Add(this.dateTimePickerLoopTimes, 8, 1);
            this.tableLayoutPanelButton.Controls.Add(this.labelElapsed, 2, 1);
            this.tableLayoutPanelButton.Controls.Add(this.tableLayoutPanel3, 9, 0);
            this.tableLayoutPanelButton.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(0, 511);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 2;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(759, 85);
            this.tableLayoutPanelButton.TabIndex = 4;
            // 
            // buttonPrev
            // 
            this.buttonPrev.AutoSize = true;
            this.buttonPrev.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPrev.FlatAppearance.BorderSize = 0;
            this.buttonPrev.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonPrev.ImageIndex = 0;
            this.buttonPrev.ImageList = this.imageListSmall;
            this.buttonPrev.Location = new System.Drawing.Point(3, 3);
            this.buttonPrev.Name = "buttonPrev";
            this.buttonPrev.Size = new System.Drawing.Size(38, 54);
            this.buttonPrev.TabIndex = 0;
            this.buttonPrev.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonPrev, "Previous Track");
            this.buttonPrev.UseVisualStyleBackColor = true;
            this.buttonPrev.Click += new System.EventHandler(this.buttonPrev_Click);
            // 
            // imageListSmall
            // 
            this.imageListSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListSmall.ImageStream")));
            this.imageListSmall.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListSmall.Images.SetKeyName(0, "Prev.png");
            this.imageListSmall.Images.SetKeyName(1, "Next.png");
            this.imageListSmall.Images.SetKeyName(2, "Stop.png");
            this.imageListSmall.Images.SetKeyName(3, "Freeze.png");
            this.imageListSmall.Images.SetKeyName(4, "Slow.png");
            this.imageListSmall.Images.SetKeyName(5, "Fast.png");
            this.imageListSmall.Images.SetKeyName(6, "Loop.png");
            this.imageListSmall.Images.SetKeyName(7, "Clear.png");
            this.imageListSmall.Images.SetKeyName(8, "Eject.png");
            this.imageListSmall.Images.SetKeyName(9, "Loop_Time.png");
            // 
            // buttonFast
            // 
            this.buttonFast.AutoSize = true;
            this.buttonFast.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonFast.FlatAppearance.BorderSize = 0;
            this.buttonFast.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFast.ImageIndex = 5;
            this.buttonFast.ImageList = this.imageListSmall;
            this.buttonFast.Location = new System.Drawing.Point(321, 3);
            this.buttonFast.Name = "buttonFast";
            this.buttonFast.Size = new System.Drawing.Size(38, 54);
            this.buttonFast.TabIndex = 7;
            this.buttonFast.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonFast, "Fast");
            this.buttonFast.UseVisualStyleBackColor = true;
            this.buttonFast.Click += new System.EventHandler(this.buttonFast_Click);
            // 
            // buttonSlow
            // 
            this.buttonSlow.AutoSize = true;
            this.buttonSlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonSlow.FlatAppearance.BorderSize = 0;
            this.buttonSlow.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSlow.ImageIndex = 4;
            this.buttonSlow.ImageList = this.imageListSmall;
            this.buttonSlow.Location = new System.Drawing.Point(277, 3);
            this.buttonSlow.Name = "buttonSlow";
            this.buttonSlow.Size = new System.Drawing.Size(38, 54);
            this.buttonSlow.TabIndex = 5;
            this.buttonSlow.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonSlow, "Slow");
            this.buttonSlow.UseVisualStyleBackColor = true;
            this.buttonSlow.Click += new System.EventHandler(this.buttonSlow_Click);
            // 
            // buttonFreeze
            // 
            this.buttonFreeze.AutoSize = true;
            this.buttonFreeze.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFreeze.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonFreeze.FlatAppearance.BorderSize = 0;
            this.buttonFreeze.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonFreeze.ImageIndex = 3;
            this.buttonFreeze.ImageList = this.imageListSmall;
            this.buttonFreeze.Location = new System.Drawing.Point(233, 3);
            this.buttonFreeze.Name = "buttonFreeze";
            this.buttonFreeze.Size = new System.Drawing.Size(38, 54);
            this.buttonFreeze.TabIndex = 4;
            this.buttonFreeze.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonFreeze, "Freeze");
            this.buttonFreeze.UseVisualStyleBackColor = true;
            this.buttonFreeze.Click += new System.EventHandler(this.buttonFreeze_Click);
            // 
            // buttonPlay
            // 
            this.buttonPlay.AllowDrop = true;
            this.buttonPlay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPlay.FlatAppearance.BorderSize = 0;
            this.buttonPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonPlay.ImageIndex = 0;
            this.buttonPlay.ImageList = this.imageListBig;
            this.buttonPlay.Location = new System.Drawing.Point(47, 3);
            this.buttonPlay.Name = "buttonPlay";
            this.tableLayoutPanelButton.SetRowSpan(this.buttonPlay, 2);
            this.buttonPlay.Size = new System.Drawing.Size(92, 79);
            this.buttonPlay.TabIndex = 1;
            this.buttonPlay.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonPlay, "Play/Pause");
            this.buttonPlay.UseVisualStyleBackColor = true;
            this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
            this.buttonPlay.DragDrop += new System.Windows.Forms.DragEventHandler(this.buttonPlay_DragDrop);
            this.buttonPlay.DragEnter += new System.Windows.Forms.DragEventHandler(this.buttonPlay_DragEnter);
            // 
            // imageListBig
            // 
            this.imageListBig.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListBig.ImageStream")));
            this.imageListBig.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListBig.Images.SetKeyName(0, "Play.png");
            this.imageListBig.Images.SetKeyName(1, "Pause.png");
            // 
            // labelSpeed
            // 
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tableLayoutPanelButton.SetColumnSpan(this.labelSpeed, 2);
            this.labelSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelSpeed.Location = new System.Drawing.Point(277, 60);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(82, 25);
            this.labelSpeed.TabIndex = 6;
            this.labelSpeed.Text = "1.00x";
            this.labelSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonStop
            // 
            this.buttonStop.AutoSize = true;
            this.buttonStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonStop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonStop.FlatAppearance.BorderSize = 0;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStop.ImageIndex = 2;
            this.buttonStop.ImageList = this.imageListSmall;
            this.buttonStop.Location = new System.Drawing.Point(189, 3);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(38, 54);
            this.buttonStop.TabIndex = 3;
            this.buttonStop.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonStop, "Stop");
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonNext
            // 
            this.buttonNext.AutoSize = true;
            this.buttonNext.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonNext.FlatAppearance.BorderSize = 0;
            this.buttonNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNext.ImageIndex = 1;
            this.buttonNext.ImageList = this.imageListSmall;
            this.buttonNext.Location = new System.Drawing.Point(145, 3);
            this.buttonNext.Name = "buttonNext";
            this.buttonNext.Size = new System.Drawing.Size(38, 54);
            this.buttonNext.TabIndex = 2;
            this.buttonNext.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonNext, "Next Track");
            this.buttonNext.UseVisualStyleBackColor = true;
            this.buttonNext.Click += new System.EventHandler(this.buttonNext_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.AutoSize = true;
            this.buttonClear.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonClear.FlatAppearance.BorderSize = 0;
            this.buttonClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonClear.ImageIndex = 7;
            this.buttonClear.ImageList = this.imageListSmall;
            this.buttonClear.Location = new System.Drawing.Point(718, 3);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(38, 54);
            this.buttonClear.TabIndex = 13;
            this.buttonClear.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonClear, "Clear List");
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // buttonEject
            // 
            this.buttonEject.AutoSize = true;
            this.buttonEject.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonEject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonEject.FlatAppearance.BorderSize = 0;
            this.buttonEject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonEject.ImageIndex = 8;
            this.buttonEject.ImageList = this.imageListSmall;
            this.buttonEject.Location = new System.Drawing.Point(674, 3);
            this.buttonEject.Name = "buttonEject";
            this.buttonEject.Size = new System.Drawing.Size(38, 54);
            this.buttonEject.TabIndex = 12;
            this.buttonEject.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.buttonEject, "Add Music files");
            this.buttonEject.UseVisualStyleBackColor = true;
            this.buttonEject.Click += new System.EventHandler(this.buttonEject_Click);
            // 
            // labelElapsed
            // 
            this.labelElapsed.AutoSize = true;
            this.labelElapsed.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tableLayoutPanelButton.SetColumnSpan(this.labelElapsed, 3);
            this.labelElapsed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelElapsed.Location = new System.Drawing.Point(144, 60);
            this.labelElapsed.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelElapsed.Name = "labelElapsed";
            this.labelElapsed.Size = new System.Drawing.Size(128, 25);
            this.labelElapsed.TabIndex = 14;
            this.labelElapsed.Text = "00:00:00";
            this.labelElapsed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.checkBox1, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.checkBoxCnvClk, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel8, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.checkBoxClip, 0, 3);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(498, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanelButton.SetRowSpan(this.tableLayoutPanel3, 2);
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(173, 85);
            this.tableLayoutPanel3.TabIndex = 15;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.AutoSize = true;
            this.tableLayoutPanel8.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel8.ColumnCount = 2;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.tableLayoutPanel8.Controls.Add(this.label19, 1, 0);
            this.tableLayoutPanel8.Controls.Add(this.numericUpDown1, 0, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(0, 40);
            this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 1;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.Size = new System.Drawing.Size(173, 23);
            this.tableLayoutPanel8.TabIndex = 68;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label19.Location = new System.Drawing.Point(45, 0);
            this.label19.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(126, 23);
            this.label19.TabIndex = 0;
            this.label19.Text = "DAC Volume %";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // contextMenuStripList
            // 
            this.contextMenuStripList.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStripList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.playToolStripMenuItem,
            this.removeToolStripMenuItem,
            this.explorerToolStripMenuItem});
            this.contextMenuStripList.Name = "contextMenuStrip1";
            this.contextMenuStripList.Size = new System.Drawing.Size(118, 70);
            // 
            // playToolStripMenuItem
            // 
            this.playToolStripMenuItem.Name = "playToolStripMenuItem";
            this.playToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.playToolStripMenuItem.Text = "&Play";
            this.playToolStripMenuItem.Click += new System.EventHandler(this.playToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.removeToolStripMenuItem.Text = "&Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // explorerToolStripMenuItem
            // 
            this.explorerToolStripMenuItem.Name = "explorerToolStripMenuItem";
            this.explorerToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.explorerToolStripMenuItem.Text = "&Explorer";
            this.explorerToolStripMenuItem.Click += new System.EventHandler(this.explorerToolStripMenuItem_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.labelLoad, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.progressBarLoad, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxWaitAlg, 2, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 596);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(759, 26);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // labelLoad
            // 
            this.labelLoad.AutoSize = true;
            this.labelLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelLoad.Location = new System.Drawing.Point(3, 0);
            this.labelLoad.Name = "labelLoad";
            this.labelLoad.Size = new System.Drawing.Size(29, 26);
            this.labelLoad.TabIndex = 0;
            this.labelLoad.Text = "Load";
            this.labelLoad.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBarLoad
            // 
            this.progressBarLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBarLoad.Location = new System.Drawing.Point(38, 3);
            this.progressBarLoad.Name = "progressBarLoad";
            this.progressBarLoad.Size = new System.Drawing.Size(630, 20);
            this.progressBarLoad.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBarLoad.TabIndex = 1;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.textBoxTitle, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.textBoxUseSystem, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.textBoxSongSystem, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label18, 2, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 461);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(759, 50);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(3, 25);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 25);
            this.label10.TabIndex = 3;
            this.label10.Text = "&Chip:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 25);
            this.label7.TabIndex = 0;
            this.label7.Text = "&Current:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxTitle
            // 
            this.textBoxTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tableLayoutPanel2.SetColumnSpan(this.textBoxTitle, 3);
            this.textBoxTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxTitle.Location = new System.Drawing.Point(54, 3);
            this.textBoxTitle.Name = "textBoxTitle";
            this.textBoxTitle.ReadOnly = true;
            this.textBoxTitle.ShortcutsEnabled = false;
            this.textBoxTitle.Size = new System.Drawing.Size(702, 19);
            this.textBoxTitle.TabIndex = 1;
            // 
            // textBoxUseSystem
            // 
            this.textBoxUseSystem.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxUseSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxUseSystem.Location = new System.Drawing.Point(415, 28);
            this.textBoxUseSystem.Name = "textBoxUseSystem";
            this.textBoxUseSystem.ReadOnly = true;
            this.textBoxUseSystem.ShortcutsEnabled = false;
            this.textBoxUseSystem.Size = new System.Drawing.Size(341, 19);
            this.textBoxUseSystem.TabIndex = 2;
            // 
            // textBoxSongSystem
            // 
            this.textBoxSongSystem.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxSongSystem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxSongSystem.Location = new System.Drawing.Point(54, 28);
            this.textBoxSongSystem.Name = "textBoxSongSystem";
            this.textBoxSongSystem.ReadOnly = true;
            this.textBoxSongSystem.ShortcutsEnabled = false;
            this.textBoxSongSystem.Size = new System.Drawing.Size(340, 19);
            this.textBoxSongSystem.TabIndex = 2;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label18.Location = new System.Drawing.Point(399, 25);
            this.label18.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(11, 25);
            this.label18.TabIndex = 4;
            this.label18.Text = "⇒";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Video Game Music Files(*.vgm;*.vgz;*.xgm;*.mgs;*.kss;*.m3u)|*.vgm;*.vgz;*.xgm;*.m" +
    "gs;*.kss;*.m3u";
            this.openFileDialog.Multiselect = true;
            this.openFileDialog.Title = "Select VGM/XGM files";
            // 
            // splitter1
            // 
            this.splitter1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 224);
            this.splitter1.Margin = new System.Windows.Forms.Padding(2);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(759, 6);
            this.splitter1.TabIndex = 6;
            this.splitter1.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 622);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip1.Size = new System.Drawing.Size(759, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(71, 17);
            this.toolStripStatusLabel.Text = "(Status Text)";
            this.toolStripStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // saveFileDialogM3U
            // 
            this.saveFileDialogM3U.DefaultExt = "m3u";
            this.saveFileDialogM3U.FileName = "playlist.m3u";
            this.saveFileDialogM3U.Filter = "M3U|*.m3u";
            this.saveFileDialogM3U.Title = "Save playlist";
            // 
            // checkBoxLoop
            // 
            this.checkBoxLoop.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.Loop;
            this.checkBoxLoop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxLoop.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "Loop", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxLoop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxLoop.FlatAppearance.BorderSize = 0;
            this.checkBoxLoop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.checkBoxLoop.ImageIndex = 6;
            this.checkBoxLoop.ImageList = this.imageListSmall;
            this.checkBoxLoop.Location = new System.Drawing.Point(365, 3);
            this.checkBoxLoop.Name = "checkBoxLoop";
            this.checkBoxLoop.Size = new System.Drawing.Size(53, 54);
            this.checkBoxLoop.TabIndex = 8;
            this.checkBoxLoop.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.checkBoxLoop, "Loop Count");
            this.checkBoxLoop.UseVisualStyleBackColor = true;
            this.checkBoxLoop.CheckedChanged += new System.EventHandler(this.checkBoxLoop_CheckedChanged);
            // 
            // numericUpDownLooped
            // 
            this.numericUpDownLooped.AutoSize = true;
            this.numericUpDownLooped.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "LoopCount", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownLooped.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownLooped.Location = new System.Drawing.Point(365, 63);
            this.numericUpDownLooped.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numericUpDownLooped.Name = "numericUpDownLooped";
            this.numericUpDownLooped.Size = new System.Drawing.Size(53, 19);
            this.numericUpDownLooped.TabIndex = 9;
            this.numericUpDownLooped.Value = global::zanac.VGMPlayer.Properties.Settings.Default.LoopCount;
            this.numericUpDownLooped.ValueChanged += new System.EventHandler(this.numericUpDownLooped_ValueChanged);
            // 
            // checkBoxLoopTimes
            // 
            this.checkBoxLoopTimes.AutoSize = true;
            this.checkBoxLoopTimes.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.LoopTime;
            this.checkBoxLoopTimes.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "LoopTime", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxLoopTimes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxLoopTimes.FlatAppearance.BorderSize = 0;
            this.checkBoxLoopTimes.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.checkBoxLoopTimes.ImageIndex = 9;
            this.checkBoxLoopTimes.ImageList = this.imageListSmall;
            this.checkBoxLoopTimes.Location = new System.Drawing.Point(424, 3);
            this.checkBoxLoopTimes.Name = "checkBoxLoopTimes";
            this.checkBoxLoopTimes.Size = new System.Drawing.Size(71, 54);
            this.checkBoxLoopTimes.TabIndex = 10;
            this.checkBoxLoopTimes.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip1.SetToolTip(this.checkBoxLoopTimes, "Loop Time");
            this.checkBoxLoopTimes.UseVisualStyleBackColor = true;
            this.checkBoxLoopTimes.CheckedChanged += new System.EventHandler(this.checkBoxLoopTimes_CheckedChanged);
            // 
            // dateTimePickerLoopTimes
            // 
            this.dateTimePickerLoopTimes.CustomFormat = "hh:mm:ss";
            this.dateTimePickerLoopTimes.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "LoopTimes", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.dateTimePickerLoopTimes.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePickerLoopTimes.Location = new System.Drawing.Point(423, 62);
            this.dateTimePickerLoopTimes.Margin = new System.Windows.Forms.Padding(2);
            this.dateTimePickerLoopTimes.MaxDate = new System.DateTime(1753, 1, 1, 23, 59, 59, 0);
            this.dateTimePickerLoopTimes.Name = "dateTimePickerLoopTimes";
            this.dateTimePickerLoopTimes.ShowUpDown = true;
            this.dateTimePickerLoopTimes.Size = new System.Drawing.Size(73, 19);
            this.dateTimePickerLoopTimes.TabIndex = 11;
            this.dateTimePickerLoopTimes.Value = global::zanac.VGMPlayer.Properties.Settings.Default.LoopTimes;
            this.dateTimePickerLoopTimes.ValueChanged += new System.EventHandler(this.dateTimePickerLoopTimes_ValueChanged);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.DisableDAC;
            this.checkBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "DisableDAC", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox1.Location = new System.Drawing.Point(2, 22);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(2);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(90, 16);
            this.checkBox1.TabIndex = 67;
            this.checkBox1.Text = "DAC Disable";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBoxCnvClk
            // 
            this.checkBoxCnvClk.AutoSize = true;
            this.checkBoxCnvClk.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.ConvertClock;
            this.checkBoxCnvClk.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "ConvertClock", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxCnvClk.Location = new System.Drawing.Point(2, 2);
            this.checkBoxCnvClk.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxCnvClk.Name = "checkBoxCnvClk";
            this.checkBoxCnvClk.Size = new System.Drawing.Size(97, 16);
            this.checkBoxCnvClk.TabIndex = 66;
            this.checkBoxCnvClk.Text = "Clock Convert";
            this.checkBoxCnvClk.UseVisualStyleBackColor = true;
            this.checkBoxCnvClk.CheckedChanged += new System.EventHandler(this.checkBoxCnvClk_CheckedChanged);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.AutoSize = true;
            this.numericUpDown1.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "DacVolume", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown1.Location = new System.Drawing.Point(2, 2);
            this.numericUpDown1.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(39, 19);
            this.numericUpDown1.TabIndex = 1;
            this.numericUpDown1.Value = global::zanac.VGMPlayer.Properties.Settings.Default.DacVolume;
            // 
            // checkBoxClip
            // 
            this.checkBoxClip.AutoSize = true;
            this.checkBoxClip.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.DACClipping;
            this.checkBoxClip.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "DACClipping", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxClip.Location = new System.Drawing.Point(3, 66);
            this.checkBoxClip.Name = "checkBoxClip";
            this.checkBoxClip.Size = new System.Drawing.Size(106, 16);
            this.checkBoxClip.TabIndex = 69;
            this.checkBoxClip.Text = "Smart DAC Clip";
            this.checkBoxClip.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelPort
            // 
            this.tableLayoutPanelPort.AutoScroll = true;
            this.tableLayoutPanelPort.ColumnCount = 6;
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelPort.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPCE, 2, 14);
            this.tableLayoutPanelPort.Controls.Add(this.tableLayoutPanel4, 2, 4);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownClkWidthDCSG, 3, 2);
            this.tableLayoutPanelPort.Controls.Add(this.label9, 3, 0);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortYm2413, 4, 3);
            this.tableLayoutPanelPort.Controls.Add(this.label4, 1, 3);
            this.tableLayoutPanelPort.Controls.Add(this.label3, 1, 2);
            this.tableLayoutPanelPort.Controls.Add(this.label2, 2, 0);
            this.tableLayoutPanelPort.Controls.Add(this.label1, 1, 0);
            this.tableLayoutPanelPort.Controls.Add(this.label6, 4, 0);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxDCSG, 2, 2);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortSN76489, 4, 2);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortSCC, 4, 4);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPLL, 3, 3);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownSCC, 3, 4);
            this.tableLayoutPanelPort.Controls.Add(this.label12, 1, 5);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxY8910, 2, 5);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownY8910, 3, 5);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortY8910, 4, 5);
            this.tableLayoutPanelPort.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnDCSG, 0, 2);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPLL, 0, 3);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnSCC, 0, 4);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnY8910, 0, 5);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPN2, 0, 1);
            this.tableLayoutPanelPort.Controls.Add(this.label5, 1, 1);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPN2, 2, 1);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPN2, 3, 1);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortYM2612, 4, 1);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxSccType, 1, 4);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPM, 0, 6);
            this.tableLayoutPanelPort.Controls.Add(this.label11, 1, 6);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPM, 3, 6);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortOPM, 4, 6);
            this.tableLayoutPanelPort.Controls.Add(this.tableLayoutPanel5, 2, 6);
            this.tableLayoutPanelPort.Controls.Add(this.tableLayoutPanel6, 2, 3);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPL3, 0, 7);
            this.tableLayoutPanelPort.Controls.Add(this.label14, 1, 7);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPL3, 2, 7);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPL3, 3, 7);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortOPL3, 4, 7);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPNA, 0, 8);
            this.tableLayoutPanelPort.Controls.Add(this.label15, 1, 8);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPNA, 2, 8);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPNA, 3, 8);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortOPNA, 4, 8);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnY8950, 0, 9);
            this.tableLayoutPanelPort.Controls.Add(this.label16, 1, 9);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownY8950, 3, 9);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortY8950, 4, 9);
            this.tableLayoutPanelPort.Controls.Add(this.tableLayoutPanel7, 2, 9);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPN, 0, 10);
            this.tableLayoutPanelPort.Controls.Add(this.label13, 1, 10);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPN, 2, 10);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPN, 3, 10);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortOPN, 4, 10);
            this.tableLayoutPanelPort.Controls.Add(this.label17, 5, 0);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPN2Div, 5, 1);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownDCSGDiv, 5, 2);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPLLDiv, 5, 3);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownSCCDiv, 5, 4);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownPSGDiv, 5, 5);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPMDiv, 5, 6);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPL3Div, 5, 7);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPNADiv, 5, 8);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDown8950Div, 5, 9);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownOPNDiv, 5, 10);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnNES, 0, 11);
            this.tableLayoutPanelPort.Controls.Add(this.label20, 1, 11);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxNES, 2, 11);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownNES, 3, 11);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortNES, 4, 11);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownNESDiv, 5, 11);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnMCD, 0, 12);
            this.tableLayoutPanelPort.Controls.Add(this.label21, 1, 12);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxMCD, 2, 12);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownMCD, 3, 12);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortMCD, 4, 12);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownMCDDiv, 5, 12);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnSAA, 0, 13);
            this.tableLayoutPanelPort.Controls.Add(this.label22, 1, 13);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxSAA, 2, 13);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownSAA, 3, 13);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortSAA, 4, 13);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownSAADiv, 5, 13);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnPCE, 0, 14);
            this.tableLayoutPanelPort.Controls.Add(this.label23, 1, 14);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortPCE, 4, 14);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnOPNB, 0, 15);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPNB, 2, 15);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortOPNB, 4, 15);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxOPNBType, 1, 15);
            this.tableLayoutPanelPort.Controls.Add(this.checkBoxConnPWM, 0, 16);
            this.tableLayoutPanelPort.Controls.Add(this.label24, 1, 16);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPWM, 2, 16);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownPWM, 3, 16);
            this.tableLayoutPanelPort.Controls.Add(this.comboBoxPortPWM, 4, 16);
            this.tableLayoutPanelPort.Controls.Add(this.numericUpDownPWMDiv, 5, 16);
            this.tableLayoutPanelPort.DataBindings.Add(new System.Windows.Forms.Binding("Size", global::zanac.VGMPlayer.Properties.Settings.Default, "PaneHeight", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.tableLayoutPanelPort.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanelPort.Location = new System.Drawing.Point(0, 24);
            this.tableLayoutPanelPort.Name = "tableLayoutPanelPort";
            this.tableLayoutPanelPort.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanelPort.RowCount = 18;
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelPort.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelPort.Size = global::zanac.VGMPlayer.Properties.Settings.Default.PaneHeight;
            this.tableLayoutPanelPort.TabIndex = 0;
            // 
            // comboBoxPCE
            // 
            this.comboBoxPCE.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "PCE_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPCE.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPCE.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPCE.FormattingEnabled = true;
            this.comboBoxPCE.Items.AddRange(new object[] {
            "VSIF - PCE(Turbo EverDrive)"});
            this.comboBoxPCE.Location = new System.Drawing.Point(163, 362);
            this.comboBoxPCE.Name = "comboBoxPCE";
            this.comboBoxPCE.Size = new System.Drawing.Size(336, 20);
            this.comboBoxPCE.TabIndex = 70;
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.AutoSize = true;
            this.tableLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.45552F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.54448F));
            this.tableLayoutPanel4.Controls.Add(this.comboBoxSCC, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxSccSlot, 1, 0);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(160, 99);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel4.Size = new System.Drawing.Size(342, 26);
            this.tableLayoutPanel4.TabIndex = 26;
            // 
            // comboBoxSCC
            // 
            this.comboBoxSCC.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SCC_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxSCC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxSCC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSCC.FormattingEnabled = true;
            this.comboBoxSCC.Items.AddRange(new object[] {
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxSCC.Location = new System.Drawing.Point(3, 3);
            this.comboBoxSCC.Name = "comboBoxSCC";
            this.comboBoxSCC.Size = new System.Drawing.Size(204, 20);
            this.comboBoxSCC.TabIndex = 0;
            // 
            // comboBoxSccSlot
            // 
            this.comboBoxSccSlot.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SCC_Slot", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxSccSlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxSccSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSccSlot.FormattingEnabled = true;
            this.comboBoxSccSlot.Items.AddRange(new object[] {
            "ID1",
            "ID0",
            "Slot0_Basic",
            "Slot0_Ext0",
            "Slot0_Ext1",
            "Slot0_Ext2",
            "Slot0_Ext3",
            "Slot1_Basic",
            "Slot1_Ext0",
            "Slot1_Ext1",
            "Slot1_Ext2",
            "Slot1_Ext3",
            "Slot2_Basic",
            "Slot2_Ext0",
            "Slot2_Ext1",
            "Slot2_Ext2",
            "Slot2_Ext3",
            "Slot3_Basic",
            "Slot3_Ext0",
            "Slot3_Ext1",
            "Slot3_Ext2",
            "Slot3_Ext3"});
            this.comboBoxSccSlot.Location = new System.Drawing.Point(213, 3);
            this.comboBoxSccSlot.Name = "comboBoxSccSlot";
            this.comboBoxSccSlot.Size = new System.Drawing.Size(126, 20);
            this.comboBoxSccSlot.TabIndex = 1;
            // 
            // numericUpDownClkWidthDCSG
            // 
            this.numericUpDownClkWidthDCSG.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitDCSG", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownClkWidthDCSG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownClkWidthDCSG.Location = new System.Drawing.Point(505, 50);
            this.numericUpDownClkWidthDCSG.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownClkWidthDCSG.Name = "numericUpDownClkWidthDCSG";
            this.numericUpDownClkWidthDCSG.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownClkWidthDCSG.TabIndex = 15;
            this.numericUpDownClkWidthDCSG.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitDCSG;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(505, 6);
            this.label9.Margin = new System.Windows.Forms.Padding(3);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 12);
            this.label9.TabIndex = 3;
            this.label9.Text = "FTDI Clk[%]";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxPortYm2413
            // 
            this.comboBoxPortYm2413.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPLL_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortYm2413.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortYm2413.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortYm2413.FormattingEnabled = true;
            this.comboBoxPortYm2413.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortYm2413.Location = new System.Drawing.Point(576, 76);
            this.comboBoxPortYm2413.Name = "comboBoxPortYm2413";
            this.comboBoxPortYm2413.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortYm2413.TabIndex = 22;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(43, 76);
            this.label4.Margin = new System.Windows.Forms.Padding(3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(114, 20);
            this.label4.TabIndex = 19;
            this.label4.Text = "YM2&413(OPLL)";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label4.Click += new System.EventHandler(this.label3_Click);
            this.label4.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(43, 50);
            this.label3.Margin = new System.Windows.Forms.Padding(3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(114, 20);
            this.label3.TabIndex = 13;
            this.label3.Text = "SN&76489(DCSG)";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label3.Click += new System.EventHandler(this.label3_Click);
            this.label3.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(163, 6);
            this.label2.Margin = new System.Windows.Forms.Padding(3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(336, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "VSIF Type, Slot type (MSX only)";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(43, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "Chip";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(576, 6);
            this.label6.Margin = new System.Windows.Forms.Padding(3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(79, 12);
            this.label6.TabIndex = 4;
            this.label6.Text = "FDTI/COM No";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // comboBoxDCSG
            // 
            this.comboBoxDCSG.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "DCSG_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxDCSG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxDCSG.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDCSG.FormattingEnabled = true;
            this.comboBoxDCSG.Items.AddRange(new object[] {
            "VSIF - MD/Genesis(UART 163Kbps)",
            "VSIF - MD/Genesis(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - SMS(UART 115Kbps)",
            "VSIF - MD/Genesis(UART 115Kbps)",
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - SMS(FTDI2XX Clk: 232R=20~,H=25~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "NANO DRIVE",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxDCSG.Location = new System.Drawing.Point(163, 50);
            this.comboBoxDCSG.Name = "comboBoxDCSG";
            this.comboBoxDCSG.Size = new System.Drawing.Size(336, 20);
            this.comboBoxDCSG.TabIndex = 14;
            // 
            // comboBoxPortSN76489
            // 
            this.comboBoxPortSN76489.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "DCSG_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortSN76489.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortSN76489.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortSN76489.FormattingEnabled = true;
            this.comboBoxPortSN76489.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortSN76489.Location = new System.Drawing.Point(576, 50);
            this.comboBoxPortSN76489.Name = "comboBoxPortSN76489";
            this.comboBoxPortSN76489.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortSN76489.TabIndex = 16;
            // 
            // comboBoxPortSCC
            // 
            this.comboBoxPortSCC.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SCC_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortSCC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortSCC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortSCC.FormattingEnabled = true;
            this.comboBoxPortSCC.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortSCC.Location = new System.Drawing.Point(576, 102);
            this.comboBoxPortSCC.Name = "comboBoxPortSCC";
            this.comboBoxPortSCC.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortSCC.TabIndex = 28;
            // 
            // numericUpDownOPLL
            // 
            this.numericUpDownOPLL.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPLL", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPLL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPLL.Location = new System.Drawing.Point(505, 76);
            this.numericUpDownOPLL.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPLL.Name = "numericUpDownOPLL";
            this.numericUpDownOPLL.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPLL.TabIndex = 21;
            this.numericUpDownOPLL.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPLL;
            // 
            // numericUpDownSCC
            // 
            this.numericUpDownSCC.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitSCC", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownSCC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownSCC.Location = new System.Drawing.Point(505, 102);
            this.numericUpDownSCC.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSCC.Name = "numericUpDownSCC";
            this.numericUpDownSCC.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownSCC.TabIndex = 27;
            this.numericUpDownSCC.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitSCC;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label12.Location = new System.Drawing.Point(43, 128);
            this.label12.Margin = new System.Windows.Forms.Padding(3);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(114, 20);
            this.label12.TabIndex = 31;
            this.label12.Text = "&AY-3-8910(PSG)";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label12, "SSG of OPN2 fallbacks to this.\r\n");
            this.label12.Click += new System.EventHandler(this.label3_Click);
            this.label12.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxY8910
            // 
            this.comboBoxY8910.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8910_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxY8910.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxY8910.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxY8910.FormattingEnabled = true;
            this.comboBoxY8910.Items.AddRange(new object[] {
            "VSIF - MSX/PC-6001(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~),",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxY8910.Location = new System.Drawing.Point(163, 128);
            this.comboBoxY8910.Name = "comboBoxY8910";
            this.comboBoxY8910.Size = new System.Drawing.Size(336, 20);
            this.comboBoxY8910.TabIndex = 32;
            // 
            // numericUpDownY8910
            // 
            this.numericUpDownY8910.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitAY8910", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownY8910.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownY8910.Location = new System.Drawing.Point(505, 128);
            this.numericUpDownY8910.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownY8910.Name = "numericUpDownY8910";
            this.numericUpDownY8910.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownY8910.TabIndex = 33;
            this.numericUpDownY8910.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitAY8910;
            // 
            // comboBoxPortY8910
            // 
            this.comboBoxPortY8910.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8910_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortY8910.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortY8910.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortY8910.FormattingEnabled = true;
            this.comboBoxPortY8910.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortY8910.Location = new System.Drawing.Point(576, 128);
            this.comboBoxPortY8910.Name = "comboBoxPortY8910";
            this.comboBoxPortY8910.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortY8910.TabIndex = global::zanac.VGMPlayer.Properties.Settings.Default.Y8910_Port;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(6, 6);
            this.label8.Margin = new System.Windows.Forms.Padding(3);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(31, 12);
            this.label8.TabIndex = 0;
            this.label8.Text = "Conn";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label8.Click += new System.EventHandler(this.label8_DoubleClick);
            this.label8.DoubleClick += new System.EventHandler(this.label8_DoubleClick);
            // 
            // checkBoxConnDCSG
            // 
            this.checkBoxConnDCSG.AutoSize = true;
            this.checkBoxConnDCSG.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.DCSG_Enable;
            this.checkBoxConnDCSG.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "DCSG_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnDCSG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnDCSG.Location = new System.Drawing.Point(6, 50);
            this.checkBoxConnDCSG.Name = "checkBoxConnDCSG";
            this.checkBoxConnDCSG.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnDCSG.TabIndex = 12;
            this.checkBoxConnDCSG.UseVisualStyleBackColor = true;
            this.checkBoxConnDCSG.CheckedChanged += new System.EventHandler(this.checkBoxConnDCSG_CheckedChanged);
            // 
            // checkBoxConnOPLL
            // 
            this.checkBoxConnOPLL.AutoSize = true;
            this.checkBoxConnOPLL.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPLL_Enable;
            this.checkBoxConnOPLL.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPLL_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPLL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPLL.Location = new System.Drawing.Point(6, 76);
            this.checkBoxConnOPLL.Name = "checkBoxConnOPLL";
            this.checkBoxConnOPLL.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPLL.TabIndex = 18;
            this.checkBoxConnOPLL.UseVisualStyleBackColor = true;
            this.checkBoxConnOPLL.CheckedChanged += new System.EventHandler(this.checkBoxConnOPLL_CheckedChanged);
            // 
            // checkBoxConnSCC
            // 
            this.checkBoxConnSCC.AutoSize = true;
            this.checkBoxConnSCC.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.SCC_Enable;
            this.checkBoxConnSCC.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "SCC_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnSCC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnSCC.Location = new System.Drawing.Point(6, 102);
            this.checkBoxConnSCC.Name = "checkBoxConnSCC";
            this.checkBoxConnSCC.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnSCC.TabIndex = 24;
            this.checkBoxConnSCC.UseVisualStyleBackColor = true;
            this.checkBoxConnSCC.CheckedChanged += new System.EventHandler(this.checkBoxConnSCC_CheckedChanged);
            // 
            // checkBoxConnY8910
            // 
            this.checkBoxConnY8910.AutoSize = true;
            this.checkBoxConnY8910.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.Y8910_Enable;
            this.checkBoxConnY8910.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8910_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnY8910.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnY8910.Location = new System.Drawing.Point(6, 128);
            this.checkBoxConnY8910.Name = "checkBoxConnY8910";
            this.checkBoxConnY8910.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnY8910.TabIndex = 30;
            this.checkBoxConnY8910.UseVisualStyleBackColor = true;
            this.checkBoxConnY8910.CheckedChanged += new System.EventHandler(this.checkBoxConnY8910_CheckedChanged);
            // 
            // checkBoxConnOPN2
            // 
            this.checkBoxConnOPN2.AutoSize = true;
            this.checkBoxConnOPN2.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPN2_Enable;
            this.checkBoxConnOPN2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN2_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPN2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPN2.Location = new System.Drawing.Point(6, 24);
            this.checkBoxConnOPN2.Name = "checkBoxConnOPN2";
            this.checkBoxConnOPN2.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPN2.TabIndex = 6;
            this.checkBoxConnOPN2.UseVisualStyleBackColor = true;
            this.checkBoxConnOPN2.CheckedChanged += new System.EventHandler(this.checkBoxConnOPN2_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(43, 24);
            this.label5.Margin = new System.Windows.Forms.Padding(3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 20);
            this.label5.TabIndex = 7;
            this.label5.Text = "YM2&612(OPN2)";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label5, "OPN fallbacks to this.");
            this.label5.Click += new System.EventHandler(this.label3_Click);
            this.label5.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxOPN2
            // 
            this.comboBoxOPN2.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN2_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPN2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPN2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPN2.FormattingEnabled = true;
            this.comboBoxOPN2.Items.AddRange(new object[] {
            "VSIF - MD/Genesis(UART 163Kbps)",
            "VSIF - MD/Genesis(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MD/Genesis(UART 115Kbps)",
            "VSIF - MSX/PC-6001(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "NANO DRIVE",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPN2.Location = new System.Drawing.Point(163, 24);
            this.comboBoxOPN2.Name = "comboBoxOPN2";
            this.comboBoxOPN2.Size = new System.Drawing.Size(336, 20);
            this.comboBoxOPN2.TabIndex = 8;
            // 
            // numericUpDownOPN2
            // 
            this.numericUpDownOPN2.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPN2", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPN2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPN2.Location = new System.Drawing.Point(505, 24);
            this.numericUpDownOPN2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPN2.Name = "numericUpDownOPN2";
            this.numericUpDownOPN2.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPN2.TabIndex = 9;
            this.numericUpDownOPN2.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPN2;
            // 
            // comboBoxPortYM2612
            // 
            this.comboBoxPortYM2612.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN2_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortYM2612.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortYM2612.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortYM2612.FormattingEnabled = true;
            this.comboBoxPortYM2612.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortYM2612.Location = new System.Drawing.Point(576, 24);
            this.comboBoxPortYM2612.Name = "comboBoxPortYM2612";
            this.comboBoxPortYM2612.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortYM2612.TabIndex = 10;
            // 
            // comboBoxSccType
            // 
            this.comboBoxSccType.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SCC_Type", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxSccType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxSccType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSccType.FormattingEnabled = true;
            this.comboBoxSccType.Items.AddRange(new object[] {
            "SCC1",
            "SCC1(SCC mode)",
            "SCC"});
            this.comboBoxSccType.Location = new System.Drawing.Point(42, 101);
            this.comboBoxSccType.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxSccType.Name = "comboBoxSccType";
            this.comboBoxSccType.Size = new System.Drawing.Size(116, 20);
            this.comboBoxSccType.TabIndex = 25;
            // 
            // checkBoxConnOPM
            // 
            this.checkBoxConnOPM.AutoSize = true;
            this.checkBoxConnOPM.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPM_Enable;
            this.checkBoxConnOPM.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPM_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPM.Location = new System.Drawing.Point(6, 154);
            this.checkBoxConnOPM.Name = "checkBoxConnOPM";
            this.checkBoxConnOPM.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPM.TabIndex = 36;
            this.checkBoxConnOPM.UseVisualStyleBackColor = true;
            this.checkBoxConnOPM.CheckedChanged += new System.EventHandler(this.checkBoxConnOPM_CheckedChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.Location = new System.Drawing.Point(43, 154);
            this.label11.Margin = new System.Windows.Forms.Padding(3);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(114, 20);
            this.label11.TabIndex = 37;
            this.label11.Text = "YM21&51(OPM)";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label11.Click += new System.EventHandler(this.label3_Click);
            this.label11.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // numericUpDownOPM
            // 
            this.numericUpDownOPM.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPM", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPM.Location = new System.Drawing.Point(505, 154);
            this.numericUpDownOPM.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPM.Name = "numericUpDownOPM";
            this.numericUpDownOPM.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPM.TabIndex = 39;
            this.numericUpDownOPM.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPM;
            // 
            // comboBoxPortOPM
            // 
            this.comboBoxPortOPM.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPM_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortOPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortOPM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortOPM.FormattingEnabled = true;
            this.comboBoxPortOPM.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortOPM.Location = new System.Drawing.Point(576, 154);
            this.comboBoxPortOPM.Name = "comboBoxPortOPM";
            this.comboBoxPortOPM.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortOPM.TabIndex = 40;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.AutoSize = true;
            this.tableLayoutPanel5.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.71429F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.28571F));
            this.tableLayoutPanel5.Controls.Add(this.comboBoxOPM, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.comboBoxOpmSlot, 1, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(160, 151);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(342, 26);
            this.tableLayoutPanel5.TabIndex = 38;
            // 
            // comboBoxOPM
            // 
            this.comboBoxOPM.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPM_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPM.FormattingEnabled = true;
            this.comboBoxOPM.Items.AddRange(new object[] {
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "SPFM Light - 4759 Player",
            "SPFM - 4759 Player",
            "G.I.M.I.C",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPM.Location = new System.Drawing.Point(3, 3);
            this.comboBoxOPM.Name = "comboBoxOPM";
            this.comboBoxOPM.Size = new System.Drawing.Size(205, 20);
            this.comboBoxOPM.TabIndex = 0;
            // 
            // comboBoxOpmSlot
            // 
            this.comboBoxOpmSlot.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPM_Slot", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOpmSlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOpmSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOpmSlot.FormattingEnabled = true;
            this.comboBoxOpmSlot.Items.AddRange(new object[] {
            "ID0",
            "ID1"});
            this.comboBoxOpmSlot.Location = new System.Drawing.Point(214, 3);
            this.comboBoxOpmSlot.Name = "comboBoxOpmSlot";
            this.comboBoxOpmSlot.Size = new System.Drawing.Size(125, 20);
            this.comboBoxOpmSlot.TabIndex = 1;
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.AutoSize = true;
            this.tableLayoutPanel6.ColumnCount = 2;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.64773F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.35227F));
            this.tableLayoutPanel6.Controls.Add(this.comboBoxOPLL, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.comboBoxOpllSlot, 1, 0);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(160, 73);
            this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 1;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(342, 26);
            this.tableLayoutPanel6.TabIndex = 20;
            // 
            // comboBoxOPLL
            // 
            this.comboBoxOPLL.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPLL_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPLL.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPLL.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPLL.FormattingEnabled = true;
            this.comboBoxOPLL.Items.AddRange(new object[] {
            "VSIF - SMS(UART 115Kbps)",
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - SMS(FTDI2XX Clk: 232R=20~,H=25~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPLL.Location = new System.Drawing.Point(3, 3);
            this.comboBoxOPLL.Name = "comboBoxOPLL";
            this.comboBoxOPLL.Size = new System.Drawing.Size(204, 20);
            this.comboBoxOPLL.TabIndex = 0;
            // 
            // comboBoxOpllSlot
            // 
            this.comboBoxOpllSlot.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPLL_Slot", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOpllSlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOpllSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOpllSlot.FormattingEnabled = true;
            this.comboBoxOpllSlot.Items.AddRange(new object[] {
            "IO",
            "MMIO_1",
            "MMIO_2"});
            this.comboBoxOpllSlot.Location = new System.Drawing.Point(213, 3);
            this.comboBoxOpllSlot.Name = "comboBoxOpllSlot";
            this.comboBoxOpllSlot.Size = new System.Drawing.Size(126, 20);
            this.comboBoxOpllSlot.TabIndex = 1;
            // 
            // checkBoxConnOPL3
            // 
            this.checkBoxConnOPL3.AutoSize = true;
            this.checkBoxConnOPL3.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPL3_Enable;
            this.checkBoxConnOPL3.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPL3_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPL3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPL3.Location = new System.Drawing.Point(6, 180);
            this.checkBoxConnOPL3.Name = "checkBoxConnOPL3";
            this.checkBoxConnOPL3.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPL3.TabIndex = 42;
            this.checkBoxConnOPL3.UseVisualStyleBackColor = true;
            this.checkBoxConnOPL3.CheckedChanged += new System.EventHandler(this.checkBoxConnOPL3_CheckedChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label14.Location = new System.Drawing.Point(43, 180);
            this.label14.Margin = new System.Windows.Forms.Padding(3);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(114, 20);
            this.label14.TabIndex = 43;
            this.label14.Text = "YM&F262(OPL3)";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label14, "OPL fallbacks to this.");
            this.label14.Click += new System.EventHandler(this.label3_Click);
            this.label14.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxOPL3
            // 
            this.comboBoxOPL3.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPL3_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPL3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPL3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPL3.FormattingEnabled = true;
            this.comboBoxOPL3.Items.AddRange(new object[] {
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPL3.Location = new System.Drawing.Point(163, 180);
            this.comboBoxOPL3.Name = "comboBoxOPL3";
            this.comboBoxOPL3.Size = new System.Drawing.Size(336, 20);
            this.comboBoxOPL3.TabIndex = 44;
            // 
            // numericUpDownOPL3
            // 
            this.numericUpDownOPL3.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPL3", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPL3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPL3.Location = new System.Drawing.Point(505, 180);
            this.numericUpDownOPL3.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPL3.Name = "numericUpDownOPL3";
            this.numericUpDownOPL3.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPL3.TabIndex = 45;
            this.numericUpDownOPL3.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPL3;
            // 
            // comboBoxPortOPL3
            // 
            this.comboBoxPortOPL3.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPL3_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortOPL3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortOPL3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortOPL3.FormattingEnabled = true;
            this.comboBoxPortOPL3.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortOPL3.Location = new System.Drawing.Point(576, 180);
            this.comboBoxPortOPL3.Name = "comboBoxPortOPL3";
            this.comboBoxPortOPL3.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortOPL3.TabIndex = 46;
            // 
            // checkBoxConnOPNA
            // 
            this.checkBoxConnOPNA.AutoSize = true;
            this.checkBoxConnOPNA.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPNA_Enable;
            this.checkBoxConnOPNA.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNA_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPNA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPNA.Location = new System.Drawing.Point(6, 206);
            this.checkBoxConnOPNA.Name = "checkBoxConnOPNA";
            this.checkBoxConnOPNA.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPNA.TabIndex = 48;
            this.checkBoxConnOPNA.UseVisualStyleBackColor = true;
            this.checkBoxConnOPNA.CheckedChanged += new System.EventHandler(this.checkBoxOPNA_CheckedChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label15.Location = new System.Drawing.Point(43, 206);
            this.label15.Margin = new System.Windows.Forms.Padding(3);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(114, 20);
            this.label15.TabIndex = 49;
            this.label15.Text = "YM260&8(OPNA)";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label15, "OPN fallbacks to this.");
            this.label15.Click += new System.EventHandler(this.label3_Click);
            this.label15.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxOPNA
            // 
            this.comboBoxOPNA.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNA_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPNA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPNA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPNA.FormattingEnabled = true;
            this.comboBoxOPNA.Items.AddRange(new object[] {
            "VSIF - MSX/PC-6001(FTDI2XX Clk: 232R=25~,H=32~)",
            "SPFM Light - 4759 Player",
            "SPFM - 4759 Player",
            "G.I.M.I.C",
            "VSIF - PC-8801 V2(FTDI2XX Clk: 232R=5~,H=10~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPNA.Location = new System.Drawing.Point(163, 206);
            this.comboBoxOPNA.Name = "comboBoxOPNA";
            this.comboBoxOPNA.Size = new System.Drawing.Size(336, 20);
            this.comboBoxOPNA.TabIndex = 50;
            // 
            // numericUpDownOPNA
            // 
            this.numericUpDownOPNA.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPNA", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPNA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPNA.Location = new System.Drawing.Point(505, 206);
            this.numericUpDownOPNA.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPNA.Name = "numericUpDownOPNA";
            this.numericUpDownOPNA.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPNA.TabIndex = 51;
            this.numericUpDownOPNA.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPNA;
            // 
            // comboBoxPortOPNA
            // 
            this.comboBoxPortOPNA.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNA_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortOPNA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortOPNA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortOPNA.FormattingEnabled = true;
            this.comboBoxPortOPNA.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortOPNA.Location = new System.Drawing.Point(576, 206);
            this.comboBoxPortOPNA.Name = "comboBoxPortOPNA";
            this.comboBoxPortOPNA.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortOPNA.TabIndex = 52;
            // 
            // checkBoxConnY8950
            // 
            this.checkBoxConnY8950.AutoSize = true;
            this.checkBoxConnY8950.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.Y8950_Enable;
            this.checkBoxConnY8950.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8950_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnY8950.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnY8950.Location = new System.Drawing.Point(6, 232);
            this.checkBoxConnY8950.Name = "checkBoxConnY8950";
            this.checkBoxConnY8950.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnY8950.TabIndex = 54;
            this.checkBoxConnY8950.UseVisualStyleBackColor = true;
            this.checkBoxConnY8950.CheckedChanged += new System.EventHandler(this.checkBoxConnY8950_CheckedChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label16.Location = new System.Drawing.Point(43, 232);
            this.label16.Margin = new System.Windows.Forms.Padding(3);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(114, 20);
            this.label16.TabIndex = 55;
            this.label16.Text = "&Y8950(MSX-AUDIO)";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.label16, "OPL fallbacks to this.");
            this.label16.Click += new System.EventHandler(this.label3_Click);
            this.label16.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // numericUpDownY8950
            // 
            this.numericUpDownY8950.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitY8950", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownY8950.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownY8950.Location = new System.Drawing.Point(505, 232);
            this.numericUpDownY8950.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownY8950.Name = "numericUpDownY8950";
            this.numericUpDownY8950.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownY8950.TabIndex = 57;
            this.numericUpDownY8950.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitY8950;
            // 
            // comboBoxPortY8950
            // 
            this.comboBoxPortY8950.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8950_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortY8950.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortY8950.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortY8950.FormattingEnabled = true;
            this.comboBoxPortY8950.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortY8950.Location = new System.Drawing.Point(576, 232);
            this.comboBoxPortY8950.Name = "comboBoxPortY8950";
            this.comboBoxPortY8950.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortY8950.TabIndex = 58;
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.AutoSize = true;
            this.tableLayoutPanel7.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel7.ColumnCount = 2;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 61.93182F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 38.06818F));
            this.tableLayoutPanel7.Controls.Add(this.comboBoxY8950, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.comboBoxY8950Slot, 1, 0);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(160, 229);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 1;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(342, 26);
            this.tableLayoutPanel7.TabIndex = 56;
            // 
            // comboBoxY8950
            // 
            this.comboBoxY8950.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8950_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxY8950.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxY8950.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxY8950.FormattingEnabled = true;
            this.comboBoxY8950.Items.AddRange(new object[] {
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxY8950.Location = new System.Drawing.Point(3, 3);
            this.comboBoxY8950.Name = "comboBoxY8950";
            this.comboBoxY8950.Size = new System.Drawing.Size(205, 20);
            this.comboBoxY8950.TabIndex = 0;
            // 
            // comboBoxY8950Slot
            // 
            this.comboBoxY8950Slot.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8950_Slot", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxY8950Slot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxY8950Slot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxY8950Slot.FormattingEnabled = true;
            this.comboBoxY8950Slot.Items.AddRange(new object[] {
            "ID0 256K RAM",
            "ID1 256K RAM"});
            this.comboBoxY8950Slot.Location = new System.Drawing.Point(214, 3);
            this.comboBoxY8950Slot.Name = "comboBoxY8950Slot";
            this.comboBoxY8950Slot.Size = new System.Drawing.Size(125, 20);
            this.comboBoxY8950Slot.TabIndex = 1;
            // 
            // checkBoxConnOPN
            // 
            this.checkBoxConnOPN.AutoSize = true;
            this.checkBoxConnOPN.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPN_Enable;
            this.checkBoxConnOPN.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPN.Location = new System.Drawing.Point(6, 258);
            this.checkBoxConnOPN.Name = "checkBoxConnOPN";
            this.checkBoxConnOPN.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPN.TabIndex = 60;
            this.checkBoxConnOPN.UseVisualStyleBackColor = true;
            this.checkBoxConnOPN.CheckedChanged += new System.EventHandler(this.checkBoxConnOPN_CheckedChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label13.Location = new System.Drawing.Point(43, 258);
            this.label13.Margin = new System.Windows.Forms.Padding(3);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(114, 20);
            this.label13.TabIndex = 61;
            this.label13.Text = "YM220&3(OPN)";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label13.Click += new System.EventHandler(this.label3_Click);
            this.label13.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxOPN
            // 
            this.comboBoxOPN.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPN.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPN.FormattingEnabled = true;
            this.comboBoxOPN.Items.AddRange(new object[] {
            "VSIF - MSX/PC-6001(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - PC-8801 V2(FTDI2XX Clk: 232R=5~,H=10~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxOPN.Location = new System.Drawing.Point(163, 258);
            this.comboBoxOPN.Name = "comboBoxOPN";
            this.comboBoxOPN.Size = new System.Drawing.Size(336, 20);
            this.comboBoxOPN.TabIndex = 62;
            // 
            // numericUpDownOPN
            // 
            this.numericUpDownOPN.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitOPN", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPN.Location = new System.Drawing.Point(505, 258);
            this.numericUpDownOPN.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownOPN.Name = "numericUpDownOPN";
            this.numericUpDownOPN.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownOPN.TabIndex = 63;
            this.numericUpDownOPN.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitOPN;
            // 
            // comboBoxPortOPN
            // 
            this.comboBoxPortOPN.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortOPN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortOPN.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortOPN.FormattingEnabled = true;
            this.comboBoxPortOPN.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortOPN.Location = new System.Drawing.Point(576, 258);
            this.comboBoxPortOPN.Name = "comboBoxPortOPN";
            this.comboBoxPortOPN.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortOPN.TabIndex = 64;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label17.Location = new System.Drawing.Point(661, 6);
            this.label17.Margin = new System.Windows.Forms.Padding(3);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(75, 12);
            this.label17.TabIndex = 5;
            this.label17.Text = "FTDI DivOfst";
            this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericUpDownOPN2Div
            // 
            this.numericUpDownOPN2Div.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPN2Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPN2Div.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPN2Div.Location = new System.Drawing.Point(660, 23);
            this.numericUpDownOPN2Div.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPN2Div.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPN2Div.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPN2Div.Name = "numericUpDownOPN2Div";
            this.numericUpDownOPN2Div.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPN2Div.TabIndex = 11;
            this.numericUpDownOPN2Div.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPN2Div;
            // 
            // numericUpDownDCSGDiv
            // 
            this.numericUpDownDCSGDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "DCSGDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownDCSGDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownDCSGDiv.Location = new System.Drawing.Point(660, 49);
            this.numericUpDownDCSGDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownDCSGDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownDCSGDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownDCSGDiv.Name = "numericUpDownDCSGDiv";
            this.numericUpDownDCSGDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownDCSGDiv.TabIndex = 17;
            this.numericUpDownDCSGDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.DCSGDiv;
            // 
            // numericUpDownOPLLDiv
            // 
            this.numericUpDownOPLLDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPLLDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPLLDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPLLDiv.Location = new System.Drawing.Point(660, 75);
            this.numericUpDownOPLLDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPLLDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPLLDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPLLDiv.Name = "numericUpDownOPLLDiv";
            this.numericUpDownOPLLDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPLLDiv.TabIndex = 23;
            this.numericUpDownOPLLDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPLLDiv;
            // 
            // numericUpDownSCCDiv
            // 
            this.numericUpDownSCCDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "SCCDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownSCCDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownSCCDiv.Location = new System.Drawing.Point(660, 101);
            this.numericUpDownSCCDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownSCCDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownSCCDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownSCCDiv.Name = "numericUpDownSCCDiv";
            this.numericUpDownSCCDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownSCCDiv.TabIndex = 29;
            this.numericUpDownSCCDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.SCCDiv;
            // 
            // numericUpDownPSGDiv
            // 
            this.numericUpDownPSGDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "PSGDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownPSGDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownPSGDiv.Location = new System.Drawing.Point(660, 127);
            this.numericUpDownPSGDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownPSGDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownPSGDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownPSGDiv.Name = "numericUpDownPSGDiv";
            this.numericUpDownPSGDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownPSGDiv.TabIndex = 35;
            this.numericUpDownPSGDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.PSGDiv;
            // 
            // numericUpDownOPMDiv
            // 
            this.numericUpDownOPMDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPMDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPMDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPMDiv.Location = new System.Drawing.Point(660, 153);
            this.numericUpDownOPMDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPMDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPMDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPMDiv.Name = "numericUpDownOPMDiv";
            this.numericUpDownOPMDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPMDiv.TabIndex = 41;
            this.numericUpDownOPMDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPMDiv;
            // 
            // numericUpDownOPL3Div
            // 
            this.numericUpDownOPL3Div.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPL3Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPL3Div.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPL3Div.Location = new System.Drawing.Point(660, 179);
            this.numericUpDownOPL3Div.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPL3Div.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPL3Div.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPL3Div.Name = "numericUpDownOPL3Div";
            this.numericUpDownOPL3Div.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPL3Div.TabIndex = 47;
            this.numericUpDownOPL3Div.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPL3Div;
            // 
            // numericUpDownOPNADiv
            // 
            this.numericUpDownOPNADiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNADiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPNADiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPNADiv.Location = new System.Drawing.Point(660, 205);
            this.numericUpDownOPNADiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPNADiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPNADiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPNADiv.Name = "numericUpDownOPNADiv";
            this.numericUpDownOPNADiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPNADiv.TabIndex = 53;
            this.numericUpDownOPNADiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPNADiv;
            // 
            // numericUpDown8950Div
            // 
            this.numericUpDown8950Div.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "Y8950Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown8950Div.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDown8950Div.Location = new System.Drawing.Point(660, 231);
            this.numericUpDown8950Div.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDown8950Div.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDown8950Div.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDown8950Div.Name = "numericUpDown8950Div";
            this.numericUpDown8950Div.Size = new System.Drawing.Size(77, 19);
            this.numericUpDown8950Div.TabIndex = 59;
            this.numericUpDown8950Div.Value = global::zanac.VGMPlayer.Properties.Settings.Default.Y8950Div;
            // 
            // numericUpDownOPNDiv
            // 
            this.numericUpDownOPNDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownOPNDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownOPNDiv.Location = new System.Drawing.Point(660, 257);
            this.numericUpDownOPNDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownOPNDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownOPNDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownOPNDiv.Name = "numericUpDownOPNDiv";
            this.numericUpDownOPNDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownOPNDiv.TabIndex = 65;
            this.numericUpDownOPNDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.OPNDiv;
            // 
            // checkBoxConnNES
            // 
            this.checkBoxConnNES.AutoSize = true;
            this.checkBoxConnNES.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.NES_Enable;
            this.checkBoxConnNES.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "NES_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnNES.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnNES.Location = new System.Drawing.Point(6, 284);
            this.checkBoxConnNES.Name = "checkBoxConnNES";
            this.checkBoxConnNES.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnNES.TabIndex = 66;
            this.checkBoxConnNES.UseVisualStyleBackColor = true;
            this.checkBoxConnNES.CheckedChanged += new System.EventHandler(this.checkBoxConnNES_CheckedChanged);
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label20.Location = new System.Drawing.Point(43, 284);
            this.label20.Margin = new System.Windows.Forms.Padding(3);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(114, 20);
            this.label20.TabIndex = 67;
            this.label20.Text = "NES &APU(Famicom)";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label20.Click += new System.EventHandler(this.label3_Click);
            this.label20.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxNES
            // 
            this.comboBoxNES.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "NES_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxNES.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxNES.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNES.FormattingEnabled = true;
            this.comboBoxNES.Items.AddRange(new object[] {
            "VSIF - Famicom(FTDI2XX Clk: 232R=11~,H=27~)",
            "VSIF - MAmi Cart w/ VRC7(FTDI2XX Clk: 232R=11~,H=27~)"});
            this.comboBoxNES.Location = new System.Drawing.Point(163, 284);
            this.comboBoxNES.Name = "comboBoxNES";
            this.comboBoxNES.Size = new System.Drawing.Size(336, 20);
            this.comboBoxNES.TabIndex = 62;
            // 
            // numericUpDownNES
            // 
            this.numericUpDownNES.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitNES", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownNES.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownNES.Location = new System.Drawing.Point(505, 284);
            this.numericUpDownNES.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownNES.Name = "numericUpDownNES";
            this.numericUpDownNES.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownNES.TabIndex = 63;
            this.numericUpDownNES.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitNES;
            // 
            // comboBoxPortNES
            // 
            this.comboBoxPortNES.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "NES_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortNES.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortNES.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortNES.FormattingEnabled = true;
            this.comboBoxPortNES.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortNES.Location = new System.Drawing.Point(576, 284);
            this.comboBoxPortNES.Name = "comboBoxPortNES";
            this.comboBoxPortNES.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortNES.TabIndex = 64;
            // 
            // numericUpDownNESDiv
            // 
            this.numericUpDownNESDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "NESDiv", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownNESDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownNESDiv.Location = new System.Drawing.Point(660, 283);
            this.numericUpDownNESDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownNESDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownNESDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownNESDiv.Name = "numericUpDownNESDiv";
            this.numericUpDownNESDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownNESDiv.TabIndex = 65;
            this.numericUpDownNESDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.NESDiv;
            // 
            // checkBoxConnMCD
            // 
            this.checkBoxConnMCD.AutoSize = true;
            this.checkBoxConnMCD.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.MCD_Enable;
            this.checkBoxConnMCD.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "MCD_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnMCD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnMCD.Location = new System.Drawing.Point(6, 310);
            this.checkBoxConnMCD.Name = "checkBoxConnMCD";
            this.checkBoxConnMCD.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnMCD.TabIndex = 66;
            this.checkBoxConnMCD.UseVisualStyleBackColor = true;
            this.checkBoxConnMCD.CheckedChanged += new System.EventHandler(this.checkBoxConnMCD_CheckedChanged);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label21.Location = new System.Drawing.Point(43, 310);
            this.label21.Margin = new System.Windows.Forms.Padding(3);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(114, 20);
            this.label21.TabIndex = 67;
            this.label21.Text = "&RF5C164(MEGA-CD)";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label21.Click += new System.EventHandler(this.label3_Click);
            this.label21.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxMCD
            // 
            this.comboBoxMCD.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "MCD_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxMCD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxMCD.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMCD.FormattingEnabled = true;
            this.comboBoxMCD.Items.AddRange(new object[] {
            "VSIF - MD/Genesis CD(FTDI2XX Clk: H=9~)"});
            this.comboBoxMCD.Location = new System.Drawing.Point(163, 310);
            this.comboBoxMCD.Name = "comboBoxMCD";
            this.comboBoxMCD.Size = new System.Drawing.Size(336, 20);
            this.comboBoxMCD.TabIndex = 62;
            // 
            // numericUpDownMCD
            // 
            this.numericUpDownMCD.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitMCD", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownMCD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownMCD.Location = new System.Drawing.Point(505, 310);
            this.numericUpDownMCD.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownMCD.Name = "numericUpDownMCD";
            this.numericUpDownMCD.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownMCD.TabIndex = 63;
            this.numericUpDownMCD.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitMCD;
            // 
            // comboBoxPortMCD
            // 
            this.comboBoxPortMCD.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "MCD_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortMCD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortMCD.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortMCD.FormattingEnabled = true;
            this.comboBoxPortMCD.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortMCD.Location = new System.Drawing.Point(576, 310);
            this.comboBoxPortMCD.Name = "comboBoxPortMCD";
            this.comboBoxPortMCD.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortMCD.TabIndex = 64;
            // 
            // numericUpDownMCDDiv
            // 
            this.numericUpDownMCDDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "MCD_Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownMCDDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownMCDDiv.Location = new System.Drawing.Point(660, 309);
            this.numericUpDownMCDDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownMCDDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownMCDDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownMCDDiv.Name = "numericUpDownMCDDiv";
            this.numericUpDownMCDDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownMCDDiv.TabIndex = 65;
            this.numericUpDownMCDDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.MCD_Div;
            // 
            // checkBoxConnSAA
            // 
            this.checkBoxConnSAA.AutoSize = true;
            this.checkBoxConnSAA.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.SAA_Enable;
            this.checkBoxConnSAA.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "SAA_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnSAA.Location = new System.Drawing.Point(6, 336);
            this.checkBoxConnSAA.Name = "checkBoxConnSAA";
            this.checkBoxConnSAA.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnSAA.TabIndex = 66;
            this.checkBoxConnSAA.UseVisualStyleBackColor = true;
            this.checkBoxConnSAA.CheckedChanged += new System.EventHandler(this.checkBoxCnnSAA_CheckedChanged);
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label22.Location = new System.Drawing.Point(43, 336);
            this.label22.Margin = new System.Windows.Forms.Padding(3);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(114, 20);
            this.label22.TabIndex = 67;
            this.label22.Text = "&SAA1099";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label22.Click += new System.EventHandler(this.label3_Click);
            this.label22.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxSAA
            // 
            this.comboBoxSAA.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SAA_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxSAA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSAA.FormattingEnabled = true;
            this.comboBoxSAA.Items.AddRange(new object[] {
            "VSIF - MSX(FTDI2XX Clk: 232R=25~,H=32~)",
            "VSIF - turboR(FTDI2XX Clk: 232R=8~,H=9~)",
            "VSIF - MSX(MSXπ UART)",
            "VSIF - turboR(MSXπ UART)"});
            this.comboBoxSAA.Location = new System.Drawing.Point(163, 336);
            this.comboBoxSAA.Name = "comboBoxSAA";
            this.comboBoxSAA.Size = new System.Drawing.Size(336, 20);
            this.comboBoxSAA.TabIndex = 62;
            // 
            // numericUpDownSAA
            // 
            this.numericUpDownSAA.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitSAA", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownSAA.Location = new System.Drawing.Point(505, 336);
            this.numericUpDownSAA.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSAA.Name = "numericUpDownSAA";
            this.numericUpDownSAA.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownSAA.TabIndex = 63;
            this.numericUpDownSAA.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitSAA;
            // 
            // comboBoxPortSAA
            // 
            this.comboBoxPortSAA.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "SAA_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortSAA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortSAA.FormattingEnabled = true;
            this.comboBoxPortSAA.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortSAA.Location = new System.Drawing.Point(576, 336);
            this.comboBoxPortSAA.Name = "comboBoxPortSAA";
            this.comboBoxPortSAA.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortSAA.TabIndex = 64;
            // 
            // numericUpDownSAADiv
            // 
            this.numericUpDownSAADiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "SAA_Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownSAADiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownSAADiv.Location = new System.Drawing.Point(660, 335);
            this.numericUpDownSAADiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownSAADiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownSAADiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownSAADiv.Name = "numericUpDownSAADiv";
            this.numericUpDownSAADiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownSAADiv.TabIndex = 65;
            this.numericUpDownSAADiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.SAA_Div;
            // 
            // checkBoxConnPCE
            // 
            this.checkBoxConnPCE.AutoSize = true;
            this.checkBoxConnPCE.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.PCE_Enable;
            this.checkBoxConnPCE.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "PCE_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnPCE.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnPCE.Location = new System.Drawing.Point(6, 362);
            this.checkBoxConnPCE.Name = "checkBoxConnPCE";
            this.checkBoxConnPCE.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnPCE.TabIndex = 68;
            this.checkBoxConnPCE.UseVisualStyleBackColor = true;
            this.checkBoxConnPCE.CheckedChanged += new System.EventHandler(this.checkBoxConnPCE_CheckedChanged);
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label23.Location = new System.Drawing.Point(43, 362);
            this.label23.Margin = new System.Windows.Forms.Padding(3);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(114, 20);
            this.label23.TabIndex = 69;
            this.label23.Text = "&HuC6280(PCE)";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label23.Click += new System.EventHandler(this.label3_Click);
            this.label23.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxPortPCE
            // 
            this.comboBoxPortPCE.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "PCE_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortPCE.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortPCE.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortPCE.FormattingEnabled = true;
            this.comboBoxPortPCE.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortPCE.Location = new System.Drawing.Point(576, 362);
            this.comboBoxPortPCE.Name = "comboBoxPortPCE";
            this.comboBoxPortPCE.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortPCE.TabIndex = 71;
            // 
            // checkBoxConnOPNB
            // 
            this.checkBoxConnOPNB.AutoSize = true;
            this.checkBoxConnOPNB.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.OPNB_Enable;
            this.checkBoxConnOPNB.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNB_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnOPNB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnOPNB.Location = new System.Drawing.Point(6, 388);
            this.checkBoxConnOPNB.Name = "checkBoxConnOPNB";
            this.checkBoxConnOPNB.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnOPNB.TabIndex = 72;
            this.checkBoxConnOPNB.UseVisualStyleBackColor = true;
            this.checkBoxConnOPNB.CheckedChanged += new System.EventHandler(this.checkBoxConnOPNB_CheckedChanged);
            // 
            // comboBoxOPNB
            // 
            this.comboBoxOPNB.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNB_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPNB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPNB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPNB.FormattingEnabled = true;
            this.comboBoxOPNB.Items.AddRange(new object[] {
            "VSIF - MSX NEOTRON(MSXπ UART)",
            "VSIF - turboR NEOTRON(MSXπ UART)"});
            this.comboBoxOPNB.Location = new System.Drawing.Point(163, 388);
            this.comboBoxOPNB.Name = "comboBoxOPNB";
            this.comboBoxOPNB.Size = new System.Drawing.Size(336, 20);
            this.comboBoxOPNB.TabIndex = 70;
            // 
            // comboBoxPortOPNB
            // 
            this.comboBoxPortOPNB.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNB_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortOPNB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortOPNB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortOPNB.FormattingEnabled = true;
            this.comboBoxPortOPNB.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortOPNB.Location = new System.Drawing.Point(576, 388);
            this.comboBoxPortOPNB.Name = "comboBoxPortOPNB";
            this.comboBoxPortOPNB.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortOPNB.TabIndex = 71;
            // 
            // comboBoxOPNBType
            // 
            this.comboBoxOPNBType.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "OPNB_Type", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxOPNBType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxOPNBType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxOPNBType.FormattingEnabled = true;
            this.comboBoxOPNBType.Items.AddRange(new object[] {
            "YM2610(OPNB)",
            "YM2610B(OPNB-B)"});
            this.comboBoxOPNBType.Location = new System.Drawing.Point(42, 387);
            this.comboBoxOPNBType.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxOPNBType.Name = "comboBoxOPNBType";
            this.comboBoxOPNBType.Size = new System.Drawing.Size(116, 20);
            this.comboBoxOPNBType.TabIndex = 25;
            // 
            // checkBoxConnPWM
            // 
            this.checkBoxConnPWM.AutoSize = true;
            this.checkBoxConnPWM.Checked = global::zanac.VGMPlayer.Properties.Settings.Default.PWM_Enable;
            this.checkBoxConnPWM.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::zanac.VGMPlayer.Properties.Settings.Default, "PWM_Enable", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBoxConnPWM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBoxConnPWM.Location = new System.Drawing.Point(6, 414);
            this.checkBoxConnPWM.Name = "checkBoxConnPWM";
            this.checkBoxConnPWM.Size = new System.Drawing.Size(31, 20);
            this.checkBoxConnPWM.TabIndex = 66;
            this.checkBoxConnPWM.UseVisualStyleBackColor = true;
            this.checkBoxConnPWM.CheckedChanged += new System.EventHandler(this.checkBoxConnPWM_CheckedChanged);
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label24.Location = new System.Drawing.Point(43, 414);
            this.label24.Margin = new System.Windows.Forms.Padding(3);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(114, 20);
            this.label24.TabIndex = 67;
            this.label24.Text = "P&WM(SUPER 32X)";
            this.label24.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label24.Click += new System.EventHandler(this.label3_Click);
            this.label24.DoubleClick += new System.EventHandler(this.label3_DoubleClick);
            // 
            // comboBoxPWM
            // 
            this.comboBoxPWM.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "PWM_IF", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPWM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPWM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPWM.FormattingEnabled = true;
            this.comboBoxPWM.Items.AddRange(new object[] {
            "VSIF - 32X(FTDI2XX Clk: H=9~)"});
            this.comboBoxPWM.Location = new System.Drawing.Point(163, 414);
            this.comboBoxPWM.Name = "comboBoxPWM";
            this.comboBoxPWM.Size = new System.Drawing.Size(336, 20);
            this.comboBoxPWM.TabIndex = 62;
            // 
            // numericUpDownPWM
            // 
            this.numericUpDownPWM.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "BitBangWaitPWM", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownPWM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownPWM.Location = new System.Drawing.Point(505, 414);
            this.numericUpDownPWM.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPWM.Name = "numericUpDownPWM";
            this.numericUpDownPWM.Size = new System.Drawing.Size(65, 19);
            this.numericUpDownPWM.TabIndex = 63;
            this.numericUpDownPWM.Value = global::zanac.VGMPlayer.Properties.Settings.Default.BitBangWaitMCD;
            // 
            // comboBoxPortPWM
            // 
            this.comboBoxPortPWM.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "PWM_Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxPortPWM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxPortPWM.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPortPWM.FormattingEnabled = true;
            this.comboBoxPortPWM.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256"});
            this.comboBoxPortPWM.Location = new System.Drawing.Point(576, 414);
            this.comboBoxPortPWM.Name = "comboBoxPortPWM";
            this.comboBoxPortPWM.Size = new System.Drawing.Size(79, 20);
            this.comboBoxPortPWM.TabIndex = 64;
            // 
            // numericUpDownPWMDiv
            // 
            this.numericUpDownPWMDiv.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::zanac.VGMPlayer.Properties.Settings.Default, "PWM_Div", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDownPWMDiv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownPWMDiv.Location = new System.Drawing.Point(660, 413);
            this.numericUpDownPWMDiv.Margin = new System.Windows.Forms.Padding(2);
            this.numericUpDownPWMDiv.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numericUpDownPWMDiv.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            -2147483648});
            this.numericUpDownPWMDiv.Name = "numericUpDownPWMDiv";
            this.numericUpDownPWMDiv.Size = new System.Drawing.Size(77, 19);
            this.numericUpDownPWMDiv.TabIndex = 65;
            this.numericUpDownPWMDiv.Value = global::zanac.VGMPlayer.Properties.Settings.Default.PWM_Div;
            // 
            // comboBoxWaitAlg
            // 
            this.comboBoxWaitAlg.DataBindings.Add(new System.Windows.Forms.Binding("SelectedIndex", global::zanac.VGMPlayer.Properties.Settings.Default, "WaitAlg", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.comboBoxWaitAlg.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxWaitAlg.DropDownWidth = 256;
            this.comboBoxWaitAlg.FormattingEnabled = true;
            this.comboBoxWaitAlg.Items.AddRange(new object[] {
            "Accurate (Time compensated)",
            "Wait (No time compensated)"});
            this.comboBoxWaitAlg.Location = new System.Drawing.Point(674, 3);
            this.comboBoxWaitAlg.Name = "comboBoxWaitAlg";
            this.comboBoxWaitAlg.Size = new System.Drawing.Size(82, 20);
            this.comboBoxWaitAlg.TabIndex = global::zanac.VGMPlayer.Properties.Settings.Default.WaitAlg;
            // 
            // listViewList
            // 
            this.listViewList.AllowDrop = true;
            this.listViewList.AllowItemDrag = true;
            this.listViewList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFile});
            this.listViewList.ContextMenuStrip = this.contextMenuStripList;
            this.listViewList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewList.FullRowSelect = true;
            this.listViewList.GridLines = true;
            this.listViewList.HideSelection = false;
            this.listViewList.LabelWrap = false;
            this.listViewList.Location = new System.Drawing.Point(0, 230);
            this.listViewList.Name = "listViewList";
            this.listViewList.Size = new System.Drawing.Size(759, 231);
            this.listViewList.TabIndex = 2;
            this.listViewList.UseCompatibleStateImageBehavior = false;
            this.listViewList.View = System.Windows.Forms.View.Details;
            this.listViewList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewList_ColumnClick);
            this.listViewList.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.listViewList_ItemDrag);
            this.listViewList.SizeChanged += new System.EventHandler(this.listViewList_SizeChanged);
            this.listViewList.DragDrop += new System.Windows.Forms.DragEventHandler(this.listViewList_DragDrop);
            this.listViewList.DragEnter += new System.Windows.Forms.DragEventHandler(this.listView1_DragEnter);
            this.listViewList.DragOver += new System.Windows.Forms.DragEventHandler(this.listViewList_DragOver);
            this.listViewList.DragLeave += new System.EventHandler(this.listViewList_DragLeave);
            this.listViewList.DoubleClick += new System.EventHandler(this.listViewList_DoubleClick);
            this.listViewList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listViewList_KeyDown);
            // 
            // columnHeaderFile
            // 
            this.columnHeaderFile.Text = "File name";
            this.columnHeaderFile.Width = 325;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(759, 644);
            this.Controls.Add(this.listViewList);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanelButton);
            this.Controls.Add(this.tableLayoutPanelPort);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.statusStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "Real chip VGM/XGM/MGS player V2.15 by Itoken";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormMain_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tableLayoutPanelButton.ResumeLayout(false);
            this.tableLayoutPanelButton.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel8.PerformLayout();
            this.contextMenuStripList.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLooped)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.tableLayoutPanelPort.ResumeLayout(false);
            this.tableLayoutPanelPort.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownClkWidthDCSG)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPLL)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSCC)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownY8910)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPM)).EndInit();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPL3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownY8950)).EndInit();
            this.tableLayoutPanel7.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPN2Div)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDCSGDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPLLDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSCCDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPSGDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPMDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPL3Div)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNADiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown8950Div)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownOPNDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownNES)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownNESDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMCD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMCDDiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSAA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSAADiv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPWM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPWMDiv)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fILEToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem eXITToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelPort;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelButton;
        private System.Windows.Forms.Button buttonPlay;
        private System.Windows.Forms.Button buttonSlow;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonFast;
        private System.Windows.Forms.Button buttonFreeze;
        private System.Windows.Forms.CheckBox checkBoxLoop;
        private ListViewInsertionDrag.DraggableListView listViewList;
        private System.Windows.Forms.ColumnHeader columnHeaderFile;
        private System.Windows.Forms.Button buttonPrev;
        private System.Windows.Forms.Button buttonNext;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxOPN2;
        private System.Windows.Forms.ComboBox comboBoxOPLL;
        private System.Windows.Forms.ComboBox comboBoxDCSG;
        private System.Windows.Forms.ComboBox comboBoxPortSN76489;
        private System.Windows.Forms.ComboBox comboBoxPortYM2612;
        private System.Windows.Forms.ComboBox comboBoxPortYm2413;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelLoad;
        private System.Windows.Forms.ProgressBar progressBarLoad;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ImageList imageListBig;
        private System.Windows.Forms.ImageList imageListSmall;
        private System.Windows.Forms.Label labelSpeed;
        private System.Windows.Forms.NumericUpDown numericUpDownLooped;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxTitle;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox checkBoxConnOPN2;
        private System.Windows.Forms.CheckBox checkBoxConnOPLL;
        private System.Windows.Forms.CheckBox checkBoxConnDCSG;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDownClkWidthDCSG;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripList;
        private System.Windows.Forms.ToolStripMenuItem playToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem explorerToolStripMenuItem;
        private System.Windows.Forms.Button buttonEject;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ComboBox comboBoxSCC;
        private System.Windows.Forms.ComboBox comboBoxPortSCC;
        private System.Windows.Forms.CheckBox checkBoxConnSCC;
        private System.Windows.Forms.NumericUpDown numericUpDownOPLL;
        private System.Windows.Forms.NumericUpDown numericUpDownOPN2;
        private System.Windows.Forms.NumericUpDown numericUpDownSCC;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.ComboBox comboBoxY8910;
        private System.Windows.Forms.NumericUpDown numericUpDownY8910;
        private System.Windows.Forms.ComboBox comboBoxPortY8910;
        private System.Windows.Forms.CheckBox checkBoxConnY8910;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.ComboBox comboBoxSccSlot;
        private System.Windows.Forms.ToolStripMenuItem aBOUTToolStripMenuItem;
        private System.Windows.Forms.ComboBox comboBoxSccType;
        private System.Windows.Forms.CheckBox checkBoxConnOPM;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBoxOPM;
        private System.Windows.Forms.NumericUpDown numericUpDownOPM;
        private System.Windows.Forms.ComboBox comboBoxPortOPM;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.ComboBox comboBoxOpmSlot;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.ComboBox comboBoxOpllSlot;
        private System.Windows.Forms.CheckBox checkBoxConnOPL3;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox comboBoxOPL3;
        private System.Windows.Forms.NumericUpDown numericUpDownOPL3;
        private System.Windows.Forms.ComboBox comboBoxPortOPL3;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.CheckBox checkBoxConnOPNA;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox comboBoxOPNA;
        private System.Windows.Forms.NumericUpDown numericUpDownOPNA;
        private System.Windows.Forms.ComboBox comboBoxPortOPNA;
        private System.Windows.Forms.CheckBox checkBoxConnY8950;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.NumericUpDown numericUpDownY8950;
        private System.Windows.Forms.ComboBox comboBoxPortY8950;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.ComboBox comboBoxY8950;
        private System.Windows.Forms.ComboBox comboBoxY8950Slot;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.CheckBox checkBoxLoopTimes;
        private System.Windows.Forms.DateTimePicker dateTimePickerLoopTimes;
        private System.Windows.Forms.Label labelElapsed;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox checkBoxCnvClk;
        private System.Windows.Forms.CheckBox checkBoxConnOPN;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox comboBoxOPN;
        private System.Windows.Forms.NumericUpDown numericUpDownOPN;
        private System.Windows.Forms.ComboBox comboBoxPortOPN;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDownOPN2Div;
        private System.Windows.Forms.NumericUpDown numericUpDownDCSGDiv;
        private System.Windows.Forms.NumericUpDown numericUpDownOPLLDiv;
        private System.Windows.Forms.NumericUpDown numericUpDownSCCDiv;
        private System.Windows.Forms.NumericUpDown numericUpDownPSGDiv;
        private System.Windows.Forms.NumericUpDown numericUpDownOPMDiv;
        private System.Windows.Forms.NumericUpDown numericUpDownOPL3Div;
        private System.Windows.Forms.NumericUpDown numericUpDownOPNADiv;
        private System.Windows.Forms.NumericUpDown numericUpDown8950Div;
        private System.Windows.Forms.NumericUpDown numericUpDownOPNDiv;
        private System.Windows.Forms.ToolStripMenuItem saveAsThePlaylistToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialogM3U;
        private System.Windows.Forms.ToolStripMenuItem loadPlaylistToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addPlaylistToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBoxUseSystem;
        private System.Windows.Forms.TextBox textBoxSongSystem;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.ToolStripMenuItem oPTIONSToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBoxConnNES;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ComboBox comboBoxNES;
        private System.Windows.Forms.NumericUpDown numericUpDownNES;
        private System.Windows.Forms.ComboBox comboBoxPortNES;
        private System.Windows.Forms.NumericUpDown numericUpDownNESDiv;
        private System.Windows.Forms.CheckBox checkBoxConnMCD;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ComboBox comboBoxMCD;
        private System.Windows.Forms.NumericUpDown numericUpDownMCD;
        private System.Windows.Forms.ComboBox comboBoxPortMCD;
        private System.Windows.Forms.NumericUpDown numericUpDownMCDDiv;
        private System.Windows.Forms.CheckBox checkBoxConnSAA;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.ComboBox comboBoxSAA;
        private System.Windows.Forms.NumericUpDown numericUpDownSAA;
        private System.Windows.Forms.ComboBox comboBoxPortSAA;
        private System.Windows.Forms.NumericUpDown numericUpDownSAADiv;
        private System.Windows.Forms.ComboBox comboBoxWaitAlg;
        private System.Windows.Forms.CheckBox checkBoxClip;
        private System.Windows.Forms.ToolStripMenuItem cLOUDToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem vgmripsToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBoxConnPCE;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.ComboBox comboBoxPCE;
        private System.Windows.Forms.ComboBox comboBoxPortPCE;
        private System.Windows.Forms.CheckBox checkBoxConnOPNB;
        private System.Windows.Forms.ComboBox comboBoxOPNB;
        private System.Windows.Forms.ComboBox comboBoxPortOPNB;
        private System.Windows.Forms.ComboBox comboBoxOPNBType;
        private System.Windows.Forms.CheckBox checkBoxConnPWM;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.ComboBox comboBoxPWM;
        private System.Windows.Forms.NumericUpDown numericUpDownPWM;
        private System.Windows.Forms.ComboBox comboBoxPortPWM;
        private System.Windows.Forms.NumericUpDown numericUpDownPWMDiv;
    }
}