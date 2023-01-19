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
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.MidiControl;
using MidiBard.Resources;

namespace MidiBard;

public partial class PluginUI
{
	private static unsafe void DrawCurrentPlaying()
	{
		if (MidiBard.CurrentPlayback != null)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.config.themeColor * new Vector4(1, 1, 1, 1.3f));
			ImGui.TextUnformatted(MidiBard.CurrentPlayback.DisplayName);
			ImGui.PopStyleColor();
		}
		else
		{
			var c = PlaylistManager.FilePathList.Count;
			var text = string.Format(Language.text_tracks_in_playlist, c);
			ImGui.TextUnformatted($"{text}");
		}
	}

	private static unsafe void DrawProgressBar()
	{
		//ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x800000A0);

		MetricTimeSpan currentTime = new MetricTimeSpan(0);
		MetricTimeSpan duration = new MetricTimeSpan(0);
		float progress = 0;
		ImGui.PushStyleColor(ImGuiCol.PlotHistogram, FilePlayback.IsWaiting ? *ImGui.GetStyleColorVec4(ImGuiCol.Text) : MidiBard.config.themeColor);
		ImGui.PushStyleColor(ImGuiCol.FrameBg, MidiBard.config.themeColorDark);
		try
		{
			if (FilePlayback.IsWaiting)
			{
				try
				{
					ImGui.ProgressBar(FilePlayback.GetWaitWaitProgress, new Vector2(-1, 3));
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}
			}
			else
			{
				try
				{
					if (MidiBard.CurrentPlayback != null)
					{
						currentTime = MidiBard.CurrentPlayback.GetCurrentTime<MetricTimeSpan>();
						duration = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>();
						progress = (float)currentTime.Divide(duration);

						ImGui.ProgressBar(progress, new Vector2(-1, 3));
					}
					else
					{
						ImGui.ProgressBar(progress, new Vector2(-1, 3));
					}
				}
				catch (Exception e)
				{
					//
				}
			}
		}
		catch (Exception e)
		{
			PluginLog.Error(e.ToString());
		}
		finally
		{
			ImGui.PopStyleColor();
		}

		ImGui.TextUnformatted($"{currentTime.Hours}:{currentTime.Minutes:00}:{currentTime.Seconds:00}");
		var durationText = $"{duration.Hours}:{duration.Minutes:00}:{duration.Seconds:00}";
		ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() - ImGui.CalcTextSize(durationText).X);
		ImGui.TextUnformatted(durationText);
		try
		{
			var currentInstrument = MidiBard.PlayingGuitar && !(MidiBard.config.GuitarToneMode is GuitarToneMode.OverrideByTrack)
				? (uint)(24 + MidiBard.AgentPerformance.CurrentGroupTone)
				: MidiBard.CurrentInstrument;

			string currentInstrumentText;
			if (currentInstrument != 0)
			{
				currentInstrumentText = MidiBard.InstrumentSheet.GetRow(currentInstrument).Instrument;
				if (MidiBard.PlayingGuitar && !(MidiBard.config.GuitarToneMode is GuitarToneMode.OverrideByTrack))
				{
					currentInstrumentText = currentInstrumentText.Split(':', '：').First() + ": Auto";
				}
			}
			else
			{
				currentInstrumentText = string.Empty;
			}

			ImGui.SameLine((ImGuiUtil.GetWindowContentRegionWidth() - ImGui.CalcTextSize(currentInstrumentText).X) / 2);
			ImGui.TextUnformatted(currentInstrumentText);
		}
		catch (Exception e)
		{
			//
		}
		finally
		{
			ImGui.PopStyleColor();
		}
	}
}