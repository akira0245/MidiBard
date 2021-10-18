using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using MidiBard.Managers.Ipc;

namespace MidiBard
{
	public partial class PluginUI
	{
		#region import

		private static readonly string LabelFileImportRunning = "Import in progress...".Localize();

		private void ButtonImport()
		{
			if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Plus, (string)"buttonimport"))
			{
				RunImportTask();
			}

			ImGuiUtil.ToolTip("Import midi file.".Localize());
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
						await PlaylistManager.Add(strings);
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
}