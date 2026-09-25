#nullable enable
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper;

partial class MainCropperForm
{
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
            BackColor = backColor ?? Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
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
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.White
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
            BackColor = Color.FromArgb(50, 55, 68),
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
            IconColor = iconColor ?? Color.White,
            IconSize = DpiScale(14),
            ImageAlign = ContentAlignment.MiddleCenter,
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
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
            IconColor = Color.White,
            IconSize = DpiScale(13),
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(DpiScale(6), 0, DpiScale(6), 0),
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F),
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
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private Button CreatePresetButton(string text)
    {
        Button btn = new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = DpiScale(26),
            Margin = new Padding(DpiScale(2)),
            Padding = Padding.Empty,
            BackColor = Color.FromArgb(50, 55, 68),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand,
            UseCompatibleTextRendering = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    #endregion
}
