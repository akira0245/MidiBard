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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using MidiBard.Control;
using Dalamud;
using MidiBard.Resources;

namespace MidiBard;

static class InputDeviceManager
{
	internal static readonly Thread ScanMidiDeviceThread =
		new Thread(() =>
			{
				PluginLog.Information("device scanning thread started.");

				while (ShouldScanMidiDeviceThread)
				{
					try
					{
						Devices = InputDevice.GetAll().OrderBy(i => i.Name).ToArray();
						var devicesNames = Devices.Select(i => i.DeviceName()).ToArray();

						//PluginLog.Information(string.Join(", ", devicesNames));
						//PluginLog.Information(MidiBard.config.lastUsedMidiDeviceName);

						if (CurrentInputDevice is not null)
						{
							if (!devicesNames.Contains(CurrentInputDevice.DeviceName()))
							{
								PluginLog.Debug("disposing disconnected device");
								DisposeCurrentInputDevice();
							}
						}
						else if (CurrentInputDevice is null)
						{
							//if (MidiBard.config.autoRestoreListening)
							{
								if (devicesNames.Contains(MidiBard.config.lastUsedMidiDeviceName))
								{
									PluginLog.Information($"try restoring midi device: \"{MidiBard.config.lastUsedMidiDeviceName}\"");
									var newDevice = Devices?.FirstOrDefault(i => i.Name == MidiBard.config.lastUsedMidiDeviceName);
									if (newDevice != null)
									{
										SetDevice(newDevice);
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						PluginLog.Error(e, "error in midi device scanning thread");
					}

					Thread.Sleep(500);
				}
				PluginLog.Information("device scanning thread ended.");
			})
		{ IsBackground = true, Priority = ThreadPriority.BelowNormal };

	internal static bool ShouldScanMidiDeviceThread = true;
	//internal static bool ShouldReloadMidiDevice = true;

	internal static bool IsListeningForEvents
	{
		get
		{
			var ret = false;
			try
			{
				ret = CurrentInputDevice?.IsListeningForEvents == true;
			}
			catch (Exception e)
			{
				PluginLog.Debug(e, "device maybe disposed.");
			}

			return ret;
		}
	}

	internal static string DeviceName(this InputDevice device)
	{
		return device?.Name ?? "None";
	}

	internal static InputDevice CurrentInputDevice { get; private set; }

	internal static string[] LastDevicesNames { get; private set; } = { };

	internal static InputDevice[] Devices { get; private set; } = { };

	internal static void SetDevice(InputDevice device)
	{
		DisposeCurrentInputDevice();
		MidiBard.config.lastUsedMidiDeviceName = device?.DeviceName();
		if (device is null) return;

		try
		{
			CurrentInputDevice = device;
			CurrentInputDevice.SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOff;
			CurrentInputDevice.EventReceived += InputDevice_EventReceived;
			CurrentInputDevice.StartEventsListening();
			ImGuiUtil.AddNotification(NotificationType.Success,
				string.Format(Language.text_start_event_listening, CurrentInputDevice.Name));
		}
		catch (Exception e)
		{
			MidiBard.config.lastUsedMidiDeviceName = "";
			ImGuiUtil.AddNotification(NotificationType.Error,
				string.Format(Language.notice_midi_device_error, CurrentInputDevice.Name));
			PluginLog.Error(e, "midi device is possibly being occupied.");
			DisposeCurrentInputDevice();
		}
	}

	internal static void DisposeCurrentInputDevice()
	{
		if (CurrentInputDevice == null) return;

		try
		{
			CurrentInputDevice.EventReceived -= InputDevice_EventReceived;
			CurrentInputDevice.Dispose();
			ImGuiUtil.AddNotification(NotificationType.Info, string.Format(Language.notice_midi_device_stop_listening, CurrentInputDevice.Name));
		}
		catch (Exception e)
		{
			PluginLog.Error(e, "error when disposing existing Input device");
		}
		finally
		{
			CurrentInputDevice?.Dispose();
			CurrentInputDevice = null;
		}
	}

	private static void InputDevice_EventReceived(object sender, MidiEventReceivedEventArgs e)
	{
		PluginLog.Verbose($"[{sender}]{e.Event}");
		BardPlayDevice.Instance.SendEventWithMetadata(e.Event, new BardPlayDevice.MidiDeviceMetaData());
	}
}