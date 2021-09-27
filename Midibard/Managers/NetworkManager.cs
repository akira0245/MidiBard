using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using MidiBard.Structs;

namespace MidiBard.Managers
{
	class NetworkManager : IDisposable
	{

		private unsafe void SoloSend(IntPtr dataptr)
		{
#if DEBUG
			Span<byte> notes = new Span<byte>((dataptr + 0x10).ToPointer(), 10);
			Span<byte> tones = new Span<byte>((dataptr + 0x10 + 10).ToPointer(), 10);
			PluginLog.Information($"[{nameof(SoloSend)}] {notes.toString()} : {tones.toString()}");
#endif
		}

		private unsafe void SoloRecv(uint sourceId, IntPtr data)
		{
#if DEBUG
			//var ipc = Marshal.PtrToStructure<SoloPerformanceIpc>(data);
			//PluginLog.Information($"[{nameof(SoloRecv)}] {toString(ipc.NoteNumbers)} : {toString(ipc.NoteTones)}");
#endif
		}

		private unsafe void EnsembleSend(IntPtr dataptr)
		{
#if DEBUG
			Span<byte> notes = new Span<byte>((dataptr + 0x10).ToPointer(), 60);
			Span<byte> tones = new Span<byte>((dataptr + 0x10 + 60).ToPointer(), 60);
			PluginLog.Information($"[{nameof(EnsembleSend)}] [MYSELF] {notes.toString()} : {tones.toString()}");
#endif
		}

		private unsafe void EnsembleRecv(uint sourceId, IntPtr data)
		{
#if DEBUG
			var ipc = Marshal.PtrToStructure<EnsemblePerformanceIpc>(data);
			if (MidiBard.Debug)
				foreach (var perCharacterData in ipc.EnsembleCharacterDatas.Where(i => i.IsValid))
				{
					PluginLog.Information($"[{nameof(EnsembleRecv)}] {perCharacterData.CharacterId:X} {perCharacterData.NoteNumbers.toString()}");
				}
#endif
		}

		delegate IntPtr sub_14070A1C0(uint sourceId, IntPtr data);
		private readonly Hook<sub_14070A1C0> soloReceivedHook;

		delegate IntPtr sub_14070A230(uint sourceId, IntPtr data);
		private readonly Hook<sub_14070A230> ensembleReceivedHook;

		delegate void sub_14119B2E0(IntPtr a1);
		private readonly Hook<sub_14119B2E0> soloSendHook;

		delegate void sub_14119B120(IntPtr a1);
		private readonly Hook<sub_14119B120> ensembleSendHook;

		private NetworkManager()
		{
			ensembleSendHook = new Hook<sub_14119B120>(Offsets.EnsembleSendHandler, (dataptr) =>
			{
				try
				{
					EnsembleSend(dataptr);
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"error in {nameof(ensembleSendHook)}");
				}

				ensembleSendHook.Original(dataptr);
			});

			soloSendHook = new Hook<sub_14119B2E0>(Offsets.SoloSendHandler, (dataptr) =>
			{
				try
				{
					SoloSend(dataptr);
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error in solo send handler hook");
				}

				soloSendHook.Original(dataptr);
			});

			soloReceivedHook = new Hook<sub_14070A1C0>(Offsets.SoloReceivedHandler, (id, data) =>
			{
				try
				{
					SoloRecv(id, data);
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error in solo recv handler hook");
				}
				return soloReceivedHook.Original(id, data);
			});

			ensembleReceivedHook = new Hook<sub_14070A230>(Offsets.EnsembleReceivedHandler, (id, data) =>
			{
				try
				{
					EnsembleRecv(id, data);
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error in ensemble recv handler hook");
				}
				return ensembleReceivedHook.Original(id, data);
			});


			ensembleSendHook.Enable();
			soloSendHook.Enable();
			soloReceivedHook.Enable();
			ensembleReceivedHook.Enable();
		}

		public static NetworkManager Instance { get; } = new NetworkManager();

		public void Dispose()
		{
			soloSendHook?.Dispose();
			ensembleSendHook?.Dispose();
			soloReceivedHook?.Dispose();
			ensembleReceivedHook?.Dispose();
		}
	}
}
