#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using Win04_Cropper.Services;

namespace Win04_Cropper;

partial class MainCropperForm
{
    #region Canvas Event Handlers & Input Sync

    private void OnCanvasCropRectChanged(Rectangle rect)
    {
        if (_isUpdatingInputs) return;

        UpdateInputsFromCropRect(rect);
        _isProjectDirty = true;
    }

    private void OnCanvasCursorMoved(Point imgPt, Color? pixelColor)
    {
        if (canvas.Image == null)
        {
            lblCursorInfo.Text = "Chuột: -";
            return;
        }

        if (imgPt.X >= 0 && imgPt.X < canvas.Image.Width && imgPt.Y >= 0 && imgPt.Y < canvas.Image.Height)
        {
            string colorHex = pixelColor.HasValue
                ? $" | #{pixelColor.Value.R:X2}{pixelColor.Value.G:X2}{pixelColor.Value.B:X2}"
                : "";
            lblCursorInfo.Text = $"Chuột: X={imgPt.X}, Y={imgPt.Y}{colorHex}";
        }
        else
        {
            lblCursorInfo.Text = $"Chuột: X={imgPt.X}, Y={imgPt.Y} (ngoài ảnh)";
        }
    }

    private void OnCanvasZoomChanged(float zoom)
    {
        lblZoomValue.Text = $"{Math.Round(zoom * 100)}%";
    }

