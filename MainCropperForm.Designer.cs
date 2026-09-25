#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;

namespace Win04_Cropper;

partial class MainCropperForm
{
    internal float _dpiScale = 1.0f;
    internal int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            tipActions?.Dispose();
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
        this.BackColor = Color.FromArgb(32, 35, 42);
        this.ForeColor = Color.FromArgb(235, 238, 245);
        this.Font = new Font("Segoe UI", 9F);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;

        tipActions = new ToolTip
        {
            InitialDelay = 150,
            ReshowDelay = 80,
            AutoPopDelay = 6000,
            ShowAlways = true
        };

        // -------------------------------------------------------------
        // Header Panel (Dock = Top, Height = 56)
        // -------------------------------------------------------------
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(56),
            BackColor = Color.FromArgb(32, 36, 45),
            Padding = Padding.Empty
        };
        pnlHeader.Paint += (s, e) =>
        {
            var g = e.Graphics;
            int w = pnlHeader.Width;
            int h = pnlHeader.Height;

            // Header top subtle highlight
            using Pen penHighlight = new(Color.FromArgb(35, 255, 255, 255), 1);
            g.DrawLine(penHighlight, 0, 0, w, 0);

            // Bottom elevation drop-shadow
            using Pen shadow3 = new(Color.FromArgb(60, 0, 0, 0), 1);
            using Pen shadow2 = new(Color.FromArgb(120, 0, 0, 0), 1);
            using Pen shadow1 = new(Color.FromArgb(180, 0, 0, 0), 1);
            using Pen darkBorder = new(Color.FromArgb(14, 16, 22), 1);

            g.DrawLine(shadow3, 0, h - 4, w, h - 4);
            g.DrawLine(shadow2, 0, h - 3, w, h - 3);
            g.DrawLine(shadow1, 0, h - 2, w, h - 2);
            g.DrawLine(darkBorder, 0, h - 1, w, h - 1);
        };

        picAppLogo = new IconPictureBox
        {
            IconChar = IconChar.CropSimple,
            IconColor = Color.White,
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
            ForeColor = Color.White,
            Location = new Point(picAppLogo.Right + DpiScale(8), DpiScale(15)),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblAppTitle);

        FlowLayoutPanel pnlHeaderButtons = new()
        {
            Location = new Point(DpiScale(195), DpiScale(11)),
            Height = DpiScale(36),
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlHeader.Controls.Add(pnlHeaderButtons);

        btnProject = CreateHeaderButton("Dự án", IconChar.FolderTree, Color.FromArgb(0, 122, 204));
        btnProject.Click += (s, e) => ShowProjectDialog();
        pnlHeaderButtons.Controls.Add(btnProject);

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

        // Header status and Save Project (Flow right-aligned, parallel with header buttons)
        FlowLayoutPanel pnlHeaderStatus = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, DpiScale(11), DpiScale(14), 0)
        };
        pnlHeader.Controls.Add(pnlHeaderStatus);

        btnSaveProject = CreateHeaderButton("Lưu dự án", IconChar.FloppyDisk, Color.FromArgb(16, 185, 129));
        btnSaveProject.Click += (s, e) => SaveCurrentProject();
        pnlHeaderStatus.Controls.Add(btnSaveProject);

        // -------------------------------------------------------------
        // Body Container: splitMain (Top vs Bottom)
        // -------------------------------------------------------------
        splitMain = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1400), DpiScale(820)),
            Panel1MinSize = DpiScale(150),
            Panel2MinSize = DpiScale(150),
            SplitterDistance = DpiScale(500)
        };
        pnlHeaderDivider = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(3),
            BackColor = Color.FromArgb(16, 18, 24)
        };
        pnlHeaderDivider.Paint += (s, e) =>
        {
            var g = e.Graphics;
            int w = pnlHeaderDivider.Width;
            using Pen p1 = new(Color.FromArgb(10, 12, 16), 1);
            using Pen p2 = new(Color.FromArgb(20, 23, 30), 1);
            using Pen p3 = new(Color.FromArgb(28, 32, 42), 1);
            g.DrawLine(p1, 0, 0, w, 0);
            g.DrawLine(p2, 0, 1, w, 1);
            g.DrawLine(p3, 0, 2, w, 2);
        };

        this.Controls.Add(splitMain);
        this.Controls.Add(pnlHeaderDivider);
        this.Controls.Add(pnlHeader);
        pnlHeaderDivider.SendToBack();
        pnlHeader.SendToBack();

        // -------------------------------------------------------------
        // Top Panel: splitTop (Left Canvas vs Right Properties)
        // -------------------------------------------------------------
        splitTop = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1400), DpiScale(530)),
            Panel1MinSize = DpiScale(500),
            Panel2MinSize = DpiScale(360),
            FixedPanel = FixedPanel.Panel2,
            SplitterDistance = DpiScale(1030)
        };
        splitMain.Panel1.Controls.Add(splitTop);

        // Left Canvas Container
        pnlCanvasContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 33, 40) };
        pnlCanvasHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(34),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = Padding.Empty
        };

        picCanvasIcon = new IconPictureBox
        {
            IconChar = IconChar.Image,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(7)),
            BackColor = Color.Transparent
        };
        pnlCanvasHeader.Controls.Add(picCanvasIcon);

        lblCanvasTitle = new Label
        {
            Text = "Main capture",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Location = new Point(picCanvasIcon.Right + DpiScale(6), DpiScale(7)),
            AutoSize = true
        };
        pnlCanvasHeader.Controls.Add(lblCanvasTitle);

        picCanvasGuide = new IconPictureBox
        {
            IconChar = IconChar.Lightbulb,
            IconColor = Color.FromArgb(245, 185, 30),
            IconSize = DpiScale(15),
            Size = new Size(DpiScale(18), DpiScale(18)),
            Location = new Point(lblCanvasTitle.Right + DpiScale(8), DpiScale(8)),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        tipActions.SetToolTip(picCanvasGuide, "Lăn chuột: Zoom | Chuột giữa/Phải: Pan | Kéo cạnh/góc: Resize");
        pnlCanvasHeader.Controls.Add(picCanvasGuide);

        // Right side: Mouse coordinates
        lblCursorInfo = new Label
        {
            Text = "Chuột: -",
            ForeColor = Color.FromArgb(200, 215, 235),
            Font = new Font("Segoe UI", 9.5F),
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Padding = new Padding(0, DpiScale(7), DpiScale(12), 0)
        };
        pnlCanvasHeader.Controls.Add(lblCursorInfo);

        // Center: Zoom buttons (-, 100%, +)
        FlowLayoutPanel pnlCanvasZoom = new()
        {
            Size = new Size(DpiScale(110), DpiScale(26)),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        btnZoomOut = new IconButton
        {
            IconChar = IconChar.Minus,
            IconColor = Color.White,
            IconSize = DpiScale(13),
            Size = new Size(DpiScale(28), DpiScale(24)),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(54, 59, 72),
            ForeColor = Color.White,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand
        };
        btnZoomOut.FlatAppearance.BorderSize = 0;
        btnZoomOut.Click += (s, e) => canvas.ZoomOut();
        pnlCanvasZoom.Controls.Add(btnZoomOut);

        lblZoomValue = new Label
        {
            Text = "100%",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Size = new Size(DpiScale(50), DpiScale(24)),
            Margin = Padding.Empty,
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
            BackColor = Color.FromArgb(54, 59, 72),
            ForeColor = Color.White,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand
        };
        btnZoomIn.FlatAppearance.BorderSize = 0;
        btnZoomIn.Click += (s, e) => canvas.ZoomIn();
        pnlCanvasZoom.Controls.Add(btnZoomIn);

        pnlCanvasHeader.Controls.Add(pnlCanvasZoom);
        pnlCanvasZoom.BringToFront();

        void CenterCanvasZoom()
        {
            if (pnlCanvasHeader.ClientSize.Width > 0)
            {
                int zW = pnlCanvasZoom.Width > 0 ? pnlCanvasZoom.Width : DpiScale(110);
                int zH = pnlCanvasZoom.Height > 0 ? pnlCanvasZoom.Height : DpiScale(26);
                pnlCanvasZoom.Location = new Point(
                    Math.Max(DpiScale(160), (pnlCanvasHeader.ClientSize.Width - zW) / 2),
                    Math.Max(0, (pnlCanvasHeader.ClientSize.Height - zH) / 2)
                );
            }
        }
        pnlCanvasHeader.Resize += (s, e) => CenterCanvasZoom();
        CenterCanvasZoom();

        canvas = new CanvasControl { Dock = DockStyle.Fill };
        pnlCanvasContainer.Controls.Add(canvas);
        pnlCanvasContainer.Controls.Add(pnlCanvasHeader);

        lblImageInfo = new Label { Visible = false };
        canvas.ImageOverlayInfo = "Chưa nạp ảnh (Kéo & thả ảnh vào đây)";

        splitMediaCanvas = new LiveSplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(56, 61, 74),
            Size = new Size(DpiScale(1030), DpiScale(530)),
            Panel1MinSize = DpiScale(180),
            Panel2MinSize = DpiScale(200),
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = DpiScale(350)
        };
        splitTop.Panel1.Controls.Add(splitMediaCanvas);

        mediaPanel = new MediaPanelControl(_dpiScale)
        {
            Dock = DockStyle.Fill
        };
        splitMediaCanvas.Panel1.Controls.Add(mediaPanel);
        splitMediaCanvas.Panel2.Controls.Add(pnlCanvasContainer);

        // Right Panel: Properties & Adjustments (replacing Preview)
        pnlPropertiesContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 35, 42) };
        splitTop.Panel2.Controls.Add(pnlPropertiesContainer);
        InitializePropertiesPanel();

        // -------------------------------------------------------------
        // Bottom Panel: Saved Regions Grid (occupies 100% of bottom)
        // -------------------------------------------------------------
        pnlSavedSection = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 35, 42) };
        splitMain.Panel2.Controls.Add(pnlSavedSection);
        InitializeSavedSection();

        this.ResumeLayout(true);
    }
}
