using System;
using System.Drawing;
using System.Windows.Forms;

namespace Win04_Cropper.Controls;

/// <summary>
/// A SplitContainer subclass that continuously resizes panels in real-time during mouse drag
/// instead of displaying the default legacy modal XOR gray box.
/// </summary>
public class LiveSplitContainer : SplitContainer
{
    private bool _isDragging;

    public LiveSplitContainer()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && SplitterRectangle.Contains(e.Location))
        {
            _isDragging = true;
            Capture = true;
            return; // Suppress default modal XOR focus-box drag
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isDragging)
        {
            int newDist = Orientation == Orientation.Vertical ? e.X : e.Y;
            int total = Orientation == Orientation.Vertical ? Width : Height;

            int min = Math.Max(20, Panel1MinSize);
            int max = total - Math.Max(20, Panel2MinSize) - SplitterWidth;

            if (max > min)
            {
                newDist = Math.Clamp(newDist, min, max);
                if (SplitterDistance != newDist)
                {
                    SplitterDistance = newDist;
                    Refresh();
                }
            }
            return;
        }

        if (SplitterRectangle.Contains(e.Location))
        {
            Cursor = Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit;
        }
        else
        {
            Cursor = Cursors.Default;
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Capture = false;
            Cursor = Cursors.Default;
            Invalidate();
            return;
        }
        base.OnMouseUp(e);
    }
}
