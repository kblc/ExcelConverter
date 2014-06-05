using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Data;
using System.Timers;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.Converter;
using ExelConverter.Core.DataAccess;
using ExelConverter.Core.DataObjects;
using ExelConverter.Core.Converter.CommonTypes;
using ExelConverterLite.Model;
using System.ComponentModel;
using ExelConverter.Core.Settings;
using ExelConverter.Core.Converter.Functions;
using ExelConverter.Core.DataWriter;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ExelConverter.Core.ImagesParser;
using ExelConverterLite.Utilities;
using System.Windows.Forms;
using Helpers;
using System.Threading;


namespace ExelConverterLite.ViewModel
{
    public class AviableRulesCollection : ObservableCollection<ExelConvertionRule>
    {
        public AviableRulesCollection()
            : base(App.Locator.Import.SelectedOperator.MappingRules)
        {
            Add(App.Locator.Import.NullRule);
        }
    }

    public class AviableSheetsCollection : ObservableCollection<string>
    {
        public AviableSheetsCollection()
            : base(App.Locator.Import.ExportRules.Select(r => r.SheetName ))
        {
        }
    }

    public class ImportViewModel : ViewModelBase
    {

        //private System.Timers.Timer _rulesUpdater;
        private System.Timers.Timer _locksUpdater;
        private System.Timers.Timer _repeatLocksUpdater;

        public ExelDocument document;
        public List<Operator> OperatorsList { get; set; }
        private IDataAccess _appSettingsDataAccess;

        private User currentUser = null;
        public User CurrentUser
        {
            get
            {
                return currentUser ?? (currentUser = _appSettingsDataAccess.GetUser(HttpDataClient.Default.UserLogin));
            }
        }

        public ImportViewModel(IDataAccess appSettingsDataAccess)
        {
            _appSettingsDataAccess = appSettingsDataAccess;
            OperatorsList = new List<Operator>();
            InitializeCoommands();
            InitializeData();
            StartSharpControl();
            //SheetHeaders.CollectionChanged += (s, e) =>
            //    {
            //        var abc = "a";
            //    };
        }

        ~ImportViewModel()
        {
            if (_locksUpdater != null)
                _locksUpdater.Dispose();
            _locksUpdater = null;
            if (_repeatLocksUpdater != null)
                _repeatLocksUpdater.Dispose();
            _repeatLocksUpdater = null;
        }

        private void UpdateLockedOperators()
        {
            var lockers = _appSettingsDataAccess.GetOperatorLockers();
            //update operators
            foreach (Operator op in Operators)
                op.LockedBy = lockers.Where(l => l.Key.Id == op.Id).Select(l => l.Value).FirstOrDefault();
        }

        public void InitializeData()
        {
            _locksUpdater = new System.Timers.Timer(30 * 1000);
            _locksUpdater.Elapsed += (s, e) =>
            {
                _locksUpdater.Stop();
                try
                {
                    UpdateLockedOperators();
                }
                finally
                {
                    _locksUpdater.Start();
                }
            };
            _locksUpdater.Start();

            _repeatLocksUpdater = new System.Timers.Timer(60 * 1000);
            _repeatLocksUpdater.Elapsed += (s, e) =>
            {
                _repeatLocksUpdater.Stop();
                try
                {
                    if (_selectedOperator != null)
                        _appSettingsDataAccess.SetOperatorLocker(_selectedOperator, CurrentUser, true);
                }
                finally
                {
                    _repeatLocksUpdater.Start();
                }
            };

            IsViewModelValid = true;
            UpdateData(true);

            //_rulesUpdater = new System.Timers.Timer(5000);
            //_rulesUpdater.Elapsed += (s, e) =>
            //{
            //    var rules = _appSettingsDataAccess.GetRulesByOperator(SelectedOperator);
            //    foreach (var rule in rules)
            //    {
                    
            //    }
            //};
        }

        private void InitializeCoommands()
        {
            SelectFileCommand = new RelayCommand(SelectFile);
            LoadDocumentCommand = new RelayCommand(LoadDocument, () => { return IsDocumentLoaded && IsViewModelValid; });
            UpdateOperatorCommand = new RelayCommand(UpdateOperator, () => { return IsViewModelValid; });
            UpdateMappingsTableCommand = new RelayCommand<string>(UpdateMappingsTable, (s) => { return IsDocumentLoaded; });
            ClearMappingsTableCommand = new RelayCommand<string>(ClearMappingsTable);
            ToCsvCommand = new RelayCommand(ToCsv, () => { return IsDocumentLoaded && IsViewModelValid; });
            ExportSetupCommand = new RelayCommand(ExportSetup, () => { return IsViewModelValid && SelectedOperator != null; });
            ReExportFileCommand = new RelayCommand(ReExportFile, () => IsDocumentLoaded && IsViewModelValid);
            AddMappingCommand = new RelayCommand<string>(AddMapping);
            EditOperatorCommand = new RelayCommand(EditOperator);
            AddBlockCommand = new RelayCommand(AddBlock, () => SelectedField != null );
            AddStartRuleCommand = new RelayCommand<Guid>(AddStartRule);
            ClearStartRulesCommand = new RelayCommand<Guid>(ClearStartRules);
            ClearFunctionsCommand = new RelayCommand<Guid>(ClearFunction);
            AddFunctionCommand = new RelayCommand<Guid>(AddFunction);
            DeleteBlockCommand = new RelayCommand<Guid>(DeleteBlock);
            DeleteFunctionCommand = new RelayCommand<Guid>(DeleteFunction);
            DeleteStartRuleCommand = new RelayCommand<Guid>(DeleteStartRule);
            SaveOperatorCommand = new RelayCommand(SaveOperator);
            UpdateOperatorCommand = new RelayCommand(UpdateOperator);
            UpdateFoundedHeadersCommand = new RelayCommand(UpdateFoundedHeaders, () => SelectedSheet != null);
            AddRuleCommand =
                new RelayCommand<object>(
                    (b) => 
                        {
                            if (b != null) 
                                AddRule((bool)b); 
                        },
                    (b2) =>
                        {
                            return SelectedSheet != null && SelectedOperator != null && SelectedOperator.MappingRules != null;
                        });
            SaveRuleCommand = new RelayCommand(SaveRule, () => SelectedOperator != null && SelectedOperator.MappingRule != null);
            LoadRuleCommand = new RelayCommand(LoadRule);

            SaveAllRulesCommand = new RelayCommand(SaveAllRules, () => SelectedOperator != null && SelectedOperator.MappingRule != null);
            LoadAllRulesCommand = new RelayCommand(LoadAllRules);

            RefreshOperatorListCommand = new RelayCommand(RefreshOperatorList);
            RefreshMappingTablesCommnad = new RelayCommand(RefreshMappingTables);
            ShowWebPageCommand = new RelayCommand<string>(ShowWebPage);
            ShowImageParsingWindowCommand = new RelayCommand(ShowImageParsingWindow);
            ShowLoadImageWindowCommand = new RelayCommand<string>(ShowLoadImageWindow);
        }

