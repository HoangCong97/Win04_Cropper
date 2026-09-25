#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public partial class CanvasControl : UserControl
{
    private Bitmap? _image;
    private Rectangle _cropRect = new(50, 50, 200, 150);
    private bool _isDragOver;
    private bool _isWindowResizing;

    /// <summary>
    /// Set to true during window move/resize to skip expensive rendering.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public bool IsWindowResizing
    {
        get => _isWindowResizing;
        set
        {
            if (_isWindowResizing != value)
            {
                _isWindowResizing = value;
                if (!value) Invalidate();
            }
        }
    }

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
                 ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = Color.FromArgb(20, 22, 28);
        AllowDrop = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _checkerBrush?.Dispose();
            _checkerBrush = null;
            _checkerTile?.Dispose();
            _checkerTile = null;
        }
        base.Dispose(disposing);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!_isWindowResizing)
        {
            Invalidate();
        }
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        base.OnDragEnter(drgevent);
        if (drgevent.Data != null && (drgevent.Data.GetDataPresent(DataFormats.FileDrop) || drgevent.Data.GetDataPresent(DataFormats.Bitmap)))
        {
            _isDragOver = true;
            Invalidate();
        }
    }

    protected override void OnDragLeave(EventArgs e)
    {
        base.OnDragLeave(e);
        if (_isDragOver)
        {
            _isDragOver = false;
            Invalidate();
        }
    }

    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        _isDragOver = false;
        Invalidate();
    }

    public bool IsImageValid()
    {
        if (_image == null) return false;
        try
        {
            _ = _image.Width;
            return true;
        }
        catch
        {
            _image = null;
            return false;
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Bitmap? Image
    {
        get => _image;
        set
        {
            _image = value;
            if (IsImageValid())
            {
                // Default crop box: center 40% of image
                int cw = Math.Max(10, (int)(_image!.Width * 0.4f));
                int ch = Math.Max(10, (int)(_image!.Height * 0.4f));
                int cx = (_image!.Width - cw) / 2;
                int cy = (_image!.Height - ch) / 2;
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

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public PointF PanOffset
    {
        get => _panOffset;
        set
        {
            _panOffset = value;
            Invalidate();
        }
    }

    public void SetZoomFactor(float factor)
    {
        _zoomFactor = Math.Clamp(factor, 0.05f, 20.0f);
        ZoomChanged?.Invoke(_zoomFactor);
        Invalidate();
    }

    /// <summary>
    /// Updates the currently displayed and cropped bitmap without resetting crop rectangle, zoom, or pan offset.
    /// Used for real-time filter previews (Grayscale, Threshold binarization).
    /// </summary>
    public void SetDisplayImageKeepState(Bitmap? bmp)
    {
        _image = bmp;
        Invalidate();
    }

    public void SetImageWithState(Bitmap? img, Rectangle? cropRect = null, float? zoomFactor = null, PointF? panOffset = null)
    {
        _image = img;
        if (IsImageValid())
        {
            if (cropRect.HasValue && cropRect.Value.Width > 0 && cropRect.Value.Height > 0)
            {
                SetCropRectInternal(cropRect.Value, true);
            }
            else
            {
                int cw = Math.Max(10, (int)(_image!.Width * 0.4f));
                int ch = Math.Max(10, (int)(_image!.Height * 0.4f));
                int cx = (_image!.Width - cw) / 2;
                int cy = (_image!.Height - ch) / 2;
                SetCropRectInternal(new Rectangle(cx, cy, cw, ch), true);
            }

            if (zoomFactor.HasValue && zoomFactor.Value > 0.05f)
            {
                _zoomFactor = Math.Clamp(zoomFactor.Value, 0.05f, 20.0f);
                if (panOffset.HasValue)
                {
                    _panOffset = panOffset.Value;
                }
                else
                {
                    float imgRenderW = _image!.Width * _zoomFactor;
                    float imgRenderH = _image!.Height * _zoomFactor;
                    _panOffset = new PointF((Width - imgRenderW) / 2f, (Height - imgRenderH) / 2f);
                }
                ZoomChanged?.Invoke(_zoomFactor);
            }
            else
            {
                FitImageToView();
            }
        }
        Invalidate();
    }

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
}
