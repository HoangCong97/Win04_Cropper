#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public partial class CanvasControl
{
    #region Rendering

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        // Fill background with dark checker pattern or solid dark
        DrawCheckerBackground(g);

        if (!IsImageValid())
        {
            if (_isDragOver)
            {
                DrawDragOverOverlay(g);
                return;
            }

            using Font titleFont = new("Segoe UI Semibold", 12F);
            using Font subFont = new("Segoe UI", 9.5F);
            using SolidBrush titleBrush = new(Color.FromArgb(180, 195, 215));
            using SolidBrush subBrush = new(Color.FromArgb(120, 135, 155));

            string mainHint = "Chưa nạp ảnh";
            string subHint = "Kéo ảnh từ Media vào đây hoặc nhấn vào ảnh trong Media\n(F9: Chụp Live | Ctrl+V: Dán ảnh)";

            SizeF mainSize = g.MeasureString(mainHint, titleFont);
            SizeF subSize = g.MeasureString(subHint, subFont);

            float totalH = mainSize.Height + 8 + subSize.Height;
            float startY = (Height - totalH) / 2f;

            g.DrawString(mainHint, titleFont, titleBrush, (Width - mainSize.Width) / 2f, startY);
            using StringFormat sf = new() { Alignment = StringAlignment.Center };
            g.DrawString(subHint, subFont, subBrush, new RectangleF(20, startY + mainSize.Height + 8, Width - 40, subSize.Height + 10), sf);
            return;
        }

        try
        {
            // Set pixel interpolation based on zoom
            if (_zoomFactor >= 2.0f)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
            }
            else
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            }

            // Render Image
            float imgScreenW = _image!.Width * _zoomFactor;
            float imgScreenH = _image!.Height * _zoomFactor;
            RectangleF imgScreenRect = new(_panOffset.X, _panOffset.Y, imgScreenW, imgScreenH);
            g.DrawImage(_image!, imgScreenRect);

            // Optional Pixel Grid at extreme zoom
            if (_zoomFactor >= 8.0f)
            {
                DrawPixelGrid(g, imgScreenRect);
            }

            // Image boundary border
            using (Pen imgBorderPen = new(Color.FromArgb(80, 255, 255, 255), 1))
            {
                g.DrawRectangle(imgBorderPen, imgScreenRect.X, imgScreenRect.Y, imgScreenRect.Width, imgScreenRect.Height);
            }

            // Draw CapCut-style Crop Box
            DrawCapCutCropBox(g);

            // Draw Image Info Overlay at bottom-left corner with true GDI+ semi-transparency
            DrawImageInfoOverlay(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Canvas render error: {ex.Message}");
        }

        if (_isDragOver)
        {
            DrawDragOverOverlay(g);
        }
    }

    private void DrawDragOverOverlay(Graphics g)
    {
        using SolidBrush overlayBrush = new(Color.FromArgb(70, 0, 168, 255));
        g.FillRectangle(overlayBrush, 10, 10, Width - 20, Height - 20);

        using Pen dashPen = new(Color.FromArgb(0, 220, 255), 2.5f) { DashStyle = DashStyle.Dash };
        g.DrawRectangle(dashPen, 10, 10, Width - 20, Height - 20);

        using Font dragFont = new("Segoe UI Bold", 13.5F);
        using SolidBrush textBrush = new(Color.White);
        string dropText = "Thả ảnh vào đây để nạp vào Main Capture";
        SizeF textSize = g.MeasureString(dropText, dragFont);
        g.DrawString(dropText, dragFont, textBrush, (Width - textSize.Width) / 2f, (Height - textSize.Height) / 2f);
    }

    private void DrawCheckerBackground(Graphics g)
    {
        g.Clear(Color.FromArgb(18, 20, 26));

        // Subtle dot grid or checkered tiles
        int gridSize = 24;
        using SolidBrush dotBrush = new(Color.FromArgb(28, 32, 42));
        for (int y = 0; y < Height; y += gridSize)
        {
            for (int x = 0; x < Width; x += gridSize)
            {
                if ((x / gridSize + y / gridSize) % 2 == 0)
                {
                    g.FillRectangle(dotBrush, x, y, gridSize, gridSize);
                }
            }
        }
    }

    private void DrawPixelGrid(Graphics g, RectangleF imgScreenRect)
    {
        using Pen gridPen = new(Color.FromArgb(30, 255, 255, 255), 1);
        for (float x = imgScreenRect.Left; x <= imgScreenRect.Right; x += _zoomFactor)
        {
            g.DrawLine(gridPen, x, Math.Max(0, imgScreenRect.Top), x, Math.Min(Height, imgScreenRect.Bottom));
        }
        for (float y = imgScreenRect.Top; y <= imgScreenRect.Bottom; y += _zoomFactor)
        {
            g.DrawLine(gridPen, Math.Max(0, imgScreenRect.Left), y, Math.Min(Width, imgScreenRect.Right), y);
        }
    }

    private void DrawCapCutCropBox(Graphics g)
    {
        RectangleF box = ImageToScreen(_cropRect);

        // 1. Draw Outside Mask (Dim the area outside crop box)
        using (SolidBrush maskBrush = new(MaskColor))
        {
            // Top
            if (box.Top > 0)
                g.FillRectangle(maskBrush, 0, 0, Width, box.Top);
            // Bottom
            if (box.Bottom < Height)
                g.FillRectangle(maskBrush, 0, box.Bottom, Width, Height - box.Bottom);
            // Left
            if (box.Left > 0)
                g.FillRectangle(maskBrush, 0, Math.Max(0, box.Top), box.Left, Math.Min(box.Height, Height - Math.Max(0, box.Top)));
            // Right
            if (box.Right < Width)
                g.FillRectangle(maskBrush, box.Right, Math.Max(0, box.Top), Width - box.Right, Math.Min(box.Height, Height - Math.Max(0, box.Top)));
        }

        // 2. Rule of Thirds grid lines inside crop box
        using (Pen gridPen = new(GridColor, 1) { DashStyle = DashStyle.Dash })
        {
            float stepX = box.Width / 3f;
            float stepY = box.Height / 3f;
            g.DrawLine(gridPen, box.Left + stepX, box.Top, box.Left + stepX, box.Bottom);
            g.DrawLine(gridPen, box.Left + stepX * 2, box.Top, box.Left + stepX * 2, box.Bottom);
            g.DrawLine(gridPen, box.Left, box.Top + stepY, box.Right, box.Top + stepY);
            g.DrawLine(gridPen, box.Left, box.Top + stepY * 2, box.Right, box.Top + stepY * 2);
        }

        // 3. Main border
        using (Pen borderPen = new(AccentColor, 1.5f))
        {
            g.DrawRectangle(borderPen, box.X, box.Y, box.Width, box.Height);
        }

        // 4. CapCut Handles (Thick L-corner brackets & mid-edge bars)
        DrawCapCutHandles(g, box);

        // 5. Floating HUD Badge (shows current X, Y, W, H coordinates above box)
        DrawHudBadge(g, box);
    }

    private void DrawCapCutHandles(Graphics g, RectangleF box)
    {
        float L = Math.Min(18f, Math.Min(box.Width / 3f, box.Height / 3f)); // Length of corner bracket
        float T = 3.5f; // Thickness of handle

        Color cornerColor = (_hoverHandle is DragHandle.TopLeft or DragHandle.TopRight or DragHandle.BottomLeft or DragHandle.BottomRight)
            ? HandleHoverColor : AccentColor;

        Color edgeColor = (_hoverHandle is DragHandle.Left or DragHandle.Right or DragHandle.Top or DragHandle.Bottom)
            ? HandleHoverColor : Color.White;

        using SolidBrush cornerBrush = new(cornerColor);
        using SolidBrush edgeBrush = new(edgeColor);

        // Top-Left corner (L-bracket)
        g.FillRectangle(cornerBrush, box.Left - T, box.Top - T, L + T, T);
        g.FillRectangle(cornerBrush, box.Left - T, box.Top - T, T, L + T);

        // Top-Right corner
        g.FillRectangle(cornerBrush, box.Right - L, box.Top - T, L + T, T);
        g.FillRectangle(cornerBrush, box.Right, box.Top - T, T, L + T);

        // Bottom-Left corner
        g.FillRectangle(cornerBrush, box.Left - T, box.Bottom, L + T, T);
        g.FillRectangle(cornerBrush, box.Left - T, box.Bottom - L, T, L + T);

        // Bottom-Right corner
        g.FillRectangle(cornerBrush, box.Right - L, box.Bottom, L + T, T);
        g.FillRectangle(cornerBrush, box.Right, box.Bottom - L, T, L + T);

        // Edge Grab Bars (horizontal/vertical pills in middle of edges)
        float barLen = Math.Min(22f, Math.Max(8f, box.Width / 4f));
        float barH = T;

        // Top edge bar
        g.FillRectangle(edgeBrush, box.Left + (box.Width - barLen) / 2f, box.Top - T / 2f, barLen, barH);
        // Bottom edge bar
        g.FillRectangle(edgeBrush, box.Left + (box.Width - barLen) / 2f, box.Bottom - T / 2f, barLen, barH);

        // Left edge bar
        float vBarLen = Math.Min(22f, Math.Max(8f, box.Height / 4f));
        g.FillRectangle(edgeBrush, box.Left - T / 2f, box.Top + (box.Height - vBarLen) / 2f, barH, vBarLen);
        // Right edge bar
        g.FillRectangle(edgeBrush, box.Right - T / 2f, box.Top + (box.Height - vBarLen) / 2f, barH, vBarLen);
    }

    private void DrawHudBadge(Graphics g, RectangleF box)
    {
        string badgeText = $"X:{_cropRect.X} Y:{_cropRect.Y}  |  {_cropRect.Width}×{_cropRect.Height} px";
        using Font font = new("Segoe UI Semibold", 9.5F);
        SizeF size = g.MeasureString(badgeText, font);

        float badgeW = size.Width + 14;
        float badgeH = size.Height + 6;
        float badgeX = box.Left;
        float badgeY = box.Top - badgeH - 6;

        // Flip below if outside top view
        if (badgeY < 4)
        {
            badgeY = box.Bottom + 6;
        }

        badgeX = Math.Clamp(badgeX, 4, Math.Max(4, Width - badgeW - 4));

        using (GraphicsPath path = GetRoundedRect(new RectangleF(badgeX, badgeY, badgeW, badgeH), 4))
        {
            using SolidBrush bg = new(Color.FromArgb(220, 24, 28, 36));
            using Pen border = new(Color.FromArgb(120, 0, 220, 255), 1);
            using SolidBrush textBrush = new(Color.FromArgb(240, 245, 255));

            g.FillPath(bg, path);
            g.DrawPath(border, path);
            g.DrawString(badgeText, font, textBrush, badgeX + 7, badgeY + 3);
        }
    }

    private void DrawImageInfoOverlay(Graphics g)
    {
        if (string.IsNullOrEmpty(_imageOverlayInfo)) return;

        using Font font = new("Segoe UI Semibold", 9.5F); // >= 12px
        SizeF textSize = g.MeasureString(_imageOverlayInfo, font);

        float padX = 10;
        float padY = 4;
        float boxW = textSize.Width + padX * 2;
        float boxH = textSize.Height + padY * 2;
        float boxX = 6;
        float boxY = Height - boxH - 6;

        if (boxY < 0 || boxW <= 0 || boxH <= 0) return;

        RectangleF boxRect = new(boxX, boxY, boxW, boxH);

        // True GDI+ semi-transparent glassmorphism overlay!
        using (GraphicsPath path = GetRoundedRect(boxRect, 6))
        {
            using SolidBrush bgBrush = new(Color.FromArgb(140, 16, 20, 28));
            g.FillPath(bgBrush, path);

            using Pen borderPen = new(Color.FromArgb(50, 255, 255, 255), 1);
            g.DrawPath(borderPen, path);
        }

        using SolidBrush shadowBrush = new(Color.FromArgb(160, 0, 0, 0));
        using SolidBrush textBrush = new(Color.FromArgb(235, 240, 250));

        g.DrawString(_imageOverlayInfo, font, shadowBrush, boxX + padX + 1, boxY + padY + 1);
        g.DrawString(_imageOverlayInfo, font, textBrush, boxX + padX, boxY + padY);
    }

    private static GraphicsPath GetRoundedRect(RectangleF rect, float radius)
    {
        GraphicsPath path = new();
        float d = radius * 2f;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    #endregion
}
