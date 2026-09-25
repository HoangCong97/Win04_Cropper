using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Win04_Cropper.Services;

namespace Win04_Cropper.Controls;

public class ProjectNameDialog : Form
{
    private readonly TextBox txtProjectName;
    private readonly Label lblError;
    private readonly Button btnOk;
    private readonly Button btnCancel;
    private readonly string? _excludeProjectId;

    public string ProjectName => txtProjectName.Text.Trim();

    public ProjectNameDialog(string title = "Tên dự án", string prompt = "Nhập tên cho dự án:", string defaultName = "", string? excludeProjectId = null)
    {
        _excludeProjectId = excludeProjectId;

        Text = title;
        Size = new Size(450, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(32, 35, 42);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        Label lblPrompt = new()
        {
            Text = prompt,
            UseMnemonic = false,
            Location = new Point(20, 18),
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 210, 225)
        };
        Controls.Add(lblPrompt);

        txtProjectName = new TextBox
        {
            Location = new Point(20, 45),
            Size = new Size(395, 26),
            BackColor = Color.FromArgb(45, 50, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = defaultName
        };
        txtProjectName.TextChanged += (s, e) => ValidateName();
        Controls.Add(txtProjectName);

        lblError = new Label
        {
            Location = new Point(20, 77),
            Size = new Size(395, 38),
            ForeColor = Color.FromArgb(255, 107, 107),
            Font = new Font("Segoe UI", 8.5F),
            Text = "",
            Visible = false
        };
        Controls.Add(lblError);

        btnOk = new Button
        {
            Text = "Xác nhận",
            DialogResult = DialogResult.OK,
            Location = new Point(225, 126),
            Size = new Size(95, 32),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            if (!ValidateName())
            {
                DialogResult = DialogResult.None;
            }
        };
        Controls.Add(btnOk);

        btnCancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Location = new Point(330, 126),
            Size = new Size(85, 32),
            BackColor = Color.FromArgb(60, 65, 78),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Shown += (s, e) =>
        {
            txtProjectName.Focus();
            txtProjectName.SelectAll();
            ValidateName();
        };
    }

    private bool ValidateName()
    {
        string name = txtProjectName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            lblError.Text = "Vui lòng nhập tên dự án (không được để trống)!";
            lblError.Visible = true;
            btnOk.Enabled = false;
            btnOk.BackColor = Color.FromArgb(60, 70, 80);
            return false;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.Any(c => invalidChars.Contains(c)))
        {
            lblError.Text = "Tên dự án chứa ký tự không hợp lệ cho file (\\ / : * ? \" < > |)!";
            lblError.Visible = true;
            btnOk.Enabled = false;
            btnOk.BackColor = Color.FromArgb(60, 70, 80);
            return false;
        }

        if (ProjectService.IsProjectNameExists(name, _excludeProjectId))
        {
            lblError.Text = $"Tên dự án '{name}' đã tồn tại! Vui lòng chọn tên khác.";
            lblError.Visible = true;
            btnOk.Enabled = false;
            btnOk.BackColor = Color.FromArgb(60, 70, 80);
            return false;
        }

        lblError.Visible = false;
        btnOk.Enabled = true;
        btnOk.BackColor = Color.FromArgb(16, 185, 129);
        return true;
    }
}
