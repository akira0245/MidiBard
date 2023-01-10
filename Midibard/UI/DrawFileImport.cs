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
            CheckLastOpenedFolderPath();

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
            CheckLastOpenedFolderPath();

            if (MidiBard.config.useLegacyFileDialog)
            {
                RunImportFolderTaskWin32();
            }
            else
            {
                RunImportFolderTaskImGui();
            }
        }
    }


    private void RunImportFileTaskWin32()
    {
        FileDialogs.OpenMidiFileDialog((result, filePaths) =>
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
                        MidiBard.config.lastOpenedFolderPath = Path.GetDirectoryName(filePaths[0]);
                    }
		        });
	        }
	        else
	        {
		        IsImportRunning = false;
	        }
        });
    }

    private void RunImportFileTaskImGui()
    {
        fileDialogManager.OpenFileDialog("Open", ".mid", (b, strings) =>
        {
            //PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", strings)}");
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
                        MidiBard.config.lastOpenedFolderPath = Path.GetDirectoryName(strings[0]);
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        }, 0, MidiBard.config.lastOpenedFolderPath, true);
    }

    private void RunImportFolderTaskImGui()
    {
        fileDialogManager.OpenFolderDialog("Open folder", (b, filePath) =>
        {
            //PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", filePath)}");
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
                        MidiBard.config.lastOpenedFolderPath = Directory.GetParent(filePath).FullName;
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        }, MidiBard.config.lastOpenedFolderPath, true);
    }

    private void RunImportFolderTaskWin32()
    {
        FileDialogs.FolderPicker((result, folderPath) =>
        {
            if (result == true)
            {
                Task.Run(async () =>
                {
                    if (Directory.Exists(folderPath))
                    {
                        try
                        {
                            var files = Directory.GetFiles(folderPath, "*.mid", SearchOption.AllDirectories);
                            await PlaylistManager.AddAsync(files);
                            files = Directory.GetFiles(folderPath, "*.midi", SearchOption.AllDirectories);
                            await PlaylistManager.AddAsync(files);
                        }
                        finally
                        {
                            IsImportRunning = false;
                            MidiBard.config.lastOpenedFolderPath = Directory.GetParent(folderPath).FullName;
                        }
                    }
                });
            }
            else
            {
                IsImportRunning = false;
            }
        });
    }

    private void CheckLastOpenedFolderPath()
    {
        if (!Directory.Exists(MidiBard.config.lastOpenedFolderPath))
        {
            MidiBard.config.lastOpenedFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
    
    public bool IsImportRunning { get; private set; }
    
    #endregion
}