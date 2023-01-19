// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Dalamud.Logging;

//namespace MidiBard;

//static class LocalizerExtension
//{
//    internal static string Localize(this string message) => MidiBard.Localizer.Localize(message);
//    internal static string Localize(this string format, params object[] objects) => string.Format(MidiBard.Localizer.Localize(format), objects);


//}
//class Localizer
//{
//    public UILang Language;
//    private Dictionary<string, string> zh = new();
//    private Dictionary<string, string> en = new();
//    public Localizer(UILang language)
//    {
//        Language = language;
//        LoadZh();
//    }
//    public string Localize(string message)
//    {
//        if (message == null) return null;
//        if (Language == UILang.CN && zh.ContainsKey(message)) return zh[message];
//        if (Language == UILang.EN && en.ContainsKey(message)) return en[message];

//        //PluginLog.Verbose(message);
//        return message;
//    }

//    private void LoadZh()
//    {
//        zh.Add("Import midi file\nRight click to select file dialog type", "导入MIDI文件\n右键点击选择导入文件对话框类型");
//        zh.Add("Import folder\nImports all midi files in selected folder and it's all subfolders.\nThis may take a while when you select a folder that contains multiple layers of folders.", 
//	        "导入MIDI文件\n右键点击选择导入文件对话框类型");
//        zh.Add("Clear Playlist", "清空播放列表");
//        zh.Add("UI Language", "界面语言");
//        zh.Add("Help", "常见问题");

//        zh.Add("Change the UI Language.", "改变界面语言");
//        zh.Add("Ensemble Mode Running", "合奏模式运行中");
//        zh.Add("Ensemble Mode Preparing", "合奏模式准备小节");
//        zh.Add("Import midi files to start performing!", "导入一些MIDI文件来开始演奏！");

//        zh.Add($"tracks in playlist.", "首乐曲在播放列表中。");
//        zh.Add($"Playing: ", "正在播放：");
//        zh.Add($"track in playlist.", "首乐曲在播放列表中。");

//        zh.Add($"Playmode: ", "播放模式：");
//        zh.Add("Single", "单曲播放（单曲结束后停止）");
//        zh.Add("ListOrdered", "列表顺序（列表结束后停止）");
//        zh.Add("ListRepeat", "列表循环");
//        zh.Add("SingleRepeat", "单曲循环");
//        zh.Add("Random", "随机播放");

//        zh.Add("Music control panel", "演奏控制面板");
//        zh.Add("Settings panel", "播放器设置面板");
//        zh.Add("Mini player", "切换迷你播放器");
//        zh.Add("Track Selection. \nMidiBard will only perform enabled tracks.\nLeft click to enable/disable a track, Right click to solo it.",
//            "音轨选择。\r\nMIDIBARD只会演奏被选中的音轨。\n左键单击选择/取消选择音轨，右键单击Solo该音轨。");
//        zh.Add("Track", "音轨");
//        zh.Add($"notes)", "音符)");
//        zh.Add("Transpose", "全音轨移调");
//        zh.Add("Reset##note", "重置音高");
//        zh.Add("Auto adapt notes", "自适应音高");
//        zh.Add("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play",
//            "对超出演奏范围的音符自动升/降八度直至其可以被演奏。");
//        zh.Add("Progress", "演奏进度");
//        zh.Add("Speed", "演奏速度");
//        zh.Add("Monitor ensemble", "监控合奏");
//        zh.Add("Auto start ensemble when entering in-game party ensemble mode.", "在游戏内的合奏助手运行时自动开始同步合奏。");
//        zh.Add("Auto Confirm Ensemble Ready Check", "合奏准备自动确认");
//        zh.Add("Auto open MidiBard", "自动打开MIDIBARD");
//        zh.Add("Open MidiBard window automatically when entering performance mode", "在进入演奏模式时自动打开MIDIBARD窗口。");
//        zh.Add("Import in progress...", "正在导入...");
//        zh.Add("Instrument", "乐器选择");
//        zh.Add("Auto switch instrument", "自动切换乐器");
//        zh.Add("Auto transpose", "自动移调");
//        zh.Add("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid",
//            "根据要求自动切换乐器。如果需要自动切换乐器，请在文件开头添加 #乐器名#。\n例如：#鲁特琴#demo.mid");
//        zh.Add("Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid",
//            "根据要求自动移调。如果需要自动移调，请在文件开头添加 #要移调的半音数量#。\n例如：#-12#demo.mid");

