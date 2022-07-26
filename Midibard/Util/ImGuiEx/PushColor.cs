using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace MidiBard.Util
{
    internal class PushColor:IDisposable
    {
        private int length;
        public PushColor(uint color, params ImGuiCol[] imGuiCols)
        {
            length = imGuiCols.Length;
            for (int i = 0; i < length; i++)
            {
                ImGui.PushStyleColor(imGuiCols[i], color);
            }
        }
        public PushColor(Vector4 color, params ImGuiCol[] imGuiCols)
        {
            length = imGuiCols.Length;
            for (int i = 0; i < length; i++)
            {
                ImGui.PushStyleColor(imGuiCols[i], color);
            }
        }
        public void Dispose()
        {
            ImGui.PopStyleColor(length);
        }
    }

}
