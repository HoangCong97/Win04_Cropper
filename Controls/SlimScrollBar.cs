#nullable enable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

/// <summary>
/// Reusable slim modern dark scrollbar control matching the dark UI theme.
/// Supports smooth dragging, page jumping, hover states, mouse wheel and container binding.
/// </summary>
public class SlimScrollBar : Control
{
    private readonly float _dpiScale = 1.0f;
    private int DpiScale(int px) => (int)Math.Round(px * _dpiScale);

    private int _scrollOffset;
    private int _maxScroll;
    private int _contentHeight;
    private int _viewPortHeight;

    private bool _isDragging;
    private int _dragStartY;
    private int _dragStartOffset;
    private bool _isHovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ScrollOffset => _scrollOffset;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxScroll => _maxScroll;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool NeedsScroll => _maxScroll > 0;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ContentHeight => _contentHeight;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ViewPortHeight => _viewPortHeight;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TrackColor { get; set; } = Color.FromArgb(20, 23, 30);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ThumbColor { get; set; } = Color.FromArgb(85, 95, 115);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ThumbHoverColor { get; set; } = Color.FromArgb(130, 145, 175);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ThumbActiveColor { get; set; } = Color.FromArgb(160, 180, 210);

    public event EventHandler<int>? ScrollOffsetChanged;

    public SlimScrollBar(float dpiScale = 1.0f)
    {
        _dpiScale = dpiScale > 0 ? dpiScale : 1.0f;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        this.Width = DpiScale(6);
        this.BackColor = TrackColor;
        this.Cursor = Cursors.Default;
    }

    public void UpdateScroll(int contentHeight, int viewPortHeight, int? keepOffset = null)
    {
        _contentHeight = contentHeight;
        _viewPortHeight = viewPortHeight;
        _maxScroll = Math.Max(0, contentHeight - viewPortHeight);

        bool needsScroll = _maxScroll > 0;
        if (this.Visible != needsScroll)
        {
            this.Visible = needsScroll;
        }

        int targetOffset = keepOffset ?? _scrollOffset;
        SetScrollOffset(Math.Clamp(targetOffset, 0, _maxScroll), triggerEvent: true);
        this.Invalidate();
    }

    public void SetScrollOffset(int newOffset, bool triggerEvent = true)
    {
        int clamped = Math.Clamp(newOffset, 0, _maxScroll);
        if (_scrollOffset != clamped)
        {
            _scrollOffset = clamped;
            if (triggerEvent)
            {
                ScrollOffsetChanged?.Invoke(this, _scrollOffset);
            }
            this.Invalidate();
        }
        else
        {
            this.Invalidate();
        }
    }

    public void ScrollBy(int delta)
    {
        if (_maxScroll <= 0) return;
        SetScrollOffset(_scrollOffset + delta);
    }

    public void ResetScroll()
    {
        SetScrollOffset(0);
    }

    public Rectangle GetThumbRectangle()
    {
        int trackH = this.ClientSize.Height;
        int totalH = _contentHeight;
        int viewH = _viewPortHeight;
        if (_maxScroll <= 0 || trackH <= 0 || totalH <= 0) return Rectangle.Empty;

        float ratio = (float)viewH / totalH;
        int thumbH = Math.Max(DpiScale(28), (int)(trackH * ratio));
        int travel = trackH - thumbH;
        int thumbY = travel > 0 ? (int)((float)_scrollOffset / _maxScroll * travel) : 0;
        return new Rectangle(1, thumbY, Math.Max(2, this.ClientSize.Width - 2), thumbH);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_maxScroll <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle thumbRect = GetThumbRectangle();
        if (thumbRect.IsEmpty) return;

        Color currentThumbColor = _isDragging
            ? ThumbActiveColor
            : (_isHovered ? ThumbHoverColor : ThumbColor);

        using GraphicsPath path = GetRoundedRect(new RectangleF(thumbRect.X, thumbRect.Y, thumbRect.Width, thumbRect.Height), Math.Min(thumbRect.Width / 2f, 3f));
        using SolidBrush brush = new(currentThumbColor);
        g.FillPath(brush, path);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _maxScroll <= 0) return;

