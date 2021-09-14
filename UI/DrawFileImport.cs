using System.Threading;
using System.Windows.Forms;
using Dalamud.Interface;
using ImGuiNET;

namespace MidiBard
{
	public partial class PluginUI
	{
		#region import

		private static readonly string LabelFileImportRunning = "Import in progress...".Localize();

		private void ButtonImport()
		{
			if (ImguiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Plus, (string)"buttonimport"))
			{
				RunImportTask();
			}

			ImguiUtil.ToolTip("Import midi file.".Localize());
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
				var b = new Browse((result, filePath) =>
				{
					if (result == DialogResult.OK)
					{
						PlaylistManager.ImportMidiFile(filePath, true);
					}

					_isImportRunning = false;
				});

				var t = new Thread(b.BrowseDLL);
				t.SetApartmentState(ApartmentState.STA);
				t.Start();
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