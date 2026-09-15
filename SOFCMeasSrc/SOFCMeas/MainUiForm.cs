using System.Drawing;
using System.Windows.Forms;

namespace SOFCMeas
{
    public class MainUiForm : Form
    {
        public MainUiForm() : this(null)
        {
        }

        internal MainUiForm(Control content)
        {
            SuspendLayout();
            Name = "MainUiForm";
            Text = "MAIN";
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(232, 239, 244);
            ClientSize = new Size(1920, 960);
            if (content != null)
            {
                content.Dock = DockStyle.Fill;
                Controls.Add(content);
            }
            ResumeLayout(true);
        }
    }
}
