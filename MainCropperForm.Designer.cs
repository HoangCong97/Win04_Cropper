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

    internal float _dpiScale = 1.0f;
    internal int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _editIconBmp?.Dispose();
            _deleteIconBmp?.Dispose();
            _coordIconBmp?.Dispose();
            _imageIconBmp?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();

        _dpiScale = this.DeviceDpi > 0 ? (this.DeviceDpi / 96.0f) : 1.0f;
        if (_dpiScale < 1.0f) _dpiScale = 1.0f;

        this.AutoScaleMode = AutoScaleMode.None;

        // -------------------------------------------------------------
        // Main Form Properties
        // -------------------------------------------------------------
        this.Text = "Screen Cropper Pro - Định vị tọa độ & Cắt ảnh màn hình";
        this.Size = new Size(DpiScale(1400), DpiScale(880));
        this.MinimumSize = new Size(DpiScale(1020), DpiScale(680));
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
            Height = DpiScale(56),
            BackColor = Color.FromArgb(28, 31, 40),
            Padding = new Padding(DpiScale(12), DpiScale(8), DpiScale(12), DpiScale(8))
        };

        picAppLogo = new IconPictureBox
        {
            IconChar = IconChar.CropSimple,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(24),
            Size = new Size(DpiScale(26), DpiScale(26)),
            Location = new Point(DpiScale(14), DpiScale(15)),
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(picAppLogo);

        lblAppTitle = new Label
        {
            Text = "CROPPER PRO",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(picAppLogo.Right + DpiScale(8), DpiScale(15)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblAppTitle);

        FlowLayoutPanel pnlHeaderButtons = new()
        {
            Location = new Point(DpiScale(195), DpiScale(10)),
            Height = DpiScale(36),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(pnlHeaderButtons);

        btnOpenFile = CreateHeaderButton("Nạp ảnh", IconChar.FolderOpen);
        btnOpenFile.Click += (s, e) => OpenImageFromFile();
        pnlHeaderButtons.Controls.Add(btnOpenFile);

        btnPasteClipboard = CreateHeaderButton("Dán (Ctrl+V)", IconChar.Paste);
        btnPasteClipboard.Click += (s, e) => PasteFromClipboard();
        pnlHeaderButtons.Controls.Add(btnPasteClipboard);

        btnLiveCapture = CreateHeaderButton("Chụp Live (F9)", IconChar.Camera, Color.FromArgb(0, 168, 150));
        btnLiveCapture.Click += async (s, e) => await TriggerLiveCaptureAsync();
        pnlHeaderButtons.Controls.Add(btnLiveCapture);

        btnWindowCapture = CreateHeaderButton("Cửa sổ khác", IconChar.WindowRestore);
        btnWindowCapture.Click += async (s, e) => await TriggerWindowCaptureAsync();
        pnlHeaderButtons.Controls.Add(btnWindowCapture);

        btnFitView = CreateHeaderButton("Vừa khung", IconChar.Expand);
        btnFitView.Click += (s, e) => canvas.FitImageToView();
        pnlHeaderButtons.Controls.Add(btnFitView);

        btn100View = CreateHeaderButton("100%", IconChar.MagnifyingGlass);
        btn100View.Click += (s, e) => canvas.SetZoom100();
        pnlHeaderButtons.Controls.Add(btn100View);

        // Header status labels (Flow right-aligned)
        FlowLayoutPanel pnlHeaderStatus = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(18), DpiScale(14), 0)
        };
        pnlHeader.Controls.Add(pnlHeaderStatus);

        lblImageInfo = new Label
        {
            Text = "Ảnh: Chưa nạp",
            ForeColor = Color.FromArgb(170, 185, 205),
            AutoSize = true,
            Margin = new Padding(0, 0, DpiScale(16), 0)
        };
        pnlHeaderStatus.Controls.Add(lblImageInfo);

        lblCursorInfo = new Label
        {
            Text = "Tọa độ chuột: -",
            ForeColor = Color.FromArgb(0, 220, 255),
            AutoSize = true
        };
        pnlHeaderStatus.Controls.Add(lblCursorInfo);

        // -------------------------------------------------------------
        // Body Container: splitMain (Top vs Bottom)
        // -------------------------------------------------------------
        splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(38, 42, 54),
            Size = new Size(DpiScale(1400), DpiScale(820)),
            Panel1MinSize = DpiScale(150),
            Panel2MinSize = DpiScale(150),
            SplitterDistance = DpiScale(530)
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
            BackColor = Color.FromArgb(38, 42, 54),
            Size = new Size(DpiScale(1400), DpiScale(530)),
            Panel1MinSize = DpiScale(250),
            Panel2MinSize = DpiScale(180),
            SplitterDistance = DpiScale(960)
        };
        splitMain.Panel1.Controls.Add(splitTop);

        // Left Canvas Container
        pnlCanvasContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 20, 26) };
        pnlCanvasHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(24, 27, 35),
            Padding = new Padding(DpiScale(8), DpiScale(5), DpiScale(8), DpiScale(5))
        };

        picCanvasIcon = new IconPictureBox
        {
            IconChar = IconChar.Image,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasIcon);

        lblCanvasTitle = new Label
        {
            Text = "Ảnh gốc & Khung cắt ảo",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(240, 245, 255),
            Location = new Point(picCanvasIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasTitle);

        picCanvasGuide = new IconPictureBox
        {
            IconChar = IconChar.Lightbulb,
            IconColor = Color.FromArgb(245, 185, 30),
            IconSize = DpiScale(14),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(lblCanvasTitle.Right + DpiScale(20), DpiScale(8)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasGuide);

        lblCanvasGuide = new Label
        {
            Text = "Lăn chuột: Zoom | Chuột giữa/Phải: Pan | Kéo cạnh/góc: Resize",
            ForeColor = Color.FromArgb(140, 150, 170),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(picCanvasGuide.Right + DpiScale(6), DpiScale(8)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasGuide);

        FlowLayoutPanel pnlCanvasZoom = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(4), DpiScale(8), 0)
        };
        pnlCanvasHeader.Controls.Add(pnlCanvasZoom);

        btnZoomOut = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomOut.FlatAppearance.BorderSize = 0;
        btnZoomOut.Click += (s, e) => canvas.ZoomOut();
        pnlCanvasZoom.Controls.Add(btnZoomOut);

        lblZoomValue = new Label
        {
            Text = "100%",
            ForeColor = Color.FromArgb(0, 220, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Size = new Size(DpiScale(50), DpiScale(24)),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlCanvasZoom.Controls.Add(lblZoomValue);

        btnZoomIn = new IconButton
        {
            IconChar = IconChar.Plus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomIn.Click += (s, e) => canvas.ZoomIn();
        pnlCanvasZoom.Controls.Add(btnZoomIn);

        canvas = new CanvasControl { Dock = DockStyle.Fill };
        pnlCanvasContainer.Controls.Add(canvas);
        pnlCanvasContainer.Controls.Add(pnlCanvasHeader);
        splitTop.Panel1.Controls.Add(pnlCanvasContainer);

        // Right Preview Container
        pnlPreviewContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 24, 30) };
        pnlPreviewHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(24, 27, 35),
            Padding = new Padding(DpiScale(8), DpiScale(5), DpiScale(8), DpiScale(5))
        };

        picPreviewIcon = new IconPictureBox
        {
            IconChar = IconChar.Eye,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlPreviewHeader.Controls.Add(picPreviewIcon);

        lblPreviewTitle = new Label
        {
            Text = "Hình xem trước (Fit)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(240, 245, 255),
            Location = new Point(picPreviewIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlPreviewHeader.Controls.Add(lblPreviewTitle);

        chkPixelInterp = new CheckBox
        {
            Text = "Pixel sắc nét",
            Checked = true,
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Right,
            AutoSize = true,
            Padding = new Padding(0, 0, DpiScale(8), 0),
            Cursor = Cursors.Hand
        };
        chkPixelInterp.CheckedChanged += (s, e) => preview.UsePixelInterpolation = chkPixelInterp.Checked;
        pnlPreviewHeader.Controls.Add(chkPixelInterp);

        pnlPreviewActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = DpiScale(44),
            BackColor = Color.FromArgb(28, 31, 40),
            Padding = new Padding(DpiScale(8), DpiScale(6), DpiScale(8), DpiScale(6))
        };

        btnQuickSaveImage = new IconButton
        {
            Text = " Cắt & Lưu ảnh",
            UseMnemonic = false,
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = DpiScale(32),
            AutoSize = true,
            Padding = new Padding(DpiScale(8), 0, DpiScale(8), 0),
            BackColor = Color.FromArgb(0, 168, 150),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, DpiScale(8), 0)
        };
        btnQuickSaveImage.FlatAppearance.BorderSize = 0;
        btnQuickSaveImage.Click += (s, e) => CropAndSaveImage();
        pnlPreviewActions.Controls.Add(btnQuickSaveImage);

        btnQuickCopyClipboard = new IconButton
        {
            Text = " Copy ảnh",
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = DpiScale(15),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = DpiScale(32),
            AutoSize = true,
            Padding = new Padding(DpiScale(8), 0, DpiScale(8), 0),
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
            Height = DpiScale(104),
            BackColor = Color.FromArgb(26, 29, 38),
            Padding = new Padding(DpiScale(12), DpiScale(8), DpiScale(12), DpiScale(8))
        };
        pnlBottom.Controls.Add(pnlControlsBar);

        picCoordIcon = new IconPictureBox
        {
            IconChar = IconChar.VectorSquare,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(DpiScale(14), DpiScale(10)),
            BackColor = Color.Transparent
        };
        pnlControlsBar.Controls.Add(picCoordIcon);

        lblCoordSection = new Label
        {
            Text = "ĐIỀU CHỈNH TỌA ĐỘ & KÍCH THƯỚC:",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 8.5F),
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(picCoordIcon.Right + DpiScale(6), DpiScale(10)),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblCoordSection);

        btnCancelEdit = new IconButton
        {
            Text = " Hủy sửa",
            IconChar = IconChar.Xmark,
            IconColor = Color.FromArgb(255, 140, 140),
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(8), 0),
            Size = new Size(DpiScale(90), DpiScale(24)),
            Location = new Point(lblCoordSection.Right + DpiScale(10), DpiScale(7)),
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

        // Coordinate inputs: FlowLayoutPanel avoids text collisions at any DPI
        FlowLayoutPanel pnlCoordInputs = new()
        {
            Location = new Point(DpiScale(12), DpiScale(36)),
            Height = DpiScale(36),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlControlsBar.Controls.Add(pnlCoordInputs);

        lblX = CreateParamLabel("X:", 0, 0);
        lblX.Margin = new Padding(0, DpiScale(5), DpiScale(2), 0);
        numX = CreateNumberBox(0, 0, DpiScale(72));
        numX.Margin = new Padding(0, 0, DpiScale(8), 0);
        numX.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlCoordInputs.Controls.Add(lblX);
        pnlCoordInputs.Controls.Add(numX);

        lblY = CreateParamLabel("Y:", 0, 0);
        lblY.Margin = new Padding(0, DpiScale(5), DpiScale(2), 0);
        numY = CreateNumberBox(0, 0, DpiScale(72));
        numY.Margin = new Padding(0, 0, DpiScale(8), 0);
        numY.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlCoordInputs.Controls.Add(lblY);
        pnlCoordInputs.Controls.Add(numY);

        lblW = CreateParamLabel("W:", 0, 0);
        lblW.Margin = new Padding(0, DpiScale(5), DpiScale(2), 0);
        numW = CreateNumberBox(0, 0, DpiScale(72));
        numW.Margin = new Padding(0, 0, DpiScale(8), 0);
        numW.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlCoordInputs.Controls.Add(lblW);
        pnlCoordInputs.Controls.Add(numW);

        lblH = CreateParamLabel("H:", 0, 0);
        lblH.Margin = new Padding(0, DpiScale(5), DpiScale(2), 0);
        numH = CreateNumberBox(0, 0, DpiScale(72));
        numH.Margin = new Padding(0, 0, DpiScale(8), 0);
        numH.ValueChanged += (s, e) => OnNumericInputChanged();
        pnlCoordInputs.Controls.Add(lblH);
        pnlCoordInputs.Controls.Add(numH);

        lblAspectRatio = new Label
        {
            Text = "Tỉ lệ: 4:3",
            ForeColor = Color.FromArgb(160, 175, 195),
            Font = new Font("Segoe UI", 8.5F),
            AutoSize = true,
            Margin = new Padding(DpiScale(4), DpiScale(5), 0, 0)
        };
        pnlCoordInputs.Controls.Add(lblAspectRatio);

        // Nudge Buttons (D-Pad)
        int nudgeX = DpiScale(470);
        Label lblNudge = new()
        {
            Text = "Di chuyển:",
            ForeColor = Color.FromArgb(170, 180, 200),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(nudgeX, DpiScale(8)),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblNudge);

        cboNudgeStep = new ComboBox
        {
            Location = new Point(nudgeX + DpiScale(62), DpiScale(5)),
            Width = DpiScale(62),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F)
        };
        cboNudgeStep.Items.AddRange(["1px", "5px", "10px", "50px"]);
        cboNudgeStep.SelectedIndex = 1; // Default 5px
        pnlControlsBar.Controls.Add(cboNudgeStep);

        int inputY = DpiScale(36);
        btnNudgeLeft = CreateToolButton(IconChar.ArrowLeft, nudgeX, inputY, DpiScale(28), DpiScale(28));
        btnNudgeLeft.Click += (s, e) => NudgeCrop(-GetNudgeStep(), 0);
        pnlControlsBar.Controls.Add(btnNudgeLeft);

        btnNudgeUp = CreateToolButton(IconChar.ArrowUp, nudgeX + DpiScale(31), inputY, DpiScale(28), DpiScale(28));
        btnNudgeUp.Click += (s, e) => NudgeCrop(0, -GetNudgeStep());
        pnlControlsBar.Controls.Add(btnNudgeUp);

        btnNudgeDown = CreateToolButton(IconChar.ArrowDown, nudgeX + DpiScale(62), inputY, DpiScale(28), DpiScale(28));
        btnNudgeDown.Click += (s, e) => NudgeCrop(0, GetNudgeStep());
        pnlControlsBar.Controls.Add(btnNudgeDown);

        btnNudgeRight = CreateToolButton(IconChar.ArrowRight, nudgeX + DpiScale(93), inputY, DpiScale(28), DpiScale(28));
        btnNudgeRight.Click += (s, e) => NudgeCrop(GetNudgeStep(), 0);
        pnlControlsBar.Controls.Add(btnNudgeRight);

        // Size +/- buttons
        int sizeBtnsX = nudgeX + DpiScale(130);
        Label lblSizeAdj = new()
        {
            Text = "Đổi Size:",
            ForeColor = Color.FromArgb(170, 180, 200),
            Font = new Font("Segoe UI", 8F),
            Location = new Point(sizeBtnsX, DpiScale(8)),
            AutoSize = true
        };
        pnlControlsBar.Controls.Add(lblSizeAdj);

        // Row 1: Width adjust
        int sizeY1 = inputY - DpiScale(3);
        int sizeY2 = inputY + DpiScale(22);

        btnWMinus10 = CreateMiniButton("W-10", sizeBtnsX, sizeY1, DpiScale(44), DpiScale(22));
        btnWMinus10.Click += (s, e) => ResizeCrop(-10, 0);
        pnlControlsBar.Controls.Add(btnWMinus10);

        btnWMinus1 = CreateMiniButton("W-1", sizeBtnsX + DpiScale(47), sizeY1, DpiScale(38), DpiScale(22));
        btnWMinus1.Click += (s, e) => ResizeCrop(-1, 0);
        pnlControlsBar.Controls.Add(btnWMinus1);

        btnWPlus1 = CreateMiniButton("W+1", sizeBtnsX + DpiScale(88), sizeY1, DpiScale(38), DpiScale(22));
        btnWPlus1.Click += (s, e) => ResizeCrop(1, 0);
        pnlControlsBar.Controls.Add(btnWPlus1);

        btnWPlus10 = CreateMiniButton("W+10", sizeBtnsX + DpiScale(129), sizeY1, DpiScale(46), DpiScale(22));
        btnWPlus10.Click += (s, e) => ResizeCrop(10, 0);
        pnlControlsBar.Controls.Add(btnWPlus10);

        // Row 2: Height adjust
        btnHMinus10 = CreateMiniButton("H-10", sizeBtnsX, sizeY2, DpiScale(44), DpiScale(22));
        btnHMinus10.Click += (s, e) => ResizeCrop(0, -10);
        pnlControlsBar.Controls.Add(btnHMinus10);

        btnHMinus1 = CreateMiniButton("H-1", sizeBtnsX + DpiScale(47), sizeY2, DpiScale(38), DpiScale(22));
        btnHMinus1.Click += (s, e) => ResizeCrop(0, -1);
        pnlControlsBar.Controls.Add(btnHMinus1);

        btnHPlus1 = CreateMiniButton("H+1", sizeBtnsX + DpiScale(88), sizeY2, DpiScale(38), DpiScale(22));
        btnHPlus1.Click += (s, e) => ResizeCrop(0, 1);
        pnlControlsBar.Controls.Add(btnHPlus1);

        btnHPlus10 = CreateMiniButton("H+10", sizeBtnsX + DpiScale(129), sizeY2, DpiScale(46), DpiScale(22));
        btnHPlus10.Click += (s, e) => ResizeCrop(0, 10);
        pnlControlsBar.Controls.Add(btnHPlus10);

        // Center Button
        btnCenterBox = CreateCenterButton("Căn giữa", IconChar.Bullseye, sizeBtnsX + DpiScale(182), sizeY1 + DpiScale(2), DpiScale(92), DpiScale(38));
        btnCenterBox.Click += (s, e) => CenterCropBox();
        pnlControlsBar.Controls.Add(btnCenterBox);

        // Action Buttons (Right side of control bar, docked right to always stay on right edge)
        FlowLayoutPanel pnlActionButtons = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(14), DpiScale(12), 0)
        };
        pnlControlsBar.Controls.Add(pnlActionButtons);

        btnSaveCoordinates = new IconButton
        {
            Text = " LƯU TỌA ĐỘ",
            UseMnemonic = false,
            IconChar = IconChar.FloppyDisk,
            IconColor = Color.White,
            IconSize = DpiScale(18),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(8), 0),
            Height = DpiScale(42),
            AutoSize = true,
            BackColor = Color.FromArgb(0, 180, 216),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSaveCoordinates.FlatAppearance.BorderSize = 0;
        btnSaveCoordinates.Click += (s, e) => SaveCurrentCoordinates();
        pnlActionButtons.Controls.Add(btnSaveCoordinates);

        btnCropAndSaveImage = new IconButton
        {
            Text = " CẮT & LƯU ẢNH",
            UseMnemonic = false,
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = DpiScale(18),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(8), 0),
            Height = DpiScale(42),
            AutoSize = true,
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Bold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCropAndSaveImage.FlatAppearance.BorderSize = 0;
        btnCropAndSaveImage.Click += (s, e) => CropAndSaveImage();
        pnlActionButtons.Controls.Add(btnCropAndSaveImage);

        btnCopyCroppedImage = new IconButton
        {
            Text = " COPY ẢNH",
            UseMnemonic = false,
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = DpiScale(17),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(10), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(8), 0),
            Height = DpiScale(42),
            AutoSize = true,
            BackColor = Color.FromArgb(55, 60, 78),
            ForeColor = Color.FromArgb(235, 240, 255),
            Font = new Font("Segoe UI Semibold", 8.5F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyCroppedImage.FlatAppearance.BorderSize = 0;
        btnCopyCroppedImage.Click += (s, e) => CopyCroppedImageToClipboard();
        pnlActionButtons.Controls.Add(btnCopyCroppedImage);

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
            Height = DpiScale(40),
            BackColor = Color.FromArgb(30, 34, 46),
            Padding = new Padding(DpiScale(14), DpiScale(6), DpiScale(14), DpiScale(6))
        };
        pnlSavedSection.Controls.Add(pnlSavedHeader);

        picSavedIcon = new IconPictureBox
        {
            IconChar = IconChar.RectangleList,
            IconColor = Color.FromArgb(0, 220, 255),
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(DpiScale(14), DpiScale(11)),
            BackColor = Color.Transparent
        };
        pnlSavedHeader.Controls.Add(picSavedIcon);

        lblSavedTitle = new Label
        {
            Text = "DANH SÁCH ĐÃ LƯU (TỌA ĐỘ & HÌNH ẢNH): 0 mục",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9.5F),
            ForeColor = Color.FromArgb(0, 220, 255),
            Location = new Point(picSavedIcon.Right + DpiScale(8), DpiScale(10)),
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
            Padding = new Padding(0, DpiScale(2), DpiScale(6), DpiScale(2))
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
        dgvSavedRegions.ColumnHeadersHeight = DpiScale(34);
        dgvSavedRegions.RowTemplate.Height = DpiScale(28);
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
        var colIdx = new DataGridViewTextBoxColumn { Name = "ColIndex", HeaderText = "#", Width = DpiScale(42) };
        colIdx.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colIdx);

        var colType = new DataGridViewTextBoxColumn { Name = "ColType", HeaderText = "Phân loại", Width = DpiScale(105) };
        colType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colType);

        var colName = new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Tên vùng / ảnh", Width = DpiScale(175) };
        colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvSavedRegions.Columns.Add(colName);

        var colX = new DataGridViewTextBoxColumn { Name = "ColX", HeaderText = "Tọa độ X", Width = DpiScale(75) };
        colX.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colX);

        var colY = new DataGridViewTextBoxColumn { Name = "ColY", HeaderText = "Tọa độ Y", Width = DpiScale(75) };
        colY.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colY);

        var colW = new DataGridViewTextBoxColumn { Name = "ColW", HeaderText = "Rộng (W)", Width = DpiScale(80) };
        colW.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colW);

        var colH = new DataGridViewTextBoxColumn { Name = "ColH", HeaderText = "Cao (H)", Width = DpiScale(80) };
        colH.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colH);

        var colRatio = new DataGridViewTextBoxColumn { Name = "ColRatio", HeaderText = "Tỉ lệ", Width = DpiScale(75) };
        colRatio.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvSavedRegions.Columns.Add(colRatio);

        var colCreated = new DataGridViewTextBoxColumn { Name = "ColCreated", HeaderText = "Thời gian tạo", Width = DpiScale(140) };
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
            Width = DpiScale(45),
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
            Width = DpiScale(45),
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

    private IconButton CreateHeaderButton(string text, IconChar icon, Color? backColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(8), 0, DpiScale(10), 0),
            Margin = new Padding(0, 0, DpiScale(6), 0),
            Height = DpiScale(34),
            AutoSize = true,
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

    private NumericUpDown CreateNumberBox(int x, int y, int width)
    {
        return new NumericUpDown
        {
            Location = new Point(x, y),
            Width = width,
            Height = DpiScale(28),
            Minimum = 0,
            Maximum = 10000,
            Value = 0,
            BackColor = Color.FromArgb(40, 44, 56),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI Bold", 9F)
        };
    }

    private IconButton CreateToolButton(IconChar icon, int x, int y, int w, int h, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            IconChar = icon,
            IconColor = iconColor ?? Color.FromArgb(230, 235, 245),
            IconSize = DpiScale(15),
            ImageAlign = ContentAlignment.MiddleCenter,
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

    private IconButton CreateCenterButton(string text, IconChar icon, int x, int y, int w, int h)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = Color.FromArgb(230, 235, 245),
            IconSize = DpiScale(14),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(6), 0, DpiScale(6), 0),
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
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private IconButton CreateListHeaderButton(string text, IconChar icon, int x, Color? bg = null, Color? iconColor = null)
    {
        IconButton btn = new()
        {
            Text = " " + text,
            UseMnemonic = false,
            IconChar = icon,
            IconColor = iconColor ?? Color.White,
            IconSize = DpiScale(14),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(DpiScale(8), 0, DpiScale(8), 0),
            Margin = new Padding(0, 0, DpiScale(6), 0),
            Location = new Point(x, DpiScale(5)),
            AutoSize = true,
            Height = DpiScale(30),
            BackColor = bg ?? Color.FromArgb(44, 49, 64),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    #endregion
}
