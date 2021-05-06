using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard
{
	static class LocalizerExtension
	{
		internal static string Localize(this string message)
		{
			return Plugin.localizer.Localize(message);
		}
	}
    class Localizer
    {
        public UILang Language;
        private Dictionary<string, string> zh = new Dictionary<string, string> { };
        private Dictionary<string, string> en = new Dictionary<string, string> { };
        public Localizer(UILang language)
        {
            Language = language;
            LoadZh();
        }
        public string Localize(string message)
        {
            if (message == null) return message;
            if (Language == UILang.CN) return zh.ContainsKey(message) ? zh[message] : message;
            if (Language == UILang.EN) return en.ContainsKey(message) ? en[message] : message;
            return message;
        }
        private void LoadZh()
        {
            zh.Add("Import midi file.", "导入MIDI文件");
            zh.Add("Clear Playlist", "清空播放列表");
            zh.Add("Language", "语言");
            zh.Add("Help", "常见问题");

            zh.Add("Change the UI Language.", "改变界面语言");
            zh.Add("Ensemble Mode Running", "合奏模式运行中");
            zh.Add("Ensemble Mode Preparing", "合奏模式准备小节");
            zh.Add("Import midi files to start performing!", "导入一些MIDI文件来开始演奏！");

            zh.Add($"tracks in playlist.", "首歌曲在播放列表中。");
            zh.Add($"Playing: ", "正在播放：");
            zh.Add($"track in playlist.", "首歌曲在播放列表中。");

            zh.Add($"Playmode: ", "播放模式：");
            zh.Add("Single", "单曲播放（单曲结束后停止）");
            zh.Add("ListOrdered", "列表顺序（列表结束后停止）");
            zh.Add("ListRepeat", "列表循环");
            zh.Add("SingleRepeat", "单曲循环");
            zh.Add("Random", "随机播放");

            zh.Add("Toggle player control panel", "演奏控制面板");
            zh.Add("Toggle settings panel", "播放器设置面板");
            zh.Add("Toggle mini player", "切换迷你播放器");
            zh.Add("Track Selection. \r\nMidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance.",
                "音轨选择。\r\nMIDIBARD只会播放被选中的音轨，在合奏中有用。\r\n请注意：在演奏中切换音轨将会打断当前演奏。");
            zh.Add("Track", "音轨");
            zh.Add($"notes)", "音符)");
            zh.Add("Note Offset", "音高偏移");
            zh.Add("Octave+", "升八度");
            zh.Add("Octave-", "降八度");
            zh.Add("Add 1 octave(12 Semitone) onto all notes.", "对将要演奏的所有音符升高八度（12个半音）");
            zh.Add("Subtract 1 octave(12 Semitone) onto all notes.", "对将要演奏的所有音符降低八度（12个半音）");
            zh.Add("Reset Offset", "重置偏移");
            zh.Add("AdaptNotes", "自适应音高");
            zh.Add("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play", 
	            "对超出演奏范围的音符自动升/降八度直至其可以被演奏。");
            zh.Add("Progress", "演奏进度");
            zh.Add("Speed", "演奏速度");
            zh.Add("Gets or sets the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.",
	            "设置播放速度。\r\n1代表正常速度，举例：将其设为0.5会使播放速度减半。\r\n右键点击来将它重置为1。");
            zh.Add("Monitor ensemble", "监控合奏");
            zh.Add("Auto start ensemble when entering in-game party ensemble mode.", "在游戏内的合奏助手运行时自动开始同步合奏。");
            zh.Add("Auto Confirm Ensemble Ready Check", "合奏准备自动确认");
            zh.Add("Playlist size", "播放列表大小");
            zh.Add("Play list rows number.", "播放列表窗口同时显示的行数");
            zh.Add("Player width", "播放器宽度");
            zh.Add("Player window max width.", "播放器窗口最大宽度");
            zh.Add("Auto open MidiBard window", "自动打开MIDIBARD");
            zh.Add("Open MidiBard window automatically when entering performance mode", "在进入演奏模式时自动打开MIDIBARD窗口。");
            zh.Add("Import in progress...", "正在导入...");
            zh.Add("One or more of your modpacks failed to import.\nPlease submit a bug report.", "你有一个或多个MID文件导入失败。");
            zh.Add("Select a mid file", "选择MID文件");


        }
    }
}