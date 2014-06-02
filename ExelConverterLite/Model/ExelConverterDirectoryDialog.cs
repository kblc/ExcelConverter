using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExelConverterLite.Model
{
    public class ExelConverterDirectoryDialog
    {
        public static string Show()
        {
            var dialog = new FolderBrowserDialog()
            {
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return null;

        }
    }
}
