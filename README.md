# **MidiBard**

MidiBard是基于[卫月框架](https://bbs.tggfl.com/topic/32/dalamud-%E5%8D%AB%E6%9C%88%E6%A1%86%E6%9E%B6)的强大且易用的诗人midi演奏插件。目前版本兼容国服及国际服。

# 主要特性
* 无需繁琐的键位配置，打开即用。
* 免去配置时间，使用合奏助手实现一键高精度合奏。
* 毫秒精度的音符回放和零按键延迟，最大程度还原乐曲细节。
* 超出音域音符的自适应功能，节省适配midi文件的时间。
* 内置播放列表并支持搜索，一次导入演奏所有你喜爱的乐曲。
* 通过插件界面一键切换乐器，或随乐曲播放自动切换为对应的乐器。
* 多音轨选择，可以选择任意个音轨同时演奏或合奏，演奏中切换音轨也不会打断演奏。
* 支持外部midi输入，可使用midi键盘演奏或对接虚拟midi设备，实时试听乐谱制作效果。
* 为每个midi音轨指定不同的电吉他音色并单人演奏！****国服需5.55更新电吉他音色后可用***

# 插件界面
[![2Nfn2t.png](https://z3.ax1x.com/2021/06/05/2Nfn2t.png)](https://imgtu.com/i/2Nfn2t)

[![2NWbuT.png](https://z3.ax1x.com/2021/06/05/2NWbuT.png)](https://imgtu.com/i/2NWbuT)

[![2NhtTe.png](https://z3.ax1x.com/2021/06/05/2NhtTe.png)](https://imgtu.com/i/2NhtTe)

# 安装方法
> MidiBard需要[卫月框架](https://bbs.tggfl.com/topic/32/dalamud-%E5%8D%AB%E6%9C%88%E6%A1%86%E6%9E%B6)，如未安装请参考[原帖](https://bbs.tggfl.com/topic/32/dalamud-%E5%8D%AB%E6%9C%88%E6%A1%86%E6%9E%B6)安装后继续。

正确安装卫月框架并注入后在游戏聊天框中输入`/xlsettings`打开Dalamud 设置窗口，复制该源  
`https://raw.githubusercontent.com/akira0245/DalamudPlugins/api4/pluginmaster.json` 并将其添加到插件仓库  

[![gw7vxx.png](https://z3.ax1x.com/2021/05/12/gw7vxx.png)](https://imgtu.com/i/gw7vxx)

成功添加后在`/xlplugins`中搜索MidiBard并安装即可。

# How to install
You need to add my custom plugin repository to install MidiBard.  
`https://raw.githubusercontent.com/akira0245/DalamudPlugins/api4/pluginmaster.json`  
Click the link below for more detailed instructions.  
https://github.com/akira0245/DalamudPlugins

# 使用FAQ
* **如何开始使用MIDIBARD演奏？**  
MIDIBARD窗口默认在角色进入演奏模式后自动弹出。点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表。仅支持.mid格式的乐曲。导入时按Ctrl或Shift可以选择多个文件一同导入。双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。

* **如何使用MIDIBARD进行多人合奏？**  
MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，请为每位合奏成员分别选择其需要演奏的音轨。选择音轨后队长点击节拍器窗口的“合奏准备确认”按钮，并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。  
注：节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，在乐曲结束时合奏会自动停止。

* **如何让MIDIBARD为不同乐曲自动切换音调和乐器？**  
在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。例如：原乐曲文件名为“demo.mid”，将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。  
注：可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。

* **如何为MIDIBARD配置外部Midi输入（如虚拟Midi接口或Midi键盘）？**  
在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现“正在监听Midi输入”信息后即可使用外部输入。

* **后台演奏时有轻微卡顿不流畅怎么办？**  
在游戏内 *系统设置→显示设置→帧数限制* 中取消勾选 *“程序在游戏窗口处于非激活状态时限制帧数”* 并应用设置。

---
> 本项目遵循 GNU Affero General Public License v3.0 协议开源。  
> 项目源码可在 https://github.com/akira0245/MidiBard 查看（写的很烂被迫开源  
> 欢迎补充英文翻译和pr


# 其他问题

> 有bug或功能建议或讨论可以加qq群：260985966
