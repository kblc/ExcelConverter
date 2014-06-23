using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/*using NPOI;
using NPOI.HSSF.Extractor;
using NPOI.OpenXml4Net;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.HSSF.Util;
using NPOI.XSSF.Util;
using NPOI.XSSF.UserModel;*/

using Aspose.Cells;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections;

namespace ExelConverter.Core.ExelDataReader
{
    public class AsyncDocumentLoader
    {
        private BackgroundWorker fileLoader = null;
        public BackgroundWorker FileLoader
        {
            get 
            {
                if (fileLoader == null)
                {
                    fileLoader = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
                    fileLoader.DoWork += FileLoader_DoWork;
                }
                return fileLoader; 
            }
        }

        public Workbook WorkBook { get; set; }

        private void FileLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            e.Result = LoadSheets(WorkBook, 0, new Action<int>((i) => bw.ReportProgress(i)), true);
        }

        public List<ExelSheet> LoadSheets(int count = 0)
        {
            return LoadSheets(WorkBook, count, null, DeleteEmptyRows);
        }

        public static List<ExelSheet> LoadSheets(Workbook workbook, int count = 0, Action<int> progressReport = null, bool deleteEmptyRows = true)
        {
            var result = new List<ExelSheet>();
            //var totalRowsCount = 0;
            //var loaded = 0;
            //totalRowsCount = workbook.Worksheets.Cast<Worksheet>().Select(i => i.Cells.Rows.Count).Sum();

            Helpers.PercentageProgress prg = new Helpers.PercentageProgress();
            prg.Change += (s, e) =>
                {
                    if (progressReport != null)
                        progressReport((int)e.Value);
                };

            foreach (var sheetItem in 
                            workbook
                            .Worksheets
                            .Cast<Worksheet>()
                            .Where(s => s.Cells.Rows.Count > 1)
                            .Select(s => new 
                            { 
                                Sheet = s, 
                                ProgressInfo = prg.GetChild()
                            })
                            .ToArray()
                            )
            {
                ExelSheet sht = new ExelSheet() { Name = sheetItem.Sheet.Name };
                sht.Rows.AddRange(
                    LoadRows(sheetItem.Sheet, count, new Action<int>((i) => { sheetItem.ProgressInfo.Value = i; }), deleteEmptyRows)
                    );
                if (sht.Rows.Count > 0)
                    result.Add(sht);
            }

            return result;
        }
        public static List<ExelRow> LoadRows(Worksheet sheet, int count = 0, Action<int> progressReport = null, bool deleteEmptyRows = true)
        {
            var result = new List<ExelRow>();
            var totalRowsCount = 0;
            var loaded = 0;
            totalRowsCount = sheet.Cells.Rows.Count;

            var hLinks = sheet.Hyperlinks.Cast<Hyperlink>();
            #region Select max column index for sheet
            int maxColumnIndex = sheet.Cells.Rows.Cast<Row>().Select(c =>
            {
                if (c.LastCell != null)
                    for (int i = c.LastCell.Column; i >= 0; i--)
                    {
                        var cell = c.GetCellOrNull(i);
                        if (!string.IsNullOrWhiteSpace(
                                    cell == null || cell.Value == null
                                    ? null
                                    : cell.Value.ToString().Trim()
                                )
                            )
                            return cell.IsMerged ? cell.GetMergedRange().FirstColumn + cell.GetMergedRange().ColumnCount - 1 : i;
                    }
                return 0;
            }
                )
                .Union(new int[] { 0 })
                .Max();
            #endregion
            #region Read rows
            foreach (Row row in sheet.Cells.Rows.Cast<Row>().Where(r => count <= 0 || r.Index < count))
            {
                var index = row.Index;

                var rowHyperLinks = hLinks.Where(hl => hl.Area.StartRow <= index && hl.Area.EndRow >= index).ToArray();
                var r = new ExelRow() { Index = index };
                if (sheet.Cells.Count > 0)
                {
                    var currMaxColumnsIndex = row.LastCell == null ? 0 : row.LastCell.Column;

                    #region Read data from cells

                    for (var k = 0; k <= Math.Min(currMaxColumnsIndex, maxColumnIndex); k++) //sheet.Cells.Columns.Count
                    {
                        var cell = sheet.Cells[index, k];
                        var c = new ExelCell();

                        var hLinks2 = rowHyperLinks.Where(l => l.Area.StartColumn <= k && l.Area.EndColumn >= k);

                        var link = hLinks2.OrderBy(
                            i1 =>
                            {
                                double res = 0.0;

                                //res += (i1.Area.StartColumn == k) ? 1.0 : 0.0;
                                //res += (i1.Area.EndColumn == k) ? 1.0 : 0.0;
                                //res += (i1.Area.StartRow == index) ? 1.0 : 0.0;
                                //res += (i1.Area.EndRow == index) ? 1.0 : 0.0;

                                res += Math.Abs(i1.Area.StartColumn - k);
                                res += Math.Abs(i1.Area.EndColumn - k);
                                res += Math.Abs(i1.Area.StartRow - index);
                                res += Math.Abs(i1.Area.EndRow - index);

                                return res / 4;
                            }
                            ).FirstOrDefault();
                        if (link != null)
                        {
                            c.HyperLink = link.Address;
                        }
                        else if (cell.Formula != null)
                        {
                            var formula = cell.Formula;
                            if (formula != null)
                            {
                                c.HyperLink = formula.Split(new char[] { '\"' }).Where(str => str.Contains("http")).FirstOrDefault();
                            }
                        }
                        if (cell.IsMerged)
                        {
                            c.IsMerged = true;
                            var intersect = new Func<int, int, int, int, bool>((x1, x2, y1, y2) =>
                            {
                                return
                                    (x1 <= y1 && x2 >= y1)
                                    || (x2 >= y1 && x2 <= y2)
                                    ;
                            });


                            var range = cell.GetMergedRange();
                            var mLinks = hLinks.FirstOrDefault(
                                l =>
                                    intersect(l.Area.StartColumn, l.Area.EndColumn, range.FirstColumn, range.FirstColumn + range.ColumnCount)
                                    && intersect(l.Area.StartRow, l.Area.EndRow, range.FirstRow, range.FirstRow + range.RowCount)
                                    );
                            if (mLinks != null)
                                c.HyperLink = mLinks.Address;

                            var content = string.Empty;
                            var values = (IEnumerable)cell.GetMergedRange().Value;
                            if (values != null)
                                foreach (var value in values)
                                {
                                    content += value;
                                }

                            c.Value = content;
                        }
                        else if (cell.Value != null)
                        {
                            c.Value = cell.Value.ToString();
                        }
                        else
                        {
                            c.Value = string.Empty;
                        }

                        var style = cell.GetStyle();
                        c.CellStyle = style;
                        c.Color = (style != null) ? (DefColors.Any(clr => ColorsEqual(clr, style.BackgroundColor)) ? style.ForegroundColor : style.BackgroundColor) : System.Drawing.Color.White;

                        c.Color = System.Drawing.Color.FromArgb((c.Color.R > byte.MinValue || c.Color.G > byte.MinValue || c.Color.B > byte.MinValue) && c.Color.A == byte.MinValue ? byte.MaxValue : c.Color.A, c.Color.R, c.Color.G, c.Color.B);

                        r.Cells.Add(c);
                    }

                    #endregion

                    var lastStyle = (maxColumnIndex > 0) ? r.Cells[Math.Min(currMaxColumnsIndex, maxColumnIndex)].CellStyle : new Style();

                    for (int i = 0; i < Math.Abs(maxColumnIndex - currMaxColumnsIndex); i++)
                        r.Cells.Add(new ExelCell() { Value = string.Empty, CellStyle = lastStyle });

                    result.Add(r);
                    loaded++;
                    if (progressReport != null)
                        progressReport((int)((double)loaded * 100 / (double)totalRowsCount));
                }

            }
            #endregion

            //FillMergedCells(sht);

            //Delete empty rows from end
            if (deleteEmptyRows)
                for (int z = result.Count - 1; z >= 0; z--)
                {
                    var r1 = result[z];
                    if (r1.IsEmpty)
                        result.RemoveAt(z);
                    else
                        break;
                }

            if (result.Count > 0)
            {
                #region Delete bottom

                //#### Try to delete bottom info ####
                //get last empty index
                int lastEmptyRowsInDataIndex =
                    result
                    .Where(row => row.IsEmpty)
                    .Select(row => result.IndexOf(row))
                    .OrderByDescending(i => i)
                    .FirstOrDefault();
                //get last non empty index before last empty to check similarity
                int lastNotEmptyRowsInDataIndexBeforeEmpty =
                    result
                    .Where(row => !row.IsEmpty)
                    .Select(row => result.IndexOf(row))
                    .Where(i => i < lastEmptyRowsInDataIndex)
                    .OrderByDescending(i => i)
                    .FirstOrDefault();

                if (lastEmptyRowsInDataIndex > 4 && result.Count - lastEmptyRowsInDataIndex < 15)
                {
                    List<ExelRow> similarityIndexes = new List<ExelRow>();
                    for (int i = lastNotEmptyRowsInDataIndexBeforeEmpty; i >= 0 && i > lastNotEmptyRowsInDataIndexBeforeEmpty - 10; i--)
                        similarityIndexes.Add(result[i]);

                    for (int z = result.Count - 1; z >= lastEmptyRowsInDataIndex; z--)
                    {
                        if (result[z].NotEmptyCells.Count() <= 4 && !similarityIndexes.Select(s => s.Similarity(result[z])).Any(d => d > 0.6))
                            result.RemoveAt(z);
                        else
                            break;
                    }
                }

                #endregion

                //Delete all empty rows from data
                if (deleteEmptyRows)
                    for (int z = result.Count - 1; z >= 0; z--)
                    {
                        var r1 = result[z];
                        if (r1.IsEmpty)
                            result.RemoveAt(z);
                    }
            }

            return result;
        }

