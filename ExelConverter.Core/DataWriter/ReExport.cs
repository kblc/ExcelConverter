using Aspose.Cells;
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
                    bool showLogAnytime = false;

                    BackgroundWorker current = s as BackgroundWorker;
                    PercentageProgress fullProgress = new PercentageProgress();

                    var readRulesProgress = fullProgress.GetChild();
                    var getLinksProgress = fullProgress.GetChild();
                    var writeExcelFileProgress = fullProgress.GetChild();

                    fullProgress.Change += (s2, args) => { current.ReportProgress((int)args.Value); };

                    ObservableCollection<OutputRow> rowsToExport = new ObservableCollection<OutputRow>();

                    bool wasException = false;
                    Guid logSession = Log.SessionStart("ReExport.ReExport()", true);
                    try
                    {
                        #region Read Rules

                        Log.Add(logSession, string.Format("total sheets count: '{0}'", ruleToSheets.Count()));
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
                                    Log.Add(logSession, string.Format("should update main header row..."));

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
                                Log.Add(logSession, string.Format("row count on sheet '{0}' : '{1}'", ds.Name, oc.Count));
                                rowsToExport = new ObservableCollection<OutputRow>(rowsToExport.Union(oc));
                                Log.Add(logSession, string.Format("subtotal row count on sheets: '{0}'", rowsToExport.Count));
                            }

                            ind++;
                            readRulesProgress.Value = ((float)ind / (float)rulesToSheets) * 100;
                        }
                        Log.Add(logSession, string.Format("total row count to export: '{0}'", rowsToExport.Count));

                        #endregion
                        #region Get Links
                        Log.Add(logSession, string.Format("Try to get links..."));
                        List<ReExportData> idsToGet = new List<ReExportData>(
                            rowsToExport
                            .Select(i => new ReExportData(i.Code.Trim()))
                            .Cast<ReExportData>()
                            );

                        string outerMap;
                        string outerPdf;

                        HttpDataClient.Default.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);

                        //Log.Add(logSession, string.Format("Get CODES from server..."));
                        //foreach (var i in idsToGet)
                        //    Log.Add(logSession, string.Format("[Code:'{0}',Location:'{1}',Map:'{2}',Photo:'{3}']", i.Code, i.LinkLocation, i.LinkMap, i.LinkPhoto));
                        //Log.Add(logSession, string.Format("Get CODES done"));

                        //HttpDataAccess.GetResourcesList(currentOperatorId, idsToGet, out outerMap, out outerPdf);
                        Log.Add(logSession, string.Format("Links getted"));
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
                                    var headerRow = sheet.Cells.Rows[r.Sheet.MainHeader.Index];

                                    int fHeader = headerRow.FirstCell == null ? 0 : headerRow.FirstCell.Column;
                                    int lHeader = headerRow.LastCell == null ? 0 : headerRow.LastCell.Column;
                                    for (int i = fHeader; i <= lHeader; i++)
                                    {
                                        var cell = headerRow.GetCellOrNull(i);
                                        if (cell != null)
                                        {
                                            string cellValue = cell.StringValue;
                                            if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.ToLower().Trim() == CodeColumnName.ToLower().Trim())
                                            {
                                                CodeColumnIndex = i;
                                                break;
                                            }
                                        }
                                    }
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
                                                    var normalCode = rowsToExport.FirstOrDefault(itm => itm.OriginalCode.Trim() == codeInExcel.Trim());
                                                    if (normalCode != null)
                                                    {
                                                        var linksForCode = idsToGet.FirstOrDefault(itm => itm.Code == normalCode.Code.Trim());
                                                        if (linksForCode != null)
                                                        {
                                                            if (!string.IsNullOrWhiteSpace(linksForCode.LinkPhoto))
                                                            {
                                                                var c1 = sheet.Cells[i, lastIndex + 1];
                                                                c1.Value = "фото";
                                                                sheet.Hyperlinks.Add(c1.Name, 1, 1, linksForCode.LinkPhoto);
                                                            }
                                                            if (!string.IsNullOrWhiteSpace(linksForCode.LinkLocation))
                                                            {
                                                                var c2 = sheet.Cells[i, lastIndex + 2];
                                                                c2.Value = "схема";
                                                                sheet.Hyperlinks.Add(c2.Name, 1, 1, linksForCode.LinkLocation);
                                                            }
                                                            if (!string.IsNullOrWhiteSpace(linksForCode.LinkMap))
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
                                    else
                                        throw new Exception(string.Format("в Excel-файле невозможно найти колонку с кодом '{0}'", CodeColumnName));
                                    #endregion
                                }
                                else
                                    throw new Exception("Отсутствует правило для определения кода");
                            }
                        }
                        wb.Save(fileName);
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        wasException = true;
                        Log.Add(ex);
                        throw ex;
                    }
                    finally
                    {
                        current.ReportProgress(100);
                        Log.SessionEnd(logSession, wasException || showLogAnytime);
                    }
                };
            result.WorkerReportsProgress = true;
            result.WorkerSupportsCancellation = true;

            return result;

        }
    }
}
