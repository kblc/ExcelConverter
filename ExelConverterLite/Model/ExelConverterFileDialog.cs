using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace ExelConverterLite.Model
{
    public static class ExelConverterFileDialog
    {
        public static string Show()
        {
            var openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|xlsx files (*.xlsx)|*.xlsx|xls files (*.xls)|*.xls";
            //openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }
    }

    public class ImageLoadFileDialog
    {
        public static string Show()
        {
            var openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }
    }
}
