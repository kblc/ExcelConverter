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

                Dictionary<int, double> weightDict = new Dictionary<int, double>();
                for (var i = searchRowsCount - 1; i >= 0; i--)
                    if (!Rows[i].IsEmpty)
                    {
                        double weight = 0.0;
                        var uniqueCells = Rows[i].UniqueNotEmptyCells.Select(c => c.Value.Trim().ToLower()).ToArray();
                        weight = ExcludedTags(tags).Any(t => uniqueCells.Any(c => c.Like(t)) )
                                ? 0
                                : uniqueCells.Count( c => IncludedTags(tags).Any(t => c.Like(t)) );
                        weightDict.Add(i, weight);
                    }

                double maxWeight = weightDict.Select( kvp => kvp.Value).OrderByDescending( i => i).FirstOrDefault();
                if (maxWeight > 0)
                {

                    var headerRows = weightDict.Where(kvp => kvp.Value == maxWeight).Select(kvp => kvp.Key).OrderBy(kvp => kvp);
                    int mainHeaderIndex = headerRows.FirstOrDefault();
                    MainHeaderRowCount = headerRows.Count() > 3 ? 1 : headerRows.Count();
                    MainHeader = Rows.ElementAt(mainHeaderIndex);
                    return;
                }

                //for (var i = searchRowsCount - 1; i >= 0; i--)
                //{
                //    if (Rows.ElementAt(i).Cells.Count != 0)
                //    {
                //        for (var j = 0; j < Rows.ElementAt(i).Cells.Count; j++)
                //        {
                //            if (tags.Any(s => Rows.ElementAt(i).Cells.ElementAt(j).Value.ToLower().Contains(s.ToLower())))
                //            {
                //                MainHeader = Rows.ElementAt(i);
                //                return;
                //            }
                //        }
                //    }
                //}
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
            
            var headersLevel1 = new ObservableCollection<SheetHeader>(
                HeaderLineNumbers.Select(
                    i => new SheetHeader
                    {
                        Header = Rows[i].Cells.Where(r => ((ExelCell)r).Value.Length == Rows[i].Cells.Max(rw => ((ExelCell)rw).Value.Length)).Select(c => (ExelCell)c).First().Value,
                        RowNumber = i
                    }
                ).ToArray());

            var headersLevel2 = new ObservableCollection<SheetHeader>(
                SubheaderLineNumbers.Select(
                    i => new SheetHeader
                    {
                        Header = Rows[i].Cells.Where(r => ((ExelCell)r).Value.Length == Rows[i].Cells.Max(rw => ((ExelCell)rw).Value.Length)).Select(c => (ExelCell)c).First().Value,
                        RowNumber = i
                    }
                ).ToArray());

            //if (headersLevel1.Count > 0 || 1 == 1) //always
            //{
            SheetHeaders.Headers = headersLevel1;
            SheetHeaders.Subheaders = headersLevel2;
            //}
            //else
            //{
            //    SheetHeaders.Headers = headersLevel2;
            //    SheetHeaders.Subheaders = new ObservableCollection<SheetHeader>();
            //}
        }

        private List<int> getAllHeadersData = null;
        private List<int> GetAllHeadersData(string[] tags = null)
        {
            if (getAllHeadersData != null)
                return getAllHeadersData;

            var result = new List<int>();
            var endHeaderRowIndex = Rows.IndexOf(MainHeader) + (MainHeaderRowCount - 1);

            Guid logSession = Log.SessionStart("ExelSheet.GetAllHeadersData()", true);
            Log.Add(logSession, string.Format("HeaderSearchAlgorithm = {0}", ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm));
            Log.Add(logSession, string.Format("rows count for import: '{0}' from '{1}' main row", Rows.Count, endHeaderRowIndex));
            try
            {
                if (Rows.Count > endHeaderRowIndex)
                {
                    #region Tagged

                    var rowIndexesForHeadersWithTags =
                            Rows
                            .AsParallel()
                            .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                            .Where(r => r.UniqueWeight + (IsIntersected(tags, r.UniqueNotEmptyCells.Select(c => c.Value).ToArray()) ? 0.3 : -1) > 0.8)
                            .Select(r => Rows.IndexOf(r))
                            .ToArray();

                    Log.Add(logSession, string.Format("founded '{0}' TAGGED rows for sheets candidate.", rowIndexesForHeadersWithTags.Length));
                    
                    #endregion
                    #region By values

                    Log.Add(logSession, string.Format("calculating by values..."));

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

                    Log.Add(logSession, string.Format("founded '{0}' rows for sheets candidate; Need check.", rowIndexesForHeadersByValue.Length));

                    #endregion
                    #region By style
                    Log.Add(logSession, string.Format("calculating by styles..."));

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

                    Log.Add(logSession, string.Format("founded '{0}' rows for sheets candidate; Need check.", rowIndexesForHeadersByWeight.Length));

                    #endregion

                    List<int> res = new List<int>(
                        rowIndexesForHeadersWithTags
                        .Union(rowIndexesForHeadersByValue)
                        .Union(rowIndexesForHeadersByWeight)
                        .Distinct()
                        .OrderBy(i => i)
                        );

                    Log.Add(logSession, string.Format("Calculation for '{0}' rows starts...", res.Count));
                    SomeCalculations(res, endHeaderRowIndex, tags);
                    Log.Add(logSession, string.Format("Calculation end with '{0}' rows.", res.Count));
                    result.AddRange(res);
                }
            }
            finally
            {
                Log.SessionEnd(logSession);
            }
            return getAllHeadersData = result;
        }

        private bool IsIntersected(string[] allTags, string[] items)
        {
            return GetIntersectedCount(allTags, items) > 0;
        }

        private string DelStars(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) 
                return string.Empty; 

            while (str.Contains("**")) 
                str = str.Replace("**", "*"); 
            
            return str;
        }

        private string[] IncludedTags(string[] allTags)
        {
            return
                allTags
                .Where(t => !string.IsNullOrEmpty(t) && t[0] != '-')
                .Select(i => i != null ? DelStars("*" + i.Trim().ToLower().Replace(' ', '*') + "*") : null)
                .Where(i => !string.IsNullOrWhiteSpace(i) && i != "*")
                .ToArray()
                ;
        }

        private string[] ExcludedTags(string[] allTags)
        {
            return
                allTags
                .Where(t => !string.IsNullOrEmpty(t) && t[0] == '-')
                .Select(t => t.Substring(1))
                .Select(i => i != null ? DelStars("*" + i.Trim().ToLower().Replace(' ', '*') + "*") : null)
                .Where(i => !string.IsNullOrWhiteSpace(i) && i != "*")
                .ToArray();
        }

        private int GetIntersectedCount(string[] allTags, string[] items)
        {
            if (allTags == null || items == null)
                return 0;

            items = items
                .Select(i => i != null ? DelStars(i.Trim().ToLower()).Replace("*", " ").Trim() : null)
                .Where(i => i != null)
                .ToArray();

            int result = 0;

            if (!ExcludedTags(allTags).Any(t => items.Any(i => i.Like(t)))) //if find excluded tag, then return zero
                foreach (var i0 in IncludedTags(allTags))
                    foreach(var i1 in items)
                        if (i1.Like(i0))
                            result++;

            return result;           
        }

        private void SomeCalculations(List<int> result, int endHeaderRowIndex, string[] tags)
        {
            var logSessiong = Log.SessionStart("ExelSheet.SomeCalculations()", true);
            bool wasException = false;

            try
            { 
                string indexes = string.Empty;
                string tags2 = string.Empty;

                if (result != null)
                    foreach (var ind in result)
                        indexes += ind.ToString() + ";";

                if (tags != null)
                    foreach (var tag in tags)
                        tags2 += tag + ";";

                Log.Add(logSessiong, string.Format("Incoming indexes: '{0}'.", indexes));
                Log.Add(logSessiong, string.Format("Incoming tags: '{0}'.", tags2));

                if (result != null)
                { 
                    var Coeff = new Func<ExelRow, double>((row) =>
                    {
                        var cnt = row.UniqueNotEmptyCells.Count();
                        var defCoeff = 1.0;
                        if (cnt > 0)
                            return
                                defCoeff -
                                (GetIntersectedCount(tags, row.UniqueNotEmptyCells.Select(c => c.Value).ToArray()) / row.UniqueNotEmptyCells.Count()) * 0.3;
                        else
                            return defCoeff;
                    });

                    #region remove headers if step by step > 2
                    for (int i = result.Count - 1; i >= 0; i--)
                    {
                        int n = 1;
                        int index = result[i];

                        double similarityD = 0;
                        double similatityD4 = 0;

                        while (
                                result.Contains(index - n) &&
                                (similarityD = Rows[index].Similarity(Rows[index - n])) * Coeff(Rows[index]) * Coeff(Rows[index-n]) > 0.45 &&
                                (similatityD4 = Rows[index].Similarity(Rows[index - n], 4)) * Coeff(Rows[index]) * Coeff(Rows[index - n]) > 0.8)
                        {
                            n++;
                        }
                        if (n > 2)
                        {
                            int deleted = 0;
                            for (int z = 0; z < n; z++)
                            {
                                result.Remove(index - z);
                                deleted++;
                            }
                            i -= deleted - 1;
                        }
                    }
                    #endregion

                    #region remove bottom groups
                    if (result.Count > 0)
                    {
                        for (int i = Rows.Count - 1; i > endHeaderRowIndex; i++)
                        {
                            if (result.Contains(i))
                                result.Remove(i);
                            else
                                break;
                        }
                    }
                    #endregion

                    #region calc similarity with other (non headers) lines and remove header if similary like simple row

                    var oldResCnt = 0;
                    while (result.Count != oldResCnt)
                    {
                        oldResCnt = result.Count;
                        foreach(var index in result.OrderByDescending(i=>i).ToArray())
                        {
                            double coeff = Coeff(Rows[index]);
                            int maxCountToTake = 7;
                            var simIndexesRight =
                                    Rows
                                        .AsParallel()
                                        .Select(r => Rows.IndexOf(r))
                                        .Where(inx => inx > index && !result.Contains(inx))
                                        .OrderBy(inx => inx)
                                        .Take(maxCountToTake);
                            var simIndexesLeft =
                                    Rows
                                        .AsParallel()
                                        .Select(r => Rows.IndexOf(r))
                                        .Where(inx => inx < index && inx > endHeaderRowIndex && !result.Contains(inx))
                                        .OrderByDescending(inx => inx)
                                        .Take(maxCountToTake);

                            if (simIndexesLeft
                                    .Union(simIndexesRight)
                                    .OrderBy(inx => (double)Math.Abs(inx - index) + ((inx - index) < 0 ? 0.5 : 0))
                                    .AsParallel()
                                    .Any(similarityIndex =>
                                            {
                                                double similarityD = Rows[index].Similarity(Rows[similarityIndex]);
                                                double similarityD4 = Rows[index].Similarity(Rows[similarityIndex], 4);
                                                double similarityHalf = Rows[index].Similarity(Rows[similarityIndex], (int)((float)Rows[similarityIndex].Cells.Count / (float)2));

                                                return (similarityD * coeff > 0.78 && similarityD4 * coeff > 0.60)
                                                       || (similarityD * coeff > 0.65 && similarityD4 * coeff > 0.95)
                                                       || (Rows[similarityIndex].Cells.Count >= 10 && similarityD * coeff > 0.3 && similarityD4 * coeff > 0.8 && similarityHalf > 0.7)
                                                       ;
                                            }))
                                result.Remove(index);
                        }
                    }

                    #region add
                    if (result.Count > 0)
                    {
                        double averageWeight = result.Select(i => Rows[i].UniqueWeight).Average();
                        double maxWeight = result.Select(i => Rows[i].UniqueWeight).Max();
                        double minWeight = result.Select(i => Rows[i].UniqueWeight).Min();
                        double moduleWeight = (maxWeight - minWeight) / 4;

                        if (moduleWeight == 0.0)
                            moduleWeight = 0.001;

                        for (int i = result.Count - 1; i >= 0; i--)
                            if (Rows[result[i]].UniqueWeight < averageWeight - moduleWeight)
                            {
                                int index = result[i];

                                List<int> similarityIndexes = new List<int>();

                                for (int n = 1; n < Rows.Count; n++)
                                {
                                    if (!result.Contains(index + n) && (index + n) < Rows.Count)
                                        similarityIndexes.Add(index + n);
                                    if (!result.Contains(index - n) && (index - n) > Rows.IndexOf(MainHeader) + MainHeaderRowCount - 1)
                                        similarityIndexes.Add(index - n);

                                    if (similarityIndexes.Count > 15)
                                        break;
                                }

                                foreach (int similarityIndex in similarityIndexes)
                                {
                                    double similarityD = Rows[index].Similarity(Rows[similarityIndex], (int)((double)Rows[index].Cells.Count / 4.0));
                                    if (similarityD > 0.78)
                                    {
                                        result.Remove(index);
                                        break;
                                    }
                                }
                            }
                    }
                    #endregion

                    #endregion

                    #region remove excluded tags

                    string[] excludedTags = ExcludedTags(tags);

                    if (result.Count > 0 && excludedTags.Length > 0)
                    {
                        var excludeHeaderIndexes =
                                Rows
                                    .Where(r => result.Contains(Rows.IndexOf(r)))
                                    .Where(r => r.Cells.Any(c => excludedTags.Any(t => c.Value.Like(t))))
                                    .Select(r => Rows.IndexOf(r));

                        foreach (int ex in excludeHeaderIndexes)
                            result.Remove(ex);
                    }
                    #endregion
                }
            }
            catch(Exception ex)
            {
                wasException = true;
                Log.Add(logSessiong, ex);
            }
            finally
            {
                Log.SessionEnd(logSessiong, wasException);
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
                        new ExelCell[] { new ExelCell() { Value = (i+1).ToString() }, new ExelCell() { Value = (rows[i].Index+1).ToString() } }
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
