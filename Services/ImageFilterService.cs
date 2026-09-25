#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Win04_Cropper.Services;

/// <summary>
/// Provides high-performance image processing filters such as Grayscale conversion
/// and Binary Thresholding (Binarization) using direct memory manipulation.
/// </summary>
public static class ImageFilterService
{
    /// <summary>
    /// Applies Grayscale and/or Binary Thresholding filters to a source Bitmap.
    /// Returns a new 32bppArgb Bitmap, or null if no filter is active.
    /// </summary>
    /// <param name="src">The source bitmap (never modified).</param>
    /// <param name="grayscale">True to convert image to grayscale.</param>
    /// <param name="thresholdEnabled">True to apply binary thresholding (pixels &lt; threshold become black, &gt;= threshold become white).</param>
    /// <param name="threshold">Threshold value in the range [0..255]. Default is typically 128.</param>
    /// <returns>A new Bitmap with the filters applied, or null if neither filter is enabled.</returns>
    public static Bitmap? ApplyFilters(Bitmap? src, bool grayscale, bool thresholdEnabled, int threshold)
    {
        if (src == null) return null;
        if (!grayscale && !thresholdEnabled) return null;

        int w = src.Width;
        int h = src.Height;
        if (w <= 0 || h <= 0) return null;

        threshold = Math.Clamp(threshold, 0, 255);

        // Always work with Format32bppArgb for predictable, lightning-fast 4-byte pixel strides
        Bitmap dst = new(w, h, PixelFormat.Format32bppArgb);

        BitmapData srcData = src.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        BitmapData dstData = dst.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                for (int y = 0; y < h; y++)
                {
                    byte* srcRow = srcPtr + y * srcStride;
                    byte* dstRow = dstPtr + y * dstStride;

                    for (int x = 0; x < w; x++)
                    {
                        int px = x * 4;
                        byte b = srcRow[px];
                        byte g = srcRow[px + 1];
                        byte r = srcRow[px + 2];
                        byte a = srcRow[px + 3];

                        // Standard Rec. 601 luminance weighting: 0.299*R + 0.587*G + 0.114*B
                        int lum = (r * 299 + g * 587 + b * 114) / 1000;

                        if (thresholdEnabled)
                        {
                            // "các điểm ảnh sẽ so sánh với điểm này, nếu bé hơn thì là đen, nếu lớn hơn thì là trắng"
                            byte val = (lum >= threshold) ? (byte)255 : (byte)0;
                            dstRow[px] = val;
                            dstRow[px + 1] = val;
                            dstRow[px + 2] = val;
                            dstRow[px + 3] = a;
                        }
                        else
                        {
                            // Grayscale
                            byte gray = (byte)Math.Clamp(lum, 0, 255);
                            dstRow[px] = gray;
                            dstRow[px + 1] = gray;
                            dstRow[px + 2] = gray;
                            dstRow[px + 3] = a;
                        }
                    }
                }
            }
        }
        finally
        {
            src.UnlockBits(srcData);
            dst.UnlockBits(dstData);
        }

        return dst;
    }
}
