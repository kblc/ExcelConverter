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
            if (tags == null)
            {
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

                if (headersLevel1.Count > 0 || 1 == 1) //always
                {
                    SheetHeaders.Headers = headersLevel1;
                    SheetHeaders.Subheaders = headersLevel2;
                }
                else
                {
                    SheetHeaders.Headers = headersLevel2;
                    SheetHeaders.Subheaders = new ObservableCollection<SheetHeader>();
                }
            }
        }

        private List<int> getAllHeadersData = null;
        private List<int> GetAllHeadersData()
        {
            if (getAllHeadersData != null)
                return getAllHeadersData;

            var result = new List<int>();
            var initialRow = Rows.IndexOf(MainHeader) + MainHeaderRowCount;

            Guid logSession = Log.SessionStart("GetAllHeadersData()");
            Log.Add(logSession, string.Format("HeaderSearchAlgorithm = {0}", ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm));
            Log.Add(logSession, string.Format("rows count for import: '{0}' from '{1}' main row", Rows.Count, initialRow));
            try
            {
                if (Rows.Count > initialRow + 1)
                {
                    if (ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm == 2)
                    {
                        //second algorithm version (ver=2)
                        //key = lenght, value = count
                        object lockChangeObject = new Object();
                        Dictionary<uint, uint> lengthsDictionary = new Dictionary<uint, uint>();
                        Dictionary<double, uint> weightsDictionary = new Dictionary<double, uint>();

                        List<ExelRow> notEmptyRows = new List<ExelRow>(Rows.Where(row =>
                                {
                                    int index = Rows.IndexOf(row);
                                    return index >= initialRow
//                                        && (last == 0 || index <= last)
                                        && row.UniqueNotEmptyCells.Count() > 0;
                                }
                            ));

                        Log.Add(logSession, string.Format("not empty rows for analize found: '{0}' from '{1}';", notEmptyRows.Count, initialRow));//, last));

                        Parallel.ForEach(notEmptyRows, row =>
                            {
                                uint equalsElementCount = (uint)row.UniqueNotEmptyCells.Count();
                                lock (lockChangeObject)
                                {
                                    if (lengthsDictionary.ContainsKey(equalsElementCount))
                                        lengthsDictionary[equalsElementCount]++;
                                    else
                                        lengthsDictionary.Add(equalsElementCount, 1);
                                }
                            }
                        );  

                        #region By values

                        Log.Add(logSession, string.Format("calculating by values..."));

                        var orderedDictionary = lengthsDictionary.OrderBy(kvp => kvp.Key);
                        //var simpleStringPosElement = orderedDictionary.Where(kvp => kvp.Value > ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount).OrderBy(kvp => kvp.Key).FirstOrDefault();
                        uint simpleStringPos = 0;// Math.Min(simpleStringPosElement.Key, ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxCellsFillCount + 1);

                        List<KeyValuePair<uint, uint>> list = new List<KeyValuePair<uint, uint>>(lengthsDictionary.Where(kvp => kvp.Key > 0 && (simpleStringPos == 0 || kvp.Key < simpleStringPos) && kvp.Value <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount).OrderBy(kvp => kvp.Key));                        
                        
                        var maxN = Math.Min(5, list.Count());
                        //List<uint> lengthsList = new List<uint>(list.Where(i => list.IndexOf(i) < maxN).Select(i => i.Key));
                        List<uint> lengthsList = new List<uint>(list.Take(maxN).Select(i => i.Key));

                        string msg = string.Empty;
                        foreach (uint i in lengthsList)
                            msg += i + ";";

                        Log.Add(logSession, string.Format("cells lenghtes: '{0}'", msg));

                        if (maxN > 0)
                        {
                            List<int> res = new List<int>(notEmptyRows.Where(row => (lengthsList.Any(i => row.UniqueNotEmptyCells.Count() == i) && !row.IsNotHeader && row.UniqueWeight > 0.0) || row.UniqueWeight >= 0.80).Select(row => Rows.IndexOf(row)));
                            SomeCalculations(res);

                            Log.Add(logSession, string.Format("cells count with this lengthes: '{0}'", res.Count));

                            foreach (int i in res)
                                if (!result.Contains(i))
                                    result.Add(i);
                        }

                        #endregion
                        if (result.Count < 4)
                        {
                            #region By style
                            Log.Add(logSession, string.Format("calculating by styles..."));

                            Parallel.ForEach(notEmptyRows, row =>
                                {
                                    double weight = (double)row.UniqueWeight;
                                    lock (lockChangeObject)
                                    {
                                        if (weightsDictionary.ContainsKey(weight))
                                            weightsDictionary[weight]++;
                                        else
                                            weightsDictionary.Add(weight, 1);
                                    }
                                }
                            );

                            List<KeyValuePair<double, uint>> listWeight = new List<KeyValuePair<double, uint>>(weightsDictionary.Where(kvp => kvp.Value <= ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_MaxHeaderCount).OrderByDescending(kvp => kvp.Key));
                            var maxM = Math.Min(3, listWeight.Count());
                            List<double> weightsList = new List<double>(listWeight.Where(i => listWeight.IndexOf(i) < maxM).Select(i => i.Key));

                            string msg2 = string.Empty;
                            foreach (double i in weightsList)
                                msg2 += i.ToString("0.000") + ";";

                            Log.Add(logSession, string.Format("cells weightes: '{0}'", msg2));

                            if (maxM > 0)
                            {
                                List<int> res = new List<int>(notEmptyRows.Where(row => (weightsList.Any(i => row.UniqueWeight == i) && !row.IsNotHeader)).Select(row => Rows.IndexOf(row)));
                                SomeCalculations(res);

                                Log.Add(logSession, string.Format("cells count with this lengthes: '{0}'", res.Count));

                                foreach (int i in res)
                                    if (!result.Contains(i))
                                        result.Add(i);
                            }
                            #endregion
                        }

                        
                        //if (result.Count == 0)
                        //{
                        //    //get first not empty row
                        //    for(int i =0; i < Rows.Count; i++)
                        //        if (!Rows[i].IsEmpty)
                        //        {
                        //            result.Add(i);
                        //            break;
                        //        }
                        //}
                    }
                    else
                    {
                        #region first alghoritm version
                        for (var i = initialRow; i < Rows.Count; i++)
                        {
                            var cellRow = Rows.ElementAt(i).Cells.Where(c => !string.IsNullOrWhiteSpace(c.Value) && !HasBadSymbols(c.Value, ".,")).Select(c => c.Value).ToArray();
                            string[] equalsElements;
                            if (cellRow.Length >= 1 && cellRow.Length <= 3 || ((equalsElements = GetEqualElements(cellRow)).Length <= 3 && equalsElements.Length >= 1))
                                result.Add(i);
                        }
                        #endregion
                    }
                }
            }
            finally
            {
                Log.SessionEnd(logSession);
            }



            #region Sort result
            var lines = from n
                     in result
                        orderby n
                        select n;
            #endregion
            return getAllHeadersData = new List<int>(lines.ToArray());
        }

        private void SomeCalculations(List<int> result)
        {
            #region remove headers if step by step > 2
            for (int i = result.Count - 1; i >= 0; i--)
            {
                int n = 1;
                int currentValue = result[i];

                double similarityD = 0;
                double similatityD4 = 0;

                while (
                        result.Contains(currentValue - n) &&
                        (similarityD = Rows[currentValue].Similarity(Rows[currentValue - n])) > 0.45 &&
                        (similatityD4 = Rows[currentValue].Similarity(Rows[currentValue - n], 4)) > 0.8)
                {
                    n++;
                }
                if (n > 2)
                {
                    int deleted = 0;
                    for (int z = 0; z < n; z++)
                    {
                        result.Remove(currentValue - z);
                        deleted++;
                    }
                    i -= deleted - 1;
                }
            }
            #endregion
            #region calc similarity with other liness
            for (int i = result.Count - 1; i >= 0; i--)
            {
                int index = result[i];

                List<int> similarityIndexes = new List<int>();

                for (int n = 1; n < Rows.Count; n++)
                {
                    if (!result.Contains(index + n) && (index + n) < Rows.Count)
                        similarityIndexes.Add(index + n);
                    if (!result.Contains(index - n) && (index - n) > Rows.IndexOf(MainHeader) + MainHeaderRowCount - 1)
                        similarityIndexes.Add(index - n);

                    if (similarityIndexes.Count > 10)
                        break;
                }

                foreach(int similarityIndex in similarityIndexes)
                {
                    double similarityD = Rows[index].Similarity(Rows[similarityIndex]);
                    double similarityD4 = Rows[index].Similarity(Rows[similarityIndex], 4);
                    if (similarityD > 0.78 || (similarityD > 0.65 && similarityD4 > 0.95))
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

                for (int i = result.Count - 1; i >= 0; i--)
                    if (Rows[i].UniqueWeight < averageWeight - moduleWeight)
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

                var rows = Rows.Take(count == int.MaxValue ? Rows.Count : Math.Min(Rows.Count, count)).ToArray();
                for (var i = 0; i < rows.Length; i++)
                {
                    result.Rows.Add(new ExelCell[] { new ExelCell() { Value = (i+1).ToString() }, new ExelCell() { Value = (rows[i].Index+1).ToString() } }.Union(rows[i].Cells.ToArray()).ToArray());
                }
            }
            return result;
        }

        #endregion


    }
}
