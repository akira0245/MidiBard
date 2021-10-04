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
using Melanchall.DryWetMidi.Devices;
using MidiBard.DalamudApi;

namespace MidiBard
{
	static class InputDeviceManager
	{
		internal static readonly Thread ScanMidiDeviceThread = new Thread(() =>
		{
			PluginLog.Information("device scanning thread started.");

			while (ShouldScanMidiDeviceThread)
			{
				try
				{
					Devices = InputDevice.GetAll().OrderBy(i => i.Id).ToArray();
					var newDevicesNames = Devices.Select(i => i.DeviceName()).ToArray();
					if (CurrentInputDevice is not null)
					{
						if (!newDevicesNames.Contains(CurrentInputDevice.DeviceName()))
						{
							DisposeCurrentDevice();
						}
					}
					else
					{
						if (MidiBard.config.autoStartNewListening)
						{
							var newDeviceName = newDevicesNames.Where(i => !LastDevicesNames.Contains(i)).ToArray();
							if (newDeviceName.Any())
							{
								PluginLog.Warning($"new device detected: {string.Join(", ", newDeviceName)}");
								var newDevice =
									Devices.FirstOrDefault(i => i.DeviceName() == newDeviceName.First());
								if (newDevice is not null)
								{
									SetDevice(newDevice);
								}
							}
						}

						if (ShouldReloadMidiDevice && MidiBard.config.autoRestoreListening)
						{
							PluginLog.Warning($"try restoring midi device: \"{MidiBard.config.lastUsedMidiDeviceName}\"");
							ShouldReloadMidiDevice = false;
							//auto switch back to last used midi device when start performance
							if (newDevicesNames.Contains(MidiBard.config.lastUsedMidiDeviceName))
							{
								var newDevice = Devices?.FirstOrDefault(i => i.Name == MidiBard.config.lastUsedMidiDeviceName);
								if (newDevice != null)
								{
									SetDevice(newDevice);
								}
							}
						}
					}

					LastDevicesNames = newDevicesNames;
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error in midi device scanning thread");
				}

				Thread.Sleep(100);
			}
			PluginLog.Information("device scanning thread ended.");
		})
		{ IsBackground = true, Priority = ThreadPriority.BelowNormal };

		internal static bool ShouldScanMidiDeviceThread = true;
		internal static bool ShouldReloadMidiDevice = true;

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
			DisposeCurrentDevice();
			if (device is null) return;

			try
			{
				CurrentInputDevice = device;
				MidiBard.config.lastUsedMidiDeviceName = CurrentInputDevice.Name;
				CurrentInputDevice.SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOff;
				CurrentInputDevice.EventReceived += InputDevice_EventReceived;
				CurrentInputDevice.StartEventsListening();
				ImGuiUtil.AddNotification(NotificationType.Success,
					"Start event listening on \"{0}\".".Localize(CurrentInputDevice.Name),
					"Listening input device".Localize());
			}
			catch (Exception e)
			{
				ImGuiUtil.AddNotification(NotificationType.Error,
					"\"{0}\" is not available now.\nPlease check log for further error information.".Localize(device.Name),
					"Cannot start listening Midi device".Localize());
				PluginLog.Error(e, "midi device is possibly being occupied.");
				DisposeCurrentDevice();
			}
		}

		internal static void DisposeCurrentDevice()
		{
			try
			{
				if (CurrentInputDevice != null)
				{
					CurrentInputDevice.EventReceived -= InputDevice_EventReceived;
					CurrentInputDevice.Reset();
					ImGuiUtil.AddNotification(NotificationType.Info, $"Stop event listening on \"{CurrentInputDevice.Name}\"	.", "Midi device disconnected");
				}
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
			MidiBard.CurrentOutputDevice.SendEvent(e.Event);
		}
	}
}