        public RelayCommand RefreshOperatorListCommand { get; private set; }
        private void RefreshOperatorList()
        {
            UpdateData(false);
        }

        public void UpdateData(bool changeSelectedOperator = true)
        {
            OperatorsList = _appSettingsDataAccess.GetOperators().ToList();
            Operators = new ObservableCollection<Operator>(OperatorsList);
            if (changeSelectedOperator || SelectedOperator == null)
                SelectedOperator = Operators.Where(o => o.IsLocked == false).FirstOrDefault();
        }

        private void OperatorChanged()
        {
            ExportRules = null;
            if (SelectedOperator != null)
            {
                SelectedOperator.MappingRules = new ObservableCollection<ExelConvertionRule>(_appSettingsDataAccess.GetRulesByOperator(SelectedOperator));
                SelectedOperator.MappingRule = SelectedOperator.MappingRules.FirstOrDefault();
                if (SelectedOperator.MappingRules.Count == 0)
                {
                    var mappingRule = new ExelConvertionRule
                    {
                        FkOperatorId = (int)SelectedOperator.Id,
                        Name = ExelConvertionRule.DefaultName
                    };
                    _appSettingsDataAccess.AddOperatorRule(mappingRule);
                    OperatorChanged();
                    return;
                }
                else
                {
                    SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();
                    SelectedOperator.MappingRule.InitializeImageParsingData();
                }
                AddBlockCommand.RaiseCanExecuteChanged();

                if (DocumentRows.Count == 0)
                {
                    SheetHeaders.Clear();
                    foreach (var rule in SelectedOperator.MappingRules)
                        foreach(var data in rule.ConvertionData)
                            foreach(var block in data.Blocks.Blocks)
                                {
                                    foreach (var func in block.UsedFunctions)
                                        if (func.Function != null && !string.IsNullOrWhiteSpace(func.Function.ColumnName) && !SheetHeaders.Contains(func.Function.ColumnName))
                                            SheetHeaders.Add(func.Function.ColumnName);

                                    foreach (var srule in block.StartRules)
                                        if (srule.Rule != null && !string.IsNullOrWhiteSpace(srule.Rule.ColumnName) && !SheetHeaders.Contains(srule.Rule.ColumnName))
                                            SheetHeaders.Add(srule.Rule.ColumnName);
                                }
                }
            } else
                SheetHeaders.Clear();

            //RaisePropertyChanged("SelectedOperator");
        }

        private void UpdateSelectedField()
        {
            foreach (var block in SelectedField.Blocks.Blocks)
            {
                foreach (var rule in block.StartRules)
                {
                    var name = rule.Rule.ColumnName;
                    rule.Rule.ColumnName = null;
                    rule.Rule.ColumnName = name;
                }
                foreach (var func in block.UsedFunctions)
                {
                    var name = func.Function.ColumnName;
                    func.Function.ColumnName = null;
                    func.Function.ColumnName = name;
                }
            }
        }

        public void SheetChanging()
        {
            SelectedField = null;
        }

