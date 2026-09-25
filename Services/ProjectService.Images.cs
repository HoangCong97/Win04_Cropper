using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Win04_Cropper.Services;

public static partial class ProjectService
{
    public static string GenerateThumbnailBase64(Image image, int targetW = 240, int targetH = 150)
    {
        try
        {
            using var thumb = new Bitmap(targetW, targetH);
            using (var g = Graphics.FromImage(thumb))
            {
                g.Clear(Color.FromArgb(28, 31, 38));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float ratioSrc = (float)image.Width / image.Height;
                float ratioDst = (float)targetW / targetH;

                int drawW, drawH;
                if (ratioSrc > ratioDst)
                {
                    drawW = targetW;
                    drawH = (int)Math.Round(targetW / ratioSrc);
                }
                else
                {
                    drawH = targetH;
                    drawW = (int)Math.Round(targetH * ratioSrc);
                }

                int x = (targetW - drawW) / 2;
                int y = (targetH - drawH) / 2;
                g.DrawImage(image, x, y, drawW, drawH);
            }

            using var ms = new MemoryStream();
            thumb.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to generate thumbnail: {ex.Message}");
            return string.Empty;
        }
    }

    public static Image? ImageFromBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            return Image.FromStream(ms);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to decode image from Base64: {ex.Message}");
            return null;
        }
    }

    public static Bitmap? BitmapFromBase64(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            using var temp = Image.FromStream(ms);
            return new Bitmap(temp);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to decode bitmap from Base64: {ex.Message}");
            return null;
        }
    }

    public static string ImageToBase64(Image image, ImageFormat? format = null)
    {
        try
        {
            using var ms = new MemoryStream();
            image.Save(ms, format ?? ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to encode image to Base64: {ex.Message}");
            return string.Empty;
        }
    }
}
