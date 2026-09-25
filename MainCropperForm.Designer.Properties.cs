#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Win04_Cropper.Controls;

namespace Win04_Cropper;

partial class MainCropperForm
{
    private void InitializePropertiesPanel()
    {
        pnlPropertiesHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = DpiScale(36),
            BackColor = Color.FromArgb(42, 46, 56),
            Padding = new Padding(DpiScale(10), DpiScale(4), DpiScale(8), DpiScale(4))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesHeader);

        picCoordIcon = new IconPictureBox
        {
            IconChar = IconChar.Sliders,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            Size = new Size(DpiScale(20), DpiScale(20)),
            Location = new Point(DpiScale(10), DpiScale(8)),
            BackColor = Color.Transparent
        };
        pnlPropertiesHeader.Controls.Add(picCoordIcon);

        lblCoordSection = new Label
        {
            Text = "Properties",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.White,
            Location = new Point(picCoordIcon.Right + DpiScale(6), DpiScale(8)),
            AutoSize = true
        };
        pnlPropertiesHeader.Controls.Add(lblCoordSection);

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
            Dock = DockStyle.Right,
            Width = DpiScale(85),
            BackColor = Color.FromArgb(70, 40, 48),
            ForeColor = Color.FromArgb(255, 175, 185),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            Visible = false
        };
        btnCancelEdit.FlatAppearance.BorderSize = 0;
        btnCancelEdit.Click += (s, e) => CancelEditing();
        pnlPropertiesHeader.Controls.Add(btnCancelEdit);

        // -------------------------------------------------------------
        // Fixed bottom panel for actions (Always visible, icon-only)
        // -------------------------------------------------------------
        pnlPropertiesActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = DpiScale(44),
            BackColor = Color.FromArgb(36, 40, 50),
            Padding = new Padding(DpiScale(8), DpiScale(6), DpiScale(8), DpiScale(6))
        };
        pnlPropertiesContainer.Controls.Add(pnlPropertiesActions);

        TableLayoutPanel tlpActionButtons = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        tlpActionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        tlpActionButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pnlPropertiesActions.Controls.Add(tlpActionButtons);

        btnSaveCoordinates = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.FloppyDisk,
            IconColor = Color.White,
            IconSize = DpiScale(17),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, DpiScale(3), 0),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSaveCoordinates.FlatAppearance.BorderSize = 0;
        btnSaveCoordinates.Click += (s, e) => SaveCurrentCoordinates();
        tipActions.SetToolTip(btnSaveCoordinates, "Lưu tọa độ (Ctrl+Shift+S / Ctrl+D)");
        tlpActionButtons.Controls.Add(btnSaveCoordinates, 0, 0);

        btnCropAndSaveImage = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.Crop,
            IconColor = Color.White,
            IconSize = DpiScale(17),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(DpiScale(3), 0, DpiScale(3), 0),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCropAndSaveImage.FlatAppearance.BorderSize = 0;
        btnCropAndSaveImage.Click += (s, e) => CropAndSaveImage();
        tipActions.SetToolTip(btnCropAndSaveImage, "Cắt & Lưu ảnh (Ctrl+S)");
        tlpActionButtons.Controls.Add(btnCropAndSaveImage, 1, 0);

        btnCopyCroppedImage = new IconButton
        {
            Text = string.Empty,
            IconChar = IconChar.Copy,
            IconColor = Color.White,
            IconSize = DpiScale(16),
            ImageAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = new Padding(DpiScale(3), 0, 0, 0),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCopyCroppedImage.FlatAppearance.BorderSize = 0;
        btnCopyCroppedImage.Click += (s, e) => CopyCroppedImageToClipboard();
        tipActions.SetToolTip(btnCopyCroppedImage, "Copy ảnh vào Clipboard (Ctrl+C)");
        tlpActionButtons.Controls.Add(btnCopyCroppedImage, 2, 0);

        // -------------------------------------------------------------
        // Scrollable body (Between Header and Bottom Actions)
        // -------------------------------------------------------------
        scrollBarProperties = new SlimScrollBar(_dpiScale)
        {
            Dock = DockStyle.Right,
            Width = DpiScale(6),
            BackColor = Color.FromArgb(20, 23, 30),
            Visible = false
        };

        pnlPropertiesBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = Color.FromArgb(32, 35, 42),
            Padding = Padding.Empty
        };

        pnlPropertiesContainer.Controls.Add(pnlPropertiesBody);
        pnlPropertiesContainer.Controls.Add(scrollBarProperties);
        pnlPropertiesHeader.SendToBack();
        pnlPropertiesActions.SendToBack();

        TableLayoutPanel tlpCards = new()
        {
            Dock = DockStyle.None,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCards.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlPropertiesBody.Controls.Add(tlpCards);

        // CARD 0: TỈ LỆ / KÍCH THƯỚC
        BuildRatioAndSizeCard(tlpCards);

        // ---------------------------------------------------------
        // CARD 1: TỌA ĐỘ, KÍCH THƯỚC & DI CHUYỂN (Merged Card)
        // ---------------------------------------------------------
        Panel cardCoords = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = Padding.Empty
        };
        tlpCards.Controls.Add(cardCoords, 0, 1);

        Label lblCoordsTitle = new()
        {
            Text = "TỌA ĐỘ & KÍCH THƯỚC (px)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(5))
        };

        TableLayoutPanel tlpCoords = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, DpiScale(2), 0, 0)
        };
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tlpCoords.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tlpCoords.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpCoords.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Left Column: X (Row 0), Y (Row 1)
        lblX = CreateParamLabel("X:", 0, 0);
        lblX.Dock = DockStyle.Fill;
        lblX.TextAlign = ContentAlignment.MiddleRight;
        lblX.Margin = new Padding(0, 0, DpiScale(4), DpiScale(4));
        tlpCoords.Controls.Add(lblX, 0, 0);

        numX = CreateNumberBox(0, 0, DpiScale(75));
        numX.Dock = DockStyle.Fill;
        numX.Margin = new Padding(0, 0, DpiScale(10), DpiScale(4));
        numX.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numX, 1, 0);

        lblY = CreateParamLabel("Y:", 0, 0);
        lblY.Dock = DockStyle.Fill;
        lblY.TextAlign = ContentAlignment.MiddleRight;
        lblY.Margin = new Padding(0, 0, DpiScale(4), 0);
        tlpCoords.Controls.Add(lblY, 0, 1);

        numY = CreateNumberBox(0, 0, DpiScale(75));
        numY.Dock = DockStyle.Fill;
        numY.Margin = new Padding(0, 0, DpiScale(10), 0);
        numY.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numY, 1, 1);

        // Right Column: W (Row 0), H (Row 1)
        lblW = CreateParamLabel("W:", 0, 0);
        lblW.Dock = DockStyle.Fill;
        lblW.TextAlign = ContentAlignment.MiddleRight;
        lblW.Margin = new Padding(0, 0, DpiScale(4), DpiScale(4));
        tlpCoords.Controls.Add(lblW, 2, 0);

        numW = CreateNumberBox(0, 0, DpiScale(75));
        numW.Dock = DockStyle.Fill;
        numW.Margin = new Padding(0, 0, 0, DpiScale(4));
        numW.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numW, 3, 0);

        lblH = CreateParamLabel("H:", 0, 0);
        lblH.Dock = DockStyle.Fill;
        lblH.TextAlign = ContentAlignment.MiddleRight;
        lblH.Margin = new Padding(0, 0, DpiScale(4), 0);
        tlpCoords.Controls.Add(lblH, 2, 1);

        numH = CreateNumberBox(0, 0, DpiScale(75));
        numH.Dock = DockStyle.Fill;
        numH.Margin = Padding.Empty;
        numH.ValueChanged += (s, e) => OnNumericInputChanged();
        tlpCoords.Controls.Add(numH, 3, 1);

        // Row 1: Directional arrow pad + Center button
        FlowLayoutPanel rowNudgeControls = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, DpiScale(6), 0, DpiScale(4))
        };

        btnNudgeLeft = CreateToolButton(IconChar.ArrowLeft, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeLeft.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeLeft.Click += (s, e) => NudgeCrop(-GetNudgeStep(), 0);
        tipActions.SetToolTip(btnNudgeLeft, "Di chuyển sang trái (Mũi tên Trái)");
        rowNudgeControls.Controls.Add(btnNudgeLeft);

        btnNudgeUp = CreateToolButton(IconChar.ArrowUp, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeUp.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeUp.Click += (s, e) => NudgeCrop(0, -GetNudgeStep());
        tipActions.SetToolTip(btnNudgeUp, "Di chuyển lên trên (Mũi tên Lên)");
        rowNudgeControls.Controls.Add(btnNudgeUp);

        btnNudgeDown = CreateToolButton(IconChar.ArrowDown, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeDown.Margin = new Padding(0, 0, DpiScale(3), 0);
        btnNudgeDown.Click += (s, e) => NudgeCrop(0, GetNudgeStep());
        tipActions.SetToolTip(btnNudgeDown, "Di chuyển xuống dưới (Mũi tên Xuống)");
        rowNudgeControls.Controls.Add(btnNudgeDown);

        btnNudgeRight = CreateToolButton(IconChar.ArrowRight, 0, 0, DpiScale(28), DpiScale(28));
        btnNudgeRight.Margin = new Padding(0, 0, DpiScale(10), 0);
        btnNudgeRight.Click += (s, e) => NudgeCrop(GetNudgeStep(), 0);
        tipActions.SetToolTip(btnNudgeRight, "Di chuyển sang phải (Mũi tên Phải)");
        rowNudgeControls.Controls.Add(btnNudgeRight);

        btnCenterBox = CreateCenterButton("Căn giữa", IconChar.Bullseye, 0, 0, DpiScale(100), DpiScale(28));
        btnCenterBox.Margin = Padding.Empty;
        btnCenterBox.Click += (s, e) => CenterCropBox();
        tipActions.SetToolTip(btnCenterBox, "Căn giữa vùng chọn vào khung ảnh");
        rowNudgeControls.Controls.Add(btnCenterBox);

        // Row 2: Nudge step setting
        FlowLayoutPanel rowStepControls = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(0, 0, 0, DpiScale(4))
        };

        Label lblStep = new()
        {
            Text = "Bước di chuyển:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(170, 185, 205),
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), DpiScale(6), 0)
        };
        rowStepControls.Controls.Add(lblStep);

        cboNudgeStep = new ComboBox
        {
            Width = DpiScale(75),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = DpiScale(18),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Margin = Padding.Empty
        };
        cboNudgeStep.Items.AddRange(["1px", "5px", "10px", "50px"]);
        cboNudgeStep.SelectedIndex = 1;
        cboNudgeStep.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            using SolidBrush bg = new(Color.FromArgb(50, 55, 68));
            using SolidBrush fg = new(Color.White);
            e.Graphics.FillRectangle(bg, e.Bounds);
            StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(cboNudgeStep.Items[e.Index]?.ToString() ?? "", cboNudgeStep.Font, fg, e.Bounds, sf);
        };
        rowStepControls.Controls.Add(cboNudgeStep);

        // Stack controls in cardCoords (reverse order for Dock = Top)
        cardCoords.Controls.Add(rowStepControls);
        cardCoords.Controls.Add(rowNudgeControls);
        cardCoords.Controls.Add(tlpCoords);
        cardCoords.Controls.Add(lblCoordsTitle);

        // CARD 2: BỘ LỌC HÌNH ẢNH
        BuildFiltersCard(tlpCards);

        // Bind reusable slim scrollbar to properties body and cards
        scrollBarProperties.Bind(pnlPropertiesBody, tlpCards, horizontalPadding: DpiScale(8), topPadding: DpiScale(6));
    }
}
