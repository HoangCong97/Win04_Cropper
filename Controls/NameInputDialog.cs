using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper.Controls;

public class NameInputDialog : Form
{
    private readonly TextBox txtName = new();
    private readonly TextBox txtNotes = new();
    private readonly Label lblCoords = new();
    private readonly Label lblError = new();
    private readonly IconButton btnSave = new();
    private readonly IconButton btnCancel = new();
    private readonly RadioButton radOverwrite = new();
    private readonly RadioButton radSaveAsNew = new();

    private readonly HashSet<string> _existingNames;
    private readonly string? _originalName;

    public string RegionName => txtName.Text.Trim();
    public string? Notes => string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
    public bool IsSaveAsNew => _originalName != null && radSaveAsNew.Checked;

    public NameInputDialog(
        Rectangle rect,
        string defaultName = "",
        IEnumerable<string>? existingNames = null,
        string? originalName = null,
        string title = "Lưu tọa độ vùng cắt",
        string headerText = "Lưu tọa độ vào danh sách",
        IconChar headerIcon = IconChar.FloppyDisk)
    {
        _existingNames = existingNames != null
            ? new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _originalName = originalName;

        InitializeUi(rect, defaultName, title, headerText, headerIcon);
    }

    private void InitializeUi(Rectangle rect, string defaultName, string title, string headerText, IconChar headerIcon)
    {
        Text = title;
        bool isEditing = !string.IsNullOrEmpty(_originalName);
        Size = new Size(500, isEditing ? 390 : 325);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(28, 30, 38);
        ForeColor = Color.FromArgb(240, 240, 245);
        Font = new Font("Segoe UI", 9.5F);

        // Header Icon & Title
        IconPictureBox picHeader = new()
        {
            IconChar = headerIcon,
            IconColor = Color.FromArgb(0, 215, 255),
            IconSize = 22,
            Size = new Size(24, 24),
            Location = new Point(22, 17),
            BackColor = Color.Transparent
        };
        Controls.Add(picHeader);

        Label lblHeader = new()
        {
            Text = headerText,
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 215, 255),
            Location = new Point(50, 16),
            AutoSize = true
        };
        Controls.Add(lblHeader);

        // Coordinates badge with Icon
        IconPictureBox picCoords = new()
        {
            IconChar = IconChar.LocationDot,
            IconColor = Color.FromArgb(0, 180, 216),
            IconSize = 14,
            Size = new Size(16, 16),
            Location = new Point(24, 49),
            BackColor = Color.Transparent
        };
        Controls.Add(picCoords);

        lblCoords.Text = $"Tọa độ: X={rect.X}, Y={rect.Y}  |  Kích thước: {rect.Width} × {rect.Height} px";
        lblCoords.ForeColor = Color.FromArgb(170, 185, 205);
        lblCoords.Location = new Point(44, 48);
        lblCoords.AutoSize = true;
        Controls.Add(lblCoords);

        int currentY = 76;

        // If editing existing item: Show Overwrite vs Create New choice
        if (isEditing)
        {
            Panel pnlChoice = new()
            {
                Location = new Point(24, currentY),
                Size = new Size(436, 52),
                BackColor = Color.FromArgb(34, 38, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            radOverwrite.Text = $"Lưu đè (Cập nhật '{_originalName}')";
            radOverwrite.Checked = true;
            radOverwrite.ForeColor = Color.FromArgb(0, 220, 255);
            radOverwrite.Font = new Font("Segoe UI Semibold", 8.5F);
            radOverwrite.Location = new Point(12, 4);
            radOverwrite.AutoSize = true;
            radOverwrite.Cursor = Cursors.Hand;
            radOverwrite.CheckedChanged += (s, e) =>
            {
                if (radOverwrite.Checked)
                {
                    btnSave.Text = " Lưu đè";
                    btnSave.IconChar = IconChar.FloppyDisk;
                    ValidateName();
                }
            };
            pnlChoice.Controls.Add(radOverwrite);

            radSaveAsNew.Text = "Tạo đối tượng mới (Bằng tên mới bên dưới)";
            radSaveAsNew.ForeColor = Color.FromArgb(200, 215, 240);
            radSaveAsNew.Font = new Font("Segoe UI Semibold", 8.5F);
            radSaveAsNew.Location = new Point(12, 26);
            radSaveAsNew.AutoSize = true;
            radSaveAsNew.Cursor = Cursors.Hand;
            radSaveAsNew.CheckedChanged += (s, e) =>
            {
                if (radSaveAsNew.Checked)
                {
                    btnSave.Text = " Tạo mới";
                    btnSave.IconChar = IconChar.Plus;
                    // Auto suggest new name if name currently equals original name
                    if (string.Equals(txtName.Text.Trim(), _originalName, StringComparison.OrdinalIgnoreCase))
                    {
                        txtName.Text = $"{_originalName}_copy";
                    }
                    ValidateName();
                }
            };
            pnlChoice.Controls.Add(radSaveAsNew);

            Controls.Add(pnlChoice);
            currentY += 60;
        }

        // Name Label
        Label lblName = new()
        {
            Text = "Tên định danh (bắt buộc, không trùng lặp):",
            Location = new Point(24, currentY),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F)
        };
        Controls.Add(lblName);
        currentY += 24;

        // Name TextBox
        txtName.Text = defaultName;
        txtName.Location = new Point(24, currentY);
        txtName.Size = new Size(436, 28);
        txtName.BackColor = Color.FromArgb(40, 44, 56);
        txtName.ForeColor = Color.White;
        txtName.BorderStyle = BorderStyle.FixedSingle;
        txtName.TextChanged += (s, e) => ValidateName();
        Controls.Add(txtName);
        currentY += 30;

        // Inline Error Label (for duplicate name / empty name warning)
        lblError.Text = "";
        lblError.ForeColor = Color.FromArgb(255, 95, 85);
        lblError.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblError.Location = new Point(24, currentY);
        lblError.Size = new Size(436, 18);
        lblError.Visible = false;
        Controls.Add(lblError);
        currentY += 22;

        // Notes Label
        Label lblNotes = new()
        {
            Text = "Ghi chú (tùy chọn):",
            Location = new Point(24, currentY),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F)
        };
        Controls.Add(lblNotes);
        currentY += 24;

