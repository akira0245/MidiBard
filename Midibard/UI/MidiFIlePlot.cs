using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using ImGuiNET;
using ImPlotNET;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Util;

namespace MidiBard;

public partial class PluginUI
{
	static Vector4 HSVToRGB(float h, float s, float v, float a = 1)
	{
		Vector4 c;
		ImGui.ColorConvertHSVtoRGB(h, s, v, out c.X, out c.Y, out c.Z);
		c.W = a;
		return c;
	}


	//private uint[] ChannelColorPalette = Enumerable.Range(0, 16).Select(i => ImGui.ColorConvertFloat4ToU32(HSVToRGB(i / 16f, 0.75f, 1))).ToArray();

	private bool setNextLimit;
	private double timeWindow = 10;
	private void DrawPlotWindow()
	{

		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.SetNextWindowBgAlpha(0);
		ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(640, 480), ImGuiCond.FirstUseEver);
		if (_resetPlotWindowPosition && MidiBard.config.PlotTracks)
		{
			ImGui.SetNextWindowPos(new Vector2(100), ImGuiCond.Always);
			ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(640, 480), ImGuiCond.Always);
			_resetPlotWindowPosition = false;
		}
		if (ImGui.Begin("Midi tracks##MIDIBARD", ref MidiBard.config.PlotTracks))
		{
			ImGui.PopStyleVar();
			MidiPlotWindow();
		}
		else
		{
			ImGui.PopStyleVar();
		}

		ImGui.End();
	}

	private unsafe void MidiPlotWindow()
	{
		if (ImGui.IsWindowAppearing())
		{
			RefreshPlotData();
		}

		double timelinePos = 0;

		try
		{
			var currentPlayback = MidiBard.CurrentPlayback;
			if (currentPlayback != null)
			{
				timelinePos = currentPlayback.GetCurrentTime<MetricTimeSpan>().GetTotalSeconds();
			}
		}
		catch (Exception e)
		{
			//
		}

		string songName = "";
		try
		{
			songName = PlaylistManager.FilePathList[PlaylistManager.CurrentPlaying].displayName;
		}
		catch (Exception e)
		{
			//
		}
		if (ImPlot.BeginPlot(songName + "###midiTrackPlot",
				ImGui.GetWindowSize() - ImGuiHelpers.ScaledVector2(0, ImGui.GetCursorPosY()), ImPlotFlags.NoTitle))
		{
			ImPlot.SetupAxisLimits(ImAxis.X1, 0, 20, ImPlotCond.Once);
			ImPlot.SetupAxisLimits(ImAxis.Y1, 36, 97, ImPlotCond.Once);
			ImPlot.SetupAxisTicks(ImAxis.Y1, 0, 127, 128, noteNames, false);

			if (setNextLimit)
			{
				try
				{
					if (!MidiBard.config.LockPlot)
						ImPlot.SetupAxisLimits(ImAxis.X1, 0, _plotData.Select(i => i.trackInfo.DurationMetric.GetTotalSeconds()).Max(), ImPlotCond.Always);
					setNextLimit = false;
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when try set next plot limit");
				}
			}

			if (MidiBard.config.LockPlot)
			{
				var imPlotRange = ImPlot.GetPlotLimits(ImAxis.X1).X;
				var d = (imPlotRange.Max - imPlotRange.Min) / 2;
				ImPlot.SetupAxisLimits(ImAxis.X1, timelinePos - d, timelinePos + d, ImPlotCond.Always);
			}


			var drawList = ImPlot.GetPlotDrawList();
			var xMin = ImPlot.GetPlotLimits().X.Min;
			var xMax = ImPlot.GetPlotLimits().X.Max;

			//if (!MidiBard.config.LockPlot) timeWindow = (xMax - xMin) / 2;

			ImPlot.PushPlotClipRect();


			var cp = ImGuiColors.ParsedBlue;
			cp.W = 0.05f;
			drawList.AddRectFilled(ImPlot.PlotToPixels(xMin, 48 + 37), ImPlot.PlotToPixels(xMax, 48), ImGui.ColorConvertFloat4ToU32(cp));

			if (_plotData?.Any() == true && MidiBard.CurrentPlayback != null)
			{
				var legendInfoList = new List<(string trackName, Vector4 color, int index)>();

				float ProgramNamePositionOffset = ImGui.GetTextLineHeight() * 2;

				foreach (var (trackInfo, notes) in _plotData.OrderBy(i => i.trackInfo.IsPlaying))
				{
					Vector4 GetNoteColor()
					{
						var c = System.Numerics.Vector4.One;
						try
						{
							ImGui.ColorConvertHSVtoRGB(trackInfo.Index / (float)MidiBard.CurrentPlayback.TrackInfos.Length, 0.8f, 1, out c.X, out c.Y, out c.Z);
							if (!trackInfo.IsPlaying) c.W = 0.2f;
						}
						catch (Exception e)
						{
							PluginLog.Error(e, "error when getting track color");
						}
						return c;
					}

					var noteColor = GetNoteColor();
					var noteColorRgb = ImGui.ColorConvertFloat4ToU32(noteColor);

					legendInfoList.Add(($"[{trackInfo.Index + 1:00}] {trackInfo.TrackName}", noteColor, trackInfo.Index));


					foreach (var (start, end, noteNumber) in notes.Where(i => i.end > xMin && i.start < xMax))
					{
						var translatedNoteNum =
							BardPlayDevice.GetNoteNumberTranslatedPerTrack(noteNumber, trackInfo.Index) + 48;
						drawList.AddRectFilled(
							ImPlot.PlotToPixels(start, translatedNoteNum + 1),
							ImPlot.PlotToPixels(end, translatedNoteNum),
							noteColorRgb, 4);
					}
				}

				foreach (var (trackName, color, _) in legendInfoList.OrderBy(i => i.index))
				{
					ImPlot.SetNextLineStyle(color);
					var f = double.NegativeInfinity;
					ImPlot.PlotLine(trackName, ref f, 1);
				}
			}

			DrawCurrentPlayTime(drawList, timelinePos);
			ImPlot.PopPlotClipRect();

			ImPlot.EndPlot();
		}
	}
	private static bool IsGuitarProgram(byte programNumber) => programNumber is 27 or 28 or 29 or 30 or 31;

	private static unsafe bool TryGetFfxivInstrument(byte programNumber, out Instrument instrument)
	{
		var firstOrDefault = MidiBard.Instruments.FirstOrDefault(i => i.ProgramNumber == programNumber);
		instrument = firstOrDefault;
		return firstOrDefault != default;
	}

	private static void DrawCurrentPlayTime(ImDrawListPtr drawList, double timelinePos)
	{
		drawList.AddLine(
			ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Min),
			ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Max),
			ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudRed),
			ImGuiHelpers.GlobalScale);
	}

	public unsafe void RefreshPlotData()
	{
		if (!MidiBard.config.PlotTracks) return;
		Task.Run(() =>
		{
			try
			{
				if (MidiBard.CurrentPlayback?.TrackInfos == null)
				{
					PluginLog.Debug("try RefreshPlotData but CurrentTracks is null");
					return;
				}

				var tmap = MidiBard.CurrentPlayback.TempoMap;

				_plotData = MidiBard.CurrentPlayback.TrackChunks.Select((trackChunk, index) =>
					{
						var trackNotes = trackChunk.GetNotes()
							.Select(j => (j.TimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(),
								j.EndTimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), (int)j.NoteNumber))
							.ToArray();

						return (MidiBard.CurrentPlayback.TrackInfos[index], notes: trackNotes);
					})
					.ToArray();

				setNextLimit = true;
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when refreshing plot data");
			}
		});
	}

	private static Dictionary<byte, uint> GetChannelColorPalette(byte[] allNoteChannels)
	{
		return allNoteChannels.OrderBy(i => i)
			.Select((channelNumber, index) => (channelNumber, color: HSVToRGB(index / (float)allNoteChannels.Length, 0.8f, 1)))
			.ToDictionary(tuple => (byte)tuple.channelNumber, tuple => ImGui.ColorConvertFloat4ToU32(tuple.color));
	}

	private (TrackInfo trackInfo, (double start, double end, int noteNumber)[] notes)[] _plotData;

	private string[] noteNames = Enumerable.Range(0, 128)
		.Select(i => i % 12 == 0 ? new Note(new SevenBitNumber((byte)i)).ToString() : string.Empty)
		.ToArray();

	private static unsafe T* Alloc<T>() where T : unmanaged
	{
		var allocHGlobal = (T*)Marshal.AllocHGlobal(sizeof(T));
		*allocHGlobal = new T();
		return allocHGlobal;
	}

	//private unsafe float rounding;
	//private unsafe int stride;
	//private unsafe double* height = Alloc<double>();
	//private unsafe double* shift = Alloc<double>();
	//private float[] valuex = null;
	//private float[] valuex2 = null;
	//private float[] valuey = null;
	//private float[] valuey2 = null;
	//private static bool setup = true;
}