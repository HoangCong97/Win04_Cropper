#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

public partial class CanvasControl
{
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

        if (e.Button == MouseButtons.Left && IsImageValid())
        {
            _activeHandle = HitTestHandle(e.Location);
            _dragStartMouse = e.Location;
            _dragStartCropRect = _cropRect;

            if (_activeHandle == DragHandle.DrawNew)
            {
                Point imgPt = ScreenToImage(e.Location);
                int ix = Math.Clamp(imgPt.X, 0, _image!.Width - 1);
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
        if (IsImageValid())
        {
            try
            {
                Point imgPt = ScreenToImage(e.Location);
                Color? pixelColor = null;
                if (imgPt.X >= 0 && imgPt.X < _image!.Width && imgPt.Y >= 0 && imgPt.Y < _image.Height)
                {
                    try { pixelColor = _image.GetPixel(imgPt.X, imgPt.Y); } catch { }
                }
                CursorMovedOnImage?.Invoke(imgPt, pixelColor);
            }
            catch { }
        }

        // Resizing or Moving Crop Box
        if (e.Button == MouseButtons.Left && _activeHandle != DragHandle.None && IsImageValid())
        {
            ProcessCropBoxDrag(e.Location);
            return;
        }

        // Update Cursor on Hover (only Invalidate if handle changed to save GDI+ performance)
        if (IsImageValid())
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
        if (!IsImageValid()) return;

        float deltaScreenX = currentMouse.X - _dragStartMouse.X;
        float deltaScreenY = currentMouse.Y - _dragStartMouse.Y;

        int deltaImgX = (int)Math.Round(deltaScreenX / _zoomFactor);
        int deltaImgY = (int)Math.Round(deltaScreenY / _zoomFactor);

        int imgW = _image!.Width;
        int imgH = _image!.Height;

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
}
