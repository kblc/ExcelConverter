using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using Aspose.Cells;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ExelConverter.Core.ExelDataReader
{
    public class AsyncDocumentLoader
    {
        #region Additional
        private static bool IsIntersected(int x1, int x2, int y1, int y2)
        {
            return
                (x1 <= y1 && x2 >= y1)
                || (x2 >= y1 && x2 <= y2)
                ;
        }

        private static bool IsIncluded(int x1, int x2, int y1, int y2)
        {
            return
                (y1 >= x1 && y1 <= x2)
                && (y2 >= x1 && y2 <= x2)
                ;
        }

        private static Hyperlink[] GetHyperlinksForCell(Cell cell, Worksheet sheet)
        {
            var range = cell.GetMergedRange();
            var hLinks = sheet.Hyperlinks.Cast<Hyperlink>();

            var hLinksINCLD = hLinks
                    .Where(l =>
                        IsIncluded(
                            l.Area.StartColumn,
                            l.Area.EndColumn,
                            range != null ? range.FirstColumn : cell.Column,
                            range != null ? range.FirstColumn + range.ColumnCount - 1 : cell.Column)
                        && IsIncluded(
                            l.Area.StartRow,
                            l.Area.EndRow,
                            range != null ? range.FirstRow : cell.Row,
                            range != null ? range.FirstRow + range.RowCount - 1 : cell.Row)
                        )
                    .ToArray();

            var hLinksINSCT = hLinks
                    .Where(l =>
                        IsIntersected(
                            l.Area.StartColumn,
                            l.Area.EndColumn, 
                            range != null ? range.FirstColumn : cell.Column,
                            range != null ? range.FirstColumn + range.ColumnCount - 1 : cell.Column)
                        && IsIntersected(
                            l.Area.StartRow, 
                            l.Area.EndRow, 
                            range != null ? range.FirstRow : cell.Row, 
                            range != null ? range.FirstRow + range.RowCount - 1 : cell.Row)
                        )
                    .ToArray();
            return hLinksINCLD.Union(hLinksINSCT).ToArray();
            //return hLinksINCLD.Length > 0 ? hLinksINCLD : hLinksINSCT;
        }

        private static Hyperlink GetHyperlinkForCell(Cell cell, Worksheet sheet)
        {
            var range = cell.GetMergedRange();
            var vLinks = GetHyperlinksForCell(cell, sheet);
            var mgLinks = vLinks
                .Select(i1 =>
                    {
                        double res = 0.0;
                        res += Math.Abs(i1.Area.StartColumn - (range != null ? range.FirstColumn : cell.Column));
                        res += Math.Abs(i1.Area.EndColumn - (range != null ? (range.FirstColumn + range.ColumnCount - 1) : cell.Column));
                        res += Math.Abs(i1.Area.StartRow - (range != null ? range.FirstRow : cell.Row));
                        res += Math.Abs(i1.Area.EndRow - (range != null ? (range.FirstRow + range.RowCount - 1) : cell.Row));
                        return new { Link = i1, Length = res / 4 };
                    }
                )
                .OrderBy(i1 => i1.Length)
                .ToArray();
            return mgLinks.Select(i => i.Link).FirstOrDefault();
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
        #endregion

        public static ExelSheet[] LoadSheets(Workbook workbook, int count = 0, Action<int> progressReport = null, bool deleteEmptyRows = true)
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

            return result.ToArray();
        }

        public static ExelRow[] LoadRows(Worksheet sheet, int count = 0, Action<int> progressReport = null, bool deleteEmptyRows = true)
        {
            var result = new List<ExelRow>();
            var totalRowsCount = 0;
            var loaded = 0;

            var pp = new Helpers.PercentageProgress();
            if (progressReport != null)
                pp.Change += (s, e) => { progressReport((int)e.Value); };

            var pp0 = pp.GetChild(weight: 9);
            var pp1 = pp.GetChild(weight: 1);

            try
            {
                totalRowsCount = Math.Max(sheet.Cells.MaxRow + 1, sheet.Cells.Rows.Count);
            }
            catch
            {
                totalRowsCount = sheet.Cells.Rows.Count;
            }


            if (count <= 0)
               count = totalRowsCount;

            #region Select max column index for sheet
            int maxColumnIndex = sheet.Cells.Rows.Cast<Row>().Take(count).Select(c =>
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

            if (sheet.Cells.Count > 0)
            {
                var lockObj = new Object();
                var lockCell = new Object();
                sheet.Cells.MultiThreadReading = true;

                var items =
                        sheet.Cells.Rows.Cast<Row>()
                            .Where(r => r.Index < count)
                            .AsParallel()
                            .Select(row =>
                            {
                                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
                                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

                                var r = new ExelRow() { Index = row.Index };
                                var currMaxColumnsIndex = row.LastCell == null ? 0 : row.LastCell.Column;

                                int lastFilledColumnIndex = Math.Min(currMaxColumnsIndex, maxColumnIndex);

                                #region Read data from cells

                                var cells = Enumerable.Range(0, lastFilledColumnIndex + 1)
                                    .Select(cellIndex => new { OriginalCell = row.GetCellOrNull(cellIndex), Index = cellIndex })
                                    .Select(c => new { OriginalCell = c.OriginalCell, ResultCell = new ExelCell() { Value = string.Empty, FormatedValue = string.Empty, CellStyle = new Style() }, Index = c.Index })
                                    .Select(i =>
                                        {
                                            if (i.OriginalCell != null)
                                            {
                                                lock(lockCell)
                                                    i.ResultCell.FormatedValue = i.OriginalCell.StringValue;

                                                var link = GetHyperlinkForCell(i.OriginalCell, sheet);
                                                if (link != null)
                                                    i.ResultCell.HyperLink = link.Address;
                                                else
                                                {
                                                    var formula = string.Empty;
                                                    lock(lockCell)
                                                        formula = i.OriginalCell.Formula;

                                                    if (!string.IsNullOrWhiteSpace(formula))
                                                        i.ResultCell.HyperLink = formula.Split(new char[] { '\"' }).FirstOrDefault(str => str.Contains("http"));
                                                }

                                                if (i.OriginalCell.IsMerged)
                                                {
                                                    i.ResultCell.IsMerged = true;

                                                    var content = string.Empty;
                                                    var values = (IEnumerable)i.OriginalCell.GetMergedRange().Value;
                                                    if (values != null)
                                                        foreach (var value in values)
                                                            content += value;

                                                    i.ResultCell.Value = content;
                                                }
                                                else if (i.OriginalCell.Value != null)
                                                {
                                                    i.ResultCell.Value = i.OriginalCell.Value.ToString();
                                                }
                                                else
                                                {
                                                    i.ResultCell.Value = string.Empty;
                                                }

                                                try
                                                {
                                                    var comment = sheet.Comments[i.OriginalCell.Row, i.Index];
                                                    if (comment != null)
                                                        i.ResultCell.Comment = comment.Note;
                                                }
                                                catch { }

                                                var style = i.OriginalCell.GetStyle();
                                                i.ResultCell.CellStyle = style;
                                                i.ResultCell.Color = (style != null) ? (DefColors.Any(clr => ColorsEqual(clr, style.BackgroundColor)) ? style.ForegroundColor : style.BackgroundColor) : System.Drawing.Color.White;
                                                i.ResultCell.Color = System.Drawing.Color.FromArgb((i.ResultCell.Color.R > byte.MinValue || i.ResultCell.Color.G > byte.MinValue || i.ResultCell.Color.B > byte.MinValue) && i.ResultCell.Color.A == byte.MinValue ? byte.MaxValue : i.ResultCell.Color.A, i.ResultCell.Color.R, i.ResultCell.Color.G, i.ResultCell.Color.B);
                                            }
                                            return new { ResultCell = i.ResultCell, Index = i.Index };
                                        })
                                    .OrderBy(i => i.Index)
                                    .Select(i => i.ResultCell)
                                    .ToArray();
                                r.Cells.AddRange(cells);

                                #endregion

                                var lastStyle = (maxColumnIndex > 0) ? r.Cells[lastFilledColumnIndex].CellStyle : new Style();

                                for (int i = r.Cells.Count; i <= maxColumnIndex; i++)
                                    r.Cells.Add(new ExelCell() { Value = string.Empty, CellStyle = lastStyle });

                                lock (lockObj)
                                {
                                    loaded++;
                                    pp0.Value = (decimal)loaded * 100m / (decimal)count;
                                }

                                return r;
                            })
                            .OrderBy(r => r.Index)
                            .Where(r => !deleteEmptyRows || !r.IsEmpty) // delete empty rows
                            .ToArray();
                result.AddRange(items);
            }
            
            #endregion

            if (result.Count > 0)
            {
                #region Delete bottom

                //#### Try to delete bottom info ####
                //get last empty index

                var lastEmptyRow = result.LastOrDefault(row => row.IsEmpty);
                int lastEmptyRowsInDataIndex = lastEmptyRow == null ? 0 : result.IndexOf(lastEmptyRow);

                //int lastEmptyRowsInDataIndex =
                //    result
                //    .Where(row => row.IsEmpty)
                //    .Select(row => result.IndexOf(row))
                //    .OrderByDescending(i => i)
                //    .FirstOrDefault();

                //get last non empty index before last empty to check similarity
                var lastNotEmptyRow = result.Take(lastEmptyRowsInDataIndex == 0 ? result.Count : lastEmptyRowsInDataIndex).LastOrDefault(row => !row.IsEmpty);
                int lastNotEmptyRowsInDataIndexBeforeEmpty = lastNotEmptyRow == null ? 0 : result.IndexOf(lastNotEmptyRow);

                //int lastNotEmptyRowsInDataIndexBeforeEmpty =
                //    result
                //    .Where(row => !row.IsEmpty)
                //    .Select(row => result.IndexOf(row))
                //    .Where(i => i < lastEmptyRowsInDataIndex)
                //    .OrderByDescending(i => i)
                //    .FirstOrDefault();

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

                pp1.Value = 25;

                var isRowsSimilar = new Func<ExelRow, ExelRow, bool>((r1, r2) => 
                {
                    var cnt = Math.Min(r1.Cells.Count, r2.Cells.Count);
                    for (int i = 0; i < cnt; i++)
                        if (string.Compare(r1.Cells[i].Value.Trim(), r2.Cells[i].Value.Trim(), true) != 0)
                            return false;
                    return true;
                });

                var defStart = 4;
                var minCountForDeleteSimilarity = Properties.Settings.Default.MaxRowsInGroupCountToDeleteSimilarityRows;
                for (int i=result.Count-1; i>= defStart; i--)
                {
                    var n = 1;
                    if ((i - n >= defStart) && result[i].Similarity(result[i - n]) >= 0.8 && isRowsSimilar(result[i], result[i - n]))
                    {
                        var rowIndexes = new List<int>(new int[] { i - n });
                        do
                        {
                            n++;
                            rowIndexes.Add(i - n);
                        } while ((i - n >= defStart) && result[i].Similarity(result[i - n]) >= 0.8 && isRowsSimilar(result[i], result[i - n]));

                        if (n + 1 >= minCountForDeleteSimilarity)
                        {
                            rowIndexes
                                .OrderByDescending(ind => ind)
                                .ToList()
                                .ForEach(ind => result.RemoveAt(ind));
                            i -= n;
                            continue;
                            //result.RemoveAt(i);
                        }
                    }
                }

                pp1.Value = 50;

                #endregion

                //Delete all empty rows from data
                if (deleteEmptyRows)
                    for (int z = result.Count - 1; z >= 0; z--)
                    {
                        var r1 = result[z];
                        if (r1.IsEmpty)
                            result.RemoveAt(z);
                    }

                pp1.Value = 100;
            }
            return result.ToArray();
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
    }
}
