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
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using static ImGuiNET.ImGui;

namespace MidiBard
{
	public partial class PluginUI
	{
		private bool init;
		private bool setNextLimit;
		private double timeWindow = 10;

		private unsafe void MidiPlotWindow()
		{
			#region initImplot

			if (!init)
			{
				ImPlot.SetImGuiContext(GetCurrentContext());
				var _context = ImPlot.CreateContext();
				ImPlot.SetCurrentContext(_context);

				init = true;
			}

			#endregion

			if (IsWindowAppearing())
			{
				RefeshPlotData();
			}

			//Spacing(); ;
			//SetCursorPosX(GetStyle().ItemSpacing.X / 2);
			//if (Button($"refresh data")) RefeshPlotData();
			//SameLine();
			//SetNextItemWidth(GetWindowContentRegionWidth() / 3);
			//SliderFloat("scale", ref MidiBard.config.plotScale, 1000 * 1000, 1000 * 1000 * 60,
			//	MidiBard.config.plotScale.ToString("##.##"), ImGuiSliderFlags.AlwaysClamp);
			//SameLine(GetWindowWidth()-ImGuiHelpers.GlobalScale*25);
			//if (ImGuiUtil.IconButton((FontAwesomeIcon)61453, "close")) PlotTracks = false;


			double timelinePos = 0;

			try
			{
				var currentPlayback = MidiBard.CurrentPlayback;
				timelinePos = currentPlayback.GetCurrentTime<MetricTimeSpan>().GetTotalSeconds();
				//duration = currentPlayback.GetDuration<MetricTimeSpan>().GetTotalSeconds();
				//var duram = currentPlayback.GetDuration<MetricTimeSpan>().TotalMicroseconds / 1000d / 1000d;
				//TextUnformatted(duration.ToString());
				//TextUnformatted(duram.ToString());
				//duration /= duram;
				//duration *= MidiBard.config.plotScale;

			}
			catch (Exception e)
			{
				//
			}

			ImPlot.SetNextPlotTicksY(0, 127, 128, noteNames, false);
			ImPlot.SetNextPlotLimitsY(36, 97, ImGuiCond.Appearing);
			if (setNextLimit)
			{
				ImPlot.SetNextPlotLimitsX(0, data.Select(i => i.info.DurationMetric.GetTotalSeconds()).Max(), ImGuiCond.Always);
				setNextLimit = false;
			}

			if (MidiBard.config.LockPlot)
			{
				ImPlot.SetNextPlotLimitsX(timelinePos - timeWindow, timelinePos + timeWindow, ImGuiCond.Always);
			}


			//ImPlot.SetNextPlotLimitsY(36, 96, ImGuiCond.Appearing);
			//var imPlotRange = ImPlot.GetPlotLimits().X;
			//if (timelinePos > imPlotRange.Max)

			//ImPlot.ShowStyleSelector("style");
			//ImPlot.ShowColormapSelector("colormap");


			//ImPlot.SetColormap(ImPlotColormap.Plasma);
			//ImPlot.StyleColorsLight();
			string songName = "";
			try
			{
				songName = PlaylistManager.FilePathList[PlaylistManager.CurrentPlaying].songName;
			}
			catch (Exception e)
			{
				//
			}
			if (ImPlot.BeginPlot(songName + "###midiTrackPlot", null, null, GetWindowSize() - ImGuiHelpers.ScaledVector2(0, GetCursorPosY()), ImPlotFlags.NoChild | ImPlotFlags.NoTitle | ImPlotFlags.NoMousePos))
			{
				var drawList = ImPlot.GetPlotDrawList();
				var xMin = ImPlot.GetPlotLimits().X.Min;
				var xMax = ImPlot.GetPlotLimits().X.Max;
				ImPlot.PushPlotClipRect();
				var cp = ImGuiColors.ParsedBlue;
				cp.W = 0.05f;
				drawList.AddRectFilled(ImPlot.PlotToPixels(xMin, 48 + 37), ImPlot.PlotToPixels(xMax, 48), ColorConvertFloat4ToU32(cp));


				if (data?.Any() == true)
				{
					var list = new List<(string trackName, Vector4 color, int index)>();
					foreach (var (trackInfo, notes) in data.OrderBy(i => i.info.IsPlaying))
					{
						var vector4 = Vector4.One;
						ColorConvertHSVtoRGB(trackInfo.Index / (float)MidiBard.CurrentTracks.Count, 0.8f, 1, out vector4.X, out vector4.Y, out vector4.Z);

						if (!trackInfo.IsPlaying)
						{
							vector4.W = 0.2f;
						}

						var rgb = ColorConvertFloat4ToU32(vector4);
						list.Add(($"[{trackInfo.Index + 1:00}] {trackInfo.TrackName}", vector4, trackInfo.Index));

						foreach (var note in notes.Where(i => i.end > xMin && i.start < xMax))
						{
							var translatedNoteNum = BardPlayDevice.GetTranslatedNoteNum(note.NoteNumber, trackInfo.Index, out _) + 48;
							drawList.AddRectFilled(
								ImPlot.PlotToPixels(note.start, translatedNoteNum + 1),
								ImPlot.PlotToPixels(note.end, translatedNoteNum),
								rgb, 4);
						}
					}


					foreach (var (trackName, color, _) in list.OrderBy(i => i.index))
					{
						ImPlot.SetNextLineStyle(color);
						var f = double.NegativeInfinity;
						ImPlot.PlotVLines(trackName, ref f, 1);
					}
				}

				drawList.AddLine(ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Min),
					ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Max), ColorConvertFloat4ToU32(ImGuiColors.DalamudRed));
				ImPlot.PopPlotClipRect();

				ImPlot.EndPlot();
			}
		}

		public unsafe void RefeshPlotData()
		{
			if (!MidiBard.config.PlotTracks) return;
			Task.Run(() =>
			{
				try
				{
					var tmap = MidiBard.CurrentTMap;
					data = MidiBard.CurrentTracks.Select(i =>
						{
							var trackNotes = i.trackChunk.GetNotes()
								.Select(j => (j.TimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), j.EndTimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), (int)j.NoteNumber))
								.ToArray();

							//var color = Vector4.One;
							//ColorConvertHSVtoRGB(i.trackInfo.Index / (float)MidiBard.CurrentTracks.Count, 0.8f, 1,
							//	out color.X, out color.Y, out color.Z);

							return (i.trackInfo, notes: trackNotes);
						})
						.ToArray();
					setNextLimit = true;
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}
			});
		}

		private (TrackInfo info, (double start, double end, int NoteNumber)[] notes)[] data;

		private string[] noteNames = Enumerable.Range(0, 128)
			.Select(i => { return i % 12 != 0 ? string.Empty : new Note(new SevenBitNumber((byte)i)).ToString(); })
			.ToArray();

		private static unsafe T* Alloc<T>() where T : unmanaged
		{
			var allocHGlobal = (T*)Marshal.AllocHGlobal(sizeof(T));
			*allocHGlobal = new T();
			return allocHGlobal;
		}

		private unsafe float rounding;
		private unsafe int stride;
		private unsafe double* height = Alloc<double>();
		private unsafe double* shift = Alloc<double>();
		private float[] valuex = null;
		private float[] valuex2 = null;
		private float[] valuey = null;
		private float[] valuey2 = null;
		private static bool setup = true;
	}
}