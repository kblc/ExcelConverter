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
        public List<ExelSheet> Result { get; set; }

        public BackgroundWorker FileLoader { get; set; }

        public Worksheet Sheet { get; set; }

        public Workbook WorkBook { get; set; }

        public bool IsXlsx { get; set; }

        private void FileLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var totalRowsCount = 0;
            var loaded = 0;
            totalRowsCount = WorkBook.Worksheets.Cast<Worksheet>().Select(i => i.Cells.Rows.Count).Sum();

            Result = new List<ExelSheet>();

            foreach (Worksheet sheet in WorkBook.Worksheets)
            {
                ExelSheet sht = new ExelSheet() { Name = sheet.Name };
                var hLinks = sheet.Hyperlinks.Cast<Hyperlink>();

                int maxColumnIndex = sheet.Cells.Rows.Cast<Row>().Select(c => 
                        {
                            if (c.LastCell != null)
                                for (int i = c.LastCell.Column; i >= 0; i--)
                                    if (!string.IsNullOrWhiteSpace(c.LastCell.Value == null ? null : c.LastCell.Value.ToString().Trim()))
                                        return i;
                            return 0;
                        }
                    )
                    .Union(new int[] { 0 })
                    .Max();
                #region Read all rows
                foreach (Row row in sheet.Cells.Rows)
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

                                        return res/4;
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
                                var intersect = new Func<int,int,int,int,bool>((x1,x2,y1,y2) =>
                                    {
                                        return
                                            (x1 <= y1 && x2 >= y1)
                                            || (x2 >= y1 && x2 <= y2)
                                            ;
                                    });


                                var range = cell.GetMergedRange();
                                var mLinks = hLinks.FirstOrDefault(
                                    l => 
                                        intersect(l.Area.StartColumn, l.Area.EndColumn, range.FirstColumn, range.FirstColumn+range.ColumnCount)
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

                        for (int i = 0; i < Math.Abs(maxColumnIndex - currMaxColumnsIndex); i++ )
                            r.Cells.Add(new ExelCell() { Value = string.Empty, CellStyle = lastStyle });

                        sht.Rows.Add(r);
                        loaded++;
                        FileLoader.ReportProgress((int)((double)loaded / (double)totalRowsCount));
                    }

                }
                #endregion
                
                FillMergedCells(sht);

                //Delete empty rows from end
                if (DeleteEmptyRows)
                    for (int z = sht.Rows.Count - 1; z >= 0; z--)
                    {
                        var r1 = sht.Rows[z];
                        if (r1.IsEmpty)
                            sht.Rows.RemoveAt(z);
                        else
                           break;
                    }

                if (sht.Rows.Count > 0)
                {
                    #region Delete bottom

                    //#### Try to delete bottom info ####
                    //get last empty index
                    int lastEmptyRowsInDataIndex =
                        sht.Rows
                        .Where(row => row.IsEmpty)
                        .Select(row => sht.Rows.IndexOf(row))
                        .OrderByDescending(i => i)
                        .FirstOrDefault();
                    //get last non empty index before last empty to check similarity
                    int lastNotEmptyRowsInDataIndexBeforeEmpty =
                        sht.Rows
                        .Where(row => !row.IsEmpty)
                        .Select(row => sht.Rows.IndexOf(row))
                        .Where(i => i < lastEmptyRowsInDataIndex)
                        .OrderByDescending(i => i)
                        .FirstOrDefault();

                    if (lastEmptyRowsInDataIndex > 4 && sht.Rows.Count - lastEmptyRowsInDataIndex < 15)
                    {
                        List<ExelRow> similarityIndexes = new List<ExelRow>();
                        for (int i = lastNotEmptyRowsInDataIndexBeforeEmpty; i >= 0 && i > lastNotEmptyRowsInDataIndexBeforeEmpty - 10; i--)
                            similarityIndexes.Add(sht.Rows[i]);

                        for (int z = sht.Rows.Count - 1; z >= lastEmptyRowsInDataIndex; z--)
                        {
                            if (sht.Rows[z].NotEmptyCells.Count() <= 4 && !similarityIndexes.Select(s => s.Similarity(sht.Rows[z])).Any(d => d > 0.6))
                                sht.Rows.RemoveAt(z);
                            else
                                break;
                        }
                    }

                    #endregion

                    //Delete all empty rows from data
                    if (DeleteEmptyRows)
                        for (int z = sht.Rows.Count - 1; z >= 0; z--)
                        {
                            var r1 = sht.Rows[z];
                            if (r1.IsEmpty)
                                sht.Rows.RemoveAt(z);
                        }

                    if (sht.Rows.Count > 0)
                        Result.Add(sht);
                }
            }
        }

        public List<ExelRow> LoadRows(int count)
        {
            count = count < Sheet.Cells.Rows.Count ? count : Sheet.Cells.Rows.Count;
            var hLinks = Sheet.Hyperlinks.Cast<Hyperlink>().ToArray();
            var result = new List<ExelRow>();
            for (var i = 0; i < count; i++)
                try
                {
                    var rowHyperLinks = hLinks.Where(hl => hl.Area.StartRow == i).ToArray();

                    var r = new ExelRow();
                    var index = (int)i;

                    var columnsCount = Sheet.Cells.Rows[i].LastCell == null ? 0 : Sheet.Cells.Rows[i].LastCell.Column;
                    for (var k = 0; k <= columnsCount; k++) //Sheet.Cells.Columns.Count
                    {
                        var cell = Sheet.Cells[index, k];
                        var c = new ExelCell();

                        var link = rowHyperLinks.Where(l => l.Area.StartColumn == k).FirstOrDefault();
                        if (link != null)
                        {
                            c.HyperLink = link.Address;
                        }
                        if (cell.IsMerged)
                        {
                            var content = "";
                            var values = (IEnumerable)cell.GetMergedRange().Value;
                            if (values != null)
                            {
                                foreach (var value in values)
                                {
                                    content += value;
                                }
                            }
                            c.Value = content;
                        }
                        else if (cell.Value != null)
                        {
                            c.Value = cell.Value.ToString();
                        }
                        else
                        {
                            c.Value = "";
                        }

                        var style = cell.GetStyle();
                        c.CellStyle = style;
                        c.Color = (style != null) ? (DefColors.Any(clr => ColorsEqual(clr, style.BackgroundColor)) ? style.ForegroundColor : style.BackgroundColor) : System.Drawing.Color.White;

                        //c.Color = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
                        r.Cells.Add(c);
                    }



                    //bool canAdd = !DeleteEmptyRows;
                    //if (!canAdd)
                    //    foreach (ExelCell c in r.Cells)
                    //        if (!string.IsNullOrWhiteSpace(c.Value) || !string.IsNullOrWhiteSpace(c.HyperLink) || (!ColorsEqual(c.Color, DefColor0) && !ColorsEqual(c.Color, DefColor1)))
                    //        {
                    //            canAdd = true;
                    //            break;
                    //        }

                    //if (canAdd)
                    result.Add(r);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            if (DeleteEmptyRows)
                for (int z = result.Count - 1; z >= 0; z--)
                {
                    var r1 = result[z];
                    bool canAdd = false;
                    if (!canAdd)
                        foreach (ExelCell c in r1.Cells)
                            if (!string.IsNullOrWhiteSpace(c.Value) || !string.IsNullOrWhiteSpace(c.HyperLink) || (!ColorsEqual(c.Color, DefColor0) && !ColorsEqual(c.Color, DefColor1)))
                            {
                                canAdd = true;
                                break;
                            }

                    if (canAdd)
                        break;
                    else
                        result.RemoveAt(z);
                }

            return result;
        }

        public void LoadRows()
        {
            FileLoader.DoWork +=FileLoader_DoWork;
            FileLoader.RunWorkerAsync();
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
                result += num * (int)Math.Pow(16, hex.Length - i - 1);
            }
            return result;
        }

        public void FillMergedCells(ExelSheet sheet)
        {
            var headerRow = sheet.Rows.IndexOf(sheet.MainHeader);
            var initialRow = headerRow + 1;

            var headers = sheet.SheetHeaders.Headers;
            var subHeaders = sheet.SheetHeaders.Subheaders;

            for (var i = headerRow + 1; i < sheet.Rows.Count; i++)
            {
                if (!subHeaders.Any(h => h.RowNumber == i) && !headers.Any(h => h.RowNumber == i))
                {
                    var row = sheet.Rows.ElementAt(i);
                    for (var j = 0; j < row.Cells.Count; j++)
                    {
                        var cell = row.Cells.ElementAt(j);
                        if (cell.IsMerged && sheet.Rows.ElementAt(i - 1).Cells.Count > j 
                            && String.IsNullOrEmpty(cell.Value) && sheet.Rows.ElementAt(i - 1).Cells.ElementAt(j).IsMerged)
                        {
                            try
                            {
                                cell.Value = sheet.Rows.ElementAt(i - 1).Cells.ElementAt(j).Value;
                                cell.HyperLink = sheet.Rows.ElementAt(i - 1).Cells.ElementAt(j).HyperLink;
                                cell.Color = sheet.Rows.ElementAt(i - 1).Cells.ElementAt(j).Color;
                                cell.CellStyle = sheet.Rows.ElementAt(i - 1).Cells.ElementAt(j).CellStyle;
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public bool DeleteEmptyRows { get; set; }
    }
}
