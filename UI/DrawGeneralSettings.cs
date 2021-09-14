using System.Numerics;
using ImGuiNET;

namespace MidiBard
{
	public partial class PluginUI
	{
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

			var inputDevices = DeviceManager.Devices;

			if (ImGui.BeginCombo("Input Device".Localize(), DeviceManager.CurrentInputDevice.ToDeviceString()))
			{
				if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
				{
					DeviceManager.DisposeDevice();
				}

				for (int i = 0; i < inputDevices.Length; i++)
				{
					var device = inputDevices[i];
					if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
					{
						DeviceManager.SetDevice(device);
					}
				}

				ImGui.EndCombo();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				DeviceManager.DisposeDevice();
			}

			ImguiUtil.ToolTip("Choose external midi input device. right click to reset.".Localize());


			if (ImGui.Combo("UI Language".Localize(), ref MidiBard.config.uiLang, uilangStrings, 2))
			{
				MidiBard.localizer = new Localizer((UILang)MidiBard.config.uiLang);
			}


			ImGui.Checkbox("Auto open MidiBard".Localize(), ref MidiBard.config.AutoOpenPlayerWhenPerforming);
			ImguiUtil.HelpMarker("Open MidiBard window automatically when entering performance mode".Localize());
			//ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
			//if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Monitor ensemble".Localize(), ref MidiBard.config.MonitorOnEnsemble);
			ImguiUtil.HelpMarker("Auto start ensemble when entering in-game party ensemble mode.".Localize());

			ImGui.Checkbox("Auto transpose".Localize(), ref MidiBard.config.autoTransposeByFileName);
			ImguiUtil.HelpMarker(
				"Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid"
					.Localize());

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Auto switch instrument".Localize(), ref MidiBard.config.autoSwitchInstrumentByFileName);
			ImguiUtil.HelpMarker(
				"Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid"
					.Localize());

			ImGui.Checkbox("Override guitar tones".Localize(), ref MidiBard.config.OverrideGuitarTones);
			ImguiUtil.HelpMarker("Assign different guitar tones for each midi tracks".Localize());

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
			if (ImGui.Button("Debug info", new Vector2(-2, ImGui.GetFrameHeight()))) MidiBard.Debug ^= true;
		}
	}
}