//        zh.Add("Transpose, measured by semitone. \nRight click to reset.", "移调，以半音数计算。\n点击+或-键升高或降低一个八度，右键点击来将它重置回0。");
//        zh.Add("Set the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.",
//            "设置Midi事件的播放速度倍数。\n例如将其设为0.5会使播放速度减半。\n右键点击来将它重置回1。");
//        zh.Add("Set the playing progress. \nRight click to restart current playback.", "乐曲播放进度，右键点击回到乐曲开头。");
//        zh.Add("Select current instrument. \nRight click to quit performance mode.", "设置和切换当前乐器，右键点击会退出演奏模式。");
//        zh.Add("Listening input device: ", "正在监听MIDI输入：");
//        zh.Add("Input Device", "输入设备");
//        zh.Add("Choose external midi input device. right click to reset.", "选择当前的外部midi输入设备，例如虚拟midi接口或midi键盘。\n右键点击来停止使用外部输入。");
//        zh.Add("Double click to clear playlist.", "双击来清空播放列表");
//        zh.Add("Search playlist", "搜索播放列表");
//        zh.Add("Assign different guitar tones for each midi tracks.", "为每个Midi轨道分别指定电吉他音色。");
//        zh.Add("Theme color", "主题颜色");
//        zh.Add("Enter to search", "输入开始搜索");
//        zh.Add("Delay", "间隔时间");
//        zh.Add("Delay time before play next track.", "在连续播放时每首乐曲播放结束后的等待时间。");
//        zh.Add("Midibard auto performance only supports 37-key layout.\nPlease consider switching in performance settings.", "Midibard自动演奏仅支持37键布局。\n请考虑在操作设置中切换。");
//        zh.Add("Transpose per track", "分音轨移调");
//        zh.Add("Transpose per track, right click to reset all tracks' transpose offset back to zero.", "启用分音轨移调，右键点击将全部音轨的移调偏移重置回0。");
//        zh.Add("Auto restart listening", "自动恢复监听");
//        //zh.Add("Auto listening new device", "自动开始监听");
//        zh.Add("Try auto restart listening last used midi device", "尝试监听最后使用过的MIDI输入设备。");
//        //zh.Add("Auto start listening new midi input device when idle.", "尝试自动对新连接的MIDI设备开始监听。");
//        zh.Add("Assign different guitar tones for each midi tracks", "为每个音轨指定不同的电吉他音色。");
//        zh.Add("Tracks visualization", "音轨可视化");
//        zh.Add("Draw midi tracks in a new window\nshowing the on/off and actual transposition of each track", "在新窗口中绘制MIDI音轨图像\n该图像将会反映MIDI音轨实际的开关情况和移调");
//        zh.Add("Follow playback", "跟随播放进度");
//        zh.Add("Lock tracks window and auto following current playback progress\nScroll mouse here to adjust view timeline scale", "锁定音轨窗口并自动跟随当前播放进度\n在此滚动鼠标滚轮以调整视图时间轴比例");
//        zh.Add("Lock tracks window and auto following current playback progress", "锁定音轨窗口并自动跟随当前播放进度");
//        zh.Add("Win32 file dialog", "Win32 文件对话框");
//        zh.Add("ImGui file dialog", "ImGui 文件对话框");
//        zh.Add("Import files from clipboard", "导入剪贴板中的文件");
//        zh.Add("Tone mode", "音色模式");
//        zh.Add("Choose how MidiBard will handle MIDI channels and ProgramChange events(current only affects guitar tone changing)", "选择MIDIBARD如何处理MIDI轨道和音色转换事件（当前只用于吉他音色控制）");
//        zh.Add("Off", "关闭");
//        zh.Add("Standard", "标准");
//        zh.Add("Simple", "简单");
//        zh.Add("Override", "分音轨重写");
//        zh.Add("Off: Does not take over game's guitar tone control.", "关闭：不使用自动吉他音色控制。");
//        zh.Add("Standard: Standard midi channel and ProgramChange handling, each channel will keep it's program state separately.", "标准：标准MIDI通道和音色转换事件处理，每个MIDI通道会分别保持其音色，直到同通道的另一个音色转换事件改写其音色状态。");
//        zh.Add("Simple: Simple ProgramChange handling, ProgramChange event on any channel will change all channels' program state. (This is BardMusicPlayer's default behavior.)", "简单：简单的音色转换事件处理方式，任一通道上的音色转换事件会改写所有MIDI通道的音色状态。（这是BardMusicPlayer的默认处理方式。）");
//        zh.Add("Override by track: Assign guitar tone manually for each track and ignore ProgramChange events.", "分音轨重写：为每个音轨手动指定电吉他音色，无视其音色转换事件。");
//        zh.Add("Auto switch instrument by track name(BMP Rules)", "根据音轨名自动切换乐器(BMP规则)");
//        zh.Add("Transpose/switch instrument based on first enabled midi track name.", "根据首个被启用的音轨的音轨名自动切换乐器和移调。");
//    }
//}