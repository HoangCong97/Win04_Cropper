using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public class PreviewControl : UserControl
{
    private Bitmap? _sourceImage;
    private Rectangle _cropRect;
    private Bitmap? _croppedBitmap;
    private bool _usePixelInterpolation = true;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool UsePixelInterpolation
    {
        get => _usePixelInterpolation;
        set
        {
            _usePixelInterpolation = value;
            Invalidate();
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Bitmap? CroppedBitmap => _croppedBitmap;

    public PreviewControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        BackColor = Color.FromArgb(24, 26, 32);
    }

    public void UpdateCrop(Bitmap? source, Rectangle cropRect)
    {
        _sourceImage = source;
        _cropRect = cropRect;

        _croppedBitmap?.Dispose();
        _croppedBitmap = null;

        if (_sourceImage != null && cropRect.Width > 0 && cropRect.Height > 0)
        {
            try
            {
                int imgW = _sourceImage.Width;
                int imgH = _sourceImage.Height;

                int x = Math.Clamp(cropRect.X, 0, Math.Max(0, imgW - 1));
                int y = Math.Clamp(cropRect.Y, 0, Math.Max(0, imgH - 1));
                int w = Math.Clamp(cropRect.Width, 1, imgW - x);
                int h = Math.Clamp(cropRect.Height, 1, imgH - y);

                Rectangle validRect = new(x, y, w, h);
                _croppedBitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(_croppedBitmap))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(_sourceImage, new Rectangle(0, 0, w, h), validRect, GraphicsUnit.Pixel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview crop failed: {ex.Message}");
            }
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        // Draw dark checkerboard background
        DrawCheckerBackground(g);

        if (_croppedBitmap == null)
        {
            using Font hintFont = new("Segoe UI", 10F, FontStyle.Italic);
            using SolidBrush hintBrush = new(Color.FromArgb(120, 130, 150));
            string noImageText = "Chưa có hình xem trước";
            SizeF textSize = g.MeasureString(noImageText, hintFont);
            g.DrawString(noImageText, hintFont, hintBrush, (Width - textSize.Width) / 2f, (Height - textSize.Height) / 2f);
            return;
        }

        // Calculate fit scale (auto zoom in or zoom out to fit perfectly inside panel)
        float padding = 20f;
        float availableW = Math.Max(10f, Width - padding * 2);
        float availableH = Math.Max(10f, Height - padding * 2 - 30); // 30px for bottom info bar

        float scaleX = availableW / _croppedBitmap.Width;
        float scaleY = availableH / _croppedBitmap.Height;
        float fitScale = Math.Min(scaleX, scaleY);

        float renderW = _croppedBitmap.Width * fitScale;
        float renderH = _croppedBitmap.Height * fitScale;

        float renderX = (Width - renderW) / 2f;
        float renderY = padding + (availableH - renderH) / 2f;

        RectangleF renderRect = new(renderX, renderY, renderW, renderH);

        // Drawing interpolation
        if (_usePixelInterpolation || fitScale >= 1.5f)
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
        }
        else
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        // Drop shadow for the preview image
        using (SolidBrush shadow = new(Color.FromArgb(80, 0, 0, 0)))
        {
            g.FillRectangle(shadow, renderX + 4, renderY + 4, renderW, renderH);
        }

        // Draw cropped image
        g.DrawImage(_croppedBitmap, renderRect);

        // Border around preview
        using (Pen borderPen = new(Color.FromArgb(70, 80, 100), 1.5f))
        {
            g.DrawRectangle(borderPen, renderRect.X, renderRect.Y, renderRect.Width, renderRect.Height);
        }

        // Draw bottom info badge
        string infoText = $"{_croppedBitmap.Width} × {_croppedBitmap.Height} px  |  Tỉ lệ: {GetRatioStr(_croppedBitmap.Width, _croppedBitmap.Height)}  |  Fit: {Math.Round(fitScale * 100)}%";
        using Font infoFont = new("Segoe UI Semibold", 9F);
        using SolidBrush barBg = new(Color.FromArgb(200, 16, 18, 24));
        using SolidBrush barText = new(Color.FromArgb(220, 230, 245));

        RectangleF infoBarRect = new(10, Height - 28, Width - 20, 22);
        g.FillRectangle(barBg, infoBarRect);
        using (Pen p = new(Color.FromArgb(50, 60, 80)))
        {
            g.DrawRectangle(p, infoBarRect.X, infoBarRect.Y, infoBarRect.Width, infoBarRect.Height);
        }

        SizeF infoSize = g.MeasureString(infoText, infoFont);
        g.DrawString(infoText, infoFont, barText, (Width - infoSize.Width) / 2f, Height - 25);
    }

    private static string GetRatioStr(int w, int h)
    {
        if (h <= 0) return "-";
        int gcd = GetGcd(w, h);
        return $"{w / gcd}:{h / gcd}";
    }

    private static int GetGcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a == 0 ? 1 : a;
    }

    private void DrawCheckerBackground(Graphics g)
    {
        g.Clear(Color.FromArgb(20, 22, 28));

        int tileSize = 16;
        using SolidBrush darkTile = new(Color.FromArgb(26, 29, 38));
        for (int y = 0; y < Height; y += tileSize)
        {
            for (int x = 0; x < Width; x += tileSize)
            {
                if ((x / tileSize + y / tileSize) % 2 == 0)
                {
                    g.FillRectangle(darkTile, x, y, tileSize, tileSize);
                }
            }
        }
    }
}
