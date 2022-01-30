using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using Microsoft.Win32;
using MidiBard.DalamudApi;
using MidiBard.Managers.Ipc;
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
            if (!_isImportRunning)
            {
                _isImportRunning = true;
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
                    Task.Delay(2000).ContinueWith(task => _isImportRunning = false);
                }
            }
        }

        if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Plus, (string)"buttonimport"))
        {
            if (MidiBard.config.useLegacyFileDialog)
            {
                RunImportTaskLegacy();
            }
            else
            {
                RunImportTask();
            }
        }

			
        ImGui.OpenPopupOnItemClick("OpenFileDialog_selection", ImGuiPopupFlags.MouseButtonRight);
			
        ImGuiUtil.ToolTip("Import midi file.\nRight click to select file dialog type\nPress ctrl+V to import files from clipboard".Localize());
    }

    private void ButtonImportInProgress()
    {
        ImGui.Button(LabelFileImportRunning);

        //if (_texToolsImport == null)
        //{
        //	return;
        //}

        //switch (_texToolsImport.State)
        //{
        //	case ImporterState.None:
        //		break;
        //	case ImporterState.WritingPackToDisk:
        //		ImGui.Text(TooltipModpack1);
        //		break;
        //	case ImporterState.ExtractingModFiles:
        //	{
        //		var str =
        //			$"{_texToolsImport.CurrentModPack} - {_texToolsImport.CurrentProgress} of {_texToolsImport.TotalProgress} files";

        //		ImGui.ProgressBar(_texToolsImport.Progress, ImportBarSize, str);
        //		break;
        //	}
        //	case ImporterState.Done:
        //		break;
        //	default:
        //		throw new ArgumentOutOfRangeException();
        //}
    }

    private bool _isImportRunning;

    void RunImportTaskLegacy()
    {
        if (!_isImportRunning)
        {
            _isImportRunning = true;

            var b = new Browse((result, filePath) =>
            {
                if (result == true)
                {
                    Task.Run(async () =>
                    {
                        await PlaylistManager.AddAsync(filePath);
                        MidiBard.SaveConfig();
                    });
                }

                _isImportRunning = false;
            });

            var t = new Thread(b.BrowseDLL);
            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
    private void RunImportTask()
    {
        if (!_isImportRunning)
        {
            _isImportRunning = true;
            fileDialogManager.OpenFileDialog("Import midi files", ".mid", (b, strings) =>
            {
                PluginLog.Debug($"dialog result: {b}\n{string.Join("\n", strings)}");
                if (b) Task.Run(async () =>
                {
                    await PlaylistManager.AddAsync(strings);
                    MidiBard.SaveConfig();
                });
                _isImportRunning = false;
            });
        }

        //_isImportRunning = true;
        //Task.Run(async () =>
        //{
        //	var picker = new OpenFileDialog
        //	{
        //		Multiselect = true,
        //		Filter = "midi file (*.mid)|*.mid",
        //		CheckFileExists = true,
        //		Title = "Select a mid file"
        //	};

        //	var result = await picker.ShowDialogAsync();

        //	if (result == DialogResult.OK)
        //	{
        //		_hasError = false;

        //		PlaylistManager.ImportMidiFile(picker.FileNames, true);

        //		//_texToolsImport = null;
        //		//_base.ReloadMods();
        //	}

        //	_isImportRunning = false;
        //});
    }

    #endregion
}