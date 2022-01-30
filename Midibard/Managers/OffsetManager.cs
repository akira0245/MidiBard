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