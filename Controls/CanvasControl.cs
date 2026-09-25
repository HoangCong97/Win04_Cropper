using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public enum DragHandle
{
    None,
    Inside,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    DrawNew
}

public class CanvasControl : UserControl
{
    private Bitmap? _image;
    private Rectangle _cropRect = new(50, 50, 200, 150);

    // Zoom and Pan
    private float _zoomFactor = 1.0f;
    private PointF _panOffset = new(20, 20);

    // Mouse Interaction State
    private bool _isPanning;
    private Point _lastMousePos;
    private DragHandle _activeHandle = DragHandle.None;
    private DragHandle _hoverHandle = DragHandle.None;
    private Point _dragStartMouse;
    private Rectangle _dragStartCropRect;

    // Drawing settings
    private const int HandleTolerance = 10;
    private static readonly Color MaskColor = Color.FromArgb(150, 12, 16, 24);
    private static readonly Color AccentColor = Color.FromArgb(0, 220, 255); // Cyan
    private static readonly Color HandleHoverColor = Color.FromArgb(255, 230, 0); // Yellow
    private static readonly Color GridColor = Color.FromArgb(60, 255, 255, 255);

    // Events
    public event Action<Rectangle>? CropRectChanged;
    public event Action<Point, Color?>? CursorMovedOnImage;
    public event Action<float>? ZoomChanged;

    /// <summary>
    /// When set to a value (e.g. 1.0f for 1:1, 16f/9f, etc.), forces the crop rectangle to preserve this aspect ratio during resizing.
    /// Null means free aspect ratio.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public float? LockedAspectRatio { get; set; } = null;

    private string? _imageOverlayInfo;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string? ImageOverlayInfo
    {
        get => _imageOverlayInfo;
        set
        {
            if (_imageOverlayInfo != value)
            {
                _imageOverlayInfo = value;
                Invalidate();
            }
        }
    }

