using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Microsoft.Win32;
using Dalamud;
using MidiBard.Resources;
using MidiBard.Util;
using System.IO;

namespace MidiBard.UI.Win32;

static class FileDialogs
{
	//public delegate void MultiFileSelectedCallback(bool? fileDialogResult, string[] filePaths);

	//public delegate void FileSelectedCallback(bool? fileDialogResult, string filePath);

	//public delegate void FolderSelectedCallback(bool? fileDialogResult, string folderPath);

	//public delegate void SaveFileDialogCallback(bool? fileDialogResult, string filePath);

	public static void OpenMidiFileDialog(Action<bool?, string[]> callback)
	{
		var t = new Thread(() =>
		{
			var dialog = new OpenFileDialog {
				Filter = "midi file (*.mid)|*.mid",
				RestoreDirectory = true,
				CheckFileExists = true,
				Multiselect = true,
				InitialDirectory = MidiBard.config.lastOpenedFolderPath
			};

			callback(dialog.ShowDialog(), dialog.FileNames);
		});
		t.IsBackground = true;
		t.SetApartmentState(ApartmentState.STA);
		t.Start();
	}

	public static void OpenPlaylistDialog(Action<bool?, string> callback)
	{
		var t = new Thread(() =>
		{
			var dialog = new OpenFileDialog() {
				Filter = "Midibard playlist (*.mpl)|*.mpl",
				RestoreDirectory = true,
				CheckFileExists = true,
				Multiselect = false,
                InitialDirectory = MidiBard.config.lastOpenedFolderPath
            };
			callback(dialog.ShowDialog(), dialog.FileName);
		});

		t.IsBackground = true;
		t.SetApartmentState(ApartmentState.STA);
		t.Start();
	}

	public static void FolderPicker(Action<bool?, string> callback)
	{
		var t = new Thread(() =>
		{
			var dlg = new FolderPicker();
            if (Directory.Exists(MidiBard.config.lastOpenedFolderPath))
            {
                dlg.InputPath = MidiBard.config.lastOpenedFolderPath;
            }
            callback(dlg.ShowDialog(api.PluginInterface.UiBuilder.WindowHandlePtr), dlg.ResultPath);
		});
		t.IsBackground = true;
		t.SetApartmentState(ApartmentState.STA);
		t.Start();
	}

	public static void SavePlaylistDialog(Action<bool?, string> callback, string filename)
	{
		var t = new Thread(() =>
		{
			var dialog = new SaveFileDialog {
				Filter = $"{Language.text_midibard_playlist} (*.mpl)|*.mpl",
				RestoreDirectory = true,
				AddExtension = true,
				DefaultExt = ".mpl",
				OverwritePrompt = true,
				FileName = filename,
                InitialDirectory = MidiBard.config.lastOpenedFolderPath
            };
			callback(dialog.ShowDialog(), dialog.FileName);
		});
		t.IsBackground = true;
		t.SetApartmentState(ApartmentState.STA);
		t.Start();
	}
}