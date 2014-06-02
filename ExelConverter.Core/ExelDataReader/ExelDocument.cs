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

namespace ExelConverter.Core.ExelDataReader
{
    public class ExelDocument
    {
        public AsyncDocumentLoader Loader { get; set; }

        public ExelDocument(string path, bool deleteEmptyRows)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);// path.Split(new char[] { '\\' })[path.Split(new char[] { '\\' }).Length - 1];
            Loader = new AsyncDocumentLoader()
            {
                Result = new List<ExelSheet>(),
                FileLoader = new System.ComponentModel.BackgroundWorker()
                {
                    WorkerReportsProgress = true
                },
                DeleteEmptyRows = deleteEmptyRows
            };
            Sheets = new List<ExelSheet>();
            Loader.FileLoader.RunWorkerCompleted += (s, e) => 
            {
                Sheets = Loader.Result;
            };
            LoadDocAsync();
        }

        private void LoadDocAsync()
        {
            Workbook wb = new Workbook(Path);
            object addLockObject = new Object();

            Dictionary<int, ExelSheet> sheetDic = new Dictionary<int, ExelSheet>();

            //Parallel.For(0, wb.Worksheets.Count - 1, i =>
            for (var i = 0; i < wb.Worksheets.Count; i++)
            {
                var s = new ExelSheet();
                var sheet = wb.Worksheets[i];
                s.Name = sheet.Name;

                Loader.Sheet = sheet;
                Loader.WorkBook = wb;

                var time = DateTime.Now;
                s.Rows = Loader.LoadRows(Settings.SettingsProvider.CurrentSettings.PreloadedRowsCount);
                var sec = (DateTime.Now - time).TotalSeconds;
                if (s.Rows.Count > 0)
                    lock (addLockObject)
                    {
                        sheetDic.Add(i, s);
                    }
            }
            

            Sheets = new List<ExelSheet>(sheetDic.OrderBy(kvp => kvp.Key).Select( kvp => kvp.Value ));
            Loader.LoadRows();
        }

        #region Commented
        /*
        private void LoadDocument()
        {
            var isXlsx = Path.Contains("xlsx");

            var wb = WorkbookFactory.Create(Path);
            for (var i = 0; i < wb.NumberOfSheets; i++)
            {
                var s = new ExelSheet();
                var sheet = wb.GetSheetAt(i);
                s.Name = sheet.SheetName;
                for (var j = 0; j < sheet.LastRowNum; j++)
                {
                    var r = new ExelRow();
                    var row = sheet.GetRow(j);
                    if (row != null)
                    {
                        for (var k = 0; k < row.Cells.Count; k++)
                        {
                            var cell = row.Cells[k];
                            var c = new ExelCell();
                            
                            c.IsMerged = cell.IsMergedCell;
                            if (cell.Hyperlink != null)
                            {
                                c.HyperLink = cell.Hyperlink.Address;
                            }
                            if (cell.CellType.ToString() == "NUMERIC")
                            {
                                c.Value = cell.NumericCellValue.ToString();
                            }
                            else if (cell.CellType.ToString() == "STRING")
                            {
                                c.Value = cell.StringCellValue.ToString();
                            }
                            else if (cell.CellType.ToString() == "BLANK")
                            {
                                c.Value = string.Empty;
                            }
                            else if (cell.CellType.ToString() == "FORMULA")
                            {
                                c.Value = cell.CellFormula;
                            }
                            else
                            {
                                throw new Exception(cell.CellType.ToString());
                            }

                            Color color; 
                            if (isXlsx)
                            {
                                color = GetBrush(((XSSFColor)cell.CellStyle.FillForegroundColorColor).GetARgb());
                            }
                            else
                            {
                                color = GetBrush(((HSSFColor)cell.CellStyle.FillForegroundColorColor).GetHexString());
                            }
                            c.Color = color;
                            r.Cells.Add(c);
                        }
                    }
                    s.Rows.Add(r);
                }
                FillMergedCells(s);
                Sheets.Add(s);
            }
        }

        private Color GetBrush(string color)
        {
            
            double coef = 255.0 / 65535.0;
            var rgbArray = color.Split(new char[] { ':' });

            var r = (byte)(GetDec(rgbArray[0]) * coef);
            var g = (byte)(GetDec(rgbArray[1]) * coef);
            var b = (byte)(GetDec(rgbArray[2]) * coef);

            return Color.FromRgb(r, g, b);

        }

        private Color GetBrush(byte[] color)
        {
            return Color.FromArgb(color[0], color[1], color[2], color[3]);
        }

        private int GetDec(string hex)
        {
            var result = 0;
            for (var i = 0; i < hex.Length; i++)
            {
                var num = 0;
                if (hex[i] == 'F')
                {
                    num = 15;
                }
                else if (hex[i] == 'E')
                {
                    num = 14;
                }
                else if (hex[i] == 'D')
                {
                    num = 13;
                }
                else if (hex[i] == 'C')
                {
                    num = 12;
                }
                else if (hex[i] == 'B')
                {
                    num = 11;
                }
                else if (hex[i] == 'A')
                {
                    num = 10;
                }
                else
                {
                    num = int.Parse(hex[i].ToString());
                }
                result += num * (int)Math.Pow(16, hex.Length - i-1);
            }
            return result;
        }
        */
        #endregion

        public string Name { get; private set; }

        public string Path { get; private set; }

        public List<ExelSheet> Sheets 
        { 
            get;
            private set;
        }

    }
}