        Rectangle thumbRect = GetThumbRectangle();
        if (thumbRect.Contains(e.Location))
        {
            _isDragging = true;
            _dragStartY = e.Y;
            _dragStartOffset = _scrollOffset;
            this.Capture = true;
            this.Invalidate();
        }
        else
        {
            int pageDelta = Math.Max(DpiScale(40), _viewPortHeight - DpiScale(40));
            if (e.Y < thumbRect.Y)
            {
                SetScrollOffset(_scrollOffset - pageDelta);
            }
            else
            {
                SetScrollOffset(_scrollOffset + pageDelta);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging)
        {
            int trackH = this.ClientSize.Height;
            Rectangle thumbRect = GetThumbRectangle();
            int travel = trackH - thumbRect.Height;
            if (travel > 0)
            {
                int deltaY = e.Y - _dragStartY;
                float scrollPerPixel = (float)_maxScroll / travel;
                SetScrollOffset((int)(_dragStartOffset + deltaY * scrollPerPixel));
            }
            return;
        }

        bool hovered = GetThumbRectangle().Contains(e.Location);
        if (hovered != _isHovered)
        {
            _isHovered = hovered;
            this.Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            this.Capture = false;
            this.Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_isDragging && _isHovered)
        {
            _isHovered = false;
            this.Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_maxScroll <= 0) return;
        int delta = -Math.Sign(e.Delta) * DpiScale(48);
        ScrollBy(delta);
    }

    /// <summary>
    /// Binds this scrollbar to a viewport panel and its inner content control.
    /// Handles layout resize, mouse wheel routing, and smooth positioning.
    /// </summary>
    public void Bind(Panel viewport, Control content, int horizontalPadding = 0, int topPadding = 0)
    {
        viewport.AutoScroll = false;
        content.Dock = DockStyle.None;
        bool _isUpdatingLayout = false;

        void UpdateLayout()
        {
            if (_isUpdatingLayout) return;
            _isUpdatingLayout = true;
            try
            {
                if (viewport.ClientSize.Width <= 0 || viewport.ClientSize.Height <= 0) return;

                int targetW = Math.Max(DpiScale(50), viewport.ClientSize.Width - horizontalPadding * 2);
                if (content.Width != targetW)
                {
                    content.Width = targetW;
                }

                int contentH = content.PreferredSize.Height > 0
                    ? Math.Max(content.Height, content.PreferredSize.Height)
                    : content.Height;
                contentH += topPadding * 2;
                int viewH = viewport.ClientSize.Height;

                UpdateScroll(contentH, viewH);

                content.Location = new Point(horizontalPadding, topPadding - _scrollOffset);
            }
            finally
            {
                _isUpdatingLayout = false;
            }
        }

        this.ScrollOffsetChanged += (s, offset) =>
        {
            content.Top = topPadding - offset;
        };

        viewport.Resize += (s, e) => UpdateLayout();
        viewport.Layout += (s, e) => UpdateLayout();
        viewport.VisibleChanged += (s, e) => UpdateLayout();
        content.SizeChanged += (s, e) => UpdateLayout();
        content.Layout += (s, e) => UpdateLayout();

        void OnWheel(object? s, MouseEventArgs e)
        {
            if (_maxScroll <= 0) return;
            if (s is NumericUpDown || s?.GetType().Name.Contains("UpDown") == true) return;
            int delta = -Math.Sign(e.Delta) * DpiScale(48);
            ScrollBy(delta);
            if (e is HandledMouseEventArgs he) he.Handled = true;
        }

        HookWheelRecursive(viewport, OnWheel);
        HookWheelRecursive(content, OnWheel);

        UpdateLayout();
    }

    private static void HookWheelRecursive(Control parent, MouseEventHandler handler)
    {
        parent.MouseWheel -= handler;
        parent.MouseWheel += handler;
        parent.ControlAdded += (s, e) =>
        {
            if (e.Control != null) HookWheelRecursive(e.Control, handler);
        };
        foreach (Control child in parent.Controls)
        {
            HookWheelRecursive(child, handler);
        }
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
}
