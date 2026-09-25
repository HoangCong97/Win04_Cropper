#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public partial class MediaPanelControl
{
    private static void DrawImageCover(Graphics g, Image img, Rectangle destRect)
    {
        if (destRect.Width <= 0 || destRect.Height <= 0 || img.Width <= 0 || img.Height <= 0) return;

        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.SmoothingMode = SmoothingMode.HighQuality;

        float scale = Math.Max((float)destRect.Width / img.Width, (float)destRect.Height / img.Height);
        float srcW = destRect.Width / scale;
        float srcH = destRect.Height / scale;
        float srcX = Math.Max(0f, (img.Width - srcW) / 2f);
        float srcY = Math.Max(0f, (img.Height - srcH) / 2f);
        srcW = Math.Min(img.Width - srcX, srcW);
        srcH = Math.Min(img.Height - srcY, srcH);

        RectangleF srcRect = new(srcX, srcY, srcW, srcH);
        g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
    }
}