    private void UpdateInputsFromCropRect(Rectangle rect)
    {
        _isUpdatingInputs = true;
        try
        {
            numX.Value = Math.Clamp(rect.X, numX.Minimum, numX.Maximum);
            numY.Value = Math.Clamp(rect.Y, numY.Minimum, numY.Maximum);
            numW.Value = Math.Clamp(rect.Width, numW.Minimum, numW.Maximum);
            numH.Value = Math.Clamp(rect.Height, numH.Minimum, numH.Maximum);

            if (lblAspectRatio != null)
            {
                lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(rect.Width, rect.Height)}";
            }
        }
        finally
        {
            _isUpdatingInputs = false;
        }
    }

    private void OnNumericInputChanged()
    {
        if (_isUpdatingInputs || canvas.Image == null) return;

        int newX = (int)numX.Value;
        int newY = (int)numY.Value;
        int newW = (int)numW.Value;
        int newH = (int)numH.Value;

        if (canvas.LockedAspectRatio is float ratio && ratio > 0)
        {
            Rectangle curCrop = canvas.CropRect;
            bool wChanged = (newW != curCrop.Width);
            bool hChanged = (newH != curCrop.Height);

            if (numW.Focused || (wChanged && !hChanged))
            {
                newH = (int)Math.Round(newW / ratio);
                _isUpdatingInputs = true;
                numH.Value = Math.Clamp(newH, numH.Minimum, numH.Maximum);
                _isUpdatingInputs = false;
            }
            else if (numH.Focused || (hChanged && !wChanged))
            {
                newW = (int)Math.Round(newH * ratio);
                _isUpdatingInputs = true;
                numW.Value = Math.Clamp(newW, numW.Minimum, numW.Maximum);
                _isUpdatingInputs = false;
            }
            else if (wChanged)
            {
                newH = (int)Math.Round(newW / ratio);
                _isUpdatingInputs = true;
                numH.Value = Math.Clamp(newH, numH.Minimum, numH.Maximum);
                _isUpdatingInputs = false;
            }
        }

        Rectangle newRect = new(newX, newY, (int)numW.Value, (int)numH.Value);
        canvas.SetCropRect(newRect);

        if (lblAspectRatio != null)
        {
            lblAspectRatio.Text = $"Tỉ lệ: {GetRatioStr(newRect.Width, newRect.Height)}";
        }
        _isProjectDirty = true;
    }

    private void SetActiveRatioButton(Button? activeBtn)
    {
        foreach (var btn in _ratioButtons)
        {
            if (btn == activeBtn)
            {
                btn.BackColor = Color.FromArgb(70, 130, 240);
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.BackColor = Color.FromArgb(50, 55, 68);
                btn.ForeColor = Color.White;
            }
        }
    }

    private void ApplyAspectRatio(int rw, int rh, Button btn)
    {
        SetActiveRatioButton(btn);

        if (rw <= 0 || rh <= 0)
        {
            canvas.LockedAspectRatio = null;
            return;
        }

        float ratio = (float)rw / rh;
        canvas.LockedAspectRatio = ratio;

        if (canvas.Image == null) return;

        Rectangle cur = canvas.CropRect;
        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int newW = cur.Width > 0 ? cur.Width : Math.Min(imgW, DpiScale(400));
        int newH = (int)Math.Round(newW / ratio);

        if (newH > imgH)
        {
            newH = imgH;
            newW = (int)Math.Round(newH * ratio);
        }
        if (newW > imgW)
        {
            newW = imgW;
            newH = (int)Math.Round(newW / ratio);
        }

        int newX = cur.Width > 0 ? Math.Clamp(cur.X, 0, imgW - newW) : (imgW - newW) / 2;
        int newY = cur.Height > 0 ? Math.Clamp(cur.Y, 0, imgH - newH) : (imgH - newH) / 2;

        Rectangle newCrop = new(newX, newY, newW, newH);
        canvas.SetCropRect(newCrop);
        UpdateInputsFromCropRect(newCrop);
    }

    private void ApplyScreenResolution(int targetW, int targetH)
    {
        if (canvas.Image == null) return;

        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int finalW = Math.Min(targetW, imgW);
        int finalH = Math.Min(targetH, imgH);

        Rectangle cur = canvas.CropRect;
        int posX = (cur.Width > 0)
            ? Math.Clamp(cur.X, 0, Math.Max(0, imgW - finalW))
            : (imgW - finalW) / 2;
        int posY = (cur.Height > 0)
            ? Math.Clamp(cur.Y, 0, Math.Max(0, imgH - finalH))
            : (imgH - finalH) / 2;

        Rectangle newRect = new(posX, posY, finalW, finalH);

        float screenRatio = (float)targetW / targetH;
        canvas.LockedAspectRatio = screenRatio;

        if (targetW == 1920 && targetH == 1080) SetActiveRatioButton(btnRatio16x9);
        else if (targetW == 1280 && targetH == 720) SetActiveRatioButton(btnRatio16x9);
        else if (targetW == 2560 && targetH == 1440) SetActiveRatioButton(btnRatio16x9);
        else if (targetW == 1600 && targetH == 900) SetActiveRatioButton(btnRatio16x9);
        else if (targetW == 1024 && targetH == 768) SetActiveRatioButton(btnRatio4x3);
        else SetActiveRatioButton(btnRatioFree);

        canvas.SetCropRect(newRect);
        UpdateInputsFromCropRect(newRect);
    }

    private void ApplyFullImageResolution()
    {
        if (canvas.Image == null) return;

        canvas.LockedAspectRatio = null;
        SetActiveRatioButton(btnRatioFree);

        Rectangle fullRect = new(0, 0, canvas.Image.Width, canvas.Image.Height);
        canvas.SetCropRect(fullRect);
        UpdateInputsFromCropRect(fullRect);
    }

    private int GetNudgeStep()
    {
        string? sel = cboNudgeStep.SelectedItem?.ToString();
        return sel switch
        {
            "1 px" => 1,
            "10 px" => 10,
            "50 px" => 50,
            _ => 1
        };
    }

    private void NudgeCrop(int dx, int dy)
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int newX = Math.Clamp(r.X + dx, 0, Math.Max(0, imgW - r.Width));
        int newY = Math.Clamp(r.Y + dy, 0, Math.Max(0, imgH - r.Height));

        Rectangle nudged = new(newX, newY, r.Width, r.Height);
        canvas.SetCropRect(nudged);
    }

    private void ResizeCrop(int dw, int dh)
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int imgW = canvas.Image.Width;
        int imgH = canvas.Image.Height;

        int newW = Math.Clamp(r.Width + dw, 1, imgW - r.X);
        int newH = Math.Clamp(r.Height + dh, 1, imgH - r.Y);

        Rectangle resized = new(r.X, r.Y, newW, newH);
        canvas.SetCropRect(resized);
    }

    private void CenterCropBox()
    {
        if (canvas.Image == null) return;

        Rectangle r = canvas.CropRect;
        int cx = (canvas.Image.Width - r.Width) / 2;
        int cy = (canvas.Image.Height - r.Height) / 2;

        canvas.SetCropRect(new Rectangle(cx, cy, r.Width, r.Height));
    }

    #endregion

    #region Image Filters (Grayscale & Threshold Binarization)

    private void OnGrayscaleFilterToggled()
    {
        if (!chkGrayscale.Checked && chkThreshold.Checked)
        {
            chkThreshold.Checked = false;
        }
        ApplyCurrentFilters();
    }

    private void OnThresholdFilterToggled()
    {
        pnlThresholdControls.Enabled = chkThreshold.Checked;
        if (chkThreshold.Checked && !chkGrayscale.Checked)
        {
            chkGrayscale.Checked = true;
        }
        ApplyCurrentFilters();
    }

    private void OnThresholdValueChanged()
    {
        lblThresholdVal.Text = $"Điểm ngưỡng: {trkThreshold.Value}";
        if (chkThreshold.Checked)
        {
            ApplyCurrentFilters();
        }
    }

    private void ApplyCurrentFilters()
    {
        Bitmap? baseBmp = _activeMediaItem?.Bitmap;
        if (baseBmp == null && canvas.Image != null && canvas.Image != _currentFilteredBitmap)
        {
            baseBmp = canvas.Image;
        }

        if (baseBmp == null) return;

        bool isGray = chkGrayscale.Checked;
        bool isThresh = chkThreshold.Checked;
        int threshold = trkThreshold.Value;

        if (!isGray && !isThresh)
        {
            if (_currentFilteredBitmap != null)
            {
                _currentFilteredBitmap.Dispose();
                _currentFilteredBitmap = null;
            }
            canvas.SetDisplayImageKeepState(baseBmp);
        }
        else
        {
            Bitmap? filtered = ImageFilterService.ApplyFilters(baseBmp, isGray, isThresh, threshold);
            if (filtered != null)
            {
                canvas.SetDisplayImageKeepState(filtered);
                if (_currentFilteredBitmap != null && _currentFilteredBitmap != filtered)
                {
                    _currentFilteredBitmap.Dispose();
                }
                _currentFilteredBitmap = filtered;
            }
        }
    }

    #endregion

    #region Aspect Ratio Helpers

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

    #endregion
}
