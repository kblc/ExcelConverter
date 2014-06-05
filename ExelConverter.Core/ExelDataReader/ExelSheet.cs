using System;
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
                        weight = tags.Where(tag => uniqueCells.Contains(tag.ToLower())).Count();
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

            Guid logSession = Log.SessionStart("GetAllHeadersData()");
            Log.Add(logSession, string.Format("HeaderSearchAlgorithm = {0}", ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm));
            Log.Add(logSession, string.Format("rows count for import: '{0}' from '{1}' main row", Rows.Count, endHeaderRowIndex));
            try
            {
                if (Rows.Count > endHeaderRowIndex)
                {
                    #region By values

                    Log.Add(logSession, string.Format("calculating by values..."));

                    var lengthsList =
                        Rows
                            .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                            .AsParallel()
                            .Select(r => r.UniqueNotEmptyCells.Count())
                            .GroupBy(r => r)
                            .Select(g =>
                                new
                                {
                                    Length = g.FirstOrDefault(),
                                    Count = g.Count()
                                }
                                )
                            .Where(i =>
                                i.Length > 0
                                && i.Count <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount
                                )
                            .OrderBy(d => d.Count)
                            .Select(i => i.Length)
                            .ToArray();

                    //only first N items
                    if (lengthsList.Length > 10)
                        lengthsList = lengthsList.Take(10).ToArray();

                    var rowIndexesForHeaders =
                        Rows
                        //.AsParallel()
                        .Where(r => 
                            Rows.IndexOf(r) > endHeaderRowIndex
                            )
                        .Where(r =>
                            lengthsList.Contains(r.UniqueNotEmptyCells.Count())
                            || IsIntersected(tags, r.UniqueNotEmptyCells.Select(c => c.Value).ToArray())
                            )
                        .Where(r =>
                                (
                                    !r.IsNotHeader
                                    && r.UniqueWeight > 0.0
                                ) || r.UniqueWeight + (IsIntersected(tags, r.UniqueNotEmptyCells.Select(c => c.Value).ToArray()) ? 0.3 : 0) > 0.8
                            )
                        .Select(r => Rows.IndexOf(r))
                        .ToArray();

                    string msg = string.Empty;
                    foreach (uint i in lengthsList)
                        msg += i + ";";
                    Log.Add(logSession, string.Format("cells lenghtes: '{0}'", msg));

                    List<int> res = new List<int>(rowIndexesForHeaders);
                    SomeCalculations(res, endHeaderRowIndex, tags);
                    Log.Add(logSession, string.Format("cells count with this lengthes: '{0}'", res.Count));

                    //foreach (int i in res)
                    //    if (!result.Contains(i))
                    //        result.Add(i);

                    #endregion
                    #region By style
                    Log.Add(logSession, string.Format("calculating by styles..."));

                    var weightsList =
                            Rows
                                .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                                .AsParallel()
                                .Select(r => r.UniqueWeight)
                                .GroupBy(r => r)
                                .Select(g =>
                                    new
                                    {
                                        Wight = g.FirstOrDefault(),
                                        Count = g.Count()
                                    }
                                    )
                                .Where(i =>
                                    i.Wight > 0
                                    && i.Count <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount
                                    )
                                .OrderBy(d => d.Count)
                                .Select(i => i.Wight)
                                .ToArray();

                    if (weightsList.Length > 3)
                        weightsList = weightsList.Take(3).ToArray();

                    var rowIndexesForHeaders2 =
                        Rows
                        .Where(r => Rows.IndexOf(r) > endHeaderRowIndex)
                        .Where(r =>
                            (
                                !r.IsNotHeader
                                && weightsList.Contains(r.UniqueWeight)
                            )
                            || IsIntersected(tags, r.UniqueNotEmptyCells.Select(c => c.Value).ToArray())
                            )
                        .Select(r => Rows.IndexOf(r))
                        .ToArray();

                    string msg2 = string.Empty;
                    foreach (double i in weightsList)
                        msg2 += i.ToString("0.000") + ";";

                    Log.Add(logSession, string.Format("cells weightes: '{0}'", msg2));

                    List<int> res2 = new List<int>(rowIndexesForHeaders2);
                    SomeCalculations(res2, endHeaderRowIndex, tags);

                    Log.Add(logSession, string.Format("cells count with this lengthes: '{0}'", res2.Count));

                    #endregion
                    result.AddRange(res.Union(res2).Distinct().OrderBy(i => i));
                }
            }
            finally
            {
                Log.SessionEnd(logSession);
            }
            return getAllHeadersData = result;
        }

        private bool IsIntersected(string[] tags, string[] items)
        {
            return GetIntersectedCount(tags, items) > 0;
        }
        private int GetIntersectedCount(string[] tags, string[] items)
        {
            if (tags == null || items == null)
                return 0;

            var delStars = new Func<string, string>((str) => { if (str == null) return string.Empty; while (str.Contains("**")) str = str.Replace("**", "*"); return str; });

            tags = tags
                .Select(i => i != null ? delStars("*" + i.Trim().ToLower().Replace(' ', '*') + "*") : null)
                .Where(i => i != null && i != "*")
                .ToArray();
            items = items
                .Select(i => i != null ? delStars(i.Trim().ToLower()).Replace("*"," ").Trim() : null)
                .Where(i => i != null)
                .ToArray();

            int result = 0;

            foreach (var i0 in tags)
                foreach(var i1 in items)
                if (i1.Like(i0))
                    result++;

            return result;           
        }

        private void SomeCalculations(List<int> result, int endHeaderRowIndex, string[] tags)
        {
            var Coeff = new Func<ExelRow, double>((row) =>
            {
                return 1.0 - (GetIntersectedCount(tags, row.UniqueNotEmptyCells.Select(c => c.Value).ToArray()) / row.UniqueNotEmptyCells.Count()) * 0.3;
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
                for(int i=Rows.Count-1; i>=0; i++)
                {
                    if (result.Contains(i))
                        result.Remove(i);
                    else
                        break;
                }
            }
            #endregion

            #region calc similarity with other lines
            for (int i = result.Count - 1; i >= 0; i--)
            {
                int index = result[i];

                List<int> similarityIndexes = new List<int>();

                for (int n = 1; n < Rows.Count; n++)
                {
                    if (!result.Contains(index + n) && (index + n) < Rows.Count)
                        similarityIndexes.Add(index + n);
                    if (!result.Contains(index - n) && (index - n) > endHeaderRowIndex)
                        similarityIndexes.Add(index - n);
                    if (similarityIndexes.Count > 10)
                        break;
                }

                double coeff = Coeff(Rows[index]);

                foreach(int similarityIndex in similarityIndexes)
                {
                    double similarityD = Rows[index].Similarity(Rows[similarityIndex]);
                    double similarityD4 = Rows[index].Similarity(Rows[similarityIndex], 4);
                    double similarityHalf = Rows[index].Similarity(Rows[similarityIndex], (int)((float)Rows[similarityIndex].Cells.Count / (float)2));
                    if (
                        similarityD * coeff > 0.78 
                        || (similarityD * coeff > 0.65 && similarityD4 * coeff > 0.95)
                        || (Rows[similarityIndex].Cells.Count > 10 && similarityD * coeff > 0.3 && similarityD4 * coeff > 0.8 && similarityHalf > 0.7)
                        )
                    {
                        result.Remove(index);
                        break;
                    }
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
