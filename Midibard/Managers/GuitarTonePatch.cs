using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Hooking;
using Dalamud.Logging;
using MidiBard.DalamudApi;

namespace MidiBard.Managers;

internal unsafe static class GuitarTonePatch
{
    //the function accessing tone value when play notes
    public delegate long PlayNoteWithToneDelegate(long a1, long a2, long a3, uint a4, uint a5, byte a6);
    private static Hook<PlayNoteWithToneDelegate> PlayNoteWithToneHook;

    //.text:000000014119AD70                         ; =============== S U B R O U T I N E =======================================
    //.text:000000014119AD70
    //.text:000000014119AD70
    //.text:000000014119AD70                         EnsembleGetNote_14119AD70 proc near     ; DATA XREF: .rdata:000000014185C708↓o
    //.text:000000014119AD70 8B C2                                   mov     eax, edx
    //.text:000000014119AD72 0F B6 44 08 10                          movzx   eax, byte ptr [rax+rcx+16]
    //.text:000000014119AD77 C3                                      retn
    //.text:000000014119AD77                         EnsembleGetNote_14119AD70 endp
    //.text:000000014119AD77
    //.text:000000014119AD77                         ; ---------------------------------------------------------------------------
    //.text:000000014119AD78 CC CC CC CC CC CC CC CC                 align 20h
    //.text:000000014119AD80
    //.text:000000014119AD80                         ; =============== S U B R O U T I N E =======================================
    //.text:000000014119AD80
    //.text:000000014119AD80
    //.text:000000014119AD80                         GetNote_14119AD80 proc near             ; DATA XREF: .rdata:000000014185C6E0↓o
    //.text:000000014119AD80 8B C2                                   mov     eax, edx
    //.text:000000014119AD82 0F B6 44 08 10                          movzx   eax, byte ptr [rax+rcx+16]
    //.text:000000014119AD87 C3                                      retn
    //.text:000000014119AD87                         GetNote_14119AD80 endp
    //.text:000000014119AD87
    //.text:000000014119AD87                         ; ---------------------------------------------------------------------------
    //.text:000000014119AD88 CC CC CC CC CC CC CC CC                 align 10h

    //original:
    //ffxiv_dx11.exe+119AD80 - 8B C2                 - mov eax,edx
    //ffxiv_dx11.exe+119AD82 - 0FB6 44 08 10         - movzx eax,byte ptr [rax+rcx+10]
    //ffxiv_dx11.exe+119AD87 - C3                    - ret 
    //ffxiv_dx11.exe+119AD88 - CC                    - int 3 
    //ffxiv_dx11.exe+119AD89 - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8A - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8B - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8C - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8D - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8E - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8F - CC                    - int 3 

    //fixed: 
    //ffxiv_dx11.exe+119AD80 - 8B C2                 - mov eax,edx
    //ffxiv_dx11.exe+119AD82 - 0FB6 44 08 10         - movzx eax,byte ptr [rax+rcx+10]
    //ffxiv_dx11.exe+119AD87 - 44 0FB6 74 08 1A      - movzx r14d,byte ptr [rax+rcx+1A]
    //ffxiv_dx11.exe+119AD8D - C3                    - ret 
    //ffxiv_dx11.exe+119AD8E - CC                    - int 3 
    //ffxiv_dx11.exe+119AD8F - CC                    - int 3 

    static readonly byte[] soloTonePatch = { 0x8B, 0xC2, 0x0F, 0xB6, 0x44, 0x08, 0x10, 0x44, 0x0F, 0xB6, 0x74, 0x08, 0x1A, 0xC3 };

    //original:
    //ffxiv_dx11.exe+119AD70 - 8B C2                 - mov eax,edx
    //ffxiv_dx11.exe+119AD72 - 0FB6 44 08 10         - movzx eax,byte ptr [rax+rcx+10]
    //ffxiv_dx11.exe+119AD77 - C3                    - ret 
    //ffxiv_dx11.exe+119AD78 - CC                    - int 3 
    //ffxiv_dx11.exe+119AD79 - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7A - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7B - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7C - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7D - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7E - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7F - CC                    - int 3 

    //fixed: 
    //ffxiv_dx11.exe+119AD70 - 8B C2                 - mov eax,edx
    //ffxiv_dx11.exe+119AD72 - 0FB6 44 08 10         - movzx eax,byte ptr [rax+rcx+10]
    //ffxiv_dx11.exe+119AD77 - 44 0FB6 7C 08 4C      - movzx r15d,byte ptr [rax+rcx+4C]
    //ffxiv_dx11.exe+119AD7D - C3                    - ret 
    //ffxiv_dx11.exe+119AD7E - CC                    - int 3 
    //ffxiv_dx11.exe+119AD7F - CC                    - int 3 

    static readonly byte[] ensembleTonePatch = { 0x8B, 0xC2, 0x0F, 0xB6, 0x44, 0x08, 0x10, 0x44, 0x0F, 0xB6, 0x7C, 0x08, 0x4C, 0xC3 };

    static readonly byte[] original = { 0x8B, 0xC2, 0x0F, 0xB6, 0x44, 0x08, 0x10, 0xC3, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC };
    private static IntPtr* _getNoteVtbl;
    private static IntPtr _getNoteFunction;


    public static void InitAndApply()
    {
        try
        {
            _getNoteVtbl = (IntPtr*)api.SigScanner.GetStaticAddressFromSig("4C 8D 0D ?? ?? ?? ?? 41 B8 ?? ?? ?? ?? 4C 8D 15 ?? ?? ?? ?? 4C 89 8E E0 0B 00 00");
            _getNoteFunction = _getNoteVtbl[4];
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "error when getting getNoteFunction");
        }
			

        //local solo tone fix
        var scanText = api.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 63 1B FE");
        PlayNoteWithToneHook = new Hook<PlayNoteWithToneDelegate>(scanText,
            (a1, a2, a3, a4, a5, a6) =>
            {
                if (a4 > 0 && a6 == 1) a5 = (uint)PerformanceStruct.Instance->PlayingNoteTone;

                var ret = PlayNoteWithToneHook.Original(a1, a2, a3, a4, a5, a6);
#if DEBUG
					//PluginLog.Warning($"ret:{ret:X} a1:{a1:X} a2:{a2:X} a3:{a3:X} a4:{a4} a5:{a5} a6:{a6}");
#endif
                return ret;
            });

        ApplyPatch();
    }

    public static void ApplyPatch()
    {
        SafeMemory.WriteBytes(_getNoteFunction, soloTonePatch);
        PluginLog.Debug($"Solo guitar tone fix patch applied. at {_getNoteFunction:X}");
        SafeMemory.WriteBytes(_getNoteFunction - 0x10, ensembleTonePatch);
        PluginLog.Debug($"Ensemble guitar tone fix patch applied. at {_getNoteFunction - 0x10:X}");
        PlayNoteWithToneHook.Enable();
        PluginLog.Debug($"PlayNoteWithToneHook enabled. at {PlayNoteWithToneHook.Address:X}");
    }

    public static void RestoreOriginal()
    {
        try
        {
            PlayNoteWithToneHook?.Disable();
            SafeMemory.WriteBytes(_getNoteFunction, original);
            SafeMemory.WriteBytes(_getNoteFunction - 0x10, original);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "error when restoring guitar tone patch");
        }
    }

    public static void Dispose()
    {
        RestoreOriginal();
        PlayNoteWithToneHook?.Dispose();
    }
}