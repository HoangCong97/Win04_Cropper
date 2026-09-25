#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper;

partial class MainCropperForm
{
    private void BuildRatioAndSizeCard(TableLayoutPanel tlpCards)
    {
        // ---------------------------------------------------------
        // CARD 0: TỈ LỆ / KÍCH THƯỚC (Aspect Ratios & Dimensions)
        // ---------------------------------------------------------
        Panel cardRatioAndSize = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpCards.Controls.Add(cardRatioAndSize, 0, 0);

        Label lblRatioAndSizeTitle = new()
        {
            Text = "TỈ LỆ / KÍCH THƯỚC",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };

        // --- SECTION 1: TỈ LỆ ---
        Label lblRatioTitle = new()
        {
            Text = "Tỉ lệ:",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), 0, DpiScale(3))
        };

        tlpRatios = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpRatios.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));
        tlpRatios.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));

        // Section 1 Row 1: Tự do, 3:4, 4:6, 9:16
        btnRatioFree = CreatePresetButton("Tự do");
        btnRatioFree.Click += (s, e) => ApplyAspectRatio(0, 0, btnRatioFree);
        tlpRatios.Controls.Add(btnRatioFree, 0, 0);

        btnRatio3x4 = CreatePresetButton("3:4");
        btnRatio3x4.Click += (s, e) => ApplyAspectRatio(3, 4, btnRatio3x4);
        tlpRatios.Controls.Add(btnRatio3x4, 1, 0);

        btnRatio4x6 = CreatePresetButton("4:6");
        btnRatio4x6.Click += (s, e) => ApplyAspectRatio(4, 6, btnRatio4x6);
        tlpRatios.Controls.Add(btnRatio4x6, 2, 0);

        btnRatio9x16 = CreatePresetButton("9:16");
        btnRatio9x16.Click += (s, e) => ApplyAspectRatio(9, 16, btnRatio9x16);
        tlpRatios.Controls.Add(btnRatio9x16, 3, 0);

        // Section 1 Row 2: 1:1, 4:3, 6:4, 16:9
        btnRatio1x1 = CreatePresetButton("1:1");
        btnRatio1x1.Click += (s, e) => ApplyAspectRatio(1, 1, btnRatio1x1);
        tlpRatios.Controls.Add(btnRatio1x1, 0, 1);

        btnRatio4x3 = CreatePresetButton("4:3");
        btnRatio4x3.Click += (s, e) => ApplyAspectRatio(4, 3, btnRatio4x3);
        tlpRatios.Controls.Add(btnRatio4x3, 1, 1);

        btnRatio6x4 = CreatePresetButton("6:4");
        btnRatio6x4.Click += (s, e) => ApplyAspectRatio(6, 4, btnRatio6x4);
        tlpRatios.Controls.Add(btnRatio6x4, 2, 1);

        btnRatio16x9 = CreatePresetButton("16:9");
        btnRatio16x9.Click += (s, e) => ApplyAspectRatio(16, 9, btnRatio16x9);
        tlpRatios.Controls.Add(btnRatio16x9, 3, 1);

        // --- SECTION 2: KÍCH THƯỚC (PX) ---
        Label lblSizeTitle = new()
        {
            Text = "Kích thước (px):",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, DpiScale(4), 0, DpiScale(3))
        };

        tlpScreenSizes = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        tlpScreenSizes.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));
        tlpScreenSizes.RowStyles.Add(new RowStyle(SizeType.Absolute, DpiScale(28)));

        // Section 2 Row 1 (Col 0..3): Toàn bộ, 1024x768, 1280x720, 1366x768
        btnResAll = CreatePresetButton("Toàn bộ");
        btnResAll.Click += (s, e) => ApplyFullImageResolution();
        tlpScreenSizes.Controls.Add(btnResAll, 0, 0);

        btnRes1024x768 = CreatePresetButton("1024x768");
        btnRes1024x768.Click += (s, e) => ApplyScreenResolution(1024, 768);
        tlpScreenSizes.Controls.Add(btnRes1024x768, 1, 0);

        btnRes1280x720 = CreatePresetButton("1280x720");
        btnRes1280x720.Click += (s, e) => ApplyScreenResolution(1280, 720);
        tlpScreenSizes.Controls.Add(btnRes1280x720, 2, 0);

        btnRes1366x768 = CreatePresetButton("1366x768");
        btnRes1366x768.Click += (s, e) => ApplyScreenResolution(1366, 768);
        tlpScreenSizes.Controls.Add(btnRes1366x768, 3, 0);

        // Section 2 Row 2 (Col 0..3): 1440x900, 1600x900, 1920x1080, 2560x1440
        btnRes1440x900 = CreatePresetButton("1440x900");
        btnRes1440x900.Click += (s, e) => ApplyScreenResolution(1440, 900);
        tlpScreenSizes.Controls.Add(btnRes1440x900, 0, 1);

        btnRes1600x900 = CreatePresetButton("1600x900");
        btnRes1600x900.Click += (s, e) => ApplyScreenResolution(1600, 900);
        tlpScreenSizes.Controls.Add(btnRes1600x900, 1, 1);

        btnRes1920x1080 = CreatePresetButton("1920x1080");
        btnRes1920x1080.Click += (s, e) => ApplyScreenResolution(1920, 1080);
        tlpScreenSizes.Controls.Add(btnRes1920x1080, 2, 1);

        btnRes2560x1440 = CreatePresetButton("2560x1440");
        btnRes2560x1440.Click += (s, e) => ApplyScreenResolution(2560, 1440);
        tlpScreenSizes.Controls.Add(btnRes2560x1440, 3, 1);

        // Stack controls in cardRatioAndSize (reverse order for Dock = Top)
        cardRatioAndSize.Controls.Add(tlpScreenSizes);
        cardRatioAndSize.Controls.Add(lblSizeTitle);
        cardRatioAndSize.Controls.Add(tlpRatios);
        cardRatioAndSize.Controls.Add(lblRatioTitle);
        cardRatioAndSize.Controls.Add(lblRatioAndSizeTitle);
    }

    private void BuildFiltersCard(TableLayoutPanel tlpCards)
    {
        // ---------------------------------------------------------
        // CARD 2: BỘ LỌC HÌNH ẢNH (Filters: Grayscale & Threshold)
        // ---------------------------------------------------------
        cardFilters = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(40, 44, 54),
            Padding = new Padding(DpiScale(10), DpiScale(7), DpiScale(10), DpiScale(8)),
            Margin = Padding.Empty
        };
        tlpCards.Controls.Add(cardFilters, 0, 2);

        Label lblFilterTitle = new()
        {
            Text = "BỘ LỌC HÌNH ẢNH",
            UseMnemonic = false,
            Font = new Font("Segoe UI Bold", 9F),
            ForeColor = Color.FromArgb(170, 185, 205),
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(6))
        };

        // Checkbox 1: Ảnh đen trắng (Grayscale)
        chkGrayscale = new CheckBox
        {
            Text = "Ảnh đen trắng (Grayscale)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, DpiScale(5))
        };
        chkGrayscale.CheckedChanged += (s, e) => OnGrayscaleFilterToggled();

        // Checkbox 2: Ngưỡng nhị phân (Threshold)
        chkThreshold = new CheckBox
        {
            Text = "Ngưỡng nhị phân (Threshold)",
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            AutoSize = true,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, DpiScale(4))
        };
        chkThreshold.CheckedChanged += (s, e) => OnThresholdFilterToggled();

        // Threshold Controls Panel
        pnlThresholdControls = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent,
            Enabled = false,
            Margin = Padding.Empty,
            Padding = new Padding(DpiScale(2), DpiScale(2), DpiScale(2), DpiScale(2))
        };

        TableLayoutPanel tlpThreshHeader = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, DpiScale(2))
        };
        tlpThreshHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        tlpThreshHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

        lblThresholdVal = new Label
        {
            Text = "Điểm ngưỡng: 128",
            Font = new Font("Segoe UI Semibold", 8.5F),
            ForeColor = Color.FromArgb(180, 210, 240),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = true
        };
        tlpThreshHeader.Controls.Add(lblThresholdVal, 0, 0);

        btnResetThreshold = new Button
        {
            Text = "Mặc định (128)",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(200, 215, 230),
            BackColor = Color.FromArgb(50, 55, 68),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
            Width = DpiScale(88),
            Height = DpiScale(22)
        };
        btnResetThreshold.FlatAppearance.BorderSize = 0;
        btnResetThreshold.Click += (s, e) =>
        {
            trkThreshold.Value = 128;
            OnThresholdValueChanged();
        };
        tlpThreshHeader.Controls.Add(btnResetThreshold, 1, 0);

        trkThreshold = new TrackBar
        {
            Dock = DockStyle.Top,
            Minimum = 0,
            Maximum = 255,
            Value = 128,
            TickFrequency = 32,
            TickStyle = TickStyle.None,
            Height = DpiScale(26),
            BackColor = Color.FromArgb(40, 44, 54),
            Cursor = Cursors.Hand
        };
        trkThreshold.ValueChanged += (s, e) => OnThresholdValueChanged();

        Label lblThresholdHint = new()
        {
            Text = "Điểm ảnh < Ngưỡng: Đen | ≥ Ngưỡng: Trắng",
            Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
            ForeColor = Color.FromArgb(140, 155, 175),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DpiScale(2))
        };

        pnlThresholdControls.Controls.Add(lblThresholdHint);
        pnlThresholdControls.Controls.Add(trkThreshold);
        pnlThresholdControls.Controls.Add(tlpThreshHeader);

        // Stack controls in cardFilters (reverse order for Dock = Top)
        cardFilters.Controls.Add(pnlThresholdControls);
        cardFilters.Controls.Add(chkThreshold);
        cardFilters.Controls.Add(chkGrayscale);
        cardFilters.Controls.Add(lblFilterTitle);
    }
}