        private static System.Drawing.Color DefColor0 = System.Drawing.Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        private static System.Drawing.Color DefColor1 = System.Drawing.Color.FromArgb(byte.MinValue, byte.MinValue, byte.MinValue, byte.MinValue);

        public static System.Drawing.Color[] DefColors
        {
            get
            {
                return new System.Drawing.Color[] { DefColor0, DefColor1 };
            }
        }
        public static bool ColorsEqual(System.Drawing.Color c1, System.Drawing.Color c2)
        {
            return c1.A == c2.A && c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;
        }
        private static Color GetBrush(string color)
        {

            double coef = 255.0 / 65535.0;
            var rgbArray = color.Split(new char[] { ':' });

            var r = (byte)(GetDec(rgbArray[0]) * coef);
            var g = (byte)(GetDec(rgbArray[1]) * coef);
            var b = (byte)(GetDec(rgbArray[2]) * coef);

            return Color.FromRgb(r, g, b);

        }
        private static Color GetBrush(byte[] color)
        {
            return Color.FromArgb(color[0], color[1], color[2], color[3]);
        }
        private static int GetDec(string hex)
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
                result += num * (int)Math.Pow(16, hex.Length - i - 1);
            }
            return result;
        }
        public bool DeleteEmptyRows { get; set; }
    }
}
