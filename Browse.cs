using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MidiBard
{
	class Browse
	{
		public delegate void FileSelectedCallback(DialogResult result, string[] filePaths);

		private FileSelectedCallback callback;

		public Browse(FileSelectedCallback callback)
		{
			this.callback = callback;
		}

		public void BrowseDLL()
		{
			using var ofd = new OpenFileDialog
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
}
