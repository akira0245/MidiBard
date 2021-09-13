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
		delegate IntPtr sub_14070A1C0(uint sourceId, IntPtr data);
		private readonly Hook<sub_14070A1C0> soloReceivedHook;

		delegate IntPtr sub_14070A230(uint sourceId, IntPtr data);
		private readonly Hook<sub_14070A230> ensembleReceivedHook;

		delegate void sub_14119B2E0(IntPtr a1);
		private readonly Hook<sub_14119B2E0> soloSendHook;

		delegate void sub_14119B120(IntPtr a1);
		private readonly Hook<sub_14119B120> ensembleSendHook;

		static string toString<T>(in Span<T> t) where T : struct => string.Join(' ', t.ToArray().Select(i => $"{i:X}"));
		static string toString<T>(in T[] t) where T : struct => string.Join(' ', t.Select(i => $"{i:X}"));

		private NetworkManager()
		{
			ensembleSendHook = new Hook<sub_14119B120>(OffsetManager.Instance.EnsembleSendHandler, (dataptr) =>
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

			soloSendHook = new Hook<sub_14119B2E0>(OffsetManager.Instance.SoloSendHandler, (dataptr) =>
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

			soloReceivedHook = new Hook<sub_14070A1C0>(OffsetManager.Instance.SoloReceivedHandler, (id, data) =>
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

			ensembleReceivedHook = new Hook<sub_14070A230>(OffsetManager.Instance.EnsembleReceivedHandler, (id, data) =>
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

		private unsafe void SoloSend(IntPtr dataptr)
		{
			unsafe
			{
				Span<byte> notes = new Span<byte>((dataptr + 0x10).ToPointer(), 10);
				Span<byte> tones = new Span<byte>((dataptr + 0x10 + 10).ToPointer(), 10);
				PluginLog.Information($"[{nameof(SoloSend)}] {toString(notes)} : {toString(tones)}");
			}
		}

		private unsafe void EnsembleSend(IntPtr dataptr)
		{
			Span<byte> notes = new Span<byte>((dataptr + 0x10).ToPointer(), 60);
			Span<byte> tones = new Span<byte>((dataptr + 0x10 + 60).ToPointer(), 60);
			PluginLog.Warning($"[{nameof(EnsembleSend)}] [MYSELF] {toString(notes)} : {toString(tones)}");
		}


		private void SoloRecv(uint sourceId, IntPtr data)
		{
			var ipc = Marshal.PtrToStructure<SoloPerformanceIpc>(data);
			PluginLog.Information($"[{nameof(SoloRecv)}] {toString(ipc.NoteNumbers)} : {toString(ipc.NoteTones)}");

		}

		private void EnsembleRecv(uint sourceId, IntPtr data)
		{
			var ipc = Marshal.PtrToStructure<EnsemblePerformanceIpc>(data);
			foreach (var perCharacterData in ipc.EnsembleCharacterDatas.Where(i => i.IsValid))
			{
				PluginLog.Information($"[{nameof(EnsembleRecv)}] {perCharacterData.CharacterId:X} {toString(perCharacterData.NoteNumbers)}");
			}
			//PluginLog.Information($"[{nameof(EnsembleRecv)}] {toString(meData.NoteNumbers)} : {toString(meData.ToneNumbers)}");

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
