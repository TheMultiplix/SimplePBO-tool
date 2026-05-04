using System.Diagnostics;
using System.Drawing;
using FPacker.Services;

namespace FPacker;

public sealed class MainForm : Form {
    private static readonly Color AppBackground = Color.FromArgb(11, 15, 20);
    private static readonly Color SurfaceBackground = Color.FromArgb(18, 24, 33);
    private static readonly Color SurfaceBorder = Color.FromArgb(39, 51, 66);
    private static readonly Color InputBackground = Color.FromArgb(13, 18, 26);
    private static readonly Color InputForeground = Color.FromArgb(232, 238, 245);
    private static readonly Color PrimaryText = Color.FromArgb(245, 247, 250);
    private static readonly Color SecondaryText = Color.FromArgb(146, 163, 184);
    private static readonly Color Accent = Color.FromArgb(54, 189, 255);
    private static readonly Color AccentHover = Color.FromArgb(82, 205, 255);
    private static readonly Color DragIdleBackground = Color.FromArgb(16, 22, 30);
    private static readonly Color DragActiveBackground = Color.FromArgb(21, 39, 56);

    private readonly TextBox _modNameTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _sourceFolderTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputFileTextBox = new() { Dock = DockStyle.Fill };

    private readonly CheckBox _relocateConfigsCheckBox = new() { Text = "Relocate config files", Checked = false, AutoSize = true };
    private readonly CheckBox _relocateScriptsCheckBox = new() { Text = "Relocate script files", Checked = false, AutoSize = true };
    private readonly CheckBox _protectConfigsCheckBox = new() { Text = "Protect config structure", Checked = false, AutoSize = true };
    private readonly CheckBox _junkFilesCheckBox = new() { Text = "Add junk files", Checked = false, AutoSize = true };
    private readonly CheckBox _binarizeConfigsCheckBox = new() { Text = "Binarize configs", Checked = false, AutoSize = true };

    private readonly Button _browseSourceButton = new() { Text = "Browse Folder", AutoSize = true };
    private readonly Button _browseOutputButton = new() { Text = "Save As", AutoSize = true };
    private readonly Button _buildButton = new() { Text = "Build PBO", AutoSize = true, Padding = new Padding(18, 10, 18, 10) };
    private readonly Button _openOutputFolderButton = new() { Text = "Open Output Folder", AutoSize = true };

    private readonly Label _statusLabel = new() { Text = "Drop your mod folder here or click Browse Folder.", AutoSize = true };
    private readonly Label _dropZoneTitleLabel = new() {
        Text = "Drop mod folder here",
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 16F, FontStyle.Regular)
    };
    private readonly Label _dropZoneSubtitleLabel = new() {
        Text = "You can also click Browse Folder.",
        AutoSize = true,
        ForeColor = SystemColors.GrayText
    };

    private readonly Panel _dropZonePanel = new() {
        Dock = DockStyle.Top,
        Height = 150,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.FromArgb(245, 248, 252),
        AllowDrop = true,
        Margin = new Padding(0, 0, 0, 16)
    };

    private bool _updatingOutputPath;
    private bool _isBusy;

    public MainForm() {
        Text = "SimplePBO-tool";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 720);
        ClientSize = new Size(1180, 820);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        BackColor = AppBackground;
        ForeColor = PrimaryText;
        AllowDrop = true;

        _modNameTextBox.TextChanged += (_, _) => UpdateSuggestedOutputFile();
        _sourceFolderTextBox.TextChanged += (_, _) => HandleSourceFolderChanged();
        _outputFileTextBox.TextChanged += (_, _) => UpdateOpenFolderButtonState();

        _browseSourceButton.Click += (_, _) => BrowseForSourceFolder();
        _browseOutputButton.Click += (_, _) => BrowseForOutputFile();
        _buildButton.Click += async (_, _) => await BuildAsync();
        _openOutputFolderButton.Click += (_, _) => OpenOutputFolder();

        ConfigureDragAndDrop(this);
        ConfigureDragAndDrop(_dropZonePanel);
        ConfigureDragAndDrop(_sourceFolderTextBox);

        _dropZonePanel.Controls.Add(CreateDropZoneContent());
        Controls.Add(CreateLayout());

        ApplyDarkTheme();
        ApplySimpleDefaults();
        UpdateSummaryText();
        UpdateOpenFolderButtonState();
    }

    private Control CreateLayout() {
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(_dropZonePanel, 0, 1);
        root.Controls.Add(CreateMainPanel(), 0, 2);
        root.Controls.Add(CreateFooterPanel(), 0, 3);
        return root;
    }

    private Control CreateHeaderPanel() {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 14)
        };

        panel.Controls.Add(new Label {
            Text = "Simple PBO Builder",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Regular),
            ForeColor = PrimaryText,
            Margin = new Padding(0, 0, 0, 6)
        });

        panel.Controls.Add(new Label {
            Text = "Pick a mod folder, confirm where the .pbo should go, then click Build.",
            AutoSize = true,
            ForeColor = SecondaryText
        });

        return panel;
    }

    private Control CreateDropZoneContent() {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _dropZonePanel.BackColor
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(new Panel(), 0, 0);
        panel.Controls.Add(_dropZoneTitleLabel, 0, 1);
        panel.Controls.Add(_dropZoneSubtitleLabel, 0, 2);

        _dropZoneTitleLabel.Anchor = AnchorStyles.None;
        _dropZoneSubtitleLabel.Anchor = AnchorStyles.None;

        return panel;
    }

    private Control CreateMainPanel() {
        var main = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        main.Controls.Add(CreateFieldsGroup(), 0, 0);
        main.Controls.Add(CreateAdvancedGroup(), 0, 1);
        return main;
    }

    private Control CreateFieldsGroup() {
        var group = new GroupBox {
            Text = "Build",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 14)
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddFieldRow(layout, 0, "Mod name", _modNameTextBox, new Label { AutoSize = true });
        AddFieldRow(layout, 1, "Source", _sourceFolderTextBox, _browseSourceButton);
        AddFieldRow(layout, 2, "Output", _outputFileTextBox, _browseOutputButton);

        group.Controls.Add(layout);
        return group;
    }

    private Control CreateAdvancedGroup() {
        var group = new GroupBox {
            Text = "Advanced Options",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12)
        };

        var layout = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false
        };

        layout.Controls.Add(new Label {
            Text = "These are optional and off by default. Leave them unchecked for a normal build.",
            AutoSize = true,
            ForeColor = SecondaryText,
            Margin = new Padding(0, 0, 0, 8)
        });

        foreach (var checkBox in GetOptionCheckBoxes()) {
            layout.Controls.Add(checkBox);
        }

        group.Controls.Add(layout);
        return group;
    }

    private Control CreateFooterPanel() {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel.MaximumSize = new Size(520, 0);
        _statusLabel.Margin = new Padding(0, 6, 12, 6);

        panel.Controls.Add(_statusLabel, 0, 0);
        panel.SetRowSpan(_statusLabel, 2);

        _openOutputFolderButton.Margin = new Padding(0, 0, 12, 0);
        panel.Controls.Add(_openOutputFolderButton, 1, 0);
        panel.SetRowSpan(_openOutputFolderButton, 2);

        panel.Controls.Add(_buildButton, 2, 0);
        panel.SetRowSpan(_buildButton, 2);

        panel.Controls.Add(new Label {
            Text = "Tip: dropping a folder anywhere on the window will fill the source path automatically.",
            AutoSize = true,
            ForeColor = SecondaryText
        }, 0, 1);

        panel.Controls.Add(new Label {
            Text = "By: Multiplix & Source Code From Ellie (Ryann/Flipper)",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            ForeColor = SecondaryText,
            Margin = new Padding(0, 6, 0, 0)
        }, 2, 1);

        return panel;
    }

    private static void AddFieldRow(TableLayoutPanel layout, int rowIndex, string labelText, Control input, Control trailingControl) {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = PrimaryText,
            Margin = new Padding(0, 8, 12, 8)
        }, 0, rowIndex);

        input.Margin = new Padding(0, 4, 12, 4);
        input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(input, 1, rowIndex);

        trailingControl.Margin = new Padding(0, 4, 0, 4);
        trailingControl.Anchor = AnchorStyles.Left;
        layout.Controls.Add(trailingControl, 2, rowIndex);
    }

    private IEnumerable<CheckBox> GetOptionCheckBoxes() {
        yield return _relocateConfigsCheckBox;
        yield return _relocateScriptsCheckBox;
        yield return _protectConfigsCheckBox;
        yield return _junkFilesCheckBox;
        yield return _binarizeConfigsCheckBox;
    }

    private void ConfigureDragAndDrop(Control control) {
        control.AllowDrop = true;
        control.DragEnter += HandleDragEnter;
        control.DragDrop += HandleDragDrop;
    }

    private void HandleDragEnter(object? sender, DragEventArgs e) {
        if (TryGetDroppedFolder(e, out _)) {
            e.Effect = DragDropEffects.Copy;
            SetDropZoneState(isActive: true);
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void HandleDragDrop(object? sender, DragEventArgs e) {
        SetDropZoneState(isActive: false);

        if (!TryGetDroppedFolder(e, out var folderPath)) {
            _statusLabel.Text = "Drop a folder, not a single file.";
            return;
        }

        UseSourceFolder(folderPath);
        _statusLabel.Text = "Folder added. Click Build PBO when you're ready.";
    }

    protected override void OnDragLeave(EventArgs e) {
        base.OnDragLeave(e);
        SetDropZoneState(isActive: false);
    }

    private static bool TryGetDroppedFolder(DragEventArgs e, out string folderPath) {
        folderPath = string.Empty;

        if (!e.Data!.GetDataPresent(DataFormats.FileDrop)) {
            return false;
        }

        var droppedItems = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (droppedItems is null || droppedItems.Length == 0) {
            return false;
        }

        var firstItem = droppedItems[0];
        if (!Directory.Exists(firstItem)) {
            return false;
        }

        folderPath = firstItem;
        return true;
    }

    private void SetDropZoneState(bool isActive) {
        _dropZonePanel.BackColor = isActive ? DragActiveBackground : DragIdleBackground;
        _dropZoneTitleLabel.Text = isActive ? "Release to use this folder" : "Drop mod folder here";
        _dropZoneSubtitleLabel.Text = isActive ? "The source folder will be filled in automatically." : "You can also click Browse Folder.";
    }

    private void BrowseForSourceFolder() {
        using var dialog = new FolderBrowserDialog {
            Description = "Choose the root mod folder to pack."
        };

        if (Directory.Exists(_sourceFolderTextBox.Text)) {
            dialog.InitialDirectory = _sourceFolderTextBox.Text;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        UseSourceFolder(dialog.SelectedPath);
        _statusLabel.Text = "Folder selected. Click Build PBO when you're ready.";
    }

    private void BrowseForOutputFile() {
        using var dialog = new SaveFileDialog {
            Filter = "PBO files (*.pbo)|*.pbo|All files (*.*)|*.*",
            Title = "Choose where to save the built PBO"
        };

        if (!string.IsNullOrWhiteSpace(_outputFileTextBox.Text)) {
            dialog.FileName = Path.GetFileName(_outputFileTextBox.Text);
            var directory = Path.GetDirectoryName(_outputFileTextBox.Text);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                dialog.InitialDirectory = directory;
            }
        }

        if (dialog.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        _outputFileTextBox.Text = dialog.FileName;
        UpdateSummaryText();
    }

    private void UseSourceFolder(string folderPath) {
        _sourceFolderTextBox.Text = folderPath;

        if (string.IsNullOrWhiteSpace(_modNameTextBox.Text)) {
            _modNameTextBox.Text = new DirectoryInfo(folderPath).Name;
        }

        UpdateSuggestedOutputFile();
        UpdateSummaryText();
    }

    private void HandleSourceFolderChanged() {
        if (string.IsNullOrWhiteSpace(_modNameTextBox.Text) && Directory.Exists(_sourceFolderTextBox.Text)) {
            _modNameTextBox.Text = new DirectoryInfo(_sourceFolderTextBox.Text).Name;
        }

        UpdateSuggestedOutputFile();
        UpdateSummaryText();
    }

    private void UpdateSuggestedOutputFile() {
        if (_updatingOutputPath) {
            return;
        }

        if (string.IsNullOrWhiteSpace(_modNameTextBox.Text) || string.IsNullOrWhiteSpace(_sourceFolderTextBox.Text)) {
            UpdateSummaryText();
            return;
        }

        var baseDirectory = Directory.Exists(_sourceFolderTextBox.Text)
            ? Directory.GetParent(_sourceFolderTextBox.Text)?.FullName
            : null;

        if (string.IsNullOrWhiteSpace(baseDirectory)) {
            UpdateSummaryText();
            return;
        }

        _updatingOutputPath = true;
        try {
            var safeFileName = string.Concat(_modNameTextBox.Text.Trim().Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            _outputFileTextBox.Text = Path.Combine(baseDirectory, safeFileName + ".pbo");
        }
        finally {
            _updatingOutputPath = false;
        }

        UpdateSummaryText();
    }

    private void UpdateSummaryText() {
        var hasSource = !string.IsNullOrWhiteSpace(_sourceFolderTextBox.Text);
        var hasOutput = !string.IsNullOrWhiteSpace(_outputFileTextBox.Text);

        if (!hasSource) {
            _statusLabel.Text = "Drop your mod folder here or click Browse Folder.";
            return;
        }

        if (!hasOutput) {
            _statusLabel.Text = "Choose where the built .pbo should be saved.";
            return;
        }

        _statusLabel.Text = $"Ready: {_modNameTextBox.Text.Trim()} -> {_outputFileTextBox.Text.Trim()}";
    }

    private void ApplySimpleDefaults() {
        _relocateConfigsCheckBox.Checked = false;
        _relocateScriptsCheckBox.Checked = false;
        _protectConfigsCheckBox.Checked = false;
        _junkFilesCheckBox.Checked = false;
        _binarizeConfigsCheckBox.Checked = false;
    }

    private async Task BuildAsync() {
        ToggleBusyState(true);
        _statusLabel.Text = "Building PBO...";

        try {
            var request = new BuildRequest {
                ModName = _modNameTextBox.Text.Trim(),
                SourceDirectory = _sourceFolderTextBox.Text.Trim(),
                OutputFile = _outputFileTextBox.Text.Trim(),
                RelocateConfigs = _relocateConfigsCheckBox.Checked,
                RelocateScripts = _relocateScriptsCheckBox.Checked,
                ProtectConfigs = _protectConfigsCheckBox.Checked,
                AddJunkFiles = _junkFilesCheckBox.Checked,
                BinarizeConfigs = _binarizeConfigsCheckBox.Checked
            };

            await Task.Run(() => PboBuildService.Build(request));

            _statusLabel.Text = $"Build complete: {request.OutputFile}";
            UpdateOpenFolderButtonState();
            MessageBox.Show(this, "Your PBO was built successfully.", "Build Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) {
            _statusLabel.Text = "Build failed. Check the error message and try again.";
            MessageBox.Show(this, ex.Message, "Build Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally {
            ToggleBusyState(false);
        }
    }

    private void OpenOutputFolder() {
        if (!TryGetExistingOutputDirectory(out var directory)) {
            MessageBox.Show(this, "Choose an output file first.", "No Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private bool TryGetExistingOutputDirectory(out string directory) {
        directory = Path.GetDirectoryName(_outputFileTextBox.Text.Trim()) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory);
    }

    private void UpdateOpenFolderButtonState() {
        _openOutputFolderButton.Enabled = !_isBusy && TryGetExistingOutputDirectory(out _);
    }

    private void ToggleBusyState(bool isBusy) {
        _isBusy = isBusy;
        _modNameTextBox.Enabled = !isBusy;
        _sourceFolderTextBox.Enabled = !isBusy;
        _outputFileTextBox.Enabled = !isBusy;
        _browseSourceButton.Enabled = !isBusy;
        _browseOutputButton.Enabled = !isBusy;
        _buildButton.Enabled = !isBusy;

        foreach (var checkBox in GetOptionCheckBoxes()) {
            checkBox.Enabled = !isBusy;
        }

        UpdateOpenFolderButtonState();
        UseWaitCursor = isBusy;
    }

    private void ApplyDarkTheme() {
        _dropZonePanel.BackColor = DragIdleBackground;
        _dropZoneTitleLabel.ForeColor = PrimaryText;
        _dropZoneSubtitleLabel.ForeColor = SecondaryText;

        StyleTextBox(_modNameTextBox);
        StyleTextBox(_sourceFolderTextBox);
        StyleTextBox(_outputFileTextBox);

        StylePrimaryButton(_buildButton);
        StyleSecondaryButton(_browseSourceButton);
        StyleSecondaryButton(_browseOutputButton);
        StyleSecondaryButton(_openOutputFolderButton);

        foreach (var checkBox in GetOptionCheckBoxes()) {
            checkBox.ForeColor = PrimaryText;
            checkBox.BackColor = SurfaceBackground;
            checkBox.FlatStyle = FlatStyle.Flat;
        }

        ApplyThemeRecursive(this);
    }

    private void ApplyThemeRecursive(Control parent) {
        foreach (Control control in parent.Controls) {
            switch (control) {
                case TableLayoutPanel tableLayoutPanel:
                    tableLayoutPanel.BackColor = Color.Transparent;
                    break;
                case FlowLayoutPanel flowLayoutPanel:
                    flowLayoutPanel.BackColor = SurfaceBackground;
                    break;
                case GroupBox groupBox:
                    groupBox.BackColor = SurfaceBackground;
                    groupBox.ForeColor = PrimaryText;
                    break;
                case Panel panel:
                    if (panel != _dropZonePanel) {
                        panel.BackColor = SurfaceBackground;
                    }
                    break;
                case Label label:
                    if (label.ForeColor == Color.Empty || label.ForeColor.ToArgb() == SystemColors.ControlText.ToArgb()) {
                        label.ForeColor = PrimaryText;
                    }
                    break;
            }

            ApplyThemeRecursive(control);
        }
    }

    private static void StyleTextBox(TextBox textBox) {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = InputBackground;
        textBox.ForeColor = InputForeground;
    }

    private static void StylePrimaryButton(Button button) {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Accent;
        button.ForeColor = Color.FromArgb(8, 12, 18);
    }

    private static void StyleSecondaryButton(Button button) {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = SurfaceBorder;
        button.BackColor = SurfaceBackground;
        button.ForeColor = PrimaryText;
    }
}
