using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win04_Cropper.Services;

public static class ScreenCaptureService
{
    /// <summary>
    /// Captures the primary screen or all virtual screens.
    /// If hideForm is provided, hides the form temporarily so it's not captured.
    /// </summary>
    public static async Task<Bitmap?> CaptureScreenAsync(Form? hideForm = null, bool allScreens = true, int delayMs = 150)
    {
        bool wasVisible = hideForm != null && hideForm.Visible;
        if (wasVisible && hideForm != null)
        {
            hideForm.Opacity = 0;
            await Task.Delay(delayMs);
        }

        Bitmap? bmp = null;
        try
        {
            Rectangle bounds;
            if (allScreens)
            {
                bounds = SystemInformation.VirtualScreen;
            }
            else
            {
                bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            }

            bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi chụp màn hình: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (wasVisible && hideForm != null)
            {
                hideForm.Opacity = 1.0;
                hideForm.BringToFront();
                hideForm.Activate();
            }
        }

        return bmp;
    }

    /// <summary>
    /// Captures the current foreground window (excluding our form).
    /// </summary>
    public static async Task<Bitmap?> CaptureForegroundWindowAsync(Form? hideForm = null, int delayMs = 150)
    {
        bool wasVisible = hideForm != null && hideForm.Visible;
        if (wasVisible && hideForm != null)
        {
            hideForm.Opacity = 0;
            await Task.Delay(delayMs);
        }

        Bitmap? bmp = null;
        try
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd != IntPtr.Zero && NativeMethods.GetWindowRect(hwnd, out var rect))
            {
                int w = Math.Max(1, rect.Width);
                int h = Math.Max(1, rect.Height);
                bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
                }
            }
            else
            {
                // Fallback to screen capture
                return await CaptureScreenAsync(null, true, 0);
            }
        }
        catch
        {
            return await CaptureScreenAsync(null, true, 0);
        }
        finally
        {
            if (wasVisible && hideForm != null)
            {
                hideForm.Opacity = 1.0;
                hideForm.BringToFront();
                hideForm.Activate();
            }
        }

        return bmp;
    }
}
