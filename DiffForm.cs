namespace PowerShellAnalyzer
{
    public class DiffForm : Form
    {
        public DiffForm(string id1, string name1, string content1, string id2, string name2, string content2, double score)
        {
            Text = "Skript-Vergleich";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterParent;

            var lblHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = $"Vergleich: [{id1}] {name1}  <--->  [{id2}] {name2}   (Ähnlichkeit: {score:P1})",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(240, 244, 248)
            };
            Controls.Add(lblHeader);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 5
            };
            Controls.Add(splitContainer);
            splitContainer.BringToFront();

            var txtLeft = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = content1,
                ReadOnly = true,
                WordWrap = false,
                BackColor = Color.White
            };
            var txtRight = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                Text = content2,
                ReadOnly = true,
                WordWrap = false,
                BackColor = Color.White
            };

            splitContainer.Panel1.Controls.Add(txtLeft);
            splitContainer.Panel2.Controls.Add(txtRight);
        }
    }
}