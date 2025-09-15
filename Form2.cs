using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xbox_360_BadUpdate_USB_Tool
{
    public partial class Form2 : Form
    {

        private readonly HttpClient _httpClient = new HttpClient();
        public bool DriveSet = true;
        public string DevicePath = "";
        private int _totalSteps;
        private int _currentStep;
        private Dictionary<string, CheckBox> _checkBoxDict;
        private string _selectedStealthServerPath = ""; 

        public Form2()
        {
            InitializeComponent();
            
            // Enable click-through for transparent areas
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            
            InitializeCheckBoxDict();
            LoadUsbDrives();

            // Initialize modern UI
            InitializeModernUI();
            SetupAnimations();
            LoadBadStickIcon();
            UpdateLogsTextBox("Welcome to Xbox 360 BadStick Setup Tool");
            UpdateLogsTextBox("Click Install to begin...");
        }
        
        
        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        
        private void ConfigureWindow()
        {
            // Always on top
            this.TopMost = true;
            
            // Remove maximize button, keep minimize and close
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            
            // Make entire form draggable
            this.MouseDown += Form2_MouseDown;
            this.MouseMove += Form2_MouseMove;
            this.MouseUp += Form2_MouseUp;
            
            // Make all child controls draggable too
            foreach (Control control in this.Controls)
            {
                MakeControlDraggable(control);
            }
        }
        
        private void MakeControlDraggable(Control control)
        {
            control.MouseDown += Form2_MouseDown;
            control.MouseMove += Form2_MouseMove;
            control.MouseUp += Form2_MouseUp;
            
            // Recursively make child controls draggable
            foreach (Control child in control.Controls)
            {
                MakeControlDraggable(child);
            }
        }
        
        private bool isDragging = false;
        private Point dragStartPoint;
        
        private void Form2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragStartPoint = e.Location;
            }
        }
        
        private void Form2_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.Location;
                newLocation.X += e.X - dragStartPoint.X;
                newLocation.Y += e.Y - dragStartPoint.Y;
                this.Location = newLocation;
            }
        }
        
        private void Form2_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
        
        private void ShowWelcomeMessageIfNeeded()
        {
            // Check if welcome message has been shown before
            string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BadStick");
            string flagFile = Path.Combine(settingsPath, "welcome_shown.flag");
            
            if (!File.Exists(flagFile))
            {
                var result = MessageBox.Show(
                    "Welcome to BadStick Setup!\n\nThis tool will help you install Xbox 360 homebrew packages to your USB drive.\n\nWould you like to see this message again in the future?",
                    "Welcome - BadStick Setup",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );
                
                if (result == DialogResult.No)
                {
                    // Create flag file to not show again
                    try
                    {
                        Directory.CreateDirectory(settingsPath);
                        File.WriteAllText(flagFile, DateTime.Now.ToString());
                    }
                    catch
                    {
                        // Ignore errors creating flag file
                    }
                }
            }
        }
        
        private void InitializeModernUI()
        {
            // Modern dark theme setup
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Text = "Xbox 360 BadStick Setup Tool";
            
            // Hide legacy controls
            HideLegacyControls();
            
            // Setup modern panels with rounded corners effect
            SetupModernPanels();
        }
        
        private void HideLegacyControls()
        {
            // Hide all old tab controls and legacy UI elements
            if (tabControl1 != null) tabControl1.Visible = false;
            if (StartBtn != null) StartBtn.Visible = false;
            if (ExitBtn != null) ExitBtn.Visible = false;
            if (RefDrivesBtn != null) RefDrivesBtn.Visible = false;
            if (label1 != null) label1.Visible = false;
            if (InfoLabel != null) InfoLabel.Visible = false;
            if (label8 != null) label8.Visible = false;
            if (groupBox4 != null) groupBox4.Visible = false;
            if (warningLabel != null) warningLabel.Visible = false;
        }
        
        private void SetupModernPanels()
        {
            // Add subtle border radius effect to panels
            driveSelectionPanel.Paint += Panel_Paint;
            optionsPanel.Paint += Panel_Paint;
            statusPanel.Paint += Panel_Paint;
        }
        
        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel != null)
            {
                // Draw subtle border
                using (Pen pen = new Pen(Color.FromArgb(60, 60, 60), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            }
        }
        
        private void SetupAnimations()
        {
            // Setup hover animations for all buttons
            SetupButtonHoverEffect(installButton);
            SetupButtonHoverEffect(discordButton);
            SetupButtonHoverEffect(githubButton);
            SetupButtonHoverEffect(supportButton);
            SetupButtonHoverEffect(browseButton);
        }
        
        private void SetupButtonHoverEffect(Button button)
        {
            Color originalColor = button.BackColor;
            Font originalFont = button.Font;
            
            button.MouseEnter += (s, e) => {
                // Enhanced hover animation with color, scale, and glow effect
                button.BackColor = ControlPaint.Light(originalColor, 0.3f);
                button.Font = new Font(originalFont.FontFamily, originalFont.Size + 1, originalFont.Style);
                button.FlatAppearance.BorderSize = 2;
                button.FlatAppearance.BorderColor = Color.FromArgb(150, 220, 50);
            };
            
            button.MouseLeave += (s, e) => {
                button.BackColor = originalColor;
                button.Font = originalFont;
                button.FlatAppearance.BorderSize = 0;
            };
        }
        
        private void LoadBadStickIcon()
        {
            try
            {
                // Create high-quality bitmap from icon
                Icon originalIcon = null;
                string iconPath = Path.Combine(Application.StartupPath, "BadStick Icon.ico");
                
                if (File.Exists(iconPath))
                {
                    originalIcon = new Icon(iconPath);
                }
                else
                {
                    // Use embedded resource if available
                    originalIcon = Xbox_360_BadStick.Properties.Resources.BadStick_Icon;
                }

                if (originalIcon != null)
                {
                    // Create high-resolution bitmap with anti-aliasing
                    int size = 256; // Use high resolution source
                    Bitmap highResBitmap = new Bitmap(size, size);
                    using (Graphics g = Graphics.FromImage(highResBitmap))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        
                        // Draw icon at high resolution
                        g.DrawIcon(originalIcon, new Rectangle(0, 0, size, size));
                    }
                    
                    BadStickIcon.Image = highResBitmap;
                }
            }
            catch
            {
                // If icon loading fails, just continue without it
                UpdateLogsTextBox("Note: Could not load BadStick icon");
            }
        }
        
        private void UpdateLogsTextBox(string message)
        {
            // Logs removed - now just update status label
            UpdateStatusLabel(message);
        }
        
        private void UpdateStatusLabel(string status)
        {
            if (statusLabel.Owner.InvokeRequired)
            {
                statusLabel.Owner.Invoke(new Action<string>(UpdateStatusLabel), status);
                return;
            }
            
            statusLabel.Text = status;
        }
        
        private void UpdateProgressBar(int percentage)
        {
            if (progressBar.Owner.InvokeRequired)
            {
                progressBar.Owner.Invoke(new Action<int>(UpdateProgressBar), percentage);
                return;
            }
            
            progressBar.Value = Math.Max(0, Math.Min(100, percentage));
        }
        
        private void CreateHeaderTitle()
        {
            // Create a modern header label
            Label headerLabel = new Label();
            headerLabel.Text = "BadStick Setup";
            headerLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            headerLabel.ForeColor = Color.FromArgb(107, 186, 24);
            headerLabel.BackColor = Color.Transparent;
            headerLabel.AutoSize = true;
            headerLabel.Location = new Point(20, 15);
            
            // Add subtle glow effect with a shadow label
            Label shadowLabel = new Label();
            shadowLabel.Text = "BadStick Setup";
            shadowLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            shadowLabel.ForeColor = Color.FromArgb(50, 8, 62, 8);
            shadowLabel.BackColor = Color.Transparent;
            shadowLabel.AutoSize = true;
            shadowLabel.Location = new Point(22, 17);
            
            this.Controls.Add(shadowLabel);
            this.Controls.Add(headerLabel);
            headerLabel.BringToFront();
        }
        
        private void ApplyModernStyling()
        {
            this.Padding = new Padding(0);
            this.MinimumSize = new Size(800, 600);
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(220, 220, 220);
            
            // Close button removed as requested
            
            if (BadStickIcon != null)
            {
                BadStickIcon.BackColor = Color.Transparent;
                BadStickIcon.SizeMode = PictureBoxSizeMode.Zoom;
                BadStickIcon.Visible = true;
                BadStickIcon.Size = new Size(200, 200);
                BadStickIcon.Location = new Point(
                    (this.ClientSize.Width - BadStickIcon.Width) / 2,
                    50
                );
                BadStickIcon.BringToFront();
            }
            
            if (DeviceList != null)
            {
                DeviceList.Visible = true;
                DeviceList.BringToFront();
                DeviceList.BackColor = Color.FromArgb(40, 40, 40);
                DeviceList.ForeColor = Color.FromArgb(107, 186, 24);
                DeviceList.Font = new Font("Segoe UI", 14, FontStyle.Regular);
                DeviceList.Size = new Size(400, 30);
                DeviceList.Location = new Point(
                    (this.ClientSize.Width - DeviceList.Width) / 2,
                    350
                );
            }
            
            if (label1 != null)
            {
                label1.Visible = true;
                label1.Text = "Select USB Drive:";
                label1.BringToFront();
                label1.ForeColor = Color.FromArgb(107, 186, 24);
                label1.BackColor = Color.Transparent;
                label1.Font = new Font("Segoe UI", 14, FontStyle.Bold);
                label1.Size = new Size(200, 25);
                label1.Location = new Point(
                    (this.ClientSize.Width - 200) / 2,
                    320
                );
            }
            
            // Progress bar removed with status strip
            
            EnhanceButtonLayout();
        }
        
        private void EnhanceButtonLayout()
        {
            StartBtn.Visible = true;
            StartBtn.BringToFront();
            StartBtn.Size = new Size(300, 60);
            StartBtn.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            StartBtn.Location = new Point(
                (this.ClientSize.Width - StartBtn.Width) / 2,
                270
            );
            
            ExitBtn.Visible = false;
            RefDrivesBtn.Visible = false;
        }
        
        private void CreateModernLayout()
        {
            if (tabControl1 != null)
            {
                tabControl1.Visible = true;
                tabControl1.BringToFront();
            }
            
            if (BadStickIcon != null)
            {
                BadStickIcon.Visible = true;
                BadStickIcon.BringToFront();
            }
            
            if (DeviceList != null)
            {
                DeviceList.Visible = true;
                DeviceList.BringToFront();
            }
            
            if (label1 != null)
            {
                label1.Visible = true;
                label1.BringToFront();
            }
            
            StartBtn.Visible = true;
            StartBtn.BringToFront();
        }
        
        private void ApplyDarkThemeToControls(Control.ControlCollection controls)
        {
            Color xboxGreen = Color.FromArgb(16, 124, 16);
            Color xboxGreenLight = Color.FromArgb(107, 186, 24);
            Color cardBackground = Color.FromArgb(40, 40, 40);
            
            foreach (Control control in controls)
            {
                if (control is CheckBox checkbox)
                {
                    checkbox.ForeColor = xboxGreenLight;
                    checkbox.BackColor = Color.Transparent;
                }
                else if (control is Label label)
                {
                    label.ForeColor = xboxGreenLight;
                    label.BackColor = Color.Transparent;
                }
                else if (control is LinkLabel linkLabel)
                {
                    linkLabel.LinkColor = xboxGreenLight;
                    linkLabel.VisitedLinkColor = xboxGreen;
                    linkLabel.ActiveLinkColor = Color.White;
                    linkLabel.BackColor = Color.Transparent;
                }
                else if (control is Button button && button != StartBtn && button != ExitBtn && button != RefDrivesBtn)
                {
                    button.BackColor = cardBackground;
                    button.ForeColor = xboxGreenLight;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = xboxGreen;
                    button.Cursor = Cursors.Hand;
                }
                else if (control is PictureBox pictureBox)
                {
                    pictureBox.BackColor = Color.Transparent;
                }
                
                if (control.HasChildren)
                {
                    ApplyDarkThemeToControls(control.Controls);
                }
            }
        }
        
        private void SetDefaultStatesAndHideElements()
        {
            // Make skip rockband always enabled/selected but hidden
            skiprbbToggle.Checked = true;
            skiprbbToggle.Visible = false;
            
            // Make skip format always enabled/selected but hidden
            skipformatToggle.Checked = true;
            skipformatToggle.Visible = false;
            skipformatQ.Visible = false;
            
            // Make xeunshackle always enabled/selected but hidden
            xeunshackleToggle.Checked = true;
            xeunshackleToggle.Visible = false;
            
            // Make install all always enabled/selected but hidden
            SelectAllToggle.Checked = true;
            SelectAllToggle.Visible = false;
            installallQ.Visible = false;
            
            // Hide exit when done checkbox
            ExitToggle.Visible = false;
            
            // Hide free my xe and skip xex menu options
            freemyxeToggle.Visible = false;
            skipxexmenuToggle.Visible = false;
            
            // Hide skip main files option
            skipmainfilesToggle.Visible = false;
            skipmainQ.Visible = false;
            
            // Hide all remaining checkboxes from all tabs
            HideAllCheckboxes();
            
            // Hide settings panel but keep functionality
            groupBox4.Visible = false;
            
            // Hide top right text elements
            InfoLabel.Visible = false;
            label8.Visible = false;
            
            // Hide status label - only show progress bar
            // StatusLabel.Visible = false;
            
            // Hide other tabs except install tab
            tabControl1.TabPages.Remove(tabPage2); // Dashboards / Launchers
            tabControl1.TabPages.Remove(tabPage3); // Homebrew
            tabControl1.TabPages.Remove(tabPage4); // Stealth Servers
            tabControl1.TabPages.Remove(tabPage5); // Plugins / Other
        }
        
        private void HideAllCheckboxes()
        {
            // Hide all dashboard/launcher checkboxes
            AuroraToggle.Visible = false;
            FSDToggle.Visible = false;
            EmeraldToggle.Visible = false;
            IngeniouXToggle.Visible = false;
            Viper360Toggle.Visible = false;
            XeXLoaderToggle.Visible = false;
            XenuToggle.Visible = false;
            
            // Hide all homebrew checkboxes
            FFPlayToggle.Visible = false;
            GODUnlockerToggle.Visible = false;
            XM360Toggle.Visible = false;
            HDDxToggle.Visible = false;
            XNAToggle.Visible = false;
            NXE2GODToggle.Visible = false;
            XPGToggle.Visible = false;
            flasherToggle.Visible = false;
            
            // Hide all stealth server checkboxes
            XBLSToggle.Visible = false;
            CipherToggle.Visible = false;
            XbGuardToggle.Visible = false;
            NfiniteToggle.Visible = false;
            tetheredToggle.Visible = false;
            ProtoToggle.Visible = false;
            KyuubiiToggle.Visible = false;
            
            // Hide all plugins/other checkboxes
            PluginsToggle.Visible = false;
            xnotifyToggle.Visible = false;
            XB1Toggle.Visible = false;
            haxfilesToggle.Visible = false;
            xefuToggle.Visible = false;
            origfilesToggle.Visible = false;
        }

        private class UsbDriveItem
        {
            public string RootPath { get; }
            public string DisplayName { get; }

            public UsbDriveItem(string rootPath, string volumeLabel)
            {
                RootPath = rootPath;
                DisplayName = string.IsNullOrEmpty(volumeLabel)
                    ? rootPath
                    : $"{rootPath} ({volumeLabel})";
            }

            public override string ToString() => DisplayName;
        }

        private void UpdateStatus(string text)
        {
            UpdateStatusLabel(text);
            UpdateLogsTextBox(text);
        }

        private void SetProgressBar(int percent)
        {
            UpdateProgressBar(percent);
        }
        
        // Modern UI Event Handlers
        private void customPathCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            bool useCustomPath = customPathCheckbox.Checked;
            customPathTextbox.Enabled = useCustomPath;
            browseButton.Enabled = useCustomPath;
            DeviceList.Enabled = !useCustomPath;
            
            if (useCustomPath)
            {
                UpdateLogsTextBox("Custom path mode enabled - select a folder");
                DevicePath = "";
                DriveSet = false;
            }
            else
            {
                UpdateLogsTextBox("Drive selection mode enabled");
                if (DeviceList.SelectedItem is UsbDriveItem selectedDrive)
                {
                    DevicePath = selectedDrive.RootPath;
                    DriveSet = true;
                }
            }
        }
        
        private void browseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select destination folder for BadStick installation";
                dialog.ShowNewFolderButton = true;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    customPathTextbox.Text = dialog.SelectedPath;
                    DevicePath = dialog.SelectedPath;
                    DriveSet = true;
                    UpdateLogsTextBox($"Custom path selected: {dialog.SelectedPath}");
                }
            }
        }
        
        private async void installButton_Click(object sender, EventArgs e)
        {
            // Use the original installation process with modern UI feedback
            UpdateLogsTextBox("Starting installation process...");
            UpdateStatusLabel("Preparing installation...");
            UpdateProgressBar(0);
            
            // Call the original installation logic but with modern UI updates
            StartBtn_Click(sender, e);
        }
        
        private void discordButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://discord.gg/xMbKazpkvf");
                UpdateLogsTextBox("Opening Discord server link...");
            }
            catch (Exception ex)
            {
                UpdateLogsTextBox($"Error opening Discord: {ex.Message}");
            }
        }
        
        private void githubButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://github.com/32BitKlepto/BadStick");
                UpdateLogsTextBox("Opening GitHub repository...");
            }
            catch (Exception ex)
            {
                UpdateLogsTextBox($"Error opening GitHub: {ex.Message}");
            }
        }
        
        private void supportButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://www.reddit.com/r/360hacks/");
                UpdateLogsTextBox("Opening r/360hacks subreddit...");
            }
            catch (Exception ex)
            {
                UpdateLogsTextBox($"Error opening Reddit: {ex.Message}");
            }
        }
        
        private void Button_MouseEnter(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                // Smooth hover animation
                button.BackColor = ControlPaint.Light(button.BackColor, 0.3f);
                button.Cursor = Cursors.Hand;
            }
        }
        
        private void Button_MouseLeave(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                // Reset to original color based on button type
                if (button == installButton)
                    button.BackColor = Color.FromArgb(107, 186, 24);
                else if (button == discordButton)
                    button.BackColor = Color.FromArgb(88, 101, 242);
                else if (button == githubButton)
                    button.BackColor = Color.FromArgb(33, 41, 60);
                else if (button == supportButton)
                    button.BackColor = Color.FromArgb(255, 165, 0);
                else if (button == browseButton)
                    button.BackColor = Color.FromArgb(107, 186, 24);
            }
        }

        private void LoadUsbDrives()
        {
            DeviceList.DroppedDown = false;
            DeviceList.BeginUpdate();
            DeviceList.Items.Clear();

            var drives = DriveInfo.GetDrives()
                .Where(d => (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed) && d.IsReady &&
                            string.Equals(d.DriveFormat, "FAT32", StringComparison.OrdinalIgnoreCase))
                .Select(d => new UsbDriveItem(
                    d.RootDirectory.FullName,
                    string.IsNullOrEmpty(d.VolumeLabel) ? "No Label" : d.VolumeLabel))
                .ToList();

            foreach (var drive in drives)
                DeviceList.Items.Add(drive);

            if (DeviceList.Items.Count > 0)
            {
                DeviceList.SelectedIndex = 0;
                var firstDrive = DeviceList.Items[0] as UsbDriveItem;
                if (firstDrive != null)
                {
                    DevicePath = firstDrive.RootPath;
                    DriveSet = true;
                }
                UpdateLogsTextBox($"Found {DeviceList.Items.Count} compatible drive(s)");
            }
            else
            {
                DevicePath = null;
                DriveSet = false;
                UpdateLogsTextBox("Warning: No FAT32 USB drives detected");
            }

            DeviceList.EndUpdate();
            DeviceList.Enabled = !customPathCheckbox.Checked;
        }
        
        // Enhanced installation logic with modern UI feedback
        private async Task ModernInstallationProcess()
        {
            try
            {
                // Validate selection
                if (!DriveSet && string.IsNullOrEmpty(DevicePath))
                {
                    UpdateLogsTextBox("Error: No target path selected");
                    MessageBox.Show("Please select a drive or custom path before installing.", "No Target Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                UpdateStatusLabel("Initializing installation...");
                UpdateProgressBar(5);
                
                // Check selected packages
                var selectedPackages = GetSelectedPackageCategories();
                if (selectedPackages.Count == 0)
                {
                    UpdateLogsTextBox("Error: No packages selected for installation");
                    MessageBox.Show("Please select at least one package category to install.", "No Packages Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                UpdateLogsTextBox($"Installing {selectedPackages.Count} package categories to: {DevicePath}");
                UpdateStatusLabel("Downloading packages...");
                
                // Disable UI during installation
                SetUIEnabled(false);
                
                int totalSteps = selectedPackages.Count * 2; // Download + Extract
                int currentStep = 0;
                
                foreach (var category in selectedPackages)
                {
                    UpdateLogsTextBox($"Processing {category} packages...");
                    UpdateProgressBar((currentStep * 100) / totalSteps);
                    
                    // Simulate package processing (replace with actual logic)
                    await Task.Delay(1000);
                    currentStep++;
                    
                    UpdateProgressBar((currentStep * 100) / totalSteps);
                    currentStep++;
                }
                
                UpdateProgressBar(100);
                UpdateStatusLabel("Installation completed successfully!");
                UpdateLogsTextBox("Installation process completed successfully!");
                
                MessageBox.Show("BadStick installation completed successfully!", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateLogsTextBox($"Installation error: {ex.Message}");
                UpdateStatusLabel("Installation failed");
                MessageBox.Show($"Installation failed: {ex.Message}", "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIEnabled(true);
                UpdateProgressBar(0);
            }
        }
        
        private List<string> GetSelectedPackageCategories()
        {
            var selected = new List<string>();
            
            if (emulatorsCheckbox.Checked)
                selected.Add("Emulators");
            if (homebrewCheckbox.Checked)
                selected.Add("Homebrew");
            if (cheatsCheckbox.Checked)
                selected.Add("Cheats");
                
            return selected;
        }
        
        private void SetUIEnabled(bool enabled)
        {
            installButton.Enabled = enabled;
            customPathCheckbox.Enabled = enabled;
            customPathTextbox.Enabled = enabled && customPathCheckbox.Checked;
            browseButton.Enabled = enabled && customPathCheckbox.Checked;
            DeviceList.Enabled = enabled && !customPathCheckbox.Checked;
            emulatorsCheckbox.Enabled = enabled;
            homebrewCheckbox.Enabled = enabled;
            // cheatsCheckbox remains disabled as it's "coming soon"
        }


        private async Task CountdownExitStatusAsync()
        {
            if (!ExitToggle.Checked)
                return;

            for (int i = 3; i >= 1; i--)
            {
                UpdateStatus($"Status: Exiting in {i}...");
                await Task.Delay(1000);
            }
            Application.Exit();
        }


        public async Task DownloadFileAsync(string url, string destinationFilePath, IProgress<int> progress = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; BadStickTool/1.0)");

                    using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            UpdateStatus($"Warning: Failed to download {Path.GetFileName(destinationFilePath)} - {response.StatusCode}");
                            return;
                        }

                        var total = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = total != -1 && progress != null;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var totalRead = 0L;
                            var buffer = new byte[8192];
                            int read;
                            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;

                                if (canReportProgress)
                                {
                                    int percent = (int)((totalRead * 100L) / total);
                                    progress.Report(percent);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Warning: Could not download {Path.GetFileName(destinationFilePath)}: {ex.Message}");
            }
        }

        private Task ExtractPackageAsync(string pkgFilePath, string destinationPath, IProgress<int> progress = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var archive = ZipFile.OpenRead(pkgFilePath))
                    {
                        int totalEntries = archive.Entries.Count;
                        int processedEntries = 0;

                        foreach (var entry in archive.Entries)
                        {
                            string fullPath;
                            
                            // Only flatten Payload-XeUnshackle.zip to root, keep others in subfolders
                            if (Path.GetFileName(pkgFilePath).Equals("Payload-XeUnshackle.zip", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract directly to USB root, flattening folder structure
                                var fileName = Path.GetFileName(entry.FullName);
                                
                                // Skip empty entries (directories)
                                if (string.IsNullOrEmpty(fileName))
                                    continue;
                                    
                                fullPath = Path.Combine(destinationPath, fileName);
                            }
                            else
                            {
                                // Keep original folder structure for other packages
                                fullPath = Path.Combine(destinationPath, entry.FullName);

                                var directory = Path.GetDirectoryName(fullPath);

                                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }
                            }

                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                entry.ExtractToFile(fullPath, overwrite: true);
                            }

                            processedEntries++;

                            if (progress != null)
                            {
                                int percent = (int)((processedEntries * 100L) / totalEntries);
                                progress.Report(percent);
                            }
                        }
                    }
                }
                catch (InvalidDataException ex)
                {
                    UpdateStatus($"Warning: Could not extract {Path.GetFileName(pkgFilePath)} - file may be corrupted: {ex.Message}");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error extracting {Path.GetFileName(pkgFilePath)}: {ex.Message}");
                }
            });
        }


        public class PackageInfo
        {
            public string FileName { get; set; }
            public string CheckBoxName { get; set; }
            public string DownloadUrl { get; set; }
            public bool AlwaysDownload => string.IsNullOrEmpty(CheckBoxName);
            public bool SkipDownload { get; set; } = false;
            public bool ExtractToRoot { get; set; } = false;
        }

        private readonly List<PackageInfo> _allPackages = new List<PackageInfo>
        {
            new PackageInfo { FileName = "Aurora.zip", CheckBoxName = "AuroraToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Aurora.zip" },
            new PackageInfo { FileName = "Freestyle.zip", CheckBoxName = "FSDToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Freestyle.zip" },
            new PackageInfo { FileName = "Emerald.zip", CheckBoxName = "EmeraldToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Emerald.zip" },
            new PackageInfo { FileName = "FFPlay.zip", CheckBoxName = "FFPlayToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/FFPlay.zip" },
            new PackageInfo { FileName = "GOD Unlocker.zip", CheckBoxName = "GODUnlockerToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/GOD Unlocker.zip" },
            new PackageInfo { FileName = "HDDx Fixer.zip", CheckBoxName = "HDDxToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/HDDx Fixer.zip" },
            new PackageInfo { FileName = "IngeniouX.zip", CheckBoxName = "IngeniousXToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/IngeniouX.zip" },
            new PackageInfo { FileName = "NXE2GOD.zip", CheckBoxName = "NXE2GODToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/NXE2GOD.zip" },
            new PackageInfo { FileName = "Payload-XeUnshackle.zip", CheckBoxName = null, DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Payload-XeUnshackle.zip", ExtractToRoot = true },
            new PackageInfo { FileName = "Payload.zip", CheckBoxName = null, DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Payload.zip", ExtractToRoot = true },
            new PackageInfo { FileName = "BadAvatar.zip", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/BadAvatar.zip", ExtractToRoot = true },
            new PackageInfo { FileName = "RBB.zip", CheckBoxName = null, DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/RBB/RBB.zip" },
            new PackageInfo { FileName = "Viper360.zip", CheckBoxName = "Viper360Toggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Viper360.zip" },
            new PackageInfo { FileName = "Xenu.zip", CheckBoxName = "XenuToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Xenu.zip" },
            new PackageInfo { FileName = "XeXLoader.zip", CheckBoxName = "XeXLoaderToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XeXLoader.zip" },
            new PackageInfo { FileName = "XeXMenu.zip", CheckBoxName = "skipxexmenuToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XeXMenu.zip" },
            new PackageInfo { FileName = "XM360.zip", CheckBoxName = "XM360Toggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XM360.zip" },
            new PackageInfo { FileName = "XNA Offline.zip", CheckBoxName = "XNAToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XNA Offline.zip" },
            new PackageInfo { FileName = "XPG Chameleon.zip", CheckBoxName = "XPGToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XPG Chameleon.zip" },
            new PackageInfo { FileName = "Plugins.zip", CheckBoxName = "PluginsToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Plugins.zip" },
            new PackageInfo { FileName = "CipherLive.zip", CheckBoxName = "CipherToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/CipherLive.zip" },
            new PackageInfo { FileName = "Flasher.zip", CheckBoxName = "flasherToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Flasher.zip" },
            new PackageInfo { FileName = "Hacked.Compatibility.Files.zip", CheckBoxName = "haxcomToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Hacked.Compatibility.Files.zip" },
            new PackageInfo { FileName = "Nfinite.zip", CheckBoxName = "NfiniteToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Nfinite.zip" },
            new PackageInfo { FileName = "Original.Compatibility.Files.zip", CheckBoxName = "origToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Original.Compatibility.Files.zip" },
            new PackageInfo { FileName = "Proto.zip", CheckBoxName = "ProtoToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Proto.zip" },
            new PackageInfo { FileName = "TetheredLive.zip", CheckBoxName = "tetheredToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/TetheredLive.zip" },
            new PackageInfo { FileName = "X-Notify.Pack.zip", CheckBoxName = "xnotifyToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/X-Notify.Pack.zip" },
            new PackageInfo { FileName = "xbGuard.zip", CheckBoxName = "XbGuardToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XbGuard.zip" },
            new PackageInfo { FileName = "XBL.Kyuubii.zip", CheckBoxName = "KyuubiiToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XBL.Kyuubii.zip" },
            new PackageInfo { FileName = "XBLS.zip", CheckBoxName = "XBLSToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XBLS.zip" },
            new PackageInfo { FileName = "Xbox.One.Files.zip", CheckBoxName = "XB1Toggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/Xbox.One.Files.zip" },
            new PackageInfo { FileName = "XEFU.Spoofer.zip", CheckBoxName = "xefuToggle", DownloadUrl = "https://github.com/32BitKlepto/BadStick/releases/download/packages/XEFU.Spoofer.zip" },
            new PackageInfo { FileName = "Stealth.Networks.zip", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/Stealth.Networks.zip" },
            new PackageInfo { FileName = "Homebrew.zip", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/Homebrew.zip" },
            new PackageInfo { FileName = "Emulation.zip", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/Emulation.zip" },
            new PackageInfo { FileName = "SystemUpdate.zip", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/SystemUpdate.zip" },
            new PackageInfo { FileName = "launch.ini", CheckBoxName = null, DownloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/launch.ini" }
        };
        private void InitializeCheckBoxDict()
        {
            _checkBoxDict = new Dictionary<string, CheckBox>
            {
                { "AuroraToggle", AuroraToggle },
                { "FSDToggle", FSDToggle },
                { "EmeraldToggle", EmeraldToggle },
                { "FFPlayToggle", FFPlayToggle },
                { "GODUnlockerToggle", GODUnlockerToggle },
                { "HDDxToggle", HDDxToggle },
                { "IngeniousXToggle", IngeniouXToggle },
                { "NXE2GODToggle", NXE2GODToggle },
                { "Viper360Toggle", Viper360Toggle },
                { "XenuToggle", XenuToggle },
                { "XeXLoaderToggle", XeXLoaderToggle },
                { "XM360Toggle", XM360Toggle },
                { "XNAToggle", XNAToggle },
                { "XPGToggle", XPGToggle },
                { "PluginsToggle", PluginsToggle },
                { "CipherToggle", CipherToggle },
                { "flasherToggle", flasherToggle },
                { "haxfilesToggle", haxfilesToggle },
                { "NfiniteToggle", NfiniteToggle },
                { "origfilesToggle", origfilesToggle },
                { "ProtoToggle", ProtoToggle },
                { "tetheredToggle", tetheredToggle },
                { "xnotifyToggle", xnotifyToggle },
                { "XbGuardToggle", XbGuardToggle },
                { "KyuubiiToggle", KyuubiiToggle },
                { "XBLSToggle", XBLSToggle },
                { "XB1Toggle", XB1Toggle },
                { "xefuToggle", xefuToggle },
                { "skipformatToggle", skipformatToggle },
                { "skipmainfilesToggle", skipmainfilesToggle },
                { "xeunshackleToggle", xeunshackleToggle },
                { "freemyxeToggle", freemyxeToggle },
                { "skipxexmenuToggle", skipxexmenuToggle }
            };
        }
        private List<PackageInfo> GetSelectedPackages()
        {
            // Modern UI: Map the new checkboxes to the original package system
            if (emulatorsCheckbox != null && homebrewCheckbox != null)
            {
                // Enable/disable packages based on modern UI selections
                UpdateLegacyCheckboxes();
            }
            
            return _allPackages.Where(pkg =>
                pkg.AlwaysDownload ||
                (_checkBoxDict.ContainsKey(pkg.CheckBoxName) && _checkBoxDict[pkg.CheckBoxName].Checked)
            ).ToList();
        }
        
        private void UpdateLegacyCheckboxes()
        {
            // First, enable all packages by default (except rockband and freemyxe which should always be skipped)
            foreach (var kvp in _checkBoxDict)
            {
                string checkboxName = kvp.Key;
                CheckBox checkbox = kvp.Value;
                
                // Always skip rockband and freemyxe
                if (checkboxName.ToLower().Contains("rockband") || checkboxName.ToLower().Contains("freemyxe"))
                {
                    checkbox.Checked = false;
                    continue;
                }
                
                // Default all other packages to enabled, then filter by category selection
                checkbox.Checked = true;
            }
            
            // Now apply category-based filtering
            if (emulatorsCheckbox.Checked)
            {
                // Keep emulator/dashboard packages enabled (already set to true above)
                // These include: Aurora, FSD, Emerald, IngeniouX, Viper360, XeXLoader, Xenu
            }
            else
            {
                // Disable emulator/dashboard packages when category is deselected
                if (_checkBoxDict.ContainsKey("AuroraToggle")) AuroraToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("FSDToggle")) FSDToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("EmeraldToggle")) EmeraldToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("IngeniouXToggle")) IngeniouXToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("Viper360Toggle")) Viper360Toggle.Checked = false;
                if (_checkBoxDict.ContainsKey("XeXLoaderToggle")) XeXLoaderToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("XenuToggle")) XenuToggle.Checked = false;
            }
            
            if (homebrewCheckbox.Checked)
            {
                // Keep homebrew packages enabled (already set to true above)
                // These include: FFPlay, GODUnlocker, XM360, HDDx, XNA, NXE2GOD, XPG, flasher, Plugins, xnotify
            }
            else
            {
                // Disable homebrew packages when category is deselected
                if (_checkBoxDict.ContainsKey("FFPlayToggle")) FFPlayToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("GODUnlockerToggle")) GODUnlockerToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("XM360Toggle")) XM360Toggle.Checked = false;
                if (_checkBoxDict.ContainsKey("HDDxToggle")) HDDxToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("XNAToggle")) XNAToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("NXE2GODToggle")) NXE2GODToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("XPGToggle")) XPGToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("flasherToggle")) flasherToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("PluginsToggle")) PluginsToggle.Checked = false;
                if (_checkBoxDict.ContainsKey("xnotifyToggle")) xnotifyToggle.Checked = false;
            }
            
            // Cheats category - for future implementation
            if (!cheatsCheckbox.Checked)
            {
                // Disable cheat-related packages when category is deselected
                // Add cheat package toggles here when they become available
            }
        }

        public async Task DownloadAndExtractPackagesAsync(
            List<PackageInfo> packages,
            Dictionary<string, CheckBox> checkBoxes,
            string usbRootPath,
            IProgress<int> progress = null)
        {
            if (string.IsNullOrWhiteSpace(usbRootPath) || !Directory.Exists(usbRootPath))
            {
                UpdateStatus("Status: Please Select A Valid USB Device");
                return;
            }

            string appTempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            if (!Directory.Exists(appTempFolder))
            {
                Directory.CreateDirectory(appTempFolder);
            }

            bool skipMainFilesChecked = checkBoxes.TryGetValue("skipmainfilesToggle", out var skipMainFilesCb) && skipMainFilesCb.Checked;
            bool skipRbbChecked = checkBoxes.TryGetValue("skiprbbToggle", out var skipRbbCb) && skipRbbCb.Checked;
            bool skipXexChecked = checkBoxes.TryGetValue("skipxexToggle", out var skipXexCb) && skipXexCb.Checked;

            int totalPackages = packages.Count;
            int currentPackageIndex = 0;

            foreach (var pkg in packages)
            {
                if (skipMainFilesChecked)
                {
                    string[] mainFilesToSkip = {
                "Payload-XeUnshackle.zip",
                "Payload.zip",
                "BadAvatar.zip",
                "XeXMenu.zip",
                "RBB.zip"
            };
                    if (mainFilesToSkip.Contains(pkg.FileName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                else
                {
                    if (pkg.FileName.Equals("RBB.zip", StringComparison.OrdinalIgnoreCase) && skipRbbChecked)
                    {
                        continue;
                    }
                    if (pkg.FileName.Equals("XeXMenu.zip", StringComparison.OrdinalIgnoreCase) && skipXexChecked)
                    {
                        continue;
                    }
                    if (pkg.FileName.Equals("Payload-XeUnshackle.zip", StringComparison.OrdinalIgnoreCase) &&
                        checkBoxes.TryGetValue("freemyxeToggle", out var freeMyXeCb) && freeMyXeCb.Checked)
                    {
                        continue;
                    }
                    if (pkg.FileName.Equals("Payload-FreeMyXe.zip", StringComparison.OrdinalIgnoreCase) &&
                        checkBoxes.TryGetValue("xeunshackleToggle", out var xeUnshackleCb) && xeUnshackleCb.Checked)
                    {
                        continue;
                    }
                }

                if (!pkg.AlwaysDownload)
                {
                    if (!checkBoxes.TryGetValue(pkg.CheckBoxName, out var cb) || !cb.Checked)
                    {
                        continue;
                    }
                }

                currentPackageIndex++;
                var tempFilePath = Path.Combine(appTempFolder, pkg.FileName);

                bool needsDownload = true;
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        using (var archive = ZipFile.OpenRead(tempFilePath))
                        {
                            needsDownload = false;
                        }
                    }
                    catch
                    {
                        needsDownload = true;
                    }
                }

                if (needsDownload)
                {
                    UpdateStatus($"Status: Downloading {pkg.FileName} ({currentPackageIndex}/{totalPackages})");
                    var downloadProgress = new Progress<int>(percent =>
                    {
                        int overallPercent = (int)(((currentPackageIndex - 1 + (percent / 100.0)) / totalPackages) * 100 * 0.5);
                        // ProgressBar.Value = overallPercent;
                    });
                    await DownloadFileAsync(pkg.DownloadUrl, tempFilePath, downloadProgress);
                }
                else
                {
                    UpdateStatus($"Status: {pkg.FileName} already exists, skipping download");
                }

                if (File.Exists(tempFilePath))
                {
                    // Handle launch.ini separately (it's not a zip file) - place on root of drive
                    if (pkg.FileName.Equals("launch.ini", StringComparison.OrdinalIgnoreCase))
                    {
                        // Get the root drive path (e.g., "D:\" instead of "D:\some\folder")
                        string drivePath = Path.GetPathRoot(usbRootPath);
                        string destinationPath = Path.Combine(drivePath, pkg.FileName);
                        File.Copy(tempFilePath, destinationPath, true);
                    }
                    else
                    {
                        UpdateStatus($"Status: Extracting {pkg.FileName} ({currentPackageIndex}/{totalPackages})");
                        var extractProgress = new Progress<int>(percent =>
                        {
                            int overallPercent = (int)(((currentPackageIndex - 1 + (percent / 100.0)) / totalPackages) * 100 * 0.5 + 50);
                            // ProgressBar.Value = overallPercent;
                        });
                        await ExtractPackageAsync(tempFilePath, usbRootPath, extractProgress);
                    }
                }
                else
                {
                    UpdateStatus($"Status: Skipping extraction of {pkg.FileName} because file does not exist");
                }
            }

            UpdateStatus("Status: Done! USB Ready.");
            // ProgressBar.Value = 100;
        }



        private void driveComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (driveComboBox.SelectedItem != null)
            {
                string selectedDrive = driveComboBox.SelectedItem.ToString();
                DevicePath = selectedDrive;
                DriveSet = true;
                
                // Update the original DeviceList combobox to maintain compatibility
                if (DeviceList.Items.Contains(selectedDrive))
                {
                    DeviceList.SelectedItem = selectedDrive;
                }
                
                UpdateLogsTextBox($"Drive selected: {selectedDrive}");
            }
            else
            {
                DevicePath = null;
                DriveSet = false;
            }
        }


        private void DeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DeviceList.SelectedItem is UsbDriveItem selectedDrive)
            {
                DevicePath = selectedDrive.RootPath;
                DriveSet = true;
                Debug.WriteLine($"Selected drive: {DevicePath}");
            }
            else
            {
                DevicePath = null;
                DriveSet = false;
            }
        }


        private void ExitBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private bool FormatDriveToFat32(string drivePath)
        {
            try
            {
                string driveLetter = Path.GetPathRoot(drivePath).TrimEnd('\\');

                string query = $"SELECT * FROM Win32_Volume WHERE DriveLetter = '{driveLetter}'";

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    var volumes = searcher.Get();

                    foreach (ManagementObject volume in volumes)
                    {
                        var inParams = volume.GetMethodParameters("Format");
                        inParams["FileSystem"] = "FAT32";
                        inParams["QuickFormat"] = true;

                        ManagementBaseObject outParams = volume.InvokeMethod("Format", inParams, null);

                        uint returnValue = (uint)(outParams.Properties["ReturnValue"].Value);

                        if (returnValue == 0)
                        {
                            return true;
                        }
                        else
                        {
                            MessageBox.Show($"Failed to format drive. WMI Format returned error code: {returnValue}", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Debug.WriteLine($"Format failed with error code: {returnValue}");
                            return false;
                        }
                    }
                }

                MessageBox.Show("Drive not found or inaccessible for formatting.", "BadStick Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error formatting drive: {ex.Message}", "BadStick Format Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async void StartBtn_Click(object sender, EventArgs e)
        {
            // Lock the button and change text
            StartBtn.Enabled = false;
            StartBtn.Text = "Installing...";
            
            if (DeviceList.SelectedItem == null)
            {
                MessageBox.Show("Please select a USB device.", "BadStick Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StartBtn.Enabled = true;
                StartBtn.Text = "Start";
                return;
            }
            

            string usbPath;

            if (DeviceList.SelectedItem is UsbDriveItem selectedDrive)
            {
                usbPath = selectedDrive.RootPath;
            }
            else
            {
                MessageBox.Show("Please select a valid USB device.", "BadStick Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StartBtn.Enabled = true;
                StartBtn.Text = "Start";
                return;
            }

            if (string.IsNullOrEmpty(usbPath) || !Directory.Exists(usbPath))
            {
                MessageBox.Show("Please select a valid USB device.", "BadStick Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StartBtn.Enabled = true;
                StartBtn.Text = "Start";
                return;
            }

            if (!skipformatToggle.Checked)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to select {usbPath} as your USB drive to format and configure? This will erase all data on the device. Please" +
                    $" ensure that this is the device that you want to use before you go ahead. I am not responsible for any accidental " +
                    $"data loss on your behalf.",
                    "Confirm Format",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes)
                {
                    UpdateStatus("Status: Format cancelled");
                    StartBtn.Enabled = true;
                    StartBtn.Text = "Start";
                    return;
                }

                // ProgressBar.Value = 0;
                bool formatSuccess = await Task.Run(() => FormatDriveToFat32(usbPath));
                if (!formatSuccess)
                {
                    MessageBox.Show("Failed to format the device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Status: Format failed");
                    StartBtn.Enabled = true;
                    StartBtn.Text = "Start";
                    return;
                }
                UpdateStatus("Status: Format completed. Starting downloads...");
            }
            else
            {
                UpdateStatus("Status: Skipping format (per user request)...");
            }

            var packagesToDownload = GetSelectedPackages();

            if (skipmainfilesToggle.Checked)
            {
                string[] mainFiles = { "RBB.zip", "Payload-FreeMyXe.zip", "XeXMenu.zip" };
                packagesToDownload = packagesToDownload
                    .Where(pkg => !mainFiles.Contains(pkg.FileName, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                if (skiprbbToggle.Checked)
                {
                    packagesToDownload = packagesToDownload
                        .Where(pkg => !string.Equals(pkg.FileName, "RBB.zip", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (_checkBoxDict.TryGetValue("freemyxeToggle", out var freeMyXeCb) && freeMyXeCb.Checked)
                {
                    packagesToDownload = packagesToDownload
                        .Where(pkg => !string.Equals(pkg.FileName, "Payload-XeUnshackle.zip", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (_checkBoxDict.TryGetValue("xeunshackleToggle", out var xeUnshackleCb) && xeUnshackleCb.Checked)
                {
                    packagesToDownload = packagesToDownload
                        .Where(pkg => !string.Equals(pkg.FileName, "Payload-FreeMyXe.zip", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (skipxexmenuToggle.Checked)
                {
                    packagesToDownload = packagesToDownload
                        .Where(pkg => !string.Equals(pkg.FileName, "XeXMenu.zip", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            _totalSteps = packagesToDownload.Count;
            foreach (var pkg in packagesToDownload)
            {
                string tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", pkg.FileName);
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        // Skip counting entries for launch.ini as it's not a zip file
                        if (!pkg.FileName.Equals("launch.ini", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var archive = ZipFile.OpenRead(tempFilePath))
                            {
                                _totalSteps += archive.Entries.Count;
                            }
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // File is corrupted or not a valid zip, skip counting entries
                        UpdateStatus($"Warning: {pkg.FileName} appears to be corrupted, skipping...");
                    }
                }
            }
            _currentStep = 0;

            var progress = new Progress<int>(percent =>
            {
                // ProgressBar.Value = percent;
            });

            // Separate payload packages from other packages (BadAvatar now downloads with others)
            var payloadPackages = packagesToDownload.Where(p => 
                p.FileName.Equals("Payload-XeUnshackle.zip", StringComparison.OrdinalIgnoreCase) ||
                p.FileName.Equals("Payload.zip", StringComparison.OrdinalIgnoreCase)
            ).ToList();
            var otherPackages = packagesToDownload.Where(p => 
                !p.FileName.Equals("Payload-XeUnshackle.zip", StringComparison.OrdinalIgnoreCase) &&
                !p.FileName.Equals("Payload.zip", StringComparison.OrdinalIgnoreCase)
            ).ToList();
            
            // Download and extract other packages first
            await DownloadAndExtractPackagesAsync(otherPackages, _checkBoxDict, usbPath, progress);

            // Show dashboard selection popup and configure launch.ini
            await ConfigureDashboardAndStealthServer(usbPath);
            
            // Extract both payload packages last to ensure their contents are at root
            if (payloadPackages.Any())
            {
                await DownloadAndExtractPackagesAsync(payloadPackages, _checkBoxDict, usbPath, progress);
            }

            MessageBox.Show(this, "Done. Your USB is ready to go, thank you for using BadStick. Now go plug it into the xbox 360 to start the Bad Update!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Install BadAvatar.zip as final step
            await InstallBadAvatarAsync(usbPath);
            
            // Re-enable button after completion
            StartBtn.Enabled = true;
            StartBtn.Text = "Start";
            
            Thread.Sleep(500);
            await CountdownExitStatusAsync();
        }

        private async Task InstallBadAvatarAsync(string usbPath)
        {
            try
            {
                UpdateStatus("Status: Installing BadAvatar.zip...");
                
                string appTempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                if (!Directory.Exists(appTempFolder))
                {
                    Directory.CreateDirectory(appTempFolder);
                }
                
                string tempFilePath = Path.Combine(appTempFolder, "BadAvatar.zip");
                string downloadUrl = "https://github.com/I-am-Xoid/badstick-test/releases/download/packages/BadAvatar.zip";
                
                // Download BadAvatar.zip if not already present
                bool needsDownload = true;
                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        using (var archive = ZipFile.OpenRead(tempFilePath))
                        {
                            needsDownload = false;
                        }
                    }
                    catch
                    {
                        needsDownload = true;
                    }
                }
                
                if (needsDownload)
                {
                    await DownloadFileAsync(downloadUrl, tempFilePath);
                }
                
                if (File.Exists(tempFilePath))
                {
                    await ExtractPackageAsync(tempFilePath, usbPath);
                    UpdateStatus("Status: BadAvatar.zip installed successfully!");
                }
                else
                {
                    UpdateStatus("Status: Failed to download BadAvatar.zip");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Status: Error installing BadAvatar.zip: {ex.Message}");
            }
        }

        private async Task ConfigureDashboardAndStealthServer(string usbPath)
        {
            try
            {
                // Show dashboard selection dialog
                var dashboardResult = ShowDashboardSelectionDialog(usbPath);
                if (dashboardResult.HasValue)
                {
                    string selectedDashboard = dashboardResult.Value.Key;
                    string dashboardPath = dashboardResult.Value.Value;
                    
                    // Show stealth server selection dialog
                    ShowStealthServerSelectionDialog();
                    
                    // Download and modify launch.ini
                    await DownloadAndModifyLaunchIni(usbPath, selectedDashboard, dashboardPath, _selectedStealthServerPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error configuring dashboard and stealth server: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private KeyValuePair<string, string>? ShowDashboardSelectionDialog(string usbPath)
        {
            var dashboards = new Dictionary<string, string>();
            
            dashboards["Normal Xbox 360 Dashboard"] = "";
            
            string dashboardsPath = Path.Combine("D:\\", "Dashboards");
            
            if (Directory.Exists(dashboardsPath))
            {
                string[] dashboardFolders = Directory.GetDirectories(dashboardsPath);
                
                foreach (string dashboardFolder in dashboardFolders)
                {
                    string dashboardName = Path.GetFileName(dashboardFolder);
                    string[] xexFiles = Directory.GetFiles(dashboardFolder, "*.xex");
                    
                    if (xexFiles.Length > 0)
                    {
                        string xexFile = xexFiles[0];
                        string relativePath = GetRelativePath(usbPath, xexFile).Replace("\\", "/");
                        dashboards[dashboardName] = relativePath;
                    }
                }
            }
            
            if (dashboards.Count == 0)
            {
                var msgForm = new Form() { TopMost = true };
                msgForm.BringToFront();
                MessageBox.Show(msgForm, "No dashboards found on USB. Using default configuration.", "Dashboard Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                msgForm.Dispose();
                return new KeyValuePair<string, string>("Default", "default.xex");
            }
            
            // Create selection dialog
            using (var form = new Form())
            {
                form.Text = "Select Default Dashboard";
                form.Size = new Size(400, 200);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.TopMost = true;
                form.BringToFront();
                
                var label = new Label()
                {
                    Text = "Select the default dashboard to use:",
                    Location = new Point(20, 20),
                    Size = new Size(350, 20)
                };
                
                var comboBox = new ComboBox()
                {
                    Location = new Point(20, 50),
                    Size = new Size(350, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                
                foreach (var dashboard in dashboards)
                {
                    comboBox.Items.Add(dashboard.Key);
                }
                
                if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;
                
                var okButton = new Button()
                {
                    Text = "OK",
                    Location = new Point(200, 100),
                    Size = new Size(75, 25),
                    DialogResult = DialogResult.OK
                };
                
                var cancelButton = new Button()
                {
                    Text = "Cancel",
                    Location = new Point(285, 100),
                    Size = new Size(75, 25),
                    DialogResult = DialogResult.Cancel
                };
                
                form.Controls.AddRange(new Control[] { label, comboBox, okButton, cancelButton });
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;
                
                if (form.ShowDialog(this) == DialogResult.OK && comboBox.SelectedItem != null)
                {
                    string selectedDashboard = comboBox.SelectedItem.ToString();
                    return new KeyValuePair<string, string>(selectedDashboard, dashboards[selectedDashboard]);
                }
            }
            
            return null;
        }
        
        private void ShowStealthServerSelectionDialog()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Stealth Server XEX File";
                openFileDialog.Filter = "XEX Files (*.xex)|*.xex|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedStealthServerPath = openFileDialog.FileName;
                }
            }
        }
        
        private async Task DownloadAndModifyLaunchIni(string usbPath, string selectedDashboard, string dashboardPath, string stealthServerPath)
        {
            try
            {
                string launchIniPath = Path.Combine(usbPath, "launch.ini");
                
                // Download launch.ini if it doesn't exist
                if (!File.Exists(launchIniPath))
                {
                    UpdateStatus("Status: Downloading launch.ini...");
                    await DownloadFileAsync("https://github.com/I-am-Xoid/badstick-test/releases/download/packages/launch.ini", launchIniPath);
                }
                
                // Read the current launch.ini content
                string[] lines = File.ReadAllLines(launchIniPath);
                
                // Modify the launch.ini content
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("Default ="))
                    {
                        if (string.IsNullOrEmpty(dashboardPath))
                        {
                            lines[i] = "Default = ";
                        }
                        else
                        {
                            lines[i] = $"Default = usb:\\{dashboardPath.Replace("/", "\\")}";
                        }
                    }
                    else if (lines[i].StartsWith("plugin1 ="))
                    {
                        lines[i] = "plugin1 = Usb:\\Plugins\\Xbdm.xex";
                    }
                    else if (lines[i].StartsWith("plugin2 ="))
                    {
                        lines[i] = "plugin2 = Usb:\\Plugins\\JRPC2.xex";
                    }
                    else if (lines[i].StartsWith("plugin3 ="))
                    {
                        lines[i] = "plugin3 = Usb:\\Plugins\\XDRPC.xex";
                    }
                    else if (lines[i].StartsWith("plugin4 ="))
                    {
                        lines[i] = "plugin4 = Usb:\\Plugins\\XRPC.xex";
                    }
                    else if (lines[i].StartsWith("plugin5 =") && !string.IsNullOrEmpty(stealthServerPath))
                    {
                        string stealthServerFileName = Path.GetFileName(stealthServerPath);
                        string stealthServerDir = Path.GetDirectoryName(stealthServerPath);
                        string relativePath = GetRelativePath(usbPath, stealthServerPath).Replace("\\", "/");
                        lines[i] = $"plugin5 = usb:\\{relativePath.Replace("/", "\\")}";
                    }
                }
                
                // Write the modified content back to launch.ini
                File.WriteAllLines(launchIniPath, lines);
                
                UpdateStatus("Status: launch.ini configured successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error modifying launch.ini: {ex.Message}", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith("\\") ? basePath : basePath + "\\");
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString());
        }

        private void SelectAllToggle_CheckedChanged(object sender, EventArgs e)
        {
            bool checkAll = SelectAllToggle.Checked;

            foreach (var kvp in _checkBoxDict)
            {
                kvp.Value.Checked = checkAll;
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void RefDrivesBtn_Click(object sender, EventArgs e)
        {
            LoadUsbDrives();
        }

        private void widBtn_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Dashlaunch is not listed here, because it cannot be ran on BadUpdate " +
                "consoles. If you were to install Dashlaunch on a BadUpdate exploited console, it would" +
                " temporarily brick your nand, and you would then have to perform a RGH to revive it.", "Where is Dashlaunch?", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void skipmainQ_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("Enabling this will skip the main files that BadStick installs by default (Rock Band" +
                " Blitz, the payload, and XeXMenu V1.2). This is useful if you already have your USB setup for the " +
                "Bad Update exploit and only want to install other packages.", "What is this?", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void skipformatQ_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
        }

        private void discordserverBtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://discord.gg/xMbKazpkvf");
        }

        private void badstickredditBtn_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.reddit.com/r/360hacks/comments/1mmaaz2/release_badstick_a_badupdate_usb_auto_installer/");
        }

        private void reddit360Btn_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.reddit.com/r/360hacks/");
        }

        private void githubpageBtn_Click_1(object sender, EventArgs e)
        {
            Process.Start("https://github.com/32BitKlepto/BadStick");
        }

        private void skipmainfilesToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (!skipmainfilesToggle.Checked)
            {
                freemyxeToggle.Enabled = true;
                xeunshackleToggle.Enabled = true;
                skiprbbToggle.Enabled = true;
                skipxexmenuToggle.Enabled = true;
                return;
            }
            else
            {
                freemyxeToggle.Checked = false;
                freemyxeToggle.Enabled = false;
                xeunshackleToggle.Checked = false;
                xeunshackleToggle.Enabled = false;
                skiprbbToggle.Checked = false;
                skiprbbToggle.Enabled = false;
                skipxexmenuToggle.Checked = false;
                skipxexmenuToggle.Enabled = false;
                return;
            }
        }

        private void xeunshackleToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (!xeunshackleToggle.Checked)
            {
                freemyxeToggle.Enabled = true;
            }
            else
            {
                freemyxeToggle.Checked = false;
                freemyxeToggle.Enabled= false;
            }
        }

        private void freemyxeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (!freemyxeToggle.Checked)
            {
                xeunshackleToggle.Enabled = true;
            }
            else
            {
                xeunshackleToggle.Checked = false;
                xeunshackleToggle.Enabled = false;
            }
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
    }
}
