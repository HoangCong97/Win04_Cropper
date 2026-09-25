using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace Win04_Cropper.Controls;

public class ProjectNameDialog : Form
{
    private readonly TextBox txtProjectName;
    private readonly Button btnOk;
    private readonly Button btnCancel;

    public string ProjectName => txtProjectName.Text.Trim();

    public ProjectNameDialog(string title = "Tên dự án", string prompt = "Nhập tên cho dự án:", string defaultName = "")
    {
        Text = title;
        Size = new Size(420, 180);
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
            Size = new Size(365, 26),
            BackColor = Color.FromArgb(45, 50, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = defaultName
        };
        Controls.Add(txtProjectName);

        btnOk = new Button
        {
            Text = "Xác nhận",
            DialogResult = DialogResult.OK,
            Location = new Point(195, 90),
            Size = new Size(95, 32),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(txtProjectName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên dự án!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
        };
        Controls.Add(btnOk);

        btnCancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Location = new Point(300, 90),
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
        };
    }
}
