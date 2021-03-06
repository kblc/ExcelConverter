﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.ExelDataReader
{
    public class ExelRow
    {
        public ExelRow()
        {
            Cells = new List<ExelCell>();
        }

        public List<ExelCell> Cells { get; set; }

        private bool? isNotHeader = null;
        public bool IsNotHeader
        {
            get
            {
                if (isNotHeader != null)
                    return (bool)isNotHeader;
                bool? result = NotEmptyCells.AsParallel().Any(cell => HasBadSymbols(cell.Value, ExelConverter.Core.Properties.Settings.Default.HeaderSearchAlgorithm_2_BadSymbols));
                result |= NotEmptyCells.All(cell => { double res; return double.TryParse(cell.Value, out res); });

                return (bool)(isNotHeader = result);
            }
        }

        private ExelCell[] notEmptyCells = null;
        public ExelCell[] NotEmptyCells
        {
            get
            {
                if (notEmptyCells != null)
                    return notEmptyCells;
                List<ExelCell> cells = Cells == null ? new List<ExelCell>() : new List<ExelCell>(Cells.Where(c => !string.IsNullOrWhiteSpace(c.Value)).ToArray());
                return notEmptyCells = cells.ToArray();
            }
        }

        private ExelCell[] uniqueNotEmptyCells = null;
        public ExelCell[] UniqueNotEmptyCells
        {
            get
            {
                if (uniqueNotEmptyCells != null)
                    return uniqueNotEmptyCells;
                List<ExelCell> cells = new List<ExelCell>(GetNotEqualElementsCells(NotEmptyCells));
                return uniqueNotEmptyCells = cells.ToArray();
            }
        }

        private ExelCell[] headerCells = null;
        public ExelCell[] HeaderCells
        {
            get
            {
                if (headerCells == null)
                {
                    var headerCells2 = Cells.ToList();
                    for (int i = headerCells2.Count() - 1; i >= 0; i--)
                    {
                        var cnt = 0;
                        for (int n = i - 1; n >= 0; n--)
                        {
                            if (string.Compare(headerCells2[n].Value, headerCells2[i].Value, true) == 0)
                                cnt++;
                        }
                        if (cnt > 0)
                            headerCells2[i].Value = string.Format("{0}[{1}]", headerCells2[i].Value, cnt);
                    }
                    this.headerCells = headerCells2.ToArray();
                }
                return headerCells;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return UniqueNotEmptyCells.Count() == 0;
            }
        }

        private double uniqueWeight = double.MinValue;
        public double UniqueWeight
        {
            get
            {
                if (uniqueWeight != double.MinValue)
                    return uniqueWeight;
                return uniqueWeight = Cells.Select(cell => cell.UniqueWeight).Sum() / Cells.Count();
            }
        }

        public void NeedRecalc()
        {
            uniqueWeight = double.MinValue;
            isNotHeader = null;
            notEmptyCells = null;
            uniqueNotEmptyCells = null;
        }

        private static ExelCell[] GetNotEqualElementsCells(ExelCell[] cells)
        {
            List<ExelCell> result = new List<ExelCell>();
            foreach (ExelCell cell in cells)
                if (!result.AsParallel().Any(resItem => resItem.Value.Trim().ToLower() == cell.Value.Trim().ToLower()))
                    result.Add(cell);
            return result.ToArray();
        }

        private static bool HasBadSymbols(string value, string badSymbols)
        {
            bool result = false;
            string val = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(badSymbols))
                Parallel.ForEach(badSymbols.ToCharArray(), chr => { if (val.Contains(chr)) result = true; });
            return result;
        }

        public double Similarity(ExelRow exelRow, int maxColumnsCount = 0)
        {
            double result = 0.0;
            bool wasError = false;
            var logSession = Helpers.Old.Log.SessionStart("ExelRow.Similarity()", true);
            try
            { 
                if (maxColumnsCount == 0)
                    maxColumnsCount = int.MaxValue;

                //if (exelRow.Cells.Count == this.Cells.Count)
                //{

                int maxValue = Math.Min(Math.Min(exelRow.Cells.Count, this.Cells.Count), maxColumnsCount);

                if (maxValue > 0)
                    for (int i = 0; i < maxValue; i++)
                    {
                        result += exelRow.Cells[i].IsMerged == this.Cells[i].IsMerged ? 0.09 : 0;
                        result += string.IsNullOrEmpty(exelRow.Cells[i].Value) == string.IsNullOrEmpty(this.Cells[i].Value) ? 0.44 : 0.0;
                        result += Math.Abs(exelRow.Cells[i].UniqueWeight - this.Cells[i].UniqueWeight) < 0.07 ? 0.15 : 0.0;
                        result += AsyncDocumentLoader.ColorsEqual(exelRow.Cells[i].CellStyle.ForegroundColor,this.Cells[i].CellStyle.ForegroundColor) ? 0.16 : 0.0;
                        result += AsyncDocumentLoader.ColorsEqual(exelRow.Cells[i].CellStyle.BackgroundColor, this.Cells[i].CellStyle.BackgroundColor) ? 0.16 : 0.0;
                    }
                result = result / maxValue;


                //}

                if (maxColumnsCount == int.MaxValue)
                {
                    result = result * 0.55;
                    result += ((double)Math.Min(this.NotEmptyCells.Count(), exelRow.NotEmptyCells.Count()) / (double)Math.Max(this.NotEmptyCells.Count(), exelRow.NotEmptyCells.Count())) * 0.4;

                    //result += (this.NotEmptyCells.Count() == exelRow.NotEmptyCells.Count()) ? 0.4 : 0;
                    result += (this.UniqueNotEmptyCells.Count() == exelRow.UniqueNotEmptyCells.Count()) ? 0.05 : 0;
                }
            }
            catch(Exception ex)
            {
                wasError = true;
                Helpers.Old.Log.Add(logSession, ex);
                throw ex;
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasError);
            }
            return result;
        }

        public int Index { get; set; }
    }
}
