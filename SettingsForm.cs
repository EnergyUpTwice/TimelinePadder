using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.PluginLogic
{
    internal sealed class SettingsForm : Form
    {
        public int PadStart { get; private set; }
        public int PadEnd   { get; private set; }

        private readonly NumericUpDown _nudStart;
        private readonly NumericUpDown _nudEnd;

        internal SettingsForm(int defaultStart = 120, int defaultEnd = 120)
        {
            Text            = "Timeline Padder – 时间轴扩展";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            ClientSize      = new Size(320, 310);
            Font            = new Font("Segoe UI", 9f);

            var grp = new GroupBox { Text = "扩展参数（毫秒）",
                Location = new Point(12, 8), Size = new Size(296, 110) };
            Controls.Add(grp);

            grp.Controls.Add(new Label { Text = "PadStart  向前提前:",
                Location = new Point(10, 28), Size = new Size(180, 22) });
            _nudStart = new NumericUpDown { Minimum = 0, Maximum = 30000,
                Value = defaultStart, Location = new Point(200, 26), Size = new Size(80, 23) };
            grp.Controls.Add(_nudStart);

            grp.Controls.Add(new Label { Text = "PadEnd  向后延长:",
                Location = new Point(10, 68), Size = new Size(180, 22) });
            _nudEnd = new NumericUpDown { Minimum = 0, Maximum = 30000,
                Value = defaultEnd, Location = new Point(200, 66), Size = new Size(80, 23) };
            grp.Controls.Add(_nudEnd);

            // 快捷预设
            Controls.Add(new Label { Text = "快捷预设", Location = new Point(12, 125), Size = new Size(100, 20), Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

            AddPresetButton("±100ms", 100, 100, new Point(12, 150), 90);
            AddPresetButton("±200ms", 200, 200, new Point(110, 150), 90);
            AddPresetButton("前300/后500", 300, 500, new Point(208, 150), 100);

            AddPresetButton("前500/后300", 500, 300, new Point(12, 190), 110);
            AddPresetButton("仅延长结尾", 0, 200, new Point(130, 190), 100);
            AddPresetButton("仅提前开头", 200, 0, new Point(12, 230), 100);

            // 底部按钮
            var btnOk = new Button { Text = "处理", DialogResult = DialogResult.OK,
                Location = new Point(136, 270), Size = new Size(80, 28) };
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel,
                Location = new Point(224, 270), Size = new Size(80, 28) };
            Controls.AddRange(new Control[] { btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            btnOk.Click += (s, e) => { PadStart = (int)_nudStart.Value; PadEnd = (int)_nudEnd.Value; };
        }

        private void AddPresetButton(string text, int start, int end, Point loc, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.System
            };
            btn.Click += (s, e) => { _nudStart.Value = start; _nudEnd.Value = end; };
            Controls.Add(btn);
        }
    }
}
