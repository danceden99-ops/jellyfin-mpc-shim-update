using FontAwesome.Sharp;
using Icon = System.Drawing.Icon;

namespace JellyfinMPCShim.Tray;

partial class MainForm
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
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        trayIcon = new NotifyIcon(components);
        trayMenu = new ContextMenuStrip(components);
        connectionToolStripMenuItem = new ToolStripMenuItem();
        settingsToolStripMenuItem = new ToolStripMenuItem();
        groupsToolStripMenuItem = new ToolStripMenuItem();
        logsToolStripMenuItem = new ToolStripMenuItem();
        exitToolStripMenuItem = new ToolStripMenuItem();
        groupBoxJellyfin = new GroupBox();
        textBoxCastName = new TextBox();
        labelCastName = new Label();
        labelServerExample = new Label();
        labelAuthStatus = new Label();
        buttonSignOut = new Button();
        buttonQuickConnect = new Button();
        textBoxJellyfinPassword = new TextBox();
        textBoxJellyfinUsername = new TextBox();
        textBoxJellyfinHost = new TextBox();
        label3 = new Label();
        label2 = new Label();
        label1 = new Label();
        groupBoxMpc = new GroupBox();
        textBoxMpcArgs = new TextBox();
        labelMpcArgs = new Label();
        comboBoxMpcFork = new ComboBox();
        labelMpcFork = new Label();
        checkBoxMpcFullscreen = new CheckBox();
        buttonSelectMpc = new IconButton();
        numericUpDownMpcPort = new NumericUpDown();
        textBoxMpcPath = new TextBox();
        label5 = new Label();
        label4 = new Label();
        buttonOk = new Button();
        buttonCancel = new Button();
        trayMenu.SuspendLayout();
        groupBoxJellyfin.SuspendLayout();
        groupBoxMpc.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numericUpDownMpcPort).BeginInit();
        SuspendLayout();
        // 
        // trayIcon
        // 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Icon = (Icon)resources.GetObject("trayIcon.Icon");
        trayIcon.Text = "Jellyfin MPC Shim";
        trayIcon.Visible = true;
        trayIcon.DoubleClick += trayIcon_DoubleClick;
        // 
        // trayMenu
        // 
        trayMenu.Items.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, settingsToolStripMenuItem, groupsToolStripMenuItem, logsToolStripMenuItem, exitToolStripMenuItem });
        trayMenu.Name = "trayMenu";
        trayMenu.Size = new Size(181, 137);
        trayMenu.Opening += trayMenu_Opening;
        // 
        // connectionToolStripMenuItem
        // 
        connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
        connectionToolStripMenuItem.Size = new Size(89, 20);
        connectionToolStripMenuItem.Text = "Connection";
        connectionToolStripMenuItem.Visible = false;
        // 
        // settingsToolStripMenuItem
        // 
        settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
        settingsToolStripMenuItem.Size = new Size(180, 22);
        settingsToolStripMenuItem.Text = "Settings";
        settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
        // 
        // groupsToolStripMenuItem
        // 
        groupsToolStripMenuItem.Name = "groupsToolStripMenuItem";
        groupsToolStripMenuItem.Size = new Size(180, 22);
        groupsToolStripMenuItem.Text = "Groups";
        // 
        // logsToolStripMenuItem
        // 
        logsToolStripMenuItem.Name = "logsToolStripMenuItem";
        logsToolStripMenuItem.Size = new Size(180, 22);
        logsToolStripMenuItem.Text = "Logs";
        logsToolStripMenuItem.Click += logsToolStripMenuItem_Click;
        // 
        // exitToolStripMenuItem
        // 
        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(180, 22);
        exitToolStripMenuItem.Text = "Exit";
        exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
        // 
        // groupBoxJellyfin
        // 
        groupBoxJellyfin.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxJellyfin.Controls.Add(textBoxCastName);
        groupBoxJellyfin.Controls.Add(labelCastName);
        groupBoxJellyfin.Controls.Add(labelServerExample);
        groupBoxJellyfin.Controls.Add(labelAuthStatus);
        groupBoxJellyfin.Controls.Add(buttonSignOut);
        groupBoxJellyfin.Controls.Add(buttonQuickConnect);
        groupBoxJellyfin.Controls.Add(textBoxJellyfinPassword);
        groupBoxJellyfin.Controls.Add(textBoxJellyfinUsername);
        groupBoxJellyfin.Controls.Add(textBoxJellyfinHost);
        groupBoxJellyfin.Controls.Add(label3);
        groupBoxJellyfin.Controls.Add(label2);
        groupBoxJellyfin.Controls.Add(label1);
        groupBoxJellyfin.Location = new Point(12, 12);
        groupBoxJellyfin.Name = "groupBoxJellyfin";
        groupBoxJellyfin.Size = new Size(514, 191);
        groupBoxJellyfin.TabIndex = 1;
        groupBoxJellyfin.TabStop = false;
        groupBoxJellyfin.Text = "Jellyfin";
        // 
        // labelCastName
        // 
        labelCastName.AutoSize = true;
        labelCastName.Location = new Point(6, 112);
        labelCastName.Name = "labelCastName";
        labelCastName.Size = new Size(65, 15);
        labelCastName.TabIndex = 10;
        labelCastName.Text = "Cast Name";
        // 
        // textBoxCastName
        // 
        textBoxCastName.Location = new Point(83, 109);
        textBoxCastName.Name = "textBoxCastName";
        textBoxCastName.PlaceholderText = "Jellyfin MPC Shim Tray";
        textBoxCastName.Size = new Size(425, 23);
        textBoxCastName.TabIndex = 6;
        // 
        // buttonQuickConnect
        // 
        buttonQuickConnect.Location = new Point(83, 141);
        buttonQuickConnect.Name = "buttonQuickConnect";
        buttonQuickConnect.Size = new Size(125, 28);
        buttonQuickConnect.TabIndex = 7;
        buttonQuickConnect.Text = "Quick Connect";
        buttonQuickConnect.UseVisualStyleBackColor = true;
        buttonQuickConnect.Click += buttonQuickConnect_Click;
        // 
        // buttonSignOut
        // 
        buttonSignOut.Location = new Point(214, 141);
        buttonSignOut.Name = "buttonSignOut";
        buttonSignOut.Size = new Size(90, 28);
        buttonSignOut.TabIndex = 8;
        buttonSignOut.Text = "Sign Out";
        buttonSignOut.UseVisualStyleBackColor = true;
        buttonSignOut.Click += buttonSignOut_Click;
        // 
        // labelAuthStatus
        // 
        labelAuthStatus.AutoSize = true;
        labelAuthStatus.Location = new Point(312, 147);
        labelAuthStatus.Name = "labelAuthStatus";
        labelAuthStatus.Size = new Size(0, 15);
        labelAuthStatus.TabIndex = 9;
        // 
        // textBoxJellyfinPassword
        // 
        textBoxJellyfinPassword.Location = new Point(83, 80);
        textBoxJellyfinPassword.Name = "textBoxJellyfinPassword";
        textBoxJellyfinPassword.PasswordChar = '*';
        textBoxJellyfinPassword.Size = new Size(425, 23);
        textBoxJellyfinPassword.TabIndex = 5;
        // 
        // textBoxJellyfinUsername
        // 
        textBoxJellyfinUsername.Location = new Point(83, 51);
        textBoxJellyfinUsername.Name = "textBoxJellyfinUsername";
        textBoxJellyfinUsername.Size = new Size(425, 23);
        textBoxJellyfinUsername.TabIndex = 4;
        // 
        // textBoxJellyfinHost
        // 
        textBoxJellyfinHost.Location = new Point(83, 22);
        textBoxJellyfinHost.Name = "textBoxJellyfinHost";
        textBoxJellyfinHost.PlaceholderText = "http://192.168.x.x:8096";
        textBoxJellyfinHost.Size = new Size(255, 23);
        textBoxJellyfinHost.TabIndex = 3;
        // 
        // labelServerExample
        // 
        labelServerExample.AutoSize = true;
        labelServerExample.ForeColor = SystemColors.GrayText;
        labelServerExample.Location = new Point(344, 25);
        labelServerExample.Name = "labelServerExample";
        labelServerExample.Size = new Size(156, 15);
        labelServerExample.TabIndex = 9;
        labelServerExample.Text = "e.g. http://192.168.x.x:8096";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Location = new Point(6, 83);
        label3.Name = "label3";
        label3.Size = new Size(57, 15);
        label3.TabIndex = 2;
        label3.Text = "Password";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(6, 54);
        label2.Name = "label2";
        label2.Size = new Size(60, 15);
        label2.TabIndex = 1;
        label2.Text = "Username";
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(6, 25);
        label1.Name = "label1";
        label1.Size = new Size(39, 15);
        label1.TabIndex = 0;
        label1.Text = "Server";
        // 
        // groupBoxMpc
        // 
        groupBoxMpc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxMpc.Controls.Add(textBoxMpcArgs);
        groupBoxMpc.Controls.Add(labelMpcArgs);
        groupBoxMpc.Controls.Add(comboBoxMpcFork);
        groupBoxMpc.Controls.Add(labelMpcFork);
        groupBoxMpc.Controls.Add(checkBoxMpcFullscreen);
        groupBoxMpc.Controls.Add(buttonSelectMpc);
        groupBoxMpc.Controls.Add(numericUpDownMpcPort);
        groupBoxMpc.Controls.Add(textBoxMpcPath);
        groupBoxMpc.Controls.Add(label5);
        groupBoxMpc.Controls.Add(label4);
        groupBoxMpc.Location = new Point(12, 209);
        groupBoxMpc.Name = "groupBoxMpc";
        groupBoxMpc.Size = new Size(514, 158);
        groupBoxMpc.TabIndex = 3;
        groupBoxMpc.TabStop = false;
        groupBoxMpc.Text = "MPC";
        // 
        // checkBoxMpcFullscreen
        // 
        checkBoxMpcFullscreen.AutoSize = true;
        checkBoxMpcFullscreen.Location = new Point(214, 53);
        checkBoxMpcFullscreen.Name = "checkBoxMpcFullscreen";
        checkBoxMpcFullscreen.Size = new Size(106, 19);
        checkBoxMpcFullscreen.TabIndex = 4;
        checkBoxMpcFullscreen.Text = "Start Fullscreen";
        checkBoxMpcFullscreen.UseVisualStyleBackColor = true;
        // 
        // labelMpcFork
        // 
        labelMpcFork.AutoSize = true;
        labelMpcFork.Location = new Point(6, 82);
        labelMpcFork.Name = "labelMpcFork";
        labelMpcFork.Size = new Size(30, 15);
        labelMpcFork.TabIndex = 5;
        labelMpcFork.Text = "Fork";
        // 
        // comboBoxMpcFork
        // 
        comboBoxMpcFork.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBoxMpcFork.FormattingEnabled = true;
        comboBoxMpcFork.Location = new Point(83, 79);
        comboBoxMpcFork.Name = "comboBoxMpcFork";
        comboBoxMpcFork.Size = new Size(240, 23);
        comboBoxMpcFork.TabIndex = 6;
        // 
        // labelMpcArgs
        // 
        labelMpcArgs.AutoSize = true;
        labelMpcArgs.Location = new Point(6, 112);
        labelMpcArgs.Name = "labelMpcArgs";
        labelMpcArgs.Size = new Size(63, 15);
        labelMpcArgs.TabIndex = 7;
        labelMpcArgs.Text = "Extra Args";
        // 
        // textBoxMpcArgs
        // 
        textBoxMpcArgs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        textBoxMpcArgs.Location = new Point(83, 109);
        textBoxMpcArgs.Name = "textBoxMpcArgs";
        textBoxMpcArgs.Size = new Size(425, 23);
        textBoxMpcArgs.TabIndex = 8;
        // 
        // buttonSelectMpc
        // 
        buttonSelectMpc.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        buttonSelectMpc.IconChar = IconChar.FolderOpen;
        buttonSelectMpc.IconColor = Color.Black;
        buttonSelectMpc.IconFont = IconFont.Auto;
        buttonSelectMpc.IconSize = 16;
        buttonSelectMpc.Location = new Point(480, 22);
        buttonSelectMpc.Name = "buttonSelectMpc";
        buttonSelectMpc.Size = new Size(28, 23);
        buttonSelectMpc.TabIndex = 3;
        buttonSelectMpc.UseVisualStyleBackColor = true;
        buttonSelectMpc.Click += buttonSelectMpc_Click;
        // 
        // numericUpDownMpcPort
        // 
        numericUpDownMpcPort.Location = new Point(83, 51);
        numericUpDownMpcPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numericUpDownMpcPort.Name = "numericUpDownMpcPort";
        numericUpDownMpcPort.Size = new Size(82, 23);
        numericUpDownMpcPort.TabIndex = 4;
        // 
        // textBoxMpcPath
        // 
        textBoxMpcPath.Location = new Point(83, 22);
        textBoxMpcPath.Name = "textBoxMpcPath";
        textBoxMpcPath.Size = new Size(391, 23);
        textBoxMpcPath.TabIndex = 2;
        // 
        // label5
        // 
        label5.AutoSize = true;
        label5.Location = new Point(7, 53);
        label5.Name = "label5";
        label5.Size = new Size(29, 15);
        label5.TabIndex = 1;
        label5.Text = "Port";
        // 
        // label4
        // 
        label4.AutoSize = true;
        label4.Location = new Point(6, 25);
        label4.Name = "label4";
        label4.Size = new Size(31, 15);
        label4.TabIndex = 0;
        label4.Text = "Path";
        // 
        // buttonOk
        // 
        buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        buttonOk.Location = new Point(360, 373);
        buttonOk.Name = "buttonOk";
        buttonOk.Size = new Size(80, 28);
        buttonOk.TabIndex = 6;
        buttonOk.Text = "OK";
        buttonOk.UseVisualStyleBackColor = true;
        buttonOk.Click += buttonSave_Click;
        // 
        // buttonCancel
        // 
        buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        buttonCancel.DialogResult = DialogResult.Cancel;
        buttonCancel.Location = new Point(446, 373);
        buttonCancel.Name = "buttonCancel";
        buttonCancel.Size = new Size(80, 28);
        buttonCancel.TabIndex = 7;
        buttonCancel.Text = "Cancel";
        buttonCancel.UseVisualStyleBackColor = true;
        buttonCancel.Click += buttonCancel_Click;
        // 
        // MainForm
        // 
        AcceptButton = buttonOk;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = buttonCancel;
        ClientSize = new Size(538, 413);
        Controls.Add(buttonCancel);
        Controls.Add(buttonOk);
        Controls.Add(groupBoxMpc);
        Controls.Add(groupBoxJellyfin);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "MainForm";
        Text = "Jellyfin MPC Shim";
        FormClosing += MainForm_FormClosing;
        Load += MainForm_Load;
        trayMenu.ResumeLayout(false);
        groupBoxJellyfin.ResumeLayout(false);
        groupBoxJellyfin.PerformLayout();
        groupBoxMpc.ResumeLayout(false);
        groupBoxMpc.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numericUpDownMpcPort).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private ToolStripMenuItem settingsToolStripMenuItem;
    private ToolStripMenuItem exitToolStripMenuItem;
    private GroupBox groupBoxJellyfin;
    private TextBox textBoxJellyfinPassword;
    private TextBox textBoxJellyfinUsername;
    private TextBox textBoxJellyfinHost;
    private Label label3;
    private Label label2;
    private Label label1;
    private GroupBox groupBoxMpc;
    private TextBox textBoxMpcPath;
    private Label label5;
    private Label label4;
    private NumericUpDown numericUpDownMpcPort;
    private ToolStripMenuItem connectionToolStripMenuItem;
    private ToolStripMenuItem logsToolStripMenuItem;
    private ToolStripMenuItem groupsToolStripMenuItem;
    private FontAwesome.Sharp.IconButton buttonSelectMpc;
    private Button buttonOk;
    private Button buttonCancel;
    private Label labelServerExample;
    private Button buttonQuickConnect;
    private Button buttonSignOut;
    private Label labelAuthStatus;
    private CheckBox checkBoxMpcFullscreen;
    private Label labelMpcFork;
    private ComboBox comboBoxMpcFork;
    private Label labelMpcArgs;
    private TextBox textBoxMpcArgs;
    private Label labelCastName;
    private TextBox textBoxCastName;
}
