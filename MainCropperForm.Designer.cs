using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;

namespace Win04_Cropper;

partial class MainCropperForm
{
    private System.ComponentModel.IContainer components = null!;

    // Header Controls
    private Panel pnlHeader = null!;
    private IconPictureBox picAppLogo = null!;
    private Label lblAppTitle = null!;
    private IconButton btnOpenFile = null!;
    private IconButton btnPasteClipboard = null!;
    private IconButton btnLiveCapture = null!;
    private IconButton btnWindowCapture = null!;
    private IconButton btnFitView = null!;
    private IconButton btn100View = null!;
    private Label lblImageInfo = null!;
    private Label lblCursorInfo = null!;

    // Body Panels
    private SplitContainer splitMain = null!;
    private SplitContainer splitTop = null!;

    // Left Panel (Canvas)
    private Panel pnlCanvasContainer = null!;
    private Panel pnlCanvasHeader = null!;
    private IconPictureBox picCanvasIcon = null!;
    private Label lblCanvasTitle = null!;
    private IconPictureBox picCanvasGuide = null!;
    private Label lblCanvasGuide = null!;
    private Label lblZoomValue = null!;
    private IconButton btnZoomIn = null!;
    private IconButton btnZoomOut = null!;
    private CanvasControl canvas = null!;

    // Right Panel (Preview)
    private Panel pnlPreviewContainer = null!;
    private Panel pnlPreviewHeader = null!;
    private IconPictureBox picPreviewIcon = null!;
    private Label lblPreviewTitle = null!;
    private CheckBox chkPixelInterp = null!;
    private PreviewControl preview = null!;
    private FlowLayoutPanel pnlPreviewActions = null!;
    private IconButton btnQuickSaveImage = null!;
    private IconButton btnQuickCopyClipboard = null!;

    // Bottom Panel Controls
    private Panel pnlBottom = null!;
    private Panel pnlControlsBar = null!;
    private IconPictureBox picCoordIcon = null!;
    private Label lblCoordSection = null!;
    private Label lblX = null!;
    private NumericUpDown numX = null!;
    private Label lblY = null!;
    private NumericUpDown numY = null!;
    private Label lblW = null!;
    private NumericUpDown numW = null!;
    private Label lblH = null!;
    private NumericUpDown numH = null!;
    private Label lblAspectRatio = null!;

    // Nudge and resize buttons
    private IconButton btnNudgeLeft = null!;
    private IconButton btnNudgeRight = null!;
    private IconButton btnNudgeUp = null!;
    private IconButton btnNudgeDown = null!;
    private ComboBox cboNudgeStep = null!;

    private Button btnWMinus10 = null!;
    private Button btnWMinus1 = null!;
    private Button btnWPlus1 = null!;
    private Button btnWPlus10 = null!;
    private Button btnHMinus10 = null!;
    private Button btnHMinus1 = null!;
    private Button btnHPlus1 = null!;
    private Button btnHPlus10 = null!;
    private IconButton btnCenterBox = null!;

    // Action buttons in controls bar
    private IconButton btnSaveCoordinates = null!;
    private IconButton btnCropAndSaveImage = null!;
    private IconButton btnCopyCroppedImage = null!;
    private IconButton btnCancelEdit = null!;

    // Saved list section
    private Panel pnlSavedSection = null!;
    private Panel pnlSavedHeader = null!;
    private IconPictureBox picSavedIcon = null!;
    private Label lblSavedTitle = null!;
    private IconButton btnClearAll = null!;
    private IconButton btnExportPackage = null!;
    private IconButton btnExportJson = null!;
    private IconButton btnImportJson = null!;
    private DataGridView dgvSavedRegions = null!;

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
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        // -------------------------------------------------------------
        // Main Form Properties
        // -------------------------------------------------------------
        this.Text = "Screen Cropper Pro - Định vị tọa độ & Cắt ảnh màn hình";
        this.Size = new Size(1400, 880);
        this.MinimumSize = new Size(1020, 680);
        this.BackColor = Color.FromArgb(20, 22, 28);
        this.ForeColor = Color.FromArgb(235, 238, 245);
        this.Font = new Font("Segoe UI", 9F);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;

