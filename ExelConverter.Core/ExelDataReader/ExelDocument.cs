using System;
using System.IO;
using System.Data.OleDb;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;

using Aspose.Cells;
using System.Collections.ObjectModel;

namespace ExelConverter.Core.ExelDataReader
{
    public class ExelDocument
    {
        public AsyncDocumentLoader Loader { get; private set; }

        public ExelDocument(string path, bool deleteEmptyRows)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
            Loader = new AsyncDocumentLoader() { DeleteEmptyRows = deleteEmptyRows };
            Loader.WorkBook = new Workbook(Path);
            Loader.FileLoader.RunWorkerCompleted += (s, e) => 
            {
                Sheets = (e.Result as List<ExelSheet>).AsReadOnly();
            };
            Sheets = Loader.LoadSheets(Settings.SettingsProvider.CurrentSettings.PreloadedRowsCount).AsReadOnly();
        }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public ReadOnlyCollection<ExelSheet> Sheets 
        { 
            get;
            private set;
        }

        public void FullLoad()
        {
            Loader.FileLoader.RunWorkerAsync();
        }
    }
}
