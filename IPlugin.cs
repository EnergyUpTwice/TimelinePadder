using System.Windows.Forms;

namespace Nikse.SubtitleEdit.PluginLogic
{
    public interface IPlugin
    {
        string Name        { get; }
        string Text        { get; }
        decimal Version    { get; }
        string Description { get; }
        string ActionType  { get; }   // "sync"
        string Shortcut    { get; }
        string DoAction(Form parentForm, string srtText, double frameRate,
            string uiLineBreak, string file, string videoFile, string rawText);
    }
}