    public CanvasControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        BackColor = Color.FromArgb(20, 22, 28);
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Bitmap? Image
    {
        get => _image;
        set
        {
            _image = value;
            if (_image != null)
            {
                // Default crop box: center 40% of image
                int cw = Math.Max(10, (int)(_image.Width * 0.4f));
                int ch = Math.Max(10, (int)(_image.Height * 0.4f));
                int cx = (_image.Width - cw) / 2;
                int cy = (_image.Height - ch) / 2;
                SetCropRectInternal(new Rectangle(cx, cy, cw, ch), true);
                FitImageToView();
            }
            Invalidate();
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Rectangle CropRect
    {
        get => _cropRect;
        set => SetCropRectInternal(value, true);
    }

    public float ZoomFactor => _zoomFactor;

    public void SetCropRect(Rectangle rect)
    {
        SetCropRectInternal(rect, true);
    }

    private void SetCropRectInternal(Rectangle rect, bool raiseEvent)
    {
        if (_image == null)
        {
            _cropRect = rect;
            if (raiseEvent) CropRectChanged?.Invoke(_cropRect);
            Invalidate();
            return;
        }

        int imgW = _image.Width;
        int imgH = _image.Height;

        int w = Math.Clamp(rect.Width, 1, imgW);
        int h = Math.Clamp(rect.Height, 1, imgH);
        int x = Math.Clamp(rect.X, 0, Math.Max(0, imgW - w));
        int y = Math.Clamp(rect.Y, 0, Math.Max(0, imgH - h));

        Rectangle clamped = new(x, y, w, h);
        if (_cropRect != clamped)
        {
            _cropRect = clamped;
            if (raiseEvent) CropRectChanged?.Invoke(_cropRect);
            Invalidate();
        }
    }

    public void FitImageToView()
    {
        if (_image == null || Width <= 40 || Height <= 40) return;

        float padding = 40f;
        float availableW = Width - padding * 2;
        float availableH = Height - padding * 2;

        float scaleX = availableW / _image.Width;
        float scaleY = availableH / _image.Height;
        _zoomFactor = Math.Clamp(Math.Min(scaleX, scaleY), 0.05f, 20.0f);

        float imgRenderW = _image.Width * _zoomFactor;
        float imgRenderH = _image.Height * _zoomFactor;

        _panOffset = new PointF(
            (Width - imgRenderW) / 2f,
            (Height - imgRenderH) / 2f
        );

        ZoomChanged?.Invoke(_zoomFactor);
        Invalidate();
    }

    public void SetZoom100()
    {
        if (_image == null) return;
        _zoomFactor = 1.0f;
        _panOffset = new PointF(
            (Width - _image.Width) / 2f,
            (Height - _image.Height) / 2f
        );
        ZoomChanged?.Invoke(_zoomFactor);
        Invalidate();
    }

    public void ZoomIn() => ZoomBy(1.25f, new Point(Width / 2, Height / 2));
    public void ZoomOut() => ZoomBy(0.8f, new Point(Width / 2, Height / 2));

    private void ZoomBy(float factor, Point center)
    {
        if (_image == null) return;

        float oldZoom = _zoomFactor;
        float newZoom = Math.Clamp(oldZoom * factor, 0.05f, 40.0f);
        if (Math.Abs(oldZoom - newZoom) < 0.001f) return;

        // Keep the image pixel under 'center' at the same screen point
        float imgX = (center.X - _panOffset.X) / oldZoom;
        float imgY = (center.Y - _panOffset.Y) / oldZoom;

        _zoomFactor = newZoom;
        _panOffset = new PointF(
            center.X - imgX * newZoom,
            center.Y - imgY * newZoom
        );

        ZoomChanged?.Invoke(_zoomFactor);
        Invalidate();
    }

    #region Coordinate Transformations

    public Point ImageToScreen(Point imgPt)
    {
        return new Point(
            (int)Math.Round(imgPt.X * _zoomFactor + _panOffset.X),
            (int)Math.Round(imgPt.Y * _zoomFactor + _panOffset.Y)
        );
    }

    public RectangleF ImageToScreen(Rectangle imgRect)
    {
        return new RectangleF(
            imgRect.X * _zoomFactor + _panOffset.X,
            imgRect.Y * _zoomFactor + _panOffset.Y,
            imgRect.Width * _zoomFactor,
            imgRect.Height * _zoomFactor
        );
    }

    public Point ScreenToImage(Point screenPt)
    {
        int x = (int)Math.Floor((screenPt.X - _panOffset.X) / _zoomFactor);
        int y = (int)Math.Floor((screenPt.Y - _panOffset.Y) / _zoomFactor);
        return new Point(x, y);
    }

    #endregion

    #region Mouse Events

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float factor = e.Delta > 0 ? 1.15f : 0.869565f;
        ZoomBy(factor, e.Location);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // Middle button or Space+Left or Right button: PAN
        if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right || (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Space)))
        {
            _isPanning = true;
            _lastMousePos = e.Location;
            Cursor = Cursors.Hand;
            return;
        }

