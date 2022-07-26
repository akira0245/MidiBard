using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using ImGuiNET;
using Microsoft.Win32;
using MidiBard.DalamudApi;
using MidiBard.Managers.Ipc;
using MidiBard.Resources;
using MidiBard.UI.Win32;
using MidiBard.Util;

namespace MidiBard;

public partial class PluginUI
{
    #region import
    private void RunImportFileTask()
    {
        if (!IsImportRunning)
        {
            IsImportRunning = true;

            if (MidiBard.config.useLegacyFileDialog)
            {
                RunImportFileTaskWin32();
            }
            else
            {
                RunImportFileTaskImGui();
            }
        }
    }

    private void RunImportFolderTask()
    {
        if (!IsImportRunning)
        {
            IsImportRunning = true;

            if (MidiBard.config.useLegacyFileDialog)
            {
                RunImportFolderTaskImGui();
            }
            else
            {
                RunImportFolderTaskWin32();
            }
        }
    }

    private void RunImportFileTaskWin32()
    {
        var b = new Browse((result, filePaths) =>
        {
            if (result == true)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await PlaylistManager.AddAsync(filePaths);
                    }
                    finally
                    {
                        IsImportRunning = false;
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        });

        var t = new Thread(b.BrowseDLL);
        t.IsBackground = true;
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
    }

    private void RunImportFileTaskImGui()
    {
        fileDialogManager.OpenFileDialog("Open", ".mid", (b, strings) =>
        {
            PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", strings)}");
            if (b)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await PlaylistManager.AddAsync(strings.ToArray());
                    }
                    finally
                    {
                        IsImportRunning = false;
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        }, 0);
    }

    private void RunImportFolderTaskWin32()
    {
        fileDialogManager.OpenFolderDialog("Open folder", (b, filePath) =>
        {
            PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", filePath)}");
            if (b)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var files = Directory.GetFiles(filePath, "*.mid", SearchOption.AllDirectories);
                        await PlaylistManager.AddAsync(files);
                    }
                    finally
                    {
                        IsImportRunning = false;
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        });
    }

    private void RunImportFolderTaskImGui()
    {
        var b = new BrowseFolder((result, filePath) =>
        {
            if (result == true)
            {
                Task.Run(async () =>
                {
                    if (Directory.Exists(filePath))
                    {
                        try
                        {
                            var files = Directory.GetFiles(filePath, "*.mid", SearchOption.AllDirectories);
                            await PlaylistManager.AddAsync(files);
                        }
                        finally
                        {
                            IsImportRunning = false;
                        }
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        });

        var t = new Thread(b.Browse);
        t.IsBackground = true;
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
    }
    
    public bool IsImportRunning { get; private set; }
    
    #endregion
}