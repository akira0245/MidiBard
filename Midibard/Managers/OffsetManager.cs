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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Logging;

namespace MidiBard.Managers;

public static class OffsetManager
{
    public static void Setup(SigScanner scanner)
    {
        var props = typeof(Offsets).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(i => (prop: i, Attribute: i.GetCustomAttribute<SigAttribute>())).Where(i => i.Attribute != null);

        List<Exception> exceptions = new List<Exception>(100);
        foreach ((PropertyInfo propertyInfo, SigAttribute sigAttribute) in props)
        {
            try
            {
                var sig = sigAttribute.SigString;
                sig = string.Join(' ', sig.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i == "?" ? "??" : i));

                IntPtr address;
                switch (sigAttribute)
                {
                    case StaticAddressAttribute:
                        address = scanner.GetStaticAddressFromSig(sig);
                        break;
                    case FunctionAttribute:
                        address = scanner.ScanText(sig);
                        break;
                    case OffsetAttribute:
                    {
                        address = scanner.ScanText(sig);
                        address += sigAttribute.Offset;
                        var structure = Marshal.PtrToStructure(address, propertyInfo.PropertyType);
                        propertyInfo.SetValue(null, structure);
                        PluginLog.Information($"[{nameof(OffsetManager)}][{propertyInfo.Name}] {propertyInfo.PropertyType.FullName} {structure}");
                        continue;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                address += sigAttribute.Offset;
                propertyInfo.SetValue(null, address);
                PluginLog.Information($"[{nameof(OffsetManager)}][{propertyInfo.Name}] {address.ToInt64():X}");
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"[{nameof(OffsetManager)}][{propertyInfo?.Name}] failed to find sig : {sigAttribute?.SigString}");
                exceptions.Add(e);
            }
        }

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }
}
	
internal abstract class SigAttribute : Attribute
{
    protected SigAttribute(string sigString, int offset = 0)
    {
        this.SigString = sigString;
        Offset = offset;
    }

    public readonly string SigString;
    public readonly int Offset;
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class StaticAddressAttribute : SigAttribute
{
    public StaticAddressAttribute(string sigString, int offset = 0) : base(sigString, offset) { }
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class FunctionAttribute : SigAttribute
{
    public FunctionAttribute(string sigString, int offset = 0) : base(sigString, offset) { }
}

[AttributeUsage(AttributeTargets.Property)]
internal sealed class OffsetAttribute : SigAttribute
{
    public OffsetAttribute(string sigString, int offset) : base(sigString, offset) { }
}