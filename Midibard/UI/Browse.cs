using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MidiBard;

class Browse
{
    public delegate void FileSelectedCallback(bool? fileDialogResult, string[] filePaths);

    private FileSelectedCallback callback;

    public Browse(FileSelectedCallback callback)
    {
        this.callback = callback;
    }

    public void BrowseDLL()
    {
        var ofd = new OpenFileDialog
        {
            Filter = "midi file (*.mid)|*.mid",
            Title = "Select a midi file",
            RestoreDirectory = true,
            CheckFileExists = true,
            Multiselect = true,
        };
        callback(ofd.ShowDialog(), ofd.FileNames);
    }
}