        // -------------------------------------------------------------
        // Header Panel (Dock = Top, Height = 56)
        // -------------------------------------------------------------
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(28, 31, 40),
            Padding = new Padding(12, 8, 12, 8)
        };

        picAppLogo = new IconPictureBox
        {
            IconChar = IconChar.CropSimple,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = 24,
            Size = new Size(24, 24),
            Location = new Point(14, 16),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picAppLogo);

        lblAppTitle = new Label
        {
            Text = "CROPPER PRO",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(44, 16),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblAppTitle);

        btnOpenFile = CreateHeaderButton("Nạp ảnh", IconChar.FolderOpen, new Point(185, 11), 105);
        btnOpenFile.Click += (s, e) => OpenImageFromFile();
        pnlHeader.Controls.Add(btnOpenFile);

        btnPasteClipboard = CreateHeaderButton("Dán (Ctrl+V)", IconChar.Paste, new Point(298, 11), 125);
        btnPasteClipboard.Click += (s, e) => PasteFromClipboard();
        pnlHeader.Controls.Add(btnPasteClipboard);

        btnLiveCapture = CreateHeaderButton("Chụp Live (F9)", IconChar.Camera, new Point(431, 11), 140, Color.FromArgb(0, 168, 150));
        btnLiveCapture.Click += async (s, e) => await TriggerLiveCaptureAsync();
        pnlHeader.Controls.Add(btnLiveCapture);

        btnWindowCapture = CreateHeaderButton("Cửa sổ khác", IconChar.WindowRestore, new Point(579, 11), 125);
        btnWindowCapture.Click += async (s, e) => await TriggerWindowCaptureAsync();
        pnlHeader.Controls.Add(btnWindowCapture);

        btnFitView = CreateHeaderButton("Vừa khung", IconChar.Expand, new Point(712, 11), 110);
        btnFitView.Click += (s, e) => canvas.FitImageToView();
        pnlHeader.Controls.Add(btnFitView);

        btn100View = CreateHeaderButton("100%", IconChar.MagnifyingGlass, new Point(830, 11), 85);
        btn100View.Click += (s, e) => canvas.SetZoom100();
        pnlHeader.Controls.Add(btn100View);

        // Header status labels
        lblImageInfo = new Label
        {
            Text = "Ảnh: Chưa nạp",
            ForeColor = Color.FromArgb(170, 185, 205),
            Location = new Point(928, 18),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblImageInfo);

        lblCursorInfo = new Label
        {
            Text = "Tọa độ chuột: -",
            ForeColor = Color.FromArgb(0, 220, 255),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1120, 18),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblCursorInfo);

        // -------------------------------------------------------------
        // Body Container: splitMain (Top vs Bottom)
        // -------------------------------------------------------------
        splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(38, 42, 54)
        };
        this.Controls.Add(splitMain);
        this.Controls.Add(pnlHeader);
        pnlHeader.SendToBack();

        // -------------------------------------------------------------
        // Top Panel: splitTop (Left Canvas vs Right Preview)
        // -------------------------------------------------------------
        splitTop = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(38, 42, 54)
        };
        splitMain.Panel1.Controls.Add(splitTop);

        // Left Canvas Container
        pnlCanvasContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 20, 26) };
        pnlCanvasHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.FromArgb(24, 27, 35),
            Padding = new Padding(8, 5, 8, 5)
        };

        picCanvasIcon = new IconPictureBox
        {
            IconChar = IconChar.Image,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = 16,
            Size = new Size(18, 18),
            Location = new Point(10, 8),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasIcon);

        lblCanvasTitle = new Label
        {
            Text = "Ảnh gốc & Khung cắt ảo",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(240, 245, 255),
            Location = new Point(32, 8),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasTitle);

        picCanvasGuide = new IconPictureBox
        {
            IconChar = IconChar.Lightbulb,
            IconColor = Color.FromArgb(245, 185, 30),
            IconSize = 14,
            Size = new Size(16, 16),
            Location = new Point(212, 10),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasGuide);

        lblCanvasGuide = new Label
        {
            Text = "Lăn chuột: Zoom | Chuột giữa/Phải: Pan | Kéo cạnh/góc: Resize",
            ForeColor = Color.FromArgb(140, 150, 170),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(232, 9),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasGuide);

        btnZoomOut = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = 13,
            Size = new Size(28, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(780, 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomOut.FlatAppearance.BorderSize = 0;
        btnZoomOut.Click += (s, e) => canvas.ZoomOut();
        pnlCanvasHeader.Controls.Add(btnZoomOut);

        lblZoomValue = new Label
        {
            Text = "100%",
            ForeColor = Color.FromArgb(0, 220, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(812, 8),
            Size = new Size(50, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlCanvasHeader.Controls.Add(lblZoomValue);

        btnZoomIn = new IconButton
        {
            IconChar = IconChar.Plus,
            IconColor = Color.White,
            IconSize = 13,
            Size = new Size(28, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(866, 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomIn.Click += (s, e) => canvas.ZoomIn();
        pnlCanvasHeader.Controls.Add(btnZoomIn);

        canvas = new CanvasControl { Dock = DockStyle.Fill };
        pnlCanvasContainer.Controls.Add(canvas);
        pnlCanvasContainer.Controls.Add(pnlCanvasHeader);
        splitTop.Panel1.Controls.Add(pnlCanvasContainer);

        // Right Preview Container
        pnlPreviewContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 24, 30) };
        pnlPreviewHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.FromArgb(24, 27, 35),
            Padding = new Padding(8, 5, 8, 5)
        };

        picPreviewIcon = new IconPictureBox
        {
            IconChar = IconChar.Eye,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = 16,
            Size = new Size(18, 18),
            Location = new Point(10, 8),
            BackColor = Color.Transparent
        };
        pnlPreviewHeader.Controls.Add(picPreviewIcon);

        lblPreviewTitle = new Label
        {
            Text = "Hình xem trước (Fit)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(240, 245, 255),
            Location = new Point(32, 8),
            AutoSize = true
        };
        pnlPreviewHeader.Controls.Add(lblPreviewTitle);

        chkPixelInterp = new CheckBox
        {
            Text = "Pixel sắc nét",
            Checked = true,
            ForeColor = Color.FromArgb(170, 185, 205),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(310, 6),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        chkPixelInterp.CheckedChanged += (s, e) => preview.UsePixelInterpolation = chkPixelInterp.Checked;
        pnlPreviewHeader.Controls.Add(chkPixelInterp);

        pnlPreviewActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BackColor = Color.FromArgb(28, 31, 40),
            Padding = new Padding(8, 6, 8, 6)
        };

        btnQuickSaveImage = new IconButton
        {
            Text = " Cắt & Lưu ảnh",
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = 16,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            Height = 32,
            Width = 145,
            BackColor = Color.FromArgb(0, 168, 150),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0)
        };
        btnQuickSaveImage.FlatAppearance.BorderSize = 0;
        btnQuickSaveImage.Click += (s, e) => CropAndSaveImage();
        pnlPreviewActions.Controls.Add(btnQuickSaveImage);

        btnQuickCopyClipboard = new IconButton
        {
            Text = " Copy ảnh",
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = 15,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            Height = 32,
            Width = 115,
            BackColor = Color.FromArgb(48, 54, 70),
            ForeColor = Color.FromArgb(230, 235, 250),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
            Cursor = Cursors.Hand
        };
        btnQuickCopyClipboard.FlatAppearance.BorderSize = 0;
        btnQuickCopyClipboard.Click += (s, e) => CopyCroppedImageToClipboard();
        pnlPreviewActions.Controls.Add(btnQuickCopyClipboard);

        preview = new PreviewControl { Dock = DockStyle.Fill };
        pnlPreviewContainer.Controls.Add(preview);
        pnlPreviewContainer.Controls.Add(pnlPreviewActions);
        pnlPreviewContainer.Controls.Add(pnlPreviewHeader);
        splitTop.Panel2.Controls.Add(pnlPreviewContainer);

        // -------------------------------------------------------------
        // Bottom Panel: Controls Bar + Saved Regions Grid
        // -------------------------------------------------------------
        pnlBottom = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 22, 28) };
        splitMain.Panel2.Controls.Add(pnlBottom);

        InitializeBottomControls();

        this.ResumeLayout(true);
    }

    private void InitializeBottomControls()
    {
        // 1. Controls Bar (Top of bottom panel)
        pnlControlsBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(26, 29, 38),
            Padding = new Padding(12, 8, 12, 8)
        };
        pnlBottom.Controls.Add(pnlControlsBar);

        picCoordIcon = new IconPictureBox
        {
            IconChar = IconChar.VectorSquare,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = 16,
            Size = new Size(18, 18),
            Location = new Point(14, 8),
            BackColor = Color.Transparent
        };
        pnlControlsBar.Controls.Add(picCoordIcon);

        lblCoordSection = new Label
        {
            Text = "ĐIỀU CHỈNH TỌA ĐỘ & KÍCH THƯỚC:",
            Font = new Font("Segoe UI Bold", 8.5F),
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(36, 8),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblCoordSection);

        btnCancelEdit = new IconButton
        {
            Text = " Hủy sửa",
            IconChar = IconChar.Xmark,
            IconColor = Color.FromArgb(255, 140, 140),
            IconSize = 13,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(6, 0, 8, 0),
            Size = new Size(86, 22),
            Location = new Point(480, 5),
            BackColor = Color.FromArgb(58, 30, 38),
            ForeColor = Color.FromArgb(255, 175, 185),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8F),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnCancelEdit.FlatAppearance.BorderSize = 0;
        btnCancelEdit.Click += (s, e) => CancelEditing();
        pnlControlsBar.Controls.Add(btnCancelEdit);

        // Coordinate inputs: X, Y, W, H
        int startX = 14;
        int inputY = 32;

        lblX = CreateParamLabel("X:", startX, inputY + 4);
        numX = CreateNumberBox(startX + 22, inputY, 70);
        numX.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlControlsBar.Controls.Add(lblX);
        pnlControlsBar.Controls.Add(numX);

        lblY = CreateParamLabel("Y:", startX + 102, inputY + 4);
        numY = CreateNumberBox(startX + 124, inputY, 70);
        numY.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlControlsBar.Controls.Add(lblY);
        pnlControlsBar.Controls.Add(numY);

        lblW = CreateParamLabel("W:", startX + 204, inputY + 4);
        numW = CreateNumberBox(startX + 230, inputY, 70);
        numW.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlControlsBar.Controls.Add(lblW);
        pnlControlsBar.Controls.Add(numW);

        lblH = CreateParamLabel("H:", startX + 310, inputY + 4);
        numH = CreateNumberBox(startX + 332, inputY, 70);
        numH.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlControlsBar.Controls.Add(lblH);
        pnlControlsBar.Controls.Add(numH);

        lblAspectRatio = new Label
        {
            Text = "Tỉ lệ: 4:3",
            ForeColor = Color.FromArgb(160, 175, 195),
            Font = new Font("Segoe UI", 8.5F),
            Location = new Point(startX + 415, inputY + 6),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblAspectRatio);

        // Nudge Buttons (D-Pad)
        int nudgeX = 490;
        Label lblNudge = new()
        {
            Text = "Di chuyển:",
            ForeColor = Color.FromArgb(170, 180, 200),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(nudgeX, 8),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblNudge);

        cboNudgeStep = new ComboBox
        {
            Location = new Point(nudgeX + 65, 5),
            Width = 60,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F)
        };
        cboNudgeStep.Items.AddRange(["1px", "5px", "10px", "50px"]);
        cboNudgeStep.SelectedIndex = 1; // Default 5px
        pnlControlsBar.Controls.Add(cboNudgeStep);

        btnNudgeLeft = CreateToolButton(IconChar.CaretLeft, nudgeX, inputY, 30, 28);
        btnNudgeLeft.Click += (s, e) => NudgeCrop(-GetNudgeStep(), 0);
        pnlControlsBar.Controls.Add(btnNudgeLeft);

        btnNudgeUp = CreateToolButton(IconChar.CaretUp, nudgeX + 34, inputY, 30, 28);
        btnNudgeUp.Click += (s, e) => NudgeCrop(0, -GetNudgeStep());
        pnlControlsBar.Controls.Add(btnNudgeUp);

        btnNudgeDown = CreateToolButton(IconChar.CaretDown, nudgeX + 68, inputY, 30, 28);
        btnNudgeDown.Click += (s, e) => NudgeCrop(0, GetNudgeStep());
        pnlControlsBar.Controls.Add(btnNudgeDown);

        btnNudgeRight = CreateToolButton(IconChar.CaretRight, nudgeX + 102, inputY, 30, 28);
        btnNudgeRight.Click += (s, e) => NudgeCrop(GetNudgeStep(), 0);
        pnlControlsBar.Controls.Add(btnNudgeRight);

        // Size +/- buttons
        int sizeBtnsX = 645;
        Label lblSizeAdj = new()
        {
            Text = "Đổi Size:",
            ForeColor = Color.FromArgb(170, 180, 200),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(sizeBtnsX, 8),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblSizeAdj);

        // Row 1: Width adjust
        btnWMinus10 = CreateMiniButton("W-10", sizeBtnsX, inputY - 3, 46, 22);
        btnWMinus10.Click += (s, e) => ResizeCrop(-10, 0);
        pnlControlsBar.Controls.Add(btnWMinus10);

        btnWMinus1 = CreateMiniButton("W-1", sizeBtnsX + 49, inputY - 3, 40, 22);
        btnWMinus1.Click += (s, e) => ResizeCrop(-1, 0);
        pnlControlsBar.Controls.Add(btnWMinus1);

        btnWPlus1 = CreateMiniButton("W+1", sizeBtnsX + 92, inputY - 3, 40, 22);
        btnWPlus1.Click += (s, e) => ResizeCrop(1, 0);
        pnlControlsBar.Controls.Add(btnWPlus1);

        btnWPlus10 = CreateMiniButton("W+10", sizeBtnsX + 135, inputY - 3, 48, 22);
        btnWPlus10.Click += (s, e) => ResizeCrop(10, 0);
        pnlControlsBar.Controls.Add(btnWPlus10);

        // Row 2: Height adjust
        btnHMinus10 = CreateMiniButton("H-10", sizeBtnsX, inputY + 22, 46, 22);
        btnHMinus10.Click += (s, e) => ResizeCrop(0, -10);
        pnlControlsBar.Controls.Add(btnHMinus10);

        btnHMinus1 = CreateMiniButton("H-1", sizeBtnsX + 49, inputY + 22, 40, 22);
        btnHMinus1.Click += (s, e) => ResizeCrop(0, -1);
        pnlControlsBar.Controls.Add(btnHMinus1);

        btnHPlus1 = CreateMiniButton("H+1", sizeBtnsX + 92, inputY + 22, 40, 22);
        btnHPlus1.Click += (s, e) => ResizeCrop(0, 1);
        pnlControlsBar.Controls.Add(btnHPlus1);

        btnHPlus10 = CreateMiniButton("H+10", sizeBtnsX + 135, inputY + 22, 48, 22);
        btnHPlus10.Click += (s, e) => ResizeCrop(0, 10);
        pnlControlsBar.Controls.Add(btnHPlus10);

        // Center Button
        btnCenterBox = CreateCenterButton("Căn giữa", IconChar.Bullseye, sizeBtnsX + 190, inputY - 1, 95, 32);
        btnCenterBox.Click += (s, e) => CenterCropBox();
        pnlControlsBar.Controls.Add(btnCenterBox);

        // Action Buttons (Right side of control bar)
        int actionX = 940;
        btnSaveCoordinates = new IconButton
        {
            Text = " LƯU TỌA ĐỘ",
            IconChar = IconChar.FloppyDisk,
            IconColor = Color.White,
            IconSize = 18,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 0, 8, 0),
            Location = new Point(actionX, 22),
            Size = new Size(140, 42),
            BackColor = Color.FromArgb(0, 180, 216),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 9.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSaveCoordinates.FlatAppearance.BorderSize = 0;
        btnSaveCoordinates.Click += (s, e) => SaveCurrentCoordinates();
        pnlControlsBar.Controls.Add(btnSaveCoordinates);

        btnCropAndSaveImage = new IconButton
        {
            Text = " CẮT & LƯU ẢNH",
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = 18,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 0, 8, 0),
            Location = new Point(actionX + 148, 22),
            Size = new Size(155, 42),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 9.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCropAndSaveImage.FlatAppearance.BorderSize = 0;
        btnCropAndSaveImage.Click += (s, e) => CropAndSaveImage();
        pnlControlsBar.Controls.Add(btnCropAndSaveImage);

        btnCopyCroppedImage = new IconButton
        {
            Text = " COPY ẢNH",
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = 17,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 0, 8, 0),
            Location = new Point(actionX + 311, 22),
            Size = new Size(125, 42),
            BackColor = Color.FromArgb(55, 60, 78),
            ForeColor = Color.FromArgb(235, 240, 255),
            Font = new Font("Segoe UI Semibold", 9F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyCroppedImage.FlatAppearance.BorderSize = 0;
        btnCopyCroppedImage.Click += (s, e) => CopyCroppedImageToClipboard();
        pnlControlsBar.Controls.Add(btnCopyCroppedImage);

        // 2. Saved Regions Section (Fill remaining bottom panel)
        pnlSavedSection = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 22, 28)
        };
        pnlBottom.Controls.Add(pnlSavedSection);
        pnlControlsBar.SendToBack();

        pnlSavedHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(30, 34, 46),
            Padding = new Padding(14, 6, 14, 6)
        };
        pnlSavedSection.Controls.Add(pnlSavedHeader);

        picSavedIcon = new IconPictureBox
        {
            IconChar = IconChar.RectangleList,
            IconColor = Color.FromArgb(0, 220, 255),
            IconSize = 16,
            Size = new Size(18, 18),
            Location = new Point(14, 11),
            BackColor = Color.Transparent
        };
        pnlSavedHeader.Controls.Add(picSavedIcon);

        lblSavedTitle = new Label
        {
            Text = "DANH SÁCH ĐÃ LƯU (TỌA ĐỘ & HÌNH ẢNH): 0 mục",
            Font = new Font("Segoe UI Bold", 9.5F),
            ForeColor = Color.FromArgb(0, 220, 255),
            Location = new Point(36, 10),
            AutoSize = true
        };
        pnlSavedHeader.Controls.Add(lblSavedTitle);

        FlowLayoutPanel pnlSavedHeaderActions = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 6, 2)
        };
        pnlSavedHeader.Controls.Add(pnlSavedHeaderActions);

        btnExportPackage = CreateListHeaderButton("Export", IconChar.BoxesPacking, 0, Color.FromArgb(16, 185, 129), Color.White);
        btnExportPackage.Font = new Font("Segoe UI Bold", 8.5F);
        btnExportPackage.Click += (s, e) => ExportPackage();
        pnlSavedHeaderActions.Controls.Add(btnExportPackage);

        btnExportJson = CreateListHeaderButton("Xuất JSON", IconChar.FileExport, 0);
        btnExportJson.Click += (s, e) => ExportRegionsToJson();
        pnlSavedHeaderActions.Controls.Add(btnExportJson);

        btnImportJson = CreateListHeaderButton("Nhập JSON", IconChar.FileImport, 0);
        btnImportJson.Click += (s, e) => ImportRegionsFromJson();
        pnlSavedHeaderActions.Controls.Add(btnImportJson);

        btnClearAll = CreateListHeaderButton("Xóa hết", IconChar.TrashCan, 0, Color.FromArgb(70, 45, 55), Color.FromArgb(255, 160, 160));
        btnClearAll.Click += (s, e) => ClearAllSavedRegions();
        pnlSavedHeaderActions.Controls.Add(btnClearAll);

        // DataGridView for Saved Regions
        dgvSavedRegions = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(18, 20, 26),
            ForeColor = Color.FromArgb(230, 235, 245),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(34, 38, 48),
            RowHeadersVisible = false,
            ColumnHeadersVisible = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AutoGenerateColumns = false,
            ShowCellToolTips = true,
            Font = new Font("Segoe UI", 9F)
        };

        dgvSavedRegions.EnableHeadersVisualStyles = false;
        dgvSavedRegions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvSavedRegions.ColumnHeadersHeight = 34;
        dgvSavedRegions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 33, 46);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(0, 215, 255);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F);
        dgvSavedRegions.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        dgvSavedRegions.DefaultCellStyle.BackColor = Color.FromArgb(22, 25, 33);
        dgvSavedRegions.DefaultCellStyle.ForeColor = Color.FromArgb(230, 235, 245);
        dgvSavedRegions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 119, 182);
        dgvSavedRegions.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvSavedRegions.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(26, 30, 40);

        // Columns definition with clear titles and alignment
        var colIdx = new DataGridViewTextBoxColumn { Name = "ColIndex", HeaderText = "#", Width = 42 };
        colIdx.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colIdx);

        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Phân loại", Width = 105 };
        colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colType);

        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên vùng / ảnh", Width = 175 };
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colName);

        var colX = new DataGridViewTextBoxColumn { Name = "ColX", HeaderText = "Tọa độ X", Width = 75 };
        colX.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colX);

        var colY = new DataGridViewTextBoxColumn { Name = "ColY", HeaderText = "Tọa độ Y", Width = 75 };
        colY.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colY);

        var colW = new DataGridViewTextBoxColumn { Name = "ColW", HeaderText = "Rộng (W)", Width = 80 };
        colW.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colW);

        var colH = new DataGridViewTextBoxColumn { Name = "ColH", HeaderText = "Cao (H)", Width = 80 };
        colH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colH);

        var colRatio = new DataGridViewTextBoxColumn { Name = "ColRatio", HeaderText = "Tỉ lệ", Width = 75 };
        colRatio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colRatio);

        var colCreated = new DataGridViewTextBoxColumn { Name = "ColCreated", HeaderText = "Thời gian tạo", Width = 140 };
        colCreated.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colCreated);

        var colNotes = new DataGridViewTextBoxColumn { Name = "ColNotes", HeaderText = "Ghi chú / Đường dẫn", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
        colNotes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colNotes);

        // Action Buttons Columns (compact icon-only buttons)
        var colEditBtn = new DataGridViewButtonColumn
        {
            Name = "ColEditBtn",
            HeaderText = "Sửa",
            Text = "",
            UseColumnTextForButtonValue = false,
            Width = 45,
            FlatStyle = FlatStyle.Flat
        };
        colEditBtn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colEditBtn);

        var colDeleteBtn = new DataGridViewButtonColumn
        {
            Name = "ColDeleteBtn",
            HeaderText = "Xóa",
            Text = "",
            UseColumnTextForButtonValue = false,
            Width = 45,
            FlatStyle = FlatStyle.Flat
        };
        colDeleteBtn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colDeleteBtn);

        dgvSavedRegions.SelectionChanged += (s, e) => OnSavedGridSelectionChanged();
        dgvSavedRegions.CellDoubleClick += (s, e) => OnSavedGridDoubleClick(e);
        dgvSavedRegions.CellContentClick += (s, e) => OnSavedGridCellContentClick(e);
        dgvSavedRegions.CellToolTipTextNeeded += (s, e) => OnSavedGridToolTipTextNeeded(e);
        dgvSavedRegions.CellPainting += (s, e) => OnSavedGridCellPainting(e);

        pnlSavedSection.Controls.Add(dgvSavedRegions);
        pnlSavedSection.Controls.Add(pnlSavedHeader);
        pnlSavedHeader.SendToBack(); // Ensures Dock=Top claims the top strip, Fill claims the rest
    }

    #region UI Helper Creators

    private static IconButton CreateHeaderButton(string text, IconChar icon, Point loc, int width, Color? backColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            IconChar = icon,
            IconColor = Color.White,
            IconSize = 16,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Location = loc,
            Size = new Size(width, 34),
            BackColor = backColor ?? Color.FromArgb(42, 47, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Label CreateParamLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(200, 210, 230)
        };
    }

    private static NumericUpDown CreateNumberBox(int x, int y, int width)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Width = width,
            Height = 28,
            Minimum = 0,
            Maximum = 10000,
            Value = 0,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI Bold", 9F)
        };
    }

    private static IconButton CreateToolButton(IconChar icon, int x, int y, int w, int h, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            IconChar = icon,
            IconColor = iconColor ?? Color.FromArgb(230, 235, 245),
            IconSize = 14,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(44, 49, 64),
            ForeColor = Color.FromArgb(230, 235, 245),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static IconButton CreateCenterButton(string text, IconChar icon, int x, int y, int w, int h)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            IconChar = icon,
            IconColor = Color.FromArgb(230, 235, 245),
            IconSize = 14,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(44, 49, 64),
            ForeColor = Color.FromArgb(230, 235, 245),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Button CreateMiniButton(string text, int x, int y, int w, int h)
    {
        Button btn = new()
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(36, 40, 52),
            ForeColor = Color.FromArgb(200, 215, 235),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static IconButton CreateListHeaderButton(string text, IconChar icon, int x, Color? bg = null, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            IconChar = icon,
            IconColor = iconColor ?? Color.White,
            IconSize = 14,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 8, 0),
            Location = new Point(x, 5),
            AutoSize = true,
            Height = 28,
            BackColor = bg ?? Color.FromArgb(44, 49, 64),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    #endregion
}
