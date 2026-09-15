using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SOFCMeas
{
    public sealed class AuthorityDialog : Form
    {
        private readonly RadioButton operatorRole = new RadioButton();
        private readonly RadioButton administratorRole = new RadioButton();
        private readonly TextBox password = new TextBox();
        private readonly Label error = new Label();
        public bool Administrator { get; private set; }

        public AuthorityDialog(bool administrator)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "사용자 권한 선택";
            ClientSize = new Size(430, 260);
            Font = new Font("맑은 고딕", 11F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            Controls.Add(new Label { Text = "권한", Location = new Point(20, 25), AutoSize = true });
            operatorRole.Name = "rdoOperator"; operatorRole.Text = "OPERATOR";
            operatorRole.SetBounds(125, 20, 280, 30);
            administratorRole.Name = "rdoAdministrator"; administratorRole.Text = "ADMINISTRATOR";
            administratorRole.SetBounds(125, 52, 280, 30);
            Controls.Add(operatorRole); Controls.Add(administratorRole);
            Controls.Add(new Label { Text = "비밀번호", Location = new Point(20, 105), AutoSize = true });
            password.Name = "txtPassword"; password.SetBounds(125, 100, 280, 32);
            password.UseSystemPasswordChar = true;
            Controls.Add(password);
            error.SetBounds(20, 145, 390, 40); error.ForeColor = Color.Firebrick; Controls.Add(error);
            var apply = new Button { Name = "btnApply", Text = "적용", Location = new Point(185, 205), Size = new Size(105, 36) };
            var cancel = new Button { Text = "취소", Location = new Point(300, 205), Size = new Size(105, 36), DialogResult = DialogResult.Cancel };
            Controls.Add(apply); Controls.Add(cancel); AcceptButton = apply; CancelButton = cancel;
            administratorRole.CheckedChanged += delegate
            {
                password.Clear(); password.Enabled = administratorRole.Checked; error.Text = string.Empty;
            };
            operatorRole.Checked = !administrator;
            administratorRole.Checked = administrator;
            password.Enabled = administrator;
            apply.Click += delegate
            {
                if (administratorRole.Checked && !PasswordMatches(password.Text))
                { error.Text = "비밀번호가 일치하지 않습니다."; password.SelectAll(); password.Focus(); return; }
                Administrator = administratorRole.Checked;
                DialogResult = DialogResult.OK;
            };
            ResumeLayout(true);
        }

        internal static void EnsureDefaultPassword()
        {
            if (ApplicationConfiguration.LoadAppSettings()["Security.AdminPassword"] == null)
                ApplicationConfiguration.SaveAppSettings(new[] { new KeyValuePair<string, string>("Security.AdminPassword", "1234") });
        }
        internal static bool PasswordMatches(string value)
        {
            var entry = ApplicationConfiguration.LoadAppSettings()["Security.AdminPassword"];
            return entry != null && string.Equals(entry.Value, value, StringComparison.Ordinal);
        }
    }
}
