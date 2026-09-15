using System.Drawing;
using System.Windows.Forms;

namespace SOFCMeas
{
    public class LogViewForm : Form
    {
        public LogViewForm() : this(null)
        {
        }

        internal LogViewForm(Control content)
        {
            SuspendLayout();
            Name = "LogViewForm";
            Text = "LOG VIEW";
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(232, 239, 244);
            ClientSize = new Size(1920, 960);
            if (content != null)
            {
                content.Dock = DockStyle.Fill;
                Controls.Add(content);
                content.Visible = true;
            }
            ResumeLayout(true);
        }
    }
}
