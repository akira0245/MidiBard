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
using MidiBard.UI.Win32;
using MidiBard.Util;

namespace MidiBard;

public partial class PluginUI
{
    #region import

    private static readonly string LabelFileImportRunning = "Import in progress...".Localize();

    private void ButtonImport()
    {
        if (ImGui.BeginPopup("OpenFileDialog_selection"))
        {
            if (ImGui.MenuItem("Win32 file dialog".Localize(), null, MidiBard.config.useLegacyFileDialog))
            {
                MidiBard.config.useLegacyFileDialog = true;
            }
            else if (ImGui.MenuItem("ImGui file dialog".Localize(), null, !MidiBard.config.useLegacyFileDialog))
            {
                MidiBard.config.useLegacyFileDialog = false;
            }

            ImGui.EndPopup();
        }

        if (api.KeyState[VirtualKey.CONTROL] && api.KeyState[VirtualKey.V])
        {
            if (!IsImportRunning)
            {
                IsImportRunning = true;
                string[] array = null;
                var t = new Thread(() =>
                {
                    PluginLog.Information($"start getting GetFileDropList thread");
                    try
                    {
                        array = Clipboard.GetFileDropList().Cast<string>().Where(i => i.EndsWith(".mid")).ToArray();
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e, "error when getting files from clipboard");
                        array = new string[] { };
                    }
                    finally
                    {
                        PluginLog.Information($"getting GetFileDropList thread end");
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();

                try
                {
                    Task.Run(async () =>
                    {
                        await Coroutine.WaitUntil(() => array != null, 5000);
                        await PlaylistManager.AddAsync(array);
                    });
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "error when importing files from clipboard");
                }
                finally
                {
                    Task.Delay(2000).ContinueWith(task => IsImportRunning = false);
                }
            }
        }
        ImGui.BeginGroup();

        if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Plus, (string)"buttonimport"))
        {
            RunImportFileTask();
        }

        ImGuiUtil.ToolTip("Import midi file\nRight click to select file dialog type\nPress ctrl+V to import files from clipboard".Localize());
        ImGui.SameLine();
        if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Folder, (string)"buttonimportFolder"))
        {
            RunImportFolderTask();
        }
        ImGuiUtil.ToolTip("Import folder\nImports all midi files in selected folder and it's subfolder.\nThis may take a while when you select a folder that contains multiple layers of folders.".Localize());
        ImGui.EndGroup();

        ImGui.OpenPopupOnItemClick("OpenFileDialog_selection", ImGuiPopupFlags.MouseButtonRight);
    }

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
        var b = new Browse((result, filePath) =>
        {
            if (result == true)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await PlaylistManager.AddAsync(filePath);
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

    private void ButtonImportInProgress()
    {
        ImGui.Button(LabelFileImportRunning);
    }

    public bool IsImportRunning { get; private set; }
    
    #endregion
}