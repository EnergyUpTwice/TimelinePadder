using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.PluginLogic
{
    public class TimelinePadder : IPlugin
    {
        // ── IPlugin Metadata ─────────────────────────────────────────
        public string  Name        => "TimelinePadder";
        public string  Text        => "Timeline Padder – 时间轴智能扩展...";
        public decimal Version     => 1.0m;
        public string  Description => "按 PadStart/PadEnd 毫秒数智能扩展时间轴，自动处理间隙/重叠。";
        public string  ActionType  => "sync";   // 出现在 Synchronization 菜单
        public string  Shortcut    => string.Empty;

        // ── 时间戳正则 ───────────────────────────────────────────────
        private static readonly Regex _tsRegex =
            new Regex(@"(\d{2}:\d{2}:\d{2}[,\.]\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}[,\.]\d{3})",
                      RegexOptions.Compiled);

        // ── Entry ─────────────────────────────────────────────────────
        public string DoAction(Form parentForm, string srtText, double frameRate,
            string uiLineBreak, string file, string videoFile, string rawText)
        {
            if (string.IsNullOrWhiteSpace(srtText))
                return string.Empty;

            // 1. 弹出参数对话框
            using (var dlg = new SettingsForm())
            {
                if (dlg.ShowDialog(parentForm) != DialogResult.OK)
                    return string.Empty;

                int padStart = dlg.PadStart;
                int padEnd   = dlg.PadEnd;

                // 2. 解析
                var subs = ParseSrt(srtText);
                if (subs.Count == 0) return string.Empty;

                // 3. 排序
                subs.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));

                // 4. 核心处理
                Process(subs, padStart, padEnd);

                // 5. 序列化输出
                return Serialize(subs);
            }
        }

        // ── 数据结构 ─────────────────────────────────────────────────
        internal class SubEntry
        {
            public int    Index;
            public long   OrigStart, OrigEnd;
            public long   StartMs,   EndMs;
            public string Text;
            // 处理后的值
            public long   NewStart, NewEnd;
        }

        // ── 解析 SRT ─────────────────────────────────────────────────
        internal List<SubEntry> ParseSrt(string srtText)
        {
            var result  = new List<SubEntry>();
            var blocks  = srtText.Replace("\r\n", "\n").Replace("\r", "\n")
                                 .Trim().Split(new[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                var lines = block.Trim().Split('\n');
                if (lines.Length < 3) continue;

                if (!int.TryParse(lines[0].Trim(), out int idx)) continue;

                var m = _tsRegex.Match(lines[1]);
                if (!m.Success) continue;

                long startMs = TsToMs(m.Groups[1].Value);
                long endMs   = TsToMs(m.Groups[2].Value);
                if (startMs < 0 || endMs < 0) continue;

                var textLines = new List<string>();
                for (int i = 2; i < lines.Length; i++)
                    textLines.Add(lines[i]);

                result.Add(new SubEntry
                {
                    Index     = idx,
                    OrigStart = startMs,
                    OrigEnd   = endMs,
                    StartMs   = startMs,
                    EndMs     = endMs,
                    NewStart  = startMs,
                    NewEnd    = endMs,
                    Text      = string.Join("\n", textLines).Trim()
                });
            }
            return result;
        }

        // ── 核心时间轴处理 ───────────────────────────────────────────
        internal void Process(List<SubEntry> subs, int padStart, int padEnd)
        {
            int total = padStart + padEnd;

            // 处理相邻接缝
            for (int i = 0; i < subs.Count - 1; i++)
            {
                var A = subs[i];
                var B = subs[i + 1];
                long gap = B.StartMs - A.EndMs;

                if (gap <= 0)
                {
                    // 情况一：重叠或无缝 → 不动
                }
                else if (total == 0)
                {
                    // padStart + padEnd = 0，无需处理
                }
                else if (gap >= total)
                {
                    // 情况二：空间足够
                    A.NewEnd   = A.EndMs   + padEnd;
                    B.NewStart = B.StartMs - padStart;
                }
                else
                {
                    // 情况三：空间不足，按比例分配
                    long giveEnd   = (long)Math.Round(gap * ((double)padEnd   / total));
                    long giveStart = (long)Math.Round(gap * ((double)padStart / total));
                    A.NewEnd   = A.EndMs   + giveEnd;
                    B.NewStart = B.StartMs - giveStart;
                }
            }

            // 首条前沿 / 末条后沿
            subs[0].NewStart                  = subs[0].StartMs - padStart;
            subs[subs.Count - 1].NewEnd       = subs[subs.Count - 1].EndMs + padEnd;

            // 兜底校验
            foreach (var s in subs)
            {
                // 零点拦截
                if (s.NewStart < 0) s.NewStart = 0;

                // 倒置拦截 → 恢复原始值
                if (s.NewStart > s.NewEnd)
                {
                    s.NewStart = s.OrigStart;
                    s.NewEnd   = s.OrigEnd;
                }
            }
        }

        // ── 序列化 ───────────────────────────────────────────────────
        internal string Serialize(List<SubEntry> subs)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < subs.Count; i++)
            {
                var s = subs[i];
                sb.AppendLine((i + 1).ToString());
                sb.AppendLine($"{MsToTs(s.NewStart)} --> {MsToTs(s.NewEnd)}");
                sb.AppendLine(s.Text);
                sb.AppendLine(); // 块间空行
            }
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        // ── 时间戳转换 ───────────────────────────────────────────────
        internal static long TsToMs(string ts)
        {
            // HH:MM:SS,mmm 或 HH:MM:SS.mmm
            var m = Regex.Match(ts, @"(\d{2}):(\d{2}):(\d{2})[,\.](\d{3})");
            if (!m.Success) return -1;
            return long.Parse(m.Groups[1].Value) * 3600000L
                 + long.Parse(m.Groups[2].Value) *   60000L
                 + long.Parse(m.Groups[3].Value) *    1000L
                 + long.Parse(m.Groups[4].Value);
        }

        internal static string MsToTs(long ms)
        {
            if (ms < 0) ms = 0;
            long h  = ms / 3600000; ms %= 3600000;
            long mi = ms /   60000; ms %=   60000;
            long s  = ms /    1000;
            long f  = ms %    1000;
            return $"{h:D2}:{mi:D2}:{s:D2},{f:D3}";
        }
    }
}
