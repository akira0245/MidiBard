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

			var inputDevices = InputDeviceManager.Devices;

			if (ImGui.BeginCombo("Input Device".Localize(), InputDeviceManager.CurrentInputDevice.ToDeviceString()))
			{
				if (ImGui.Selectable("None##device", InputDeviceManager.CurrentInputDevice is null))
				{
					InputDeviceManager.DisposeDevice();
				}

				for (int i = 0; i < inputDevices.Length; i++)
				{
					var device = inputDevices[i];
					if (ImGui.Selectable($"{device.Name}##{i}", device.Id == InputDeviceManager.CurrentInputDevice?.Id))
					{
						InputDeviceManager.SetDevice(device);
					}
				}

				ImGui.EndCombo();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				InputDeviceManager.DisposeDevice();
			}

			ImguiUtil.ToolTip("Choose external midi input device. right click to reset.".Localize());


			if (ImGui.Combo("UI Language".Localize(), ref MidiBard.config.uiLang, uilangStrings, 2))
			{
				MidiBard.Localizer = new Localizer((UILang)MidiBard.config.uiLang);
			}


			ImGui.Checkbox("Auto open MidiBard".Localize(), ref MidiBard.config.AutoOpenPlayerWhenPerforming);
			ImguiUtil.ToolTip("Open MidiBard window automatically when entering performance mode".Localize());
			//ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
			//if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Monitor ensemble".Localize(), ref MidiBard.config.MonitorOnEnsemble);
			ImguiUtil.ToolTip("Auto start ensemble when entering in-game party ensemble mode.".Localize());

			ImGui.Checkbox("Auto switch instrument".Localize(), ref MidiBard.config.autoSwitchInstrumentByFileName);
			ImguiUtil.ToolTip("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid".Localize());

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Auto transpose".Localize(), ref MidiBard.config.autoTransposeByFileName);
			ImguiUtil.ToolTip(
				"Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid"
					.Localize());

			ImGui.Checkbox("Override guitar tones".Localize(), ref MidiBard.config.OverrideGuitarTones);
			ImguiUtil.ToolTip("Assign different guitar tones for each midi tracks".Localize());
		}
	}
}