using MidiBard.DalamudApi;

namespace MidiBard.UI.Win32;

class BrowseFolder
{
    public delegate void FileSelectedCallback(bool? fileDialogResult, string folderPath);

    private FileSelectedCallback callback;

    public BrowseFolder(FileSelectedCallback callback)
    {
        this.callback = callback;
    }

    public void Browse()
    {
        var dlg = new FolderPicker();
        callback(dlg.ShowDialog(api.PluginInterface.UiBuilder.WindowHandlePtr), dlg.ResultPath);
    }
}