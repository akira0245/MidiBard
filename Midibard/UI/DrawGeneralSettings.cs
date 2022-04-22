using System;
using System.Numerics;
using ImGuiNET;

namespace MidiBard;

public partial class PluginUI
{
    private readonly string[] _toolTips = {
        "Off: Does not take over game's guitar tone control.",
        "Standard: Standard midi channel and ProgramChange handling, each channel will keep it's program state separately.",
        "Simple: Simple ProgramChange handling, ProgramChange event on any channel will change all channels' program state. (This is BardMusicPlayer's default behavior.)",
        "Override by track: Assign guitar tone manually for each track and ignore ProgramChange events.",
    };

    private void DrawPanelGeneralSettings()
    {
        //ImGui.SliderInt("Playlist size".Localize(), ref config.playlistSizeY, 2, 50,
        //	config.playlistSizeY.ToString(), ImGuiSliderFlags.AlwaysClamp);
        //ToolTip("Play list rows number.".Localize());

        //ImGui.SliderInt("Player width".Localize(), ref config.playlistSizeX, 356, 1000, config.playlistSizeX.ToString(), ImGuiSliderFlags.AlwaysClamp);
        //ToolTip("Player window max width.".Localize());

        //var inputDevices = InputDevice.GetAll().ToList();
        //var currentDeviceInt = inputDevices.FindIndex(device => device == CurrentInputDevice);

        //if (ImGui.Combo(CurrentInputDevice.ToString(), ref currentDeviceInt, inputDevices.Select(i => $"{i.Id} {i.Name}").ToArray(), inputDevices.Count))
        //{
        //	//CurrentInputDevice.Connect(CurrentOutputDevice);
        //}


        var inputDevices = InputDeviceManager.Devices;

        if (ImGui.BeginCombo("Input Device".Localize(), InputDeviceManager.CurrentInputDevice.DeviceName()))
        {
            if (ImGui.Selectable("None##device", InputDeviceManager.CurrentInputDevice is null))
            {
                InputDeviceManager.SetDevice(null);
            }

            for (int i = 0; i < inputDevices.Length; i++)
            {
                var device = inputDevices[i];
                if (ImGui.Selectable($"{device.Name}##{i}", device.Name == InputDeviceManager.CurrentInputDevice?.Name))
                {
                    InputDeviceManager.SetDevice(device);
                }
            }

            ImGui.EndCombo();
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            InputDeviceManager.SetDevice(null);
        }

        ImGuiUtil.ToolTip("Choose external midi input device. right click to reset.".Localize());

        ImGui.Checkbox("Auto restart listening".Localize(), ref MidiBard.config.autoRestoreListening);
        ImGuiUtil.ToolTip("Try auto restart listening last used midi device".Localize());
        //ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
        //ImGui.Checkbox("Auto listening new device".Localize(), ref MidiBard.config.autoStartNewListening);
        //ImGuiUtil.ToolTip("Auto start listening new midi input device when idle.".Localize());

        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

        ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionWidth() / 3.36f);
        ImGuiUtil.EnumCombo("Tone mode".Localize(), ref MidiBard.config.GuitarToneMode, _toolTips);
        ImGuiUtil.ToolTip("Choose how MidiBard will handle MIDI channels and ProgramChange events(current only affects guitar tone changing)".Localize());

        ImGui.Checkbox("Tracks visualization".Localize(), ref MidiBard.config.PlotTracks);
        ImGuiUtil.ToolTip("Draw midi tracks in a new window\nshowing the on/off and actual transposition of each track".Localize());
        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

        ImGui.Checkbox("Follow playback".Localize() + $" ({timeWindow:F2}s)###followPlayBack", ref MidiBard.config.LockPlot);
        if (ImGui.IsItemHovered())
        {
            timeWindow *= Math.Pow(Math.E, ImGui.GetIO().MouseWheel * -0.1);
        }
        ImGuiUtil.ToolTip(
            MidiBard.config.LockPlot
                ? "Lock tracks window and auto following current playback progress\nScroll mouse here to adjust view timeline scale".Localize()
                :"Lock tracks window and auto following current playback progress".Localize());
			
        ImGui.Checkbox("Auto open MidiBard".Localize(), ref MidiBard.config.AutoOpenPlayerWhenPerforming);
        ImGuiUtil.ToolTip("Open MidiBard window automatically when entering performance mode".Localize());
        //ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
        //if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

        ImGui.Checkbox("Monitor ensemble".Localize(), ref MidiBard.config.MonitorOnEnsemble);
        ImGuiUtil.ToolTip("Auto start ensemble when entering in-game party ensemble mode.".Localize());

        ImGui.Checkbox("Auto switch instrument".Localize(), ref MidiBard.config.autoSwitchInstrumentBySongName);
        ImGuiUtil.ToolTip("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid".Localize());

        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

        ImGui.Checkbox("Auto transpose".Localize(), ref MidiBard.config.autoTransposeBySongName);
        ImGuiUtil.ToolTip("Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid".Localize());

        //ImGui.Checkbox("Override guitar tones".Localize(), ref MidiBard.config.OverrideGuitarTones);
        //ImGuiUtil.ToolTip("Assign different guitar tones for each midi tracks".Localize());


        ImGuiUtil.ColorPickerWithPalette(1000, "Theme color".Localize(), ref MidiBard.config.themeColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
        //if (ImGui.ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
        //	ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
        MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
        MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            MidiBard.config.themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
            MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
            MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.TextUnformatted("Theme color".Localize());
            
        ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
        ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionWidth() / 3.36f);
        if (ImGui.Combo("UI Language".Localize(), ref MidiBard.config.uiLang, uilangStrings, 2))
            MidiBard.Localizer = new Localizer((UILang)MidiBard.config.uiLang);

        //#if DEBUG
        ImGui.Checkbox("BMP track name compatible(testing)".Localize(), ref MidiBard.config.bmpTrackNames);
        ImGuiUtil.ToolTip("Transpose/switch instrument based on first enabled midi track name.".Localize());
        //#endif
    }
}