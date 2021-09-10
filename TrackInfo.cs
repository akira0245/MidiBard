using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
  public class TrackInfo
  {
    public IEnumerable<string> TrackNameEventsText;
    public IEnumerable<string> TextEventsText;
    public IEnumerable<string> ProgramChangeEvent;
    public IEnumerable<ChannelInfo> ChannelInfos;
    public int NoteCount;
    public Note LowestNote;
    public Note HighestNote;
    public ITimeSpan Duration;

    public override string ToString()
    {
      return $"{TrackNameEventsText.FirstOrDefault() ?? "Untitled"} / {NoteCount} notes / {LowestNote}-{HighestNote}";
    }

    public string ToLongString()
    {
      return $"Track name:\n　{TrackNameEventsText.FirstOrDefault() ?? "Untitled"} \nNote count: \n　{NoteCount} notes \nRange:\n　{LowestNote}-{HighestNote} \n ProgramChange events: \n　{string.Join("\n　", ProgramChangeEvent.Distinct())} \nDuration: \n　{Duration}";
    }

    public string GetTrackName()
    {
      return TrackNameEventsText.FirstOrDefault() ?? "Untitled";
    }
  }

  public class ChannelInfo
  {
  }
}