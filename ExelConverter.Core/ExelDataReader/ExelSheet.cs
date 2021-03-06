﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter.CommonTypes;
using Helpers;

namespace ExelConverter.Core.ExelDataReader
{
    public class ExelSheet
    {
        public ExelSheet()
        {
            Rows = new List<ExelRow>();
            SheetHeaders = new SheetHeadersContainer();
        }

        public string Name { get; set; }

        private List<ExelRow> rows = null;
        public List<ExelRow> Rows
        {
            get
            {
                return rows;
            }
            set
            {
                rows = value;
                getAllHeadersData = null;
            }
        }

        public ExelRow MainHeader { get; set; }

        public int MainHeaderRowCount { get; set; }

        public SheetHeadersContainer SheetHeaders { get; set; }

        public void UpdateMainHeaderRow(string[] tags)
        {
            MainHeaderRowCount = 0;

            if (Rows.Count != 0)
            {
                var searchRowsCount = Rows.Count < 50 ? Rows.Count : 50;

                var allTags = Tag.FromStrings(tags);
                var exTags = allTags.Where(t => t.Direction == TagDirection.Exclude).ToArray();
                var inTags = allTags.Where(t => t.Direction == TagDirection.Include).ToArray();

                Dictionary<int, double> weightDict = new Dictionary<int, double>();
                for (var i = searchRowsCount - 1; i >= 0; i--)
                    if (!Rows[i].IsEmpty)
                    {
                        double weight = 0.0;
                        var uniqueCells = Rows[i].UniqueNotEmptyCells.Select(c => c.Value.Trim().ToLower()).ToArray();
                        weight = exTags.Any(t => uniqueCells.Any(c => c.Like(t.Value)))
                                ? 0
                                : uniqueCells.Count(c => inTags.Any(t => c.Like(t.Value))) + uniqueCells.Count(c => inTags.Where(t => t.IsStrong).Any(t => c.Like(t.Value))) * 5;
                        weightDict.Add(i, weight);
                    }

                double maxWeight = weightDict.Select(kvp => kvp.Value).OrderByDescending(i => i).FirstOrDefault();
                if (maxWeight > 0)
                {
                    var headerRows = weightDict.Where(kvp => kvp.Value == maxWeight).Select(kvp => kvp.Key).OrderBy(kvp => kvp);
                    int mainHeaderIndex = headerRows.FirstOrDefault();

                    int mainHeaderCnt = 1;
                    for (mainHeaderCnt = 1; mainHeaderCnt < headerRows.Count(); mainHeaderCnt++)
                        if (!headerRows.Contains(mainHeaderIndex + mainHeaderCnt))
                            break;

                    MainHeaderRowCount = mainHeaderCnt > 4 ? 1 : mainHeaderCnt;
                    MainHeader = Rows.ElementAt(mainHeaderIndex);
                    return;
                }
            }
            MainHeader = Rows.FirstOrDefault();
            MainHeaderRowCount = 1;
        }

        private List<int> HeaderLineNumbers
        {
            get
            {
                var result = new List<int>();
                var headerNumbers = GetAllHeadersData();
                foreach (int i in headerNumbers)
                    if (headerNumbers.Contains(i + 1))
                        result.Add(i);
                return result;
            }
        }

        private List<int> SubheaderLineNumbers
        {
            get
            {
                var result = new List<int>();
                var headerNumbers = GetAllHeadersData();
                foreach (int i in headerNumbers)
                    if (!headerNumbers.Contains(i + 1))
                        result.Add(i);
                return result;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public void UpdateHeaders(string[] tags = null)
        {
            getAllHeadersData = null;
            GetAllHeadersData(tags);

            var getHeaders = new Func<int[], SheetHeader[]>((rowNumbers) =>
            {
                var sHeaders = new List<SheetHeader>();
                var deepLevel = 0;
                while (true)
                {
                    deepLevel++;

                    var hdrs =
                        rowNumbers
                        .Except(sHeaders.Select(r => r.RowNumber))
                        .Select(i => new { Row = Rows[i], LineNumber = i })
                        .Select(
                            row =>
                            {
                                var headers = row.Row.Cells.Where(c => c.Value.Length > 0).OrderByDescending(c => c.Value.Length).Take(deepLevel).Select(c => c.Value);
                                var header = string.Empty;
                                foreach (var h in headers)
                                    header += (string.IsNullOrWhiteSpace(header) ? string.Empty : "_") + h;
                                return new SheetHeader { Header = header, RowNumber = row.LineNumber };
                            }
                        ).ToArray();

                    if (hdrs.Count() == 0)
                        break;

                    if (deepLevel >= 5)
                    {
                        sHeaders.AddRange(hdrs);
                        break;
                    }
                    else
                    {
                        sHeaders.AddRange(hdrs
                            .GroupBy(h => h.Header)
                            .Select(g => new { Header = g.First(), Count = g.Count() })
                            .Where(i => i.Count == 1)
                            .Select(i => i.Header)
                            );
                    }
                }
                return sHeaders.OrderBy(h => h.RowNumber).ToArray();
            });

            SheetHeaders.Headers = new ObservableCollection<SheetHeader>(getHeaders(HeaderLineNumbers.ToArray()));
            SheetHeaders.Subheaders = new ObservableCollection<SheetHeader>(getHeaders(SubheaderLineNumbers.ToArray()));
        }

        private List<int> getAllHeadersData = null;
        private List<int> GetAllHeadersData(string[] tags = null)
        {
            if (getAllHeadersData != null)
                return getAllHeadersData;

            var result = new List<int>();
            var endHeaderRowIndex = Rows.IndexOf(MainHeader) + (MainHeaderRowCount - 1);

            Guid logSession = Helpers.Old.Log.SessionStart("ExelSheet.GetAllHeadersData()", true);
            Helpers.Old.Log.Add(logSession, string.Format("HeaderSearchAlgorithm = {0}", ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm));
            Helpers.Old.Log.Add(logSession, string.Format("rows count for import: '{0}' from '{1}' main row", Rows.Count, endHeaderRowIndex));
            try
            {
                if (Rows.Count > endHeaderRowIndex)
                {
                    #region Tagged

                    var rowIndexesForHeadersWithTags =
                            Rows
                            .AsParallel()
                            .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                            .Select(r =>
                                {
                                    bool hasStrongIntersection;
                                    var res = (decimal)r.UniqueWeight + Tag.IsIntersected(tags, r.UniqueNotEmptyCells.Select(c => c.Value).ToArray(), out hasStrongIntersection, (decimal)-1);
                                    return new { Weight = res, HasStrongIntersection = hasStrongIntersection, Row = r };
                                })
                            .Where(r => r.Weight >= (decimal)0.8 || r.HasStrongIntersection)
                            .Select(r => Rows.IndexOf(r.Row))
                            .ToArray();

                    //var r2 = Rows[3];
                    //var b = IsIntersected(tags, Rows[3].UniqueNotEmptyCells.Select(c => c.Value).ToArray());

                    Helpers.Old.Log.Add(logSession, string.Format("founded '{0}' TAGGED rows for sheets candidate.", rowIndexesForHeadersWithTags.Length));

                    #endregion
                    #region By values

                    Helpers.Old.Log.Add(logSession, string.Format("calculating by values..."));

                    var rowIndexesForHeadersByValue =
                        Rows
                            .AsParallel()
                            .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                            .AsParallel()
                            .GroupBy(r => r.UniqueNotEmptyCells.Count())
                            .Select(g =>
                                new
                                {
                                    Length = g.FirstOrDefault().UniqueNotEmptyCells.Count(),
                                    Count = g.Count(),
                                    Rows = g.Cast<ExelRow>()
                                }
                                )
                            .Where(i =>
                                i.Length > 0
                                && i.Count <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount
                                )
                            .OrderBy(d => d.Count)
                            .Take(10)
                            .AsParallel()
                            .SelectMany(g => g.Rows.Select(r => Rows.IndexOf(r)))
                            .Distinct()
                            .ToArray();

                    Helpers.Old.Log.Add(logSession, string.Format("founded '{0}' rows for sheets candidate; Need check.", rowIndexesForHeadersByValue.Length));

                    #endregion
                    #region By style
                    Helpers.Old.Log.Add(logSession, string.Format("calculating by styles..."));

                    var rowIndexesForHeadersByWeight =
                        Rows
                            .AsParallel()
                            .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                            .AsParallel()
                            .GroupBy(r => r.UniqueWeight)
                            .Select(g =>
                                new
                                {
                                    Weight = g.FirstOrDefault().UniqueWeight,
                                    Count = g.Count(),
                                    Rows = g.Cast<ExelRow>()
                                }
                                )
                            .Where(i =>
                                i.Weight > 0
                                && i.Count <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount
                                )
                            .OrderBy(d => d.Count)
                            .Take(3)
                            .AsParallel()
                            .SelectMany(g => g.Rows.Select(r => Rows.IndexOf(r)))
                            .Distinct()
                            .ToArray();

                    Helpers.Old.Log.Add(logSession, string.Format("founded '{0}' rows for sheets candidate; Need check.", rowIndexesForHeadersByWeight.Length));

                    #endregion

                    var res0 = rowIndexesForHeadersWithTags
                        .Union(rowIndexesForHeadersByValue)
                        .Union(rowIndexesForHeadersByWeight)
                        .Distinct()
                        .OrderBy(i => i)
                        .ToList();

                    Helpers.Old.Log.Add(logSession, string.Format("Calculation for '{0}' rows starts...", res0.Count));
                    SomeCalculations(res0, endHeaderRowIndex, tags, strongIndexes: rowIndexesForHeadersWithTags);
                    Helpers.Old.Log.Add(logSession, string.Format("Calculation end with '{0}' rows.", res0.Count));
                    result.AddRange(res0);
                }
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession);
            }
            return getAllHeadersData = result;
        }

        private void SomeCalculations(List<int> result, int endHeaderRowIndex, string[] tags, int[] strongIndexes)
        {
            var logSessiong = Helpers.Old.Log.SessionStart("ExelSheet.SomeCalculations()", true);
            bool wasException = false;

            try
            {
                #region Log
                string indexes = string.Empty;
                string tags2 = string.Empty;

                if (result != null)
                    foreach (var ind in result)
                        indexes += ind.ToString() + ";";

                if (tags != null)
                    foreach (var tag in tags)
                        tags2 += tag + ";";

                Helpers.Old.Log.Add(logSessiong, string.Format("Incoming indexes: '{0}'.", indexes));
                Helpers.Old.Log.Add(logSessiong, string.Format("Incoming tags: '{0}'.", tags2));
                #endregion

                if (result != null)
                {
                    var Coeff = new Func<ExelRow, double>((row) =>
                    {
                        var cnt = row.UniqueNotEmptyCells.Count();
                        var defCoeff = 1.0;
                        if (cnt > 0)
                        {
                            bool hasStrongIntersection;
                            var subRes = (Tag.GetIntersectedCount(tags, row.UniqueNotEmptyCells.Select(c => c.Value).ToArray(), out hasStrongIntersection) / row.UniqueNotEmptyCells.Count());
                            return defCoeff - subRes * 0.3;
                        }
                        else
                            return defCoeff;
                    });

                    #region remove headers if step by step > 2

                    Helpers.Old.Log.Add(logSessiong, string.Format("Remove headers if step by step > 2"));

                    for (int i = result.Count - 1; i >= 0; i--)
                    {
                        int n = 1;
                        int index = result[i];

                        double similarityD = 0;
                        double similatityD4 = 0;

                        while (
                                result.Contains(index - n) &&
                                (similarityD = Rows[index].Similarity(Rows[index - n])) * Coeff(Rows[index]) * Coeff(Rows[index - n]) > 0.45 &&
                                (similatityD4 = Rows[index].Similarity(Rows[index - n], 4)) * Coeff(Rows[index]) * Coeff(Rows[index - n]) > 0.8)
                        {
                            n++;
                        }
                        if (n > 2)
                        {
                            int deleted = 0;
                            for (int z = 0; z < n; z++)
                            {
                                if (!strongIndexes.Contains(index - z))
                                {
                                    Helpers.Old.Log.Add(logSessiong, string.Format("Remove header index '{0}'", index - z));
                                    result.Remove(index - z);
                                }
                                else
                                    Helpers.Old.Log.Add(logSessiong, string.Format("Can't remove protected (by tags) index '{0}'", index - z));
                                deleted++;
                            }
                            i -= deleted - 1;
                        }
                    }
                    #endregion

                    #region remove bottom groups

                    Helpers.Old.Log.Add(logSessiong, string.Format("Remove bottom groups"));

                    if (result.Count > 0)
                    {
                        for (int i = Rows.Count - 1; i > endHeaderRowIndex; i++)
                        {
                            if (result.Contains(i))
                            {
                                Helpers.Old.Log.Add(logSessiong, string.Format("Remove index '{0}'", i));
                                result.Remove(i);
                            }
                            else
                                break;
                        }
                    }
                    #endregion

                    #region calc similarity with other (non headers) lines and remove header if similary like simple row

                    Helpers.Old.Log.Add(logSessiong, string.Format("Calc similarity with other (non headers) lines and remove header if similary like simple row"));

                    var oldResCnt = 0;
                    var calcCount = 0;
                    while (result.Count != oldResCnt)
                    {
                        calcCount++;
                        Helpers.Old.Log.Add(logSessiong, string.Format("Calc similarity.. Step #{0}", calcCount));

                        oldResCnt = result.Count;
                        foreach (var item in result
                            .Except(strongIndexes)
                            .OrderByDescending(i => i)
                            .ToArray()
                            .Select(i => new
                            {
                                Index = i,
                                Coeff = Coeff(Rows[i]),
                                Row = Rows[i]
                            }))
                        {
                            int maxCountToTake = 7;
                            var simIndexesRight =
                                    Rows
                                        .AsParallel()
                                        .Select(r => Rows.IndexOf(r))
                                        .Where(inx => inx > item.Index && !result.Contains(inx))
                                        .OrderBy(inx => inx)
                                        .Take(maxCountToTake);
                            var simIndexesLeft =
                                    Rows
                                        .AsParallel()
                                        .Select(r => Rows.IndexOf(r))
                                        .Where(inx => inx < item.Index && inx > endHeaderRowIndex && !result.Contains(inx))
                                        .OrderByDescending(inx => inx)
                                        .Take(maxCountToTake);

                            if (simIndexesLeft
                                    .Union(simIndexesRight)
                                    .Distinct()
                                    //.OrderBy(inx => (double)Math.Abs(inx - index) + ((inx - index) < 0 ? 0.5 : 0))
                                    .AsParallel()
                                    .Any(similarityIndex =>
                                            {
                                                double similarityD = item.Row.Similarity(Rows[similarityIndex]);
                                                double similarityD4 = item.Row.Similarity(Rows[similarityIndex], 4);
                                                double similarityHalf = item.Row.Similarity(Rows[similarityIndex], (int)((float)Rows[similarityIndex].Cells.Count / (float)2));

                                                return (similarityD * item.Coeff > 0.78 && similarityD4 * item.Coeff > 0.60)
                                                       || (similarityD * item.Coeff > 0.65 && similarityD4 * item.Coeff > 0.95)
                                                       || (Rows[similarityIndex].Cells.Count >= 10 && similarityD * item.Coeff > 0.3 && similarityD4 * item.Coeff > 0.8 && similarityHalf > 0.7)
                                                       ;
                                            }))
                            {
                                Helpers.Old.Log.Add(logSessiong, string.Format("Remove header with index '{0}'", item.Index));
                                result.Remove(item.Index);
                            }
                        }
                    }

                    #region add

                    Helpers.Old.Log.Add(logSessiong, string.Format("Calc similarity with other (non headers) lines and remove header if similary like simple row (Additional)"));

                    if (result.Count > 0)
                    {
                        decimal averageWeight = result.Select(i => (decimal)Rows[i].UniqueWeight).Average();
                        Helpers.Old.Log.Add(logSessiong, string.Format("Average unqiue weight: '{0}'", averageWeight));

                        decimal maxWeight = result.Select(i => (decimal)Rows[i].UniqueWeight).Max();
                        Helpers.Old.Log.Add(logSessiong, string.Format("Max unqiue weight: '{0}'", maxWeight));

                        decimal minWeight = result.Select(i => (decimal)Rows[i].UniqueWeight).Min();
                        Helpers.Old.Log.Add(logSessiong, string.Format("Min unqiue weight: '{0}'", minWeight));

                        decimal moduleWeight = (maxWeight - minWeight) / 4m;
                        if (moduleWeight == 0.0m)
                            moduleWeight = 0.001m;
                        Helpers.Old.Log.Add(logSessiong, string.Format("Module unqiue weight: '{0}'", moduleWeight));

                        var subItemsToDelete = result
                            .Except(strongIndexes)
                            .Select(i => new { Index = i, Row = Rows[i] })
                            .OrderByDescending(i => i.Index)
                            .Where(i => (decimal)i.Row.UniqueWeight < averageWeight - moduleWeight || (decimal)i.Row.UniqueWeight > averageWeight + moduleWeight)
                            .Select(item => new
                            {
                                Index = item.Index,
                                Row = item.Row,
                                SimilaryItems =
                                Rows.AsParallel()
                                    .Where(r => !result.Contains(Rows.IndexOf(r)) && r.Index > MainHeader.Index + MainHeaderRowCount - 1)
                                    .OrderBy(r => Math.Abs(item.Index - r.Index))
                                    .Take(15)
                                    .ToArray()
                            })
                            .Select(item =>
                            new
                            {
                                Index = item.Index,
                                Row = item.Row,
                                Sim4 = item.SimilaryItems.Select(r => (decimal)item.Row.Similarity(r, (int)((double)item.Row.Cells.Count / 4.0))).Union(new decimal[] { 0.0m }).Max(),
                                Sim2 = item.SimilaryItems.Select(r => (decimal)item.Row.Similarity(r, (int)((double)item.Row.Cells.Count / 2.0))).Union(new decimal[] { 0.0m }).Max(),
                                Sim1 = item.SimilaryItems.Select(r => (decimal)item.Row.Similarity(r, (int)((double)item.Row.Cells.Count / 1.0))).Union(new decimal[] { 0.0m }).Max(),
                            })
                            .ToArray();

                        var itemsToDelete = subItemsToDelete
                            .Where(item => item.Sim4 > 0.78m)
                            .Select(i => i.Index)
                            .ToArray();
                        foreach (var index in itemsToDelete)
                        {
                            Helpers.Old.Log.Add(logSessiong, string.Format("Remove index: '{0}'", index));
                            result.Remove(index);
                        }

                        //for (int i = result.Count - 1; i >= 0; i--)
                        //    if ((decimal)Rows[result[i]].UniqueWeight < averageWeight - moduleWeight)
                        //    {
                        //        int index = result[i];

                        //        List<int> similarityIndexes = new List<int>();

                        //        for (int n = 1; n < Rows.Count; n++)
                        //        {
                        //            if (!result.Contains(index + n) && (index + n) < Rows.Count)
                        //                similarityIndexes.Add(index + n);
                        //            if (!result.Contains(index - n) && (index - n) > Rows.IndexOf(MainHeader) + MainHeaderRowCount - 1)
                        //                similarityIndexes.Add(index - n);

                        //            if (similarityIndexes.Count > 15)
                        //                break;
                        //        }

                        //        foreach (int similarityIndex in similarityIndexes)
                        //        {
                        //            //Rows[index].Cells.IndexOf(Rows[index].UniqueNotEmptyCells.Last());

                        //            double similarityD = Rows[index].Similarity(Rows[similarityIndex], (int)((double)Rows[index].Cells.Count / 4.0));
                        //            if (similarityD > 0.78 && !strongIndexes.Contains(index))
                        //            {
                        //                result.Remove(index);
                        //                break;
                        //            }
                        //        }
                        //    }
                    }
                    #endregion

                    #endregion

                    #region remove excluded tags

                    Helpers.Old.Log.Add(logSessiong, string.Format("Remove excluded tags"));

                    var excludedTags = Tag.FromStrings(tags).Where(t => t.Direction == TagDirection.Exclude).ToArray();
                    if (result.Count > 0 && excludedTags.Length > 0)
                    {
                        var excludeHeaderIndexes =
                                Rows
                                    .Where(r => result.Contains(Rows.IndexOf(r)))
                                    .Where(r => r.Cells.Any(c => excludedTags.Any(t => c.Value.Like(t.Value))))
                                    .Select(r => Rows.IndexOf(r));

                        foreach (int ex in excludeHeaderIndexes)
                        {
                            Helpers.Old.Log.Add(logSessiong, string.Format("Remove index: '{0}'", ex));
                            result.Remove(ex);
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSessiong, ex);
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSessiong, wasException);
            }
        }

        #region Something for calculation version = 1

        private bool HasBadSymbols(string value, string badSymbols)
        {
            bool result = false;
            string val = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(badSymbols))
                Parallel.ForEach(badSymbols.ToCharArray(), chr => { if (val.Contains(chr)) result = true; });
            return result;
        }

        /// <summary>
        /// Удаление из строк одинаковых значений
        /// </summary>
        /// <param name="list">Строки</param>
        /// <returns>Строки без одинаковых значений</returns>
        private string[] GetEqualElements(string[] list)
        {
            return list.AsParallel().Select(str => str.Trim().ToLower()).Distinct().ToArray();
        }

        #endregion
        #region AsDataTable

        public DataTable AsDataTable(int[] rowNumbers, bool smartHyperlinks = true)
        {
            var result = new DataTable();

            var rows = Rows.Where(r => rowNumbers.Contains(Rows.IndexOf(r))).ToArray();

            List<int> hyperLinkColumns = new List<int>();
            foreach (ExelRow row in rows)
                for (int i = 0; i < row.Cells.Count; i++)
                    if (!string.IsNullOrWhiteSpace(row.Cells[i].HyperLink))
                        if (!hyperLinkColumns.Contains(i))
                            hyperLinkColumns.Add(i);

            var maxCoulumnsCount = Rows.Max(r => r.Cells.Count) + ((smartHyperlinks) ? hyperLinkColumns.Count : 0);
            for (var i = 0; i < maxCoulumnsCount; i++)
                result.Columns.Add();

            foreach (ExelRow row in rows)
            {
                var stringRow = new List<string>();
                for (int n = 0; n < row.Cells.Count; n++)
                {
                    ExelCell cell = row.Cells[n];
                    string cellValue = string.Empty;

                    cellValue = (!string.IsNullOrWhiteSpace(cell.HyperLink) && !smartHyperlinks) || (smartHyperlinks && string.IsNullOrWhiteSpace(cell.Value)) ? cell.HyperLink : cell.Value;
                    if (smartHyperlinks)
                    {
                        if (hyperLinkColumns.Contains(n))
                        {
                            cellValue = cell.Value;
                            stringRow.Add(cellValue);
                            cellValue = cell.HyperLink;
                        }
                    }
                    stringRow.Add(cellValue);
                }


                result.Rows.Add(stringRow.ToArray());

            }
            return result;
        }

        public DataTable AsDataTable(int count = int.MaxValue)
        {
            var result = new DataTable();
            if (Rows != null && Rows.Count > 0)
            {
                result.Columns.Add("#");
                result.Columns.Add("##");
                var maxCoulumnsCount = Rows.Max(r => r.Cells.Count);
                for (var i = 0; i < maxCoulumnsCount; i++)
                {
                    result.Columns.Add();
                }
                result.Columns.Add("type");

                var rows = Rows.Take(count == int.MaxValue ? Rows.Count : Math.Min(Rows.Count, count)).ToArray();
                for (var i = 0; i < rows.Length; i++)
                {
                    result.Rows.Add(
                        new ExelCell[] { new ExelCell() { Value = i.ToString() }, new ExelCell() { Value = (rows[i].Index + 1).ToString() } }
                        .Union(rows[i].Cells.ToArray())
                        .Union(new ExelCell[] { new ExelCell() { Value = string.Empty } })
                        .ToArray());
                }
            }
            return result;
        }

        #endregion
    }
}
