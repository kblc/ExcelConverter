﻿using Aspose.Cells;
using ExelConverter.Core.Converter;
using ExelConverter.Core.DataAccess;
using ExelConverter.Core.DataObjects;
using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.Settings;
using Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataWriter
{
    public class ReExportData
    {
        public readonly string Code = string.Empty;
        public string LinkPhoto = string.Empty;
        public string LinkMap = string.Empty;
        public string LinkLocation = string.Empty;

        public ReExportData(string code) 
        {
            Code = code;
        }
    }

    public static class ReExport
    {
        public static BackgroundWorker Start(string fileName, long currentOperatorId, IEnumerable<SheetRulePair> ruleToSheets)
        {
            BackgroundWorker result = new BackgroundWorker();
            result.DoWork += (s, e) =>
                {
                    BackgroundWorker current = s as BackgroundWorker;
                    PercentageProgress fullProgress = new PercentageProgress();

                    var readRulesProgress = fullProgress.GetChild();
                    var getLinksProgress = fullProgress.GetChild();
                    var writeExcelFileProgress = fullProgress.GetChild();

                    fullProgress.Change += (s2, args) => { current.ReportProgress((int)args.Value); };

                    ObservableCollection<OutputRow> rowsToExport = new ObservableCollection<OutputRow>();

                    Guid logSession = Log.SessionStart("ReExport.ReExport()");
                    try
                    {
                        #region Read Rules

                        Log.Add(string.Format("total sheets count: '{0}'", ruleToSheets.Count()));
                        int rulesToSheets = ruleToSheets.Count();

                        int ind = 0;
                        foreach (var item in ruleToSheets)
                        {
                            var mappingRule = item.Rule;
                            var ds = item.Sheet;
                            if (mappingRule != null)
                            {
                                if (ds.MainHeader == null)
                                {
                                    Log.Add(string.Format("should update main header row..."));
                                    if (mappingRule.FindMainHeaderByTags)
                                        ds.UpdateMainHeaderRow(mappingRule.MainHeaderSearchTags.Select(h => h.Tag).ToArray());
                                    else
                                        ds.UpdateMainHeaderRow(SettingsProvider.CurrentSettings.HeaderSearchTags.Split(new char[] { ',' }));
                                }

                                var oc = new ObservableCollection<OutputRow>(mappingRule.Convert(ds, new string[] { "Code" }));
                                Log.Add(string.Format("row count on sheet '{0}' : '{1}'", ds.Name, oc.Count));
                                rowsToExport = new ObservableCollection<OutputRow>(rowsToExport.Union(oc));
                                Log.Add(string.Format("subtotal row count on sheets: '{0}'", rowsToExport.Count));
                            }

                            ind++;
                            readRulesProgress.Value = ((float)ind / (float)rulesToSheets) * 100;
                        }
                        Log.Add(string.Format("total row count to export: '{0}'", rowsToExport.Count));

                        #endregion
                        #region Get Links
                        Log.Add(string.Format("Try to get links..."));
                        List<ReExportData> idsToGet = new List<ReExportData>(rowsToExport.Select(i => new ReExportData(i.Code)).Cast<ReExportData>());

                        string outerMap;
                        string outerPdf;

                        HttpDataClient.Default.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);
                        //HttpDataAccess.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);
                        Log.Add(string.Format("Links getted"));
                        getLinksProgress.Value = 100;
                        #endregion
                        #region Write Excel File

                        Workbook wb = new Workbook(fileName);
                        foreach (var r in ruleToSheets)
                        {
                            var sheet = wb.Worksheets.Cast<Worksheet>().FirstOrDefault(sht => sht.Name.ToLower().Trim() == r.Sheet.Name.ToLower().Trim());
                            if (sheet != null)
                            {
                                int CodeColumnIndex = -1;
                                string CodeColumnName = string.Empty;

                                var convData = r.Rule.ConvertionData.FirstOrDefault(cd => cd.PropertyId == "Code" );
                                if (convData != null)
                                {
                                    var block = convData.Blocks.Blocks.FirstOrDefault();
                                    if (block != null)
                                    {
                                        var func = block.UsedFunctions.FirstOrDefault();
                                        if (func != null)
                                            CodeColumnName = func.Function.ColumnName;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(CodeColumnName))
                                {
                                    var headerRow = sheet.Cells.Rows[r.Sheet.Rows.IndexOf(r.Sheet.MainHeader)];
                                    for (int i = headerRow.FirstCell == null ? 0 : headerRow.FirstCell.Column; i < (headerRow.LastCell == null ? 0 : headerRow.LastCell.Column + 1); i++)
                                    {
                                        string cellValue = headerRow.GetCellByIndex(i).StringValue;
                                        if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.ToLower().Trim() == CodeColumnName.ToLower().Trim())
                                        {
                                            CodeColumnIndex = i;
                                            break;
                                        }
                                    }
                                    #region Add cells to rows
                                    if (CodeColumnIndex >= 0)
                                    {
                                        int lastIndex = headerRow.LastCell.Column;

                                        var h0 = sheet.Cells[headerRow.Index, lastIndex + 0];
                                        var h1 = sheet.Cells[headerRow.Index, lastIndex + 1];
                                        var h2 = sheet.Cells[headerRow.Index, lastIndex + 2];
                                        var h3 = sheet.Cells[headerRow.Index, lastIndex + 3];

                                        h1.Value = "Фото";
                                        h2.Value = "Схема";
                                        h3.Value = "Карта";

                                        h1.SetStyle(h0.GetStyle());
                                        h2.SetStyle(h0.GetStyle());
                                        h3.SetStyle(h0.GetStyle());

                                        for (int i = headerRow.Index + 1; i < sheet.Cells.Rows.Count; i++)
                                        {
                                            var codeCell = sheet.Cells.Rows[i].GetCellOrNull(CodeColumnIndex);
                                            if (codeCell != null)
                                            {
                                                var codeInExcel = codeCell.StringValue;
                                                if (!string.IsNullOrWhiteSpace(codeInExcel))
                                                {
                                                    var normalCode = rowsToExport.FirstOrDefault(itm => itm.OriginalCode == codeInExcel);
                                                    if (normalCode != null)
                                                    {
                                                        var linksForCode = idsToGet.FirstOrDefault(itm => itm.Code == normalCode.Code);
                                                        if (linksForCode != null)
                                                        {
                                                            if (linksForCode.LinkPhoto != null)
                                                            {
                                                                var c1 = sheet.Cells[i, lastIndex + 1];
                                                                c1.Value = "фото";
                                                                sheet.Hyperlinks.Add(c1.Name, 1, 1, linksForCode.LinkPhoto);
                                                            }
                                                            if (linksForCode.LinkLocation != null)
                                                            {
                                                                var c2 = sheet.Cells[i, lastIndex + 2];
                                                                c2.Value = "схема";
                                                                sheet.Hyperlinks.Add(c2.Name, 1, 1, linksForCode.LinkLocation);
                                                            }
                                                            if (linksForCode.LinkMap != null)
                                                            {
                                                                var c3 = sheet.Cells[i, lastIndex + 3];
                                                                c3.Value = "карта";
                                                                sheet.Hyperlinks.Add(c3.Name, 1, 1, linksForCode.LinkMap);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (sheet.Cells.LastCell != null)
                                        {
                                            int lastRowIndex = sheet.Cells.LastCell.Row;

                                            int rowAdd = 3;
                                            
                                            if (outerPdf != null)
                                            {
                                                var outer0 = sheet.Cells[lastRowIndex + rowAdd, CodeColumnIndex + 0];
                                                var outer1 = sheet.Cells[lastRowIndex + rowAdd, CodeColumnIndex + 1];

                                                outer0.Value = "Скачать оффлайн PDF-презентацию";
                                                outer1.Value = outerPdf;
                                                sheet.Hyperlinks.Add(outer0.Name, 1, 1, outerPdf);

                                                rowAdd++;
                                            }

                                            if (outerMap != null)
                                            {
                                                var outer0 = sheet.Cells[lastRowIndex + rowAdd, CodeColumnIndex + 0];
                                                var outer1 = sheet.Cells[lastRowIndex + rowAdd, CodeColumnIndex + 1];

                                                outer0.Value = "Карта";
                                                outer1.Value = outerMap;
                                                sheet.Hyperlinks.Add(outer0.Name, 1, 1, outerMap);

                                                rowAdd++;
                                            }

                                        }

                                    }
                                    #endregion
                                }
                            }
                        }
                        wb.Save(fileName);
                        #endregion
                        e.Result = null;
                    }
                    catch (Exception ex)
                    {
                        Log.Add(logSession, string.Format("exception occured:{0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                        e.Result = ex;
                    }
                    finally
                    {
                        current.ReportProgress(100);
                        Log.SessionEnd(logSession);
                    }
                };
            result.WorkerReportsProgress = true;
            result.WorkerSupportsCancellation = true;

            return result;

        }
    }
}
