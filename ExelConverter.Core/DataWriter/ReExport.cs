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
        public int OriginalIndex = 0;
        public string OriginalSheet = string.Empty;

        public ReExportData(string code, int originalIndex, string originalSheet) 
        {
            Code = code;
            OriginalIndex = originalIndex;
            OriginalSheet = originalSheet;
        }
    }

    public static class ReExport
    {
        public static BackgroundWorker Start(string fileName, long currentOperatorId, IEnumerable<SheetRulePair> ruleToSheets)
        {
            BackgroundWorker result = new BackgroundWorker();
            result.DoWork += (s, e) =>
                {
                    StringBuilder errors = new StringBuilder();

                    bool showLogAnytime = false;

                    BackgroundWorker current = s as BackgroundWorker;
                    PercentageProgress fullProgress = new PercentageProgress();

                    var readRulesProgress = fullProgress.GetChild();
                    var getLinksProgress = fullProgress.GetChild();
                    var writeExcelFileProgress = fullProgress.GetChild();

                    fullProgress.Change += (s2, args) => { current.ReportProgress((int)args.Value); };

                    ObservableCollection<OutputRow> rowsToExport = new ObservableCollection<OutputRow>();

                    bool wasException = false;
                    var logSession = Helpers.Old.Log.SessionStart("ReExport.ReExport()", true);
                    try
                    {
                        #region Read Rules

                        Helpers.Old.Log.Add(logSession, string.Format("total sheets count: '{0}'", ruleToSheets.Count()));
                        int rulesToSheets = ruleToSheets.Count();

                        int ind = 0;
                        foreach (var item in ruleToSheets)
                        {
                            var mappingRule = item.Rule;
                            var ds = item.Sheet;
                            if (mappingRule != null && ds != null)
                            {
                                if (ds.MainHeader == null)
                                {
                                    Helpers.Old.Log.Add(logSession, string.Format("should update main header row..."));

                                    ds.UpdateMainHeaderRow(mappingRule.MainHeaderSearchTags
                                        .Select(h => h.Tag)
                                        .Union(SettingsProvider.CurrentSettings.HeaderSearchTags.Split(new char[] { ',' }))
                                        .Select(i => i.Trim())
                                        .Where(i => !string.IsNullOrEmpty(i))
                                        .Distinct()
                                        .ToArray());

                                    ds.UpdateHeaders(mappingRule.SheetHeadersSearchTags
                                        .Select(h => h.Tag.Trim())
                                        .Where(i => !string.IsNullOrEmpty(i))
                                        .Distinct()
                                        .ToArray());
                                }

                                var oc = new ObservableCollection<OutputRow>(mappingRule.Convert(ds, new string[] { "Code" }));
                                Helpers.Old.Log.Add(logSession, string.Format("row count on sheet '{0}' : '{1}'", ds.Name, oc.Count));
                                rowsToExport = new ObservableCollection<OutputRow>(rowsToExport.Union(oc));
                                Helpers.Old.Log.Add(logSession, string.Format("subtotal row count on sheets: '{0}'", rowsToExport.Count));
                            }

                            ind++;
                            readRulesProgress.Value = ((decimal)ind / (decimal)rulesToSheets) * 100m;
                        }
                        Helpers.Old.Log.Add(logSession, string.Format("total row count to export: '{0}'", rowsToExport.Count));

                        #endregion
                        #region Get Links
                        Helpers.Old.Log.Add(logSession, string.Format("Try to get links..."));
                        
                        var idsToGet = 
                            rowsToExport
                            .Select(i => new ReExportData(i.Code.Trim(), i.OriginalIndex, i.OriginalSheet))
                            .Cast<ReExportData>()
                            .ToList();

                        string outerMap;
                        string outerPdf;

                        try
                        { 
                            HttpDataClient.Default.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);
                        } 
                        catch(Exception ex)
                        {
                            throw new Exception(string.Format("Ошибка при получении ссылок для кодов (кол-во: {0})", idsToGet.Count), ex);
                        }
                        //Log.Add(logSession, string.Format("Get CODES from server..."));
                        //foreach (var i in idsToGet)
                        //    Log.Add(logSession, string.Format("[Code:'{0}',Location:'{1}',Map:'{2}',Photo:'{3}']", i.Code, i.LinkLocation, i.LinkMap, i.LinkPhoto));
                        //Log.Add(logSession, string.Format("Get CODES done"));

                        //HttpDataAccess.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);
                        Helpers.Old.Log.Add(logSession, string.Format("Links getted"));
                        getLinksProgress.Value = 100;
                        #endregion
                        #region Write Excel File

                        Workbook wb = new Workbook(fileName);
                        foreach (var r in ruleToSheets.Where(r => r.Sheet != null))
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
                                        { 
                                            CodeColumnName = func.Function.ColumnName;
                                            if (string.IsNullOrWhiteSpace(func.Function.ColumnName) || func.Function.SelectedParameter == Core.Converter.Functions.FunctionParameters.CellNumber)
                                                CodeColumnIndex = func.Function.ColumnNumber;
                                        }
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(CodeColumnName) || (CodeColumnIndex != -1))
                                {
                                    if (CodeColumnIndex == -1)
                                        CodeColumnIndex = r.Sheet.MainHeader.Cells.FirstOrDefault(c => string.Compare(c.Value, CodeColumnName, true) == 0)?.OriginalIndex ?? -1;

                                    #region Add cells to rows
                                    if (CodeColumnIndex >= 0)
                                    {
                                        int lastIndex = sheet.Cells.Rows.Cast<Row>().Select(c =>
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

                                        var h0 = sheet.Cells[r.Sheet.MainHeader.Index, lastIndex + 0];
                                        var h1 = sheet.Cells[r.Sheet.MainHeader.Index, lastIndex + 1];
                                        var h2 = sheet.Cells[r.Sheet.MainHeader.Index, lastIndex + 2];
                                        var h3 = sheet.Cells[r.Sheet.MainHeader.Index, lastIndex + 3];

                                        sheet.Cells.Columns[h1.Column].IsHidden = false;
                                        sheet.Cells.Columns[h2.Column].IsHidden = false;
                                        sheet.Cells.Columns[h3.Column].IsHidden = false;

                                        h1.Value = "Фото";
                                        h2.Value = "Схема";
                                        h3.Value = "Карта";

                                        Style h0Style = h0.GetStyle();

                                        h1.SetStyle(h0Style);
                                        h2.SetStyle(h0Style);
                                        h3.SetStyle(h0Style);

                                        foreach (var item in idsToGet.Where(i => i.OriginalSheet == sheet.Name))
                                        {
                                            if (!string.IsNullOrWhiteSpace(item.LinkPhoto))
                                            {
                                                var c1 = sheet.Cells[item.OriginalIndex, lastIndex + 1];
                                                c1.Value = "фото";
                                                sheet.Hyperlinks.Add(c1.Name, 1, 1, item.LinkPhoto);
                                            }
                                            if (!string.IsNullOrWhiteSpace(item.LinkLocation))
                                            {
                                                var c2 = sheet.Cells[item.OriginalIndex, lastIndex + 2];
                                                c2.Value = "схема";
                                                sheet.Hyperlinks.Add(c2.Name, 1, 1, item.LinkLocation);
                                            }
                                            if (!string.IsNullOrWhiteSpace(item.LinkMap))
                                            {
                                                var c3 = sheet.Cells[item.OriginalIndex, lastIndex + 3];
                                                c3.Value = "карта";
                                                sheet.Hyperlinks.Add(c3.Name, 1, 1, item.LinkMap);
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
                                    else
                                    {
                                        errors.AppendFormat("в Excel-файле на вкладке '{0}' невозможно найти колонку с именем '{1}'", sheet.Name, CodeColumnName);
                                        errors.AppendLine();
                                        //throw new Exception(string.Format("в Excel-файле на вкладке '{0}' невозможно найти колонку с кодом '{1}'", sheet.Name, CodeColumnName));
                                    }
                                    #endregion
                                }
                                else
                                {
                                    errors.AppendFormat("Отсутствует правило для определения кода для вкладки '{0}'", sheet.Name);
                                    errors.AppendLine();
                                    //throw new Exception(string.Format("Отсутствует правило для определения кода для вкладки '{0}'", sheet.Name));
                                }
                            }
                        }
                        wb.Save(fileName);
                        e.Result = errors.ToString();

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        wasException = true;
                        Helpers.Old.Log.Add(logSession, ex);
                        throw ex;
                    }
                    finally
                    {
                        current.ReportProgress(100);
                        Helpers.Old.Log.SessionEnd(logSession, wasException || showLogAnytime);
                    }
                };
            result.WorkerReportsProgress = true;
            result.WorkerSupportsCancellation = true;

            return result;

        }
    }
}