        if (e.Button == MouseButtons.Left && _image != null)
        {
            _activeHandle = HitTestHandle(e.Location);
            _dragStartMouse = e.Location;
            _dragStartCropRect = _cropRect;

            if (_activeHandle == DragHandle.DrawNew)
            {
                Point imgPt = ScreenToImage(e.Location);
                int ix = Math.Clamp(imgPt.X, 0, _image.Width - 1);
                int iy = Math.Clamp(imgPt.Y, 0, _image.Height - 1);
                _dragStartCropRect = new Rectangle(ix, iy, 1, 1);
                SetCropRectInternal(_dragStartCropRect, true);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // Panning
        if (_isPanning)
        {
            int dx = e.X - _lastMousePos.X;
            int dy = e.Y - _lastMousePos.Y;
            _panOffset = new PointF(_panOffset.X + dx, _panOffset.Y + dy);
            _lastMousePos = e.Location;
            Invalidate();
            return;
        }

        // Fire Hover Coordinate event
        if (_image != null)
        {
            Point imgPt = ScreenToImage(e.Location);
            Color? pixelColor = null;
            if (imgPt.X >= 0 && imgPt.X < _image.Width && imgPt.Y >= 0 && imgPt.Y < _image.Height)
            {
                try { pixelColor = _image.GetPixel(imgPt.X, imgPt.Y); } catch { }
            }
            CursorMovedOnImage?.Invoke(imgPt, pixelColor);
        }

        // Resizing or Moving Crop Box
        if (e.Button == MouseButtons.Left && _activeHandle != DragHandle.None && _image != null)
        {
            ProcessCropBoxDrag(e.Location);
            return;
        }

        // Update Cursor on Hover (only Invalidate if handle changed to save GDI+ performance)
        if (_image != null)
        {
            DragHandle newHandle = HitTestHandle(e.Location);
            if (newHandle != _hoverHandle)
            {
                _hoverHandle = newHandle;
                UpdateCursorForHandle(_hoverHandle);
                Invalidate();
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isPanning && (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right || e.Button == MouseButtons.Left))
        {
            _isPanning = false;
            UpdateCursorForHandle(_hoverHandle);
        }

        if (e.Button == MouseButtons.Left && _activeHandle != DragHandle.None)
        {
            _activeHandle = DragHandle.None;
            UpdateCursorForHandle(_hoverHandle);
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverHandle = DragHandle.None;
        Cursor = Cursors.Default;
        Invalidate();
    }

    private void ProcessCropBoxDrag(Point currentMouse)
    {
        if (_image == null) return;

        float deltaScreenX = currentMouse.X - _dragStartMouse.X;
        float deltaScreenY = currentMouse.Y - _dragStartMouse.Y;

        int deltaImgX = (int)Math.Round(deltaScreenX / _zoomFactor);
        int deltaImgY = (int)Math.Round(deltaScreenY / _zoomFactor);

        int imgW = _image.Width;
        int imgH = _image.Height;

        Rectangle r = _dragStartCropRect;

        if (LockedAspectRatio is float ratio && ratio > 0 && _activeHandle != DragHandle.Inside)
        {
            ProcessCropBoxDragLocked(currentMouse, r, deltaImgX, deltaImgY, ratio, imgW, imgH);
            return;
        }

        switch (_activeHandle)
        {
            case DragHandle.Inside:
                int newX = Math.Clamp(r.X + deltaImgX, 0, Math.Max(0, imgW - r.Width));
                int newY = Math.Clamp(r.Y + deltaImgY, 0, Math.Max(0, imgH - r.Height));
                SetCropRectInternal(new Rectangle(newX, newY, r.Width, r.Height), true);
                break;

            case DragHandle.DrawNew:
                Point curPt = ScreenToImage(currentMouse);
                int x1 = Math.Clamp(Math.Min(r.X, curPt.X), 0, imgW - 1);
                int y1 = Math.Clamp(Math.Min(r.Y, curPt.Y), 0, imgH - 1);
                int x2 = Math.Clamp(Math.Max(r.X, curPt.X), 0, imgW - 1);
                int y2 = Math.Clamp(Math.Max(r.Y, curPt.Y), 0, imgH - 1);
                SetCropRectInternal(new Rectangle(x1, y1, Math.Max(1, x2 - x1 + 1), Math.Max(1, y2 - y1 + 1)), true);
                break;

            case DragHandle.Left:
                int left = Math.Clamp(r.X + deltaImgX, 0, r.Right - 1);
                SetCropRectInternal(new Rectangle(left, r.Y, r.Right - left, r.Height), true);
                break;

            case DragHandle.Right:
                int right = Math.Clamp(r.Right + deltaImgX, r.Left + 1, imgW);
                SetCropRectInternal(new Rectangle(r.Left, r.Y, right - r.Left, r.Height), true);
                break;

            case DragHandle.Top:
                int top = Math.Clamp(r.Y + deltaImgY, 0, r.Bottom - 1);
                SetCropRectInternal(new Rectangle(r.X, top, r.Width, r.Bottom - top), true);
                break;

            case DragHandle.Bottom:
                int bottom = Math.Clamp(r.Bottom + deltaImgY, r.Top + 1, imgH);
                SetCropRectInternal(new Rectangle(r.X, r.Top, r.Width, bottom - r.Top), true);
                break;

            case DragHandle.TopLeft:
                int tlX = Math.Clamp(r.X + deltaImgX, 0, r.Right - 1);
                int tlY = Math.Clamp(r.Y + deltaImgY, 0, r.Bottom - 1);
                SetCropRectInternal(new Rectangle(tlX, tlY, r.Right - tlX, r.Bottom - tlY), true);
                break;

            case DragHandle.TopRight:
                int trR = Math.Clamp(r.Right + deltaImgX, r.Left + 1, imgW);
                int trY = Math.Clamp(r.Y + deltaImgY, 0, r.Bottom - 1);
                SetCropRectInternal(new Rectangle(r.Left, trY, trR - r.Left, r.Bottom - trY), true);
                break;

            case DragHandle.BottomLeft:
                int blX = Math.Clamp(r.X + deltaImgX, 0, r.Right - 1);
                int blB = Math.Clamp(r.Bottom + deltaImgY, r.Top + 1, imgH);
                SetCropRectInternal(new Rectangle(blX, r.Top, r.Right - blX, blB - r.Top), true);
                break;

            case DragHandle.BottomRight:
                int brR = Math.Clamp(r.Right + deltaImgX, r.Left + 1, imgW);
                int brB = Math.Clamp(r.Bottom + deltaImgY, r.Top + 1, imgH);
                SetCropRectInternal(new Rectangle(r.Left, r.Top, brR - r.Left, brB - r.Top), true);
                break;
        }
    }

    private void ProcessCropBoxDragLocked(Point currentMouse, Rectangle r, int deltaImgX, int deltaImgY, float ratio, int imgW, int imgH)
    {
        if (ratio <= 0) return;

        switch (_activeHandle)
        {
            case DragHandle.DrawNew:
                Point curPt = ScreenToImage(currentMouse);
                int rawW = Math.Abs(curPt.X - r.X);
                int rawH = Math.Abs(curPt.Y - r.Y);
                if (rawW > rawH * ratio)
                    rawH = (int)Math.Round(rawW / ratio);
                else
                    rawW = (int)Math.Round(rawH * ratio);

                rawW = Math.Max(1, Math.Min(rawW, imgW));
                rawH = Math.Max(1, Math.Min(rawH, imgH));

                int x = curPt.X < r.X ? r.X - rawW : r.X;
                int y = curPt.Y < r.Y ? r.Y - rawH : r.Y;
                x = Math.Clamp(x, 0, Math.Max(0, imgW - rawW));
                y = Math.Clamp(y, 0, Math.Max(0, imgH - rawH));
                SetCropRectInternal(new Rectangle(x, y, rawW, rawH), true);
                break;

            case DragHandle.BottomRight:
                int dwBR = deltaImgX;
                if (Math.Abs(deltaImgY * ratio) > Math.Abs(deltaImgX))
                    dwBR = (int)Math.Round(deltaImgY * ratio);
                int wBR = Math.Clamp(r.Width + dwBR, 5, imgW - r.Left);
                int hBR = (int)Math.Round(wBR / ratio);
                if (r.Top + hBR > imgH)
                {
                    hBR = imgH - r.Top;
                    wBR = (int)Math.Round(hBR * ratio);
                }
                wBR = Math.Max(1, Math.Min(wBR, imgW - r.Left));
                hBR = Math.Max(1, Math.Min(hBR, imgH - r.Top));
                SetCropRectInternal(new Rectangle(r.Left, r.Top, wBR, hBR), true);
                break;

            case DragHandle.Right:
                int wR = Math.Clamp(r.Width + deltaImgX, 5, imgW - r.Left);
                int hR = (int)Math.Round(wR / ratio);
                if (r.Top + hR > imgH)
                {
                    hR = imgH - r.Top;
                    wR = (int)Math.Round(hR * ratio);
                }
                wR = Math.Max(1, Math.Min(wR, imgW - r.Left));
                hR = Math.Max(1, Math.Min(hR, imgH - r.Top));
                SetCropRectInternal(new Rectangle(r.Left, r.Top, wR, hR), true);
                break;

            case DragHandle.Bottom:
                int hB = Math.Clamp(r.Height + deltaImgY, 5, imgH - r.Top);
                int wB = (int)Math.Round(hB * ratio);
                if (r.Left + wB > imgW)
                {
                    wB = imgW - r.Left;
                    hB = (int)Math.Round(wB / ratio);
                }
                wB = Math.Max(1, Math.Min(wB, imgW - r.Left));
                hB = Math.Max(1, Math.Min(hB, imgH - r.Top));
                SetCropRectInternal(new Rectangle(r.Left, r.Top, wB, hB), true);
                break;

            case DragHandle.TopLeft:
                int dwTL = -deltaImgX;
                if (Math.Abs(-deltaImgY * ratio) > Math.Abs(-deltaImgX))
                    dwTL = (int)Math.Round(-deltaImgY * ratio);
                int wTL = Math.Clamp(r.Width + dwTL, 5, r.Right);
                int hTL = (int)Math.Round(wTL / ratio);
                if (r.Bottom - hTL < 0)
                {
                    hTL = r.Bottom;
                    wTL = (int)Math.Round(hTL * ratio);
                }
                wTL = Math.Max(1, Math.Min(wTL, r.Right));
                hTL = Math.Max(1, Math.Min(hTL, r.Bottom));
                SetCropRectInternal(new Rectangle(r.Right - wTL, r.Bottom - hTL, wTL, hTL), true);
                break;

            case DragHandle.Left:
                int wL = Math.Clamp(r.Width - deltaImgX, 5, r.Right);
                int hL = (int)Math.Round(wL / ratio);
                if (r.Top + hL > imgH)
                {
                    hL = imgH - r.Top;
                    wL = (int)Math.Round(hL * ratio);
                }
                wL = Math.Max(1, Math.Min(wL, r.Right));
                hL = Math.Max(1, Math.Min(hL, imgH - r.Top));
                SetCropRectInternal(new Rectangle(r.Right - wL, r.Top, wL, hL), true);
                break;

            case DragHandle.Top:
                int hT = Math.Clamp(r.Height - deltaImgY, 5, r.Bottom);
                int wT = (int)Math.Round(hT * ratio);
                if (r.Left + wT > imgW)
                {
                    wT = imgW - r.Left;
                    hT = (int)Math.Round(wT / ratio);
                }
                wT = Math.Max(1, Math.Min(wT, imgW - r.Left));
                hT = Math.Max(1, Math.Min(hT, r.Bottom));
                SetCropRectInternal(new Rectangle(r.Left, r.Bottom - hT, wT, hT), true);
                break;

            case DragHandle.TopRight:
                int dwTR = deltaImgX;
                if (Math.Abs(-deltaImgY * ratio) > Math.Abs(deltaImgX))
                    dwTR = (int)Math.Round(-deltaImgY * ratio);
                int wTR = Math.Clamp(r.Width + dwTR, 5, imgW - r.Left);
                int hTR = (int)Math.Round(wTR / ratio);
                if (r.Bottom - hTR < 0)
                {
                    hTR = r.Bottom;
                    wTR = (int)Math.Round(hTR * ratio);
                }
                wTR = Math.Max(1, Math.Min(wTR, imgW - r.Left));
                hTR = Math.Max(1, Math.Min(hTR, r.Bottom));
                SetCropRectInternal(new Rectangle(r.Left, r.Bottom - hTR, wTR, hTR), true);
                break;

            case DragHandle.BottomLeft:
                int dwBL = -deltaImgX;
                if (Math.Abs(deltaImgY * ratio) > Math.Abs(-deltaImgX))
                    dwBL = (int)Math.Round(deltaImgY * ratio);
                int wBL = Math.Clamp(r.Width + dwBL, 5, r.Right);
                int hBL = (int)Math.Round(wBL / ratio);
                if (r.Top + hBL > imgH)
                {
                    hBL = imgH - r.Top;
                    wBL = (int)Math.Round(hBL * ratio);
                }
                wBL = Math.Max(1, Math.Min(wBL, r.Right));
                hBL = Math.Max(1, Math.Min(hBL, imgH - r.Top));
                SetCropRectInternal(new Rectangle(r.Right - wBL, r.Top, wBL, hBL), true);
                break;
        }
    }

    private DragHandle HitTestHandle(Point pt)
    {
        if (_image == null) return DragHandle.None;

        RectangleF box = ImageToScreen(_cropRect);
        float tol = HandleTolerance;

        // Corners
        if (Math.Abs(pt.X - box.Left) <= tol && Math.Abs(pt.Y - box.Top) <= tol) return DragHandle.TopLeft;
        if (Math.Abs(pt.X - box.Right) <= tol && Math.Abs(pt.Y - box.Top) <= tol) return DragHandle.TopRight;
        if (Math.Abs(pt.X - box.Left) <= tol && Math.Abs(pt.Y - box.Bottom) <= tol) return DragHandle.BottomLeft;
        if (Math.Abs(pt.X - box.Right) <= tol && Math.Abs(pt.Y - box.Bottom) <= tol) return DragHandle.BottomRight;

        // Edges (midpoint area or whole edge line)
        if (Math.Abs(pt.X - box.Left) <= tol && pt.Y >= box.Top - tol && pt.Y <= box.Bottom + tol) return DragHandle.Left;
        if (Math.Abs(pt.X - box.Right) <= tol && pt.Y >= box.Top - tol && pt.Y <= box.Bottom + tol) return DragHandle.Right;
        if (Math.Abs(pt.Y - box.Top) <= tol && pt.X >= box.Left - tol && pt.X <= box.Right + tol) return DragHandle.Top;
        if (Math.Abs(pt.Y - box.Bottom) <= tol && pt.X >= box.Left - tol && pt.X <= box.Right + tol) return DragHandle.Bottom;

        // Inside
        if (box.Contains(pt)) return DragHandle.Inside;

        // Outside image or crop box -> Draw new crop box
        return DragHandle.DrawNew;
    }

    private void UpdateCursorForHandle(DragHandle handle)
    {
        Cursor = handle switch
        {
            DragHandle.TopLeft or DragHandle.BottomRight => Cursors.SizeNWSE,
            DragHandle.TopRight or DragHandle.BottomLeft => Cursors.SizeNESW,
            DragHandle.Left or DragHandle.Right => Cursors.SizeWE,
            DragHandle.Top or DragHandle.Bottom => Cursors.SizeNS,
            DragHandle.Inside => Cursors.SizeAll,
            DragHandle.DrawNew => Cursors.Cross,
            _ => Cursors.Default
        };
    }

    #endregion

    #region Rendering

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        // Fill background with dark checker pattern or solid dark
        DrawCheckerBackground(g);

        if (_image == null)
        {
            using Font font = new("Segoe UI", 12F, FontStyle.Regular);
            using SolidBrush brush = new(Color.FromArgb(140, 150, 170));
            string hint = "Chưa nạp ảnh. Nhấn 'Nạp ảnh', 'Dán Clipboard' (Ctrl+V) hoặc 'Chụp Live' (F9) để bắt đầu";
            SizeF size = g.MeasureString(hint, font);
            g.DrawString(hint, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
            return;
        }

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
        float imgScreenW = _image.Width * _zoomFactor;
        float imgScreenH = _image.Height * _zoomFactor;
        RectangleF imgScreenRect = new(_panOffset.X, _panOffset.Y, imgScreenW, imgScreenH);
        g.DrawImage(_image, imgScreenRect);

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

        float padX = 14;
        float padY = 6;
        float boxW = textSize.Width + padX * 2;
        float boxH = textSize.Height + padY * 2;
        float boxX = 14;
        float boxY = Height - boxH - 14;

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