        // Notes TextBox
        txtNotes.Location = new Point(24, currentY);
        txtNotes.Size = new Size(436, 26);
        txtNotes.BackColor = Color.FromArgb(40, 44, 56);
        txtNotes.ForeColor = Color.White;
        txtNotes.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(txtNotes);
        currentY += 38;

        // Save Button (IconButton)
        btnSave.Text = isEditing ? " Lưu đè" : " Lưu";
        btnSave.IconChar = isEditing ? IconChar.FloppyDisk : IconChar.Check;
        btnSave.IconColor = Color.White;
        btnSave.IconSize = 16;
        btnSave.TextImageRelation = TextImageRelation.ImageBeforeText;
        btnSave.ImageAlign = ContentAlignment.MiddleLeft;
        btnSave.TextAlign = ContentAlignment.MiddleCenter;
        btnSave.Padding = new Padding(10, 0, 10, 0);
        btnSave.Location = new Point(228, currentY);
        btnSave.Size = new Size(114, 36);
        btnSave.BackColor = Color.FromArgb(0, 168, 150);
        btnSave.ForeColor = Color.White;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Cursor = Cursors.Hand;
        btnSave.Font = new Font("Segoe UI Semibold", 9.5F);
        btnSave.Click += (s, e) =>
        {
            if (!ValidateName()) return;
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnSave);

        // Cancel Button (IconButton)
        btnCancel.Text = " Hủy";
        btnCancel.IconChar = IconChar.Xmark;
        btnCancel.IconColor = Color.FromArgb(220, 220, 230);
        btnCancel.IconSize = 16;
        btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
        btnCancel.ImageAlign = ContentAlignment.MiddleLeft;
        btnCancel.TextAlign = ContentAlignment.MiddleCenter;
        btnCancel.Padding = new Padding(10, 0, 10, 0);
        btnCancel.Location = new Point(352, currentY);
        btnCancel.Size = new Size(108, 36);
        btnCancel.BackColor = Color.FromArgb(55, 60, 75);
        btnCancel.ForeColor = Color.FromArgb(220, 220, 230);
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.Font = new Font("Segoe UI", 9.5F);
        btnCancel.Click += (s, e) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Shown += (s, e) =>
        {
            txtName.Focus();
            txtName.SelectAll();
            ValidateName();
        };
    }

    private bool ValidateName()
    {
        string name = txtName.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            lblError.Text = "Vui lòng nhập tên (không được để trống)!";
            lblError.Visible = true;
            btnSave.Enabled = false;
            btnSave.BackColor = Color.FromArgb(60, 70, 80);
            return false;
        }

        bool isSaveAsNew = _originalName != null && radSaveAsNew.Checked;

        bool isDuplicate;
        if (isSaveAsNew)
        {
            // For new item, it cannot match ANY existing name
            isDuplicate = _existingNames.Contains(name);
        }
        else
        {
            // Overwriting: can match original name, but not OTHER existing names
            isDuplicate = _existingNames.Contains(name) &&
                !string.Equals(name, _originalName, StringComparison.OrdinalIgnoreCase);
        }

        if (isDuplicate)
        {
            lblError.Text = $"Tên '{name}' đã tồn tại! Vui lòng chọn tên khác.";
            lblError.Visible = true;
            btnSave.Enabled = false;
            btnSave.BackColor = Color.FromArgb(60, 70, 80);
            return false;
        }

        lblError.Visible = false;
        btnSave.Enabled = true;
        btnSave.BackColor = isSaveAsNew ? Color.FromArgb(0, 180, 216) : Color.FromArgb(0, 168, 150);
        return true;
    }
}