        public void UpdateMainHeaderRow()
        {
            SheetHeaders.Clear();

            SelectedSheet.UpdateMainHeaderRow(
                SelectedOperator.MappingRule.MainHeaderSearchTags
                    .Select(h => h.Tag)
                    .Union(SettingsProvider.CurrentSettings.HeaderSearchTags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(i => i != null ? i.Trim() : null)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Distinct()
                    .ToArray()
                    );

            foreach (var header in SelectedSheet.MainHeader.Cells)
            {
                SheetHeaders.Add(((ExelCell)header).Value);
            }

            UpdateSharp();
        }

        public void UpdateSheetHeaderRow()
        {
            SelectedSheet.UpdateHeaders(
                    SelectedOperator.MappingRule.SheetHeadersSearchTags
                    .Select(h => (h != null && h.Tag != null) ? h.Tag.Trim() : null)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Distinct()
                    .ToArray()
                );
            UpdateSharp();
        }

        private void UpdateSheetHeaders()
        {
            if (SelectedSheet != null)
            {
                Sharp = SelectedSheet.AsDataTable(SettingsProvider.CurrentSettings.PreloadedRowsCount);
                if (SelectedSheet.Rows.Count > 0)
                {
                    if (SelectedSheet != null)
                    {
                        var savedExportRule = ExportRules
                            .Where(r => r.Rule != null && r.Rule != NullRule)
                            .FirstOrDefault(r => SelectedSheet.Name.ToLower().Contains(r.SheetName.ToLower()));
                        
                        if (savedExportRule != null)
                            SelectedOperator.MappingRule = savedExportRule.Rule; 
                        else
                            SelectedOperator.MappingRule = 
                                SelectedOperator.MappingRules.FirstOrDefault(r => SelectedSheet.Name.ToLower().Contains(r.Name.ToLower()))
                                ?? SelectedOperator.MappingRule
                                ?? SelectedOperator.MappingRules.FirstOrDefault();
                    }
                    UpdateMainHeaderRow();
                    UpdateSheetHeaderRow();
                }
            }
            if (SelectedOperator != null && SelectedOperator.MappingRules.Count > 0)
            {
                SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();
            }
        }

        public void SaveRules(bool needRefresh)
        {
            if (SelectedOperator != null)
            {
                var storedRules = _appSettingsDataAccess.GetRulesByOperator(SelectedOperator);

                #region Remove old rules

                var itemsToDelete = storedRules.Where(r => !App.Locator.Import.SelectedOperator.MappingRules.Any(mr => mr.Id == r.Id)).ToArray();
                _appSettingsDataAccess.RemoveOpertaorRule(itemsToDelete);

                #endregion
                #region Save new rules

                _appSettingsDataAccess.AddOperatorRules(SelectedOperator.MappingRules.Where(mr => mr.Id == 0).ToArray());

                #endregion
                #region Update rules

                List<ExelConvertionRule> updateRules = new List<ExelConvertionRule>();

                foreach (var currentRule in SelectedOperator.MappingRules.Where(mr => mr.Id > 0))
                {
                    var oldRule = storedRules.FirstOrDefault(i => i.Id == currentRule.Id)
                                    ?? storedRules.FirstOrDefault(i => i.Name == currentRule.Name);
                    if (oldRule != null && oldRule.Serialize().Trim() != currentRule.Serialize().Trim())
                    {
                        updateRules.Add(currentRule);
                    }
                }

                if (updateRules.Count > 0)
                    _appSettingsDataAccess.UpdateOperatorRules(updateRules.ToArray());

                #endregion

                SaveImageParsingData(storedRules);

                //if (exportRules != null)
                //    SaveExportRules(exportRules.ToArray());

                if (needRefresh)
                {
                    SelectedOperator.MappingRules = new ObservableCollection<ExelConvertionRule>(_appSettingsDataAccess.GetRulesByOperator(App.Locator.Import.SelectedOperator));
                    SelectedOperator.MappingRule = App.Locator.Import.SelectedOperator.MappingRules.FirstOrDefault();
                    SelectedOperator.MappingRule.RaisePropertyChanged("ConvertionData");
                    SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();
                }
                else
                {
                    _appSettingsDataAccess.SetOperatorLocker(SelectedOperator, CurrentUser, false);
                    SelectedOperator.LockedBy = null;
                }
            }
        }

        private void SaveImageParsingData(ExelConvertionRule[] storedRules)
        {
            List<FillArea> areaList = new List<FillArea>();
            List<FillArea> areaListStored = new List<FillArea>();

            if (SelectedOperator.MappingRule != null && SelectedOperator.MappingRule.MapParsingData != null)
            {
                foreach (var mpData in SelectedOperator.MappingRule.MapParsingData)
                {
                    areaList.AddRange(mpData.DrawingArea.Children.Cast<Rectangle>().Select(
                        rect => new FillArea
                        {
                            FKOperatorID = (int)SelectedOperator.Id,
                            Height = (short)mpData.Height,
                            Type = "location",
                            Width = (short)mpData.Width,
                            X1 = (int)Canvas.GetLeft(rect),
                            Y1 = (int)Canvas.GetTop(rect),
                            X2 = (int)(Canvas.GetLeft(rect) + rect.Width),
                            Y2 = (int)(Canvas.GetTop(rect) + rect.Height)
                        }
                        ).ToArray<FillArea>()
                    );
                }
                //check stored
                var storedMappingRule = storedRules.FirstOrDefault(r => r.Name == SelectedOperator.MappingRule.Name);
                if (storedMappingRule != null && storedMappingRule.MapParsingData != null)
                    foreach (var mpData in storedMappingRule.MapParsingData)
                    {
                        areaListStored.AddRange(mpData.DrawingArea.Children.Cast<Rectangle>().Select(
                            rect => new FillArea
                            {
                                FKOperatorID = (int)SelectedOperator.Id,
                                Height = (short)mpData.Height,
                                Type = "location",
                                Width = (short)mpData.Width,
                                X1 = (int)Canvas.GetLeft(rect),
                                Y1 = (int)Canvas.GetTop(rect),
                                X2 = (int)(Canvas.GetLeft(rect) + rect.Width),
                                Y2 = (int)(Canvas.GetTop(rect) + rect.Height)
                            }
                            ).ToArray<FillArea>()
                        );
                    }
            }

            if (SelectedOperator.MappingRule != null && SelectedOperator.MappingRule.PhotoParsingData != null)
            foreach (var ppData in SelectedOperator.MappingRule.PhotoParsingData)
            {
                areaList.AddRange(ppData.DrawingArea.Children.Cast<Rectangle>().Select(
                    rect => new FillArea
                    {
                        FKOperatorID = (int)SelectedOperator.Id,
                        Height = (short)ppData.Height,
                        Type = "photo",
                        Width = (short)ppData.Width,
                        X1 = (int)Canvas.GetLeft(rect),
                        Y1 = (int)Canvas.GetTop(rect),
                        X2 = (int)(Canvas.GetLeft(rect) + rect.Width),
                        Y2 = (int)(Canvas.GetTop(rect) + rect.Height)
                    }
                    ).ToArray<FillArea>()
                );

                var storedMappingRule = storedRules.FirstOrDefault(r => r.Name == SelectedOperator.MappingRule.Name);
                if (storedMappingRule != null && storedMappingRule.PhotoParsingData != null)
                    foreach (var mpData in storedMappingRule.PhotoParsingData)
                    {
                        areaListStored.AddRange(mpData.DrawingArea.Children.Cast<Rectangle>().Select(
                            rect => new FillArea
                            {
                                FKOperatorID = (int)SelectedOperator.Id,
                                Height = (short)ppData.Height,
                                Type = "photo",
                                Width = (short)ppData.Width,
                                X1 = (int)Canvas.GetLeft(rect),
                                Y1 = (int)Canvas.GetTop(rect),
                                X2 = (int)(Canvas.GetLeft(rect) + rect.Width),
                                Y2 = (int)(Canvas.GetTop(rect) + rect.Height)
                            }
                            ).ToArray<FillArea>()
                        );
                    }
            }
            
            foreach(var item in areaList.ToArray())
            {
                if (areaListStored.Any(i =>
                        i.FKOperatorID == item.FKOperatorID
                        && i.Height == item.Height
                        && i.ID == item.ID
                        && i.Type == item.Type
                        && i.Width == item.Width
                        && i.X1 == item.X1
                        && i.X2 == item.X2
                        && i.Y1 == item.Y1
                        && i.Y2 == item.Y2
                    ))
                    areaList.Remove(item);
            }

            if (areaList.Count() > 0)
            {
                var res = _appSettingsDataAccess.FillRectExists(areaList.ToArray());
                for (int i = 0; i < areaList.Count(); i++)
                    if (!res[i])
                        _appSettingsDataAccess.AddFillRectangle(areaList[i]);
            }
        }

        private void SaveExportRules(SheetRulePair[] exportRules)
        {
            _appSettingsDataAccess.SetExportedRulesForOperator(SelectedOperator, exportRules);
        }

        private string performSearchString = string.Empty;
        private System.Timers.Timer searchTimer = null;
        private void PerformSearch(string name)
        {
            if (searchTimer == null)
            {
                searchTimer = new System.Timers.Timer(300);
                searchTimer.Elapsed += (s, e) =>
                {
                    searchTimer.Stop();
                    Operators = new ObservableCollection<Operator>(OperatorsList.Where(o => o.Name.ToLower().Contains(performSearchString.ToLower())).ToList());
                };
            }
            searchTimer.Stop();
            performSearchString = name;
            searchTimer.Start();
            //SelectedOperator = Operators.FirstOrDefault();
        }

        public RelayCommand SaveRuleCommand { get; private set; }
        private void SaveRule()
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Rule files (*.rule)|*.rule|XML rule files (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "rule";
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    if (System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower() == ".xml")
                    {
                        System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(ExelConvertionRule));
                        using (TextWriter writer = new StreamWriter(saveFileDialog.FileName))
                        {
                            s.Serialize(writer, SelectedOperator.MappingRule);
                            writer.Close();
                        }
                    }
                    else
                    {
                        string text = SelectedOperator.MappingRule.Serialize();
                        System.IO.File.WriteAllText(saveFileDialog.FileName, text);
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(string.Format("SaveRule() :: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                    MessageBox.Show(string.Format("Произошла ошибка при сохранении правила:{0}{1}", Environment.NewLine, ex.Message), "Ошибка при загрузке правила", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        public RelayCommand LoadRuleCommand { get; private set; }
        private void LoadRule()
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Rule files (*.rule)|*.rule|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    string text = System.IO.File.ReadAllText(openFileDialog.FileName);
                    var ruleDeserialized = ExelConvertionRule.DeserializeFromB64String(text);
                    var rule = ruleDeserialized;// (new ExelConvertionRule()).CopyFrom(ruleDeserialized);
                    rule.FkOperatorId = (int)SelectedOperator.Id;
                    rule.Id = 0;
                    rule.Name = SelectedOperator.MappingRule.Name;
                    var index = SelectedOperator.MappingRules.IndexOf(SelectedOperator.MappingRule);
                    SelectedOperator.MappingRules.RemoveAt(index);
                    SelectedOperator.MappingRules.Insert(index, rule);
                    SelectedOperator.MappingRule = rule;
                }
                catch (Exception ex)
                {
                    Log.Add(string.Format("LoadRule() :: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                    MessageBox.Show(string.Format("Произошла ошибка при загрузке правила:{0}{1}",Environment.NewLine, ex.Message),"Ошибка при загрузке правила",MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        public RelayCommand SaveAllRulesCommand { get; private set; }
        private void SaveAllRules()
        {
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Rules files (*.rules)|*.rules|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "rules";
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    string text = string.Empty;
                    foreach (var rule in SelectedOperator.MappingRules)
                        text += (string.IsNullOrWhiteSpace(text) ? string.Empty : Environment.NewLine) + rule.Serialize();  
                    System.IO.File.WriteAllText(saveFileDialog.FileName, text);
                }
                catch (Exception ex)
                {
                    Log.Add(string.Format("SaveRules() :: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                    MessageBox.Show(string.Format("Произошла ошибка при сохранении правил:{0}{1}", Environment.NewLine, ex.Message), "Ошибка при загрузке правил", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        public RelayCommand LoadAllRulesCommand { get; private set; }
        private void LoadAllRules()
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Rules files (*.rules)|*.rules|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                    foreach (var text in lines)
                    {
                        var ruleDeserialized = ExelConvertionRule.DeserializeFromB64String(text);
                        var rule = ruleDeserialized;// new ExelConvertionRule().CopyFrom(ruleDeserialized);
                        rule.FkOperatorId = (int)SelectedOperator.Id;
                        rule.Id = 0;
                        var replacedRule = SelectedOperator.MappingRules.Where(i => i.Name == rule.Name).FirstOrDefault();
                        var index = replacedRule == null ? -1 : SelectedOperator.MappingRules.IndexOf(replacedRule);
                        if (index >= 0)
                        {
                            SelectedOperator.MappingRules.RemoveAt(index);
                            SelectedOperator.MappingRules.Insert(index, rule);
                        }
                        else
                            SelectedOperator.MappingRules.Add(rule);
                    }
                    SelectedOperator.MappingRule = SelectedOperator.MappingRules.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Log.Add(string.Format("LoadAllRules() :: {1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                    MessageBox.Show(string.Format("Произошла ошибка при загрузке правил:{0}{1}", Environment.NewLine, ex.Message), "Ошибка при загрузке правил", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        public RelayCommand SelectFileCommand { get; private set; }
        private void SelectFile()
        {
            try
            {
                var filePath = ExelConverterFileDialog.Show();
                if (filePath != null)
                {
                    Path = filePath;
                    IsDocumentLoaded = false;
                    if (document != null && document.Loader.FileLoader.IsBusy)
                    {
                        document.Loader.FileLoader.CancelAsync();
                    }
                    document = new ExelDocument(Path, ExelConverterLite.Properties.Settings.Default.Import_DeleteEmptyRows);
                    document.Loader.FileLoader.ProgressChanged += (s, e) =>
                    {
                        LoadingProgress = e.ProgressPercentage;
                    };

                    document.Loader.FileLoader.RunWorkerCompleted += (s, e) =>
                    {
                        LoadingProgress = 10000;
                        IsDocumentLoaded = true;
                        
                        SelectedSheet = document.Sheets.Where(sht => sht.Name == SelectedSheet.Name).Single();
                        DocumentSheets = new ObservableCollection<ExelSheet>(document.Sheets);
                        Sharp = SelectedSheet.AsDataTable();
                        //UpdateMainHeaderRow();
                        UpdateSheetHeaderRow();
                    };

                    DocumentSheets = new ObservableCollection<ExelSheet>(document.Sheets);
                    SelectedSheet = DocumentSheets.FirstOrDefault();
                }
            }
            catch(Exception e)
            {
                Path = string.Empty;
                System.Windows.MessageBox.Show(App.Current.MainWindow, string.Format("Exception: {0}{2}{1}",e.Message,e.StackTrace, Environment.NewLine));
            }
        }

        public RelayCommand LoadDocumentCommand { get; private set; }
        private void LoadDocument()
        {
            try
            {
                DocumentRows = new ObservableCollection<OutputRow>(SelectedOperator.MappingRule.Convert(SelectedSheet).Take(SettingsProvider.CurrentSettings.PreloadedRowsCount).ToArray());
            }
            catch
            {
                DocumentRows = new ObservableCollection<OutputRow>();
                System.Windows.MessageBox.Show("Произошла ошибка, проверьте еще раз\nправило и попробуйте снова...","Ошибка", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        public RelayCommand EditOperatorCommand { get; private set; }
        private void EditOperator()
        {
            SaveRules(false);
            View.ViewLocator.OperatorSettingsView.ShowDialog(App.Current.MainWindow);
            ExportRules = null;
        }

        public RelayCommand<string> UpdateMappingsTableCommand { get; private set; }
        private void UpdateMappingsTable(string parameter)
        {
            if (SelectedSheet != null)
                try                
                {
                    var data = SelectedOperator.MappingRule.ConvertionData.Where(f => f.FieldName == parameter).Single();
                    var initialRow = 0;
                    var sheet = SelectedSheet;
                    initialRow = sheet.Rows.IndexOf(sheet.MainHeader) + sheet.MainHeaderRowCount;
                    for (var i = initialRow; i < sheet.Rows.Count; i++)
                    {
                        var cellResultContent = String.Empty;

                        cellResultContent = data.Blocks.Run(sheet, i, data);
                        if (cellResultContent != null)
                        {
                            cellResultContent = cellResultContent.Trim();
                        }
                        if (cellResultContent != null &&
                            !data.MappingsTable.Any(m => data.MappingsTable.AbsoluteCoincidence ? cellResultContent == m.From : cellResultContent.Contains(m.From)) &&
                            !data.MappingsTable.Any(m => cellResultContent == m.To))
                        {
                            var toString = (parameter == "Район") ? "Не определен" : string.Empty;
                            data.MappingsTable.Add(new Mapping { From = cellResultContent, To = toString });
                        }
                    }
               }
                catch
                {
                    ClearMappingsTable(parameter);
                    System.Windows.MessageBox.Show("Произошла ошибка, проверьте еще раз правило,\nкорректность загрузки сетки и попробуйте снова...", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
        }

        public RelayCommand RefreshMappingTablesCommnad { get; private set; }
        private void RefreshMappingTables()
        {
            var tables = SelectedOperator.MappingRule.ConvertionData.Where(cd => cd.MappingNeeded).ToArray();
            foreach (var table in tables)
            {
                UpdateMappingsTable(table.FieldName);
            }
        }

        public RelayCommand<string> ClearMappingsTableCommand { get; private set; }
        private void ClearMappingsTable(string parameter)
        {
            var data = SelectedOperator.MappingRule.ConvertionData.Where(f => f.FieldName == parameter).Single();
            data.MappingsTable.Clear();
        }

        private ObservableCollection<SheetRulePair> exportRules = null;
        public ObservableCollection<SheetRulePair> ExportRules
        {
            get
            {
                if (exportRules != null)
                    return exportRules;

                var savedRules = _appSettingsDataAccess.GetExportRulesIdByOperator(SelectedOperator, DocumentSheets == null ? null : DocumentSheets.AsQueryable());
                foreach (var r in savedRules.Where(r2 => r2.Rule == null))
                    r.Rule = App.Locator.Import.NullRule;
                
                exportRules = new ObservableCollection<SheetRulePair>(savedRules);

                if (DocumentSheets != null)
                    foreach (var sheet in DocumentSheets)
                    {
                        if (!exportRules.Select(s => s.SheetName.ToLower().Trim()).Contains(sheet.Name.ToLower().Trim()))
                            exportRules.Add(
                                new SheetRulePair(DocumentSheets.AsQueryable()) 
                                {
                                    Sheet = sheet,
                                    Rule = SelectedOperator.MappingRules.FirstOrDefault(r => r.Name.ToLower() == sheet.Name.ToLower()) 
                                           ?? SelectedOperator.MappingRules.FirstOrDefault(r => r.Name.ToLower() == ExelConvertionRule.DefaultName.ToLower())
                                }
                       );
                    }

                return exportRules;
            }
            set
            {
                exportRules = value;
                RaisePropertyChanged("ExportRules");
            }
        }

        public RelayCommand ExportSetupCommand { get; private set; }
        private void ExportSetup()
        {
            var res = View.ViewLocator.ExportSetupView.ShowDialog(App.Current.MainWindow);
            if (res != null && res.Value)
            {
                foreach (var rule in ExportRules.Where(r => r.AllowedSheets == null))
                    rule.AllowedSheets = DocumentSheets.AsQueryable();
                SaveExportRules(ExportRules.ToArray());
            } else
                ExportRules = null;
        }

        public RelayCommand ToCsvCommand { get; private set; }
        private void ToCsv()
        {
            try
            {
                View.ViewLocator.ExportView.ShowDialog(App.Current.MainWindow);
            }
            catch
            {
                System.Windows.MessageBox.Show(@"Произошла ошибка, проверьте еще раз правило, 
корректность загрузки сетки, правильность пути к 
папке с файлами и попробуйте снова...", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        public RelayCommand ReExportFileCommand { get; private set; }
        private void ReExportFile()
        {
            SaveFileDialog dlg = null;

            if (!File.Exists(Path))
                MessageBox.Show("Исходный файл перемещен или не существует", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                if ((dlg = new SaveFileDialog() { FileName = System.IO.Path.GetFileName(Path), Filter = "Файлы экспорта (*" + System.IO.Path.GetExtension(Path) + ")|*" + System.IO.Path.GetExtension(Path) }).ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(Path, dlg.FileName, true);
                        BackgroundWorker bw = ReExport.Start(dlg.FileName, SelectedOperator.Id, ExportRules.ToArray());
                        bw.RunWorkerCompleted += (s, e) => 
                        {
                            if (!e.Cancelled && (e.Result is Exception || e.Error != null))
                            {
                                Exception ex = e.Error != null ? e.Error : e.Result as Exception;
                                Log.Add(string.Format("ReExportFile() :: exception detected:{0}'{1}'{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                                MessageBox.Show("При попытке реэкспорта файла произошла ошибка:" + Environment.NewLine + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            //hide wait dialog
                        };
                        bw.ProgressChanged += (s, e) =>
                        {

                        };
                        //show wait dialog
                        bw.RunWorkerAsync();
                    }
                    catch(Exception ex)
                    {
                        Log.Add(string.Format("ReExportFile() :: exception detected:{0}'{1}'{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace));
                        MessageBox.Show("При попытке реэкспорта файла произошла ошибка:" + Environment.NewLine + ex.Message,"Ошибка",MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                } 
        }

        public RelayCommand<string> AddMappingCommand { get; private set; }
        private void AddMapping(string parameter)
        {
            var data = SelectedOperator.MappingRule.ConvertionData.Where(f => f.FieldName == parameter).Single();
            data.MappingsTable.Add(new Mapping());
        }

        public RelayCommand AddBlockCommand { get; private set; }
        private void AddBlock()
        {
            SelectedField.Blocks.Blocks.Add(new ExelConverter.Core.Converter.CommonTypes.FunctionsBlock());
        }

        public RelayCommand<Guid> AddStartRuleCommand { get; private set; }
        private void AddStartRule(Guid blockId)
        {
            var block = SelectedField.Blocks.Blocks.Where(b => b.Id == blockId).Single();
            block.StartRules.Add(new FunctionBlockStartRule());
        }

        public RelayCommand<Guid> DeleteStartRuleCommand { get; private set; }
        private void DeleteStartRule(Guid id)
        {
            foreach (var block in SelectedField.Blocks.Blocks)
            {
                if (block.StartRules.Any(r => r.Id == id))
                {
                    block.StartRules.Remove(block.StartRules.Where(r => r.Id == id).Single());
                }
            }
        }

        public RelayCommand<Guid> ClearStartRulesCommand { get; private set; }
        private void ClearStartRules(Guid blockId)
        {
            var block = SelectedField.Blocks.Blocks.Where(b => b.Id == blockId).Single();
            block.StartRules.Clear();
        }

        public RelayCommand<Guid> AddFunctionCommand { get; private set; }
        private void AddFunction(Guid blockId)
        {
            var block = SelectedField.Blocks.Blocks.Where(b => b.Id == blockId).Single();
            block.UsedFunctions.Add(new FunctionContainer());
        }

        public RelayCommand<Guid> DeleteFunctionCommand { get; private set; }
        private void DeleteFunction(Guid id)
        {
            foreach (var block in SelectedField.Blocks.Blocks)
            {
                if(block.UsedFunctions.Any(f=>f.Id == id))
                {
                    block.UsedFunctions.Remove(block.UsedFunctions.Where(f => f.Id == id).Single());
                }
            }
        }

        public RelayCommand<Guid> ClearFunctionsCommand { get; private set; }
        private void ClearFunction(Guid blockId)
        {
            var block = SelectedField.Blocks.Blocks.Where(b => b.Id == blockId).Single();
            block.UsedFunctions.Clear();
        }

        public RelayCommand<Guid> DeleteBlockCommand { get; private set; }
        public void DeleteBlock(Guid id)
        {
            var block = SelectedField.Blocks.Blocks.Where(b => b.Id == id).Single();
            SelectedField.Blocks.Blocks.Remove(block);
        }

        public RelayCommand SaveOperatorCommand { get; private set; }
        private void SaveOperator()
        {
            SaveRules(true);
        }

        public RelayCommand UpdateOperatorCommand { get; private set; }
        private void UpdateOperator()
        {
            OperatorChanged();
        }

        public RelayCommand UpdateFoundedHeadersCommand { get; private set; }
        private void UpdateFoundedHeaders()
        {
            UpdateMainHeaderRow();
            UpdateSheetHeaderRow();
        }

        public RelayCommand<object> AddRuleCommand { get; private set; }
        private void AddRule(bool basedOnCurrentRule)
        {
            var rule = basedOnCurrentRule ? ExelConvertionRule.DeserializeFromB64String(SelectedOperator.MappingRule.Serialize()) : new ExelConvertionRule();
            rule.FkOperatorId = (int)SelectedOperator.Id;
            rule.Name = SelectedSheet.Name;
            rule.Id = 0;
            SelectedOperator.MappingRules.Add(rule);
            SelectedOperator.MappingRule = rule;
        }

        public RelayCommand<string> ShowWebPageCommand { get; private set; }
        private void ShowWebPage(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        public RelayCommand<string> ShowLoadImageWindowCommand { get; private set; }
        public void ShowLoadImageWindow(string mode)
        {
            View.ViewLocator.LoadImageView.Mode = mode;
            View.ViewLocator.LoadImageView.ShowDialog(View.ViewLocator.ImageParsingView);
        }

        public RelayCommand ShowImageParsingWindowCommand { get; private set; }
        private void ShowImageParsingWindow()
        {
            if (SelectedSheet != null)
            {
                LoadPhotos();
                LoadMaps();
            }
            

            View.ViewLocator.ImageParsingView.ShowDialog(App.Current.MainWindow);
        }

        private void LoadPhotos()
        {
            if (SelectedSheet != null)
            {
                var photoConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Photo_img");
                var mapConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Location_img");

                var i = SelectedSheet.Rows.IndexOf(SelectedSheet.MainHeader) + SelectedSheet.MainHeaderRowCount;

                var url = photoConvertionData.Blocks.Run(SelectedSheet, i, photoConvertionData);

                if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                { 
                    var imageParser = new ImagesParser(url);
                    var bitmap = imageParser.GetImage();
                    bitmap.DownloadCompleted += (s, e) =>
                    {
                        var a = bitmap.Height;
                        if (!SelectedOperator.MappingRule.PhotoParsingData.Any(ppd => ppd.Height == bitmap.PixelHeight
                        && ppd.Width == bitmap.PixelWidth))
                        {
                            var ipd = new ImageParsingData
                            {
                                Height = bitmap.PixelHeight,
                                Width = bitmap.PixelWidth,
                                ImageUrl = url
                            };
                            ipd.DrawingArea.Background = new ImageBrush(bitmap);
                            SelectedOperator.MappingRule.PhotoParsingData.Add(ipd);
                        }
                        else
                        {
                            var ipd = SelectedOperator.MappingRule.PhotoParsingData.FirstOrDefault(ppd => ppd.Height == bitmap.PixelHeight && ppd.Width == bitmap.PixelWidth);
                            ipd.DrawingArea.Background = new ImageBrush(bitmap);
                        }
                    
                    };
                }
            }
        }

        private void LoadMaps()
        {
            if (SelectedSheet != null)
            {
                var mapConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Location_img");
                var i = SelectedSheet.Rows.IndexOf(SelectedSheet.MainHeader) + SelectedSheet.MainHeaderRowCount;
                var url = mapConvertionData.Blocks.Run(SelectedSheet, i, mapConvertionData);

                if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                { 
                    var imageParser = new ImagesParser(url);
                    var bitmap = imageParser.GetImage();
                    bitmap.DownloadCompleted += (s, e) =>
                    {
                        var a = bitmap.Height;
                        if (!SelectedOperator.MappingRule.MapParsingData.Any(ppd => ppd.Height == bitmap.PixelHeight
                        && ppd.Width == bitmap.PixelWidth))
                        {
                            var ipd = new ImageParsingData
                            {
                                Height = bitmap.PixelHeight,
                                Width = bitmap.PixelWidth,
                                ImageUrl = url
                            };
                            ipd.DrawingArea.Background = new ImageBrush(bitmap);
                            SelectedOperator.MappingRule.MapParsingData.Add(ipd);
                        }
                        else
                        {
                            var ipd = SelectedOperator.MappingRule.MapParsingData.FirstOrDefault(ppd => ppd.Height == bitmap.PixelHeight && ppd.Width == bitmap.PixelWidth);
                            ipd.DrawingArea.Background = new ImageBrush(bitmap);
                        }
                    };

                }
            }
        }

        #region Properties

        private bool _isDocumentLoaded;
        public bool IsDocumentLoaded
        {
            get { return _isDocumentLoaded; }
            set
            {
                if (_isDocumentLoaded != value)
                {
                    _isDocumentLoaded = value;
                    RaisePropertyChanged("IsDocumentLoaded");
                    UpdateMappingsTableCommand.RaiseCanExecuteChanged();
                    ClearMappingsTableCommand.RaiseCanExecuteChanged();
                    LoadDocumentCommand.RaiseCanExecuteChanged();
                    ToCsvCommand.RaiseCanExecuteChanged();
                    ReExportFileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isViewModelModelValid;
        public bool IsViewModelValid
        {
            get { return _isViewModelModelValid; }
            set
            {
                if (_isViewModelModelValid != value)
                {
                    _isViewModelModelValid = value;
                    RaisePropertyChanged("IsViewModelValid");
                    UpdateOperatorCommand.RaiseCanExecuteChanged();
                    LoadDocumentCommand.RaiseCanExecuteChanged();
                    ToCsvCommand.RaiseCanExecuteChanged();
                    ReExportFileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    _path = value;
                    RaisePropertyChanged("Path");
                }
            }
        }

        private int _loadingProgress;
        public int LoadingProgress
        {
            get { return _loadingProgress; }
            set
            {
                if (_loadingProgress != value)
                {
                    
                    _loadingProgress = value;
                    RaisePropertyChanged("LoadingProgress");
                }
            }
        }

        private ObservableCollection<OutputRow> _documentRows;
        public ObservableCollection<OutputRow> DocumentRows
        {
            get { return _documentRows ?? (_documentRows = new ObservableCollection<OutputRow>()); }
            set
            {
                if (_documentRows != value)
                {
                    _documentRows = value;
                    RaisePropertyChanged("DocumentRows");
                }
            }
        }

        private ObservableCollection<Operator> _operators;
        public ObservableCollection<Operator> Operators
        {
            get { return _operators; }
            set
            {
                if (_operators != value)
                {
                    _operators = value;
                    UpdateLockedOperators();
                    RaisePropertyChanged("Operators");
                }
            }
        }

        private Operator _selectedOperator;
        public Operator SelectedOperator
        {
            get { return _selectedOperator; }
            set
            {
                _repeatLocksUpdater.Stop();
                bool wasException = false;
                var logSession = Helpers.Log.SessionStart("ImportViewModel.SelectedOperator_set()", true);
                try
                {
                    if (_selectedOperator != value && value != null)
                    {
                        if (_selectedOperator != null)
                        {
                            SaveRules(false);
                        }

                        if (value == null || _appSettingsDataAccess.SetOperatorLocker(value, CurrentUser, true))
                        {
                            _selectedOperator = value;
                            _selectedOperator.LockedBy = currentUser;
                        }
                        else
                        {
                            _selectedOperator = null;
                            UpdateLockedOperators();
                            User usr = _appSettingsDataAccess.GetOperatorLocker(value);
                            MessageBox.Show(string.Format("Изменение выбранного оператора '{0}' невозможно, так как в данный момент данный оператор редактируется в другом сеансе пользователем '{1}'", value.Name, usr != null ? usr.ToString() : "unknown"), "Изменение оператора", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        OperatorChanged();
                    }
                }
                catch(Exception ex)
                {
                    wasException = true;
                    Log.Add(logSession, ex.GetExceptionText());
                }
                finally
                {
                    Log.SessionEnd(logSession, wasException);
                    RaisePropertyChanged("SelectedOperator");
                    _repeatLocksUpdater.Start();
                }
            }
        }

        private ObservableCollection<ExelSheet> _documentSheets;
        public ObservableCollection<ExelSheet> DocumentSheets
        {
            get { return _documentSheets; }
            set
            {
                ExportRules = null;
                if (_documentSheets != value)
                {
                    _documentSheets = value;
                    RaisePropertyChanged("DocumentSheets");
                }
            }
        }

        private ExelSheet _selectedSheet;
        public ExelSheet SelectedSheet
        {
            get { return _selectedSheet; }
            set
            {
                if (_selectedSheet != value)
                {
                    SheetChanging();
                    _selectedSheet = value;
                    RaisePropertyChanged("SelectedSheet");
                    UpdateSheetHeaders();
                    UpdateFoundedHeadersCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private FieldConvertionData _selectedField;
        public FieldConvertionData SelectedField
        {
            get { return _selectedField; }
            set
            {
                if (_selectedField != value)
                {
                    _selectedField = value;
                    RaisePropertyChanged("SelectedField");
                    RaisePropertyChanged("SelectedSheetIndex");
                }
            }
        }

        private ObservableCollection<string> _sheetHeaders;
        public ObservableCollection<string> SheetHeaders
        {
            get { return _sheetHeaders ?? (_sheetHeaders = new ObservableCollection<string>()); }
            set
            {
                if (_sheetHeaders != value)
                {
                    _sheetHeaders = value;
                    RaisePropertyChanged("SheetHeaders");
                }
            }
        }

        private DataTable _sharp;
        public DataTable Sharp
        {
            get { return _sharp; }
            set
            {
                if (_sharp != value)
                {
                    _sharp = value;
                    RaisePropertyChanged("Sharp");
                }
            }
        }

        public void StartSharpControl()
        {
            this.PropertyChanging += (s, e) =>
            {
                if (e.PropertyName == "SelectedOperator")
                {
                    if (SelectedOperator != null)
                    { 
                        SelectedOperator.PropertyChanged -= SelectedOperator_PropertyChanged;
                        if (SelectedOperator.MappingRule != null)
                            SelectedOperator.MappingRule.PropertyChanged -= MappingRule_PropertyChanged;
                    }
                }
            };

            this.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "SelectedOperator")
                    {
                        if (SelectedOperator != null)
                        { 
                            SelectedOperator.PropertyChanged += SelectedOperator_PropertyChanged;    
                            if (SelectedOperator.MappingRule != null)
                                SelectedOperator.MappingRule.PropertyChanged += MappingRule_PropertyChanged;
                        }
                    }
                };

            if (SelectedOperator != null)
            {
                SelectedOperator.PropertyChanged += SelectedOperator_PropertyChanged;
                if (SelectedOperator.MappingRule != null)
                    SelectedOperator.MappingRule.PropertyChanged += MappingRule_PropertyChanged;
            }
        }

        private ExelConvertionRule oldMappingRule = null;
        private void SelectedOperator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MappingRule")
            {
                if (oldMappingRule != null)
                    oldMappingRule.PropertyChanged -= MappingRule_PropertyChanged;
                oldMappingRule = null;
                if (SelectedOperator != null && SelectedOperator.MappingRule != null)
                {
                    oldMappingRule = SelectedOperator.MappingRule;
                    SelectedOperator.MappingRule.PropertyChanged += MappingRule_PropertyChanged;
                }
            }
        }

        private void MappingRule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Like("MainHeaderSearchTags*") && Sharp != null)
            {
                UpdateMainHeaderRow();
            }
            if (e.PropertyName.Like("SheetHeadersSearchTags*") && Sharp != null)
            {
                UpdateSheetHeaderRow();
            }
        }

        private void UpdateSharp()
        {
            int[] headers = SelectedSheet.SheetHeaders.Headers.Select(s => s.RowNumber).ToArray();
            int[] subHeaders = SelectedSheet.SheetHeaders.Subheaders.Select(s => s.RowNumber).ToArray(); 
            int[] main = new int[SelectedSheet.MainHeaderRowCount];   
            for(int i = 0; i<SelectedSheet.MainHeaderRowCount; i++)
                main[i] = SelectedSheet.Rows.IndexOf(SelectedSheet.MainHeader) + i;

            for (int i = 0; i < Sharp.Rows.Count; i++)
            {
                string rowType = string.Empty;
                if (main.Contains(i))
                    rowType = "M";
                else if (headers.Contains(i))
                    rowType = "H";
                else if (subHeaders.Contains(i))
                    rowType = "S";

                if (Sharp.Rows[i]["type"].ToString() != rowType)
                    Sharp.Rows[i]["type"] = rowType;
            }
        }

        private string _searchName;
        public string SearchName
        {
            get { return _searchName; }
            set
            {
                if (_searchName != value)
                {
                    _searchName = value;
                    RaisePropertyChanged("SearchName");
                    PerformSearch(_searchName);
                }
            }
        }

        #endregion

        private ExelConvertionRule nullRule = null;
        public ExelConvertionRule NullRule 
        {
            get
            {
                 return nullRule ?? (nullRule = new ExelConvertionRule() { Name = "" });
            }
        }
    }
}
