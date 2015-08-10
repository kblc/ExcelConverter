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
using Helpers.Serialization;
using System.Threading;
using System.Xml.Serialization;


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

        private ExelDocument document = null;
        public ExelDocument Document
        {
            get
            {
                if (document == null)
                {
                    document = new ExelDocument();
                    document.PropertyChanged += (s,e) =>
                    {
                        if (e.PropertyName == nameof(Document.IsDocumentLoaded))
                        {
                            UpdateMappingsTableCommand.RaiseCanExecuteChanged();
                            ClearMappingsTableCommand.RaiseCanExecuteChanged();
                            //LoadDocumentCommand.RaiseCanExecuteChanged();
                            ToCsvCommand.RaiseCanExecuteChanged();
                            ReExportFileCommand.RaiseCanExecuteChanged();
                        }
                        if (e.PropertyName == nameof(Document.SelectedSheet))
                        {
                            SheetChanging();
                            UpdateSheetHeaders();
                            UpdateFoundedHeadersCommand.RaiseCanExecuteChanged();
                            IsSharpLoading = true;
                        }
                    };
                }
                return document;
            }
        }

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
            if (_appSettingsDataAccess != null)
            { 
            var lockers = _appSettingsDataAccess.GetOperatorLockers();
            //update operators
            foreach (Operator op in Operators)
                op.LockedBy = lockers.Where(l => l.Key.Id == op.Id).Select(l => l.Value).FirstOrDefault();
            if (SelectedOperator != null && SelectedOperator.LockedBy == null)
                SelectedOperator.LockedBy = CurrentUser;
            }
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


            
            _repeatLocksUpdater = new System.Timers.Timer((int)((decimal)_appSettingsDataAccess.GetOperatorLockerTimeout().TotalMilliseconds / 5m));
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
            //LoadDocumentCommand = new RelayCommand(LoadDocument, () => { return IsDocumentLoaded && IsViewModelValid; });
            UpdateOperatorCommand = new RelayCommand(UpdateOperator, () => { return IsViewModelValid; });
            UpdateMappingsTableCommand = new RelayCommand<string>(UpdateMappingsTable, (s) => { return Document.IsDocumentLoaded; });
            ClearMappingsTableCommand = new RelayCommand<string>(ClearMappingsTable);
            ToCsvCommand = new RelayCommand(ToCsv, () => { return Document.IsDocumentLoaded && IsViewModelValid; });
            ExportSetupCommand = new RelayCommand(ExportSetup, () => { return IsViewModelValid && SelectedOperator != null; });
            ReExportFileCommand = new RelayCommand(ReExportFile, () => Document.IsDocumentLoaded && IsViewModelValid);
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
            UpdateFoundedHeadersCommand = new RelayCommand(UpdateFoundedHeaders, () => Document.SelectedSheet != null);
            AddRuleCommand =
                new RelayCommand<object>(
                    (b) => 
                        {
                            if (b != null) 
                                AddRule((bool)b); 
                        },
                    (b2) =>
                        {
                            return Document.SelectedSheet != null && SelectedOperator != null && SelectedOperator.MappingRules != null;
                        });
            SaveRuleCommand = new RelayCommand(() => SaveRulesToFile(true), () => SelectedOperator != null && SelectedOperator.MappingRule != null);
            LoadRuleCommand = new RelayCommand(() => LoadRulesFromFile(true));

            SaveAllRulesCommand = new RelayCommand(() => SaveRulesToFile(false), () => SelectedOperator != null && SelectedOperator.MappingRule != null);
            LoadAllRulesCommand = new RelayCommand(() => LoadRulesFromFile(false));

            RefreshOperatorListCommand = new RelayCommand(RefreshOperatorList);
            RefreshMappingTablesCommnad = new RelayCommand(RefreshMappingTables);
            ShowWebPageCommand = new RelayCommand<string>(ShowWebPage);
            ShowImageParsingWindowCommand = new RelayCommand(ShowImageParsingWindow);
            ShowLoadImageWindowCommand = new RelayCommand<string>(ShowLoadImageWindow);
        }

        private string databaseName = "Unknown";
        public string DatabaseName
        {
            get { return databaseName; }
            set 
            {
                if (value == databaseName)
                    return;
                databaseName = value;
                RaisePropertyChanged("DatabaseName");
            }
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
                RealUpdateOperatorAndRulesFromDatabase();
                UpdateAllowedColumns();
            } else
                SheetHeaders.Clear();

            //RaisePropertyChanged("SelectedOperator");
        }

        private void UpdateAllowedColumns()
        {
            if (Document.SelectedSheet == null || Document.SelectedSheet.Rows.Count == 0)
            {
                SheetHeaders.Clear();
                foreach (var rule in SelectedOperator.MappingRules)
                    foreach (var data in rule.ConvertionData)
                        foreach (var block in data.Blocks.Blocks)
                        {
                            foreach (var func in block.UsedFunctions)
                                if (func.Function != null && !string.IsNullOrWhiteSpace(func.Function.ColumnName) && !SheetHeaders.Contains(func.Function.ColumnName))
                                    SheetHeaders.Add(func.Function.ColumnName);

                            foreach (var srule in block.StartRules)
                                if (srule.Rule != null && !string.IsNullOrWhiteSpace(srule.Rule.ColumnName) && !SheetHeaders.Contains(srule.Rule.ColumnName))
                                    SheetHeaders.Add(srule.Rule.ColumnName);
                        }
            }
            else if (Document.SelectedSheet != null)
                UpdateSheetHeaders();
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

        public void UpdateMainHeaderRow(bool updateSharp)
        {
            SheetHeaders.Clear();

            Document.SelectedSheet.UpdateMainHeaderRow(
                SelectedOperator.MappingRule.MainHeaderSearchTags
                    .Select(h => h.Tag)
                    .Union(SettingsProvider.CurrentSettings.HeaderSearchTags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(i => i != null ? i.Trim() : null)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Distinct()
                    .ToArray()
                    );

            foreach (var header in Document.SelectedSheet.MainHeader.Cells)
            {
                SheetHeaders.Add(header.Value);
            }

            if (updateSharp)
                UpdateSharp(false);
        }

        public void UpdateSheetHeaderRow(bool updateSharp)
        {
            Document.SelectedSheet.UpdateHeaders(
                    SelectedOperator.MappingRule.SheetHeadersSearchTags
                    .Select(h => (h != null && h.Tag != null) ? h.Tag.Trim() : null)
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Distinct()
                    .ToArray()
                );
            if (updateSharp)
                UpdateSharp(false);
        }

        private void UpdateSheetHeaders()
        {
            if (Document.SelectedSheet != null)
            {
                if (Document.SelectedSheet.Rows.Count > 0)
                {
                    if (Document.SelectedSheet != null)
                    {
                        var savedExportRule = ExportRules
                            .Where(r => r.Rule != null && r.Rule != NullRule)
                            .FirstOrDefault(r => Document.SelectedSheet.Name.ToLower().Contains(r.SheetName.ToLower()));
                        
                        if (savedExportRule != null)
                            SelectedOperator.MappingRule = savedExportRule.Rule; 
                        else
                            SelectedOperator.MappingRule = 
                                SelectedOperator.MappingRules.FirstOrDefault(r => Document.SelectedSheet.Name.ToLower().Contains(r.Name.ToLower()))
                                ?? SelectedOperator.MappingRule
                                ?? SelectedOperator.MappingRules.FirstOrDefault();
                    }
                    UpdateMainHeaderRow(false);
                    UpdateSheetHeaderRow(false);
                }
                UpdateSharp(true);
            }
            if (SelectedOperator != null && SelectedOperator.MappingRules.Count > 0)
            {
                SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();
            }
        }

        public void RealUpdateOperatorAndRulesFromDatabase()
        {
            SelectedOperator.MappingRules = new ObservableCollection<ExelConvertionRule>(
                _appSettingsDataAccess.GetRulesByOperator(SelectedOperator)
                .OrderBy(r => (r.Name == ExelConvertionRule.DefaultName ? "0" : "1") + r.Name)
                );
            if (SelectedOperator.MappingRules.Count == 0)
            {
                var mappingRule = new ExelConvertionRule
                {
                    FkOperatorId = (int)SelectedOperator.Id,
                    Name = ExelConvertionRule.DefaultName
                };
                _appSettingsDataAccess.AddOperatorRule(mappingRule);
            } 

            SelectedOperator.MappingRule = SelectedOperator.MappingRules.FirstOrDefault();
            SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();

            AddBlockCommand.RaiseCanExecuteChanged();
        }

        public void SaveRules(bool needRefresh)
        {
            if (SelectedOperator != null)
            {
                foreach (var r in SelectedOperator.MappingRules)
                    r.FkOperatorId = (int)SelectedOperator.Id;

                var storedRules = SelectedOperator.MappingRuleSavedData.Select(d => ExelConvertionRule.DeserializeFromBytes(d.Value)).ToArray();
                //_appSettingsDataAccess.GetRulesByOperator(SelectedOperator);
                int[] storedIds = SelectedOperator.MappingRuleSavedData.Select(r => r.Key).ToArray();

                #region Remove old rules

                var itemsToDelete = storedIds.Where(r => !SelectedOperator.MappingRules.Any(mr => mr.Id == r)).ToArray();
                if (itemsToDelete.Length > 0)
                    _appSettingsDataAccess.RemoveOpertaorRule(itemsToDelete);

                #endregion
                #region Save new rules


                var itemsToInsert = SelectedOperator.MappingRules.Where(mr => mr.Id == 0).ToArray();
                if (itemsToInsert.Length > 0)
                     _appSettingsDataAccess.AddOperatorRules(itemsToInsert);

                #endregion
                #region Update rules

                List<ExelConvertionRule> updateRules = new List<ExelConvertionRule>();

                foreach (var currentRule in
                                SelectedOperator
                                .MappingRules
                                .Where(mr => mr.Id > 0 && !itemsToInsert.Contains(mr))
                                .AsParallel()
                                .Select(r => new
                                {
                                    Rule = r,
                                    StoredRule = storedRules.FirstOrDefault(i => i.Id == r.Id)
                                        ?? storedRules.FirstOrDefault(i => i.Name == r.Name)
                                })
                                .Where(r => r.StoredRule != null)
                                .ToArray()
                                .AsParallel()
                                .Select(r =>
                                     new
                                        {
                                            Rule = r.Rule,
                                            Data = r.Rule.SerializeToBytes(),
                                            StoredRule = r.StoredRule,
                                            StoredRuleData = (r.StoredRule == null ? new byte[] { } : SelectedOperator.MappingRuleSavedData[r.StoredRule.Id])
                                        }
                                )
                                .Where(r => !r.StoredRuleData.SequenceEqual(r.Data))
                                .Select(r => r.Rule)
                                .ToArray())
                    updateRules.Add(currentRule);

                if (updateRules.Count > 0)
                    _appSettingsDataAccess.UpdateOperatorRules(updateRules.ToArray());

                #endregion

                SaveImageParsingData(storedRules);

                if (needRefresh)
                    RealUpdateOperatorAndRulesFromDatabase();
            }
        }

        private void SaveImageParsingData(ExelConvertionRule[] storedRules)
        {
            List<FillArea> areaList = new List<FillArea>();
            List<FillArea> areaListStored = new List<FillArea>();

            if (SelectedOperator.MappingRule != null && SelectedOperator.MappingRule.IsMapParsingDataLoaded && SelectedOperator.MappingRule.MapParsingData != null)
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
                if (storedMappingRule != null && storedMappingRule.IsMapParsingDataLoaded && storedMappingRule.MapParsingData != null)
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

            if (SelectedOperator.MappingRule != null && SelectedOperator.MappingRule.IsPhotoParsingDataLoaded && SelectedOperator.MappingRule.PhotoParsingData != null)
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
                if (storedMappingRule != null && storedMappingRule.IsPhotoParsingDataLoaded && storedMappingRule.PhotoParsingData != null)
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
        public RelayCommand SaveAllRulesCommand { get; private set; }
        private void SaveRulesToFile(bool single)
        {
            var deleteBadChars = new Func<string,string>((s) =>
            {
                string res = s;
                foreach(var c in System.IO.Path.GetInvalidFileNameChars())
                    res = res.Replace(c.ToString(),"");
                return res;
            });            

            var saveFileDialog = new System.Windows.Forms.SaveFileDialog()
            {
                Filter = "XML rules files (*.xml)|*.xml|Rules files (*.rules)|*.rules|All files (*.*)|*.*",
                FileName = deleteBadChars(SelectedOperator.Name + ((single) ? " - " + SelectedOperator.MappingRule.Name : string.Empty)),
                DefaultExt = "xml",
                AddExtension = true,
                RestoreDirectory = true
            }; 
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    if (single)
                        SaveRulesToFile(new ExelConvertionRule[] { SelectedOperator.MappingRule }, saveFileDialog.FileName);
                    else
                        SaveRulesToFile(SelectedOperator.MappingRules.ToArray(), saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Log.Add(ex, "ImportViewModel.SaveRulesToFile()");
                    MessageBox.Show(string.Format("Произошла ошибка при сохранении правил:{0}{1}", Environment.NewLine, ex.Message), "Ошибка при загрузке правил", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        private void SaveRulesToFile(ExelConvertionRule[] rules, string fileName)
        {
            bool IsXML = (System.IO.Path.GetExtension(fileName).ToLower() == ".xml");
            string text = (new List<ExelConvertionRule>(rules)).SerializeToXML(!IsXML);
            if (IsXML)
                System.IO.File.WriteAllText(fileName, text); 
            else
                System.IO.File.WriteAllBytes(fileName, text.CompressToBytes());
        }

        public RelayCommand LoadRuleCommand { get; private set; }
        public RelayCommand LoadAllRulesCommand { get; private set; }
        private void LoadRulesFromFile(bool single)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "XML rules files (*.xml)|*.xml|Rules files (*.rules)|*.rules|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    var storedRules = LoadRulesFromFile(openFileDialog.FileName);
                    if (storedRules.Length == 0)
                        throw new Exception("Файл поврежден");

                    bool startLoad = true;

                    if (single && storedRules.Length != 1 || !single)
                        startLoad = MessageBox.Show(string.Format("В выбранно файле найдено правил: {0}. Импортировать все?", storedRules.Length),"Загрузка правил",MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

                    if (startLoad)
                    {
                        foreach (var rule in
                            storedRules
                            .Select(r => new { NewRule = r, OldRule = SelectedOperator.MappingRules.FirstOrDefault(i => i.Name == r.Name && r != i) })
                            .ToArray()
                            )
                        {
                            if (single && storedRules.Length == 1)
                                rule.NewRule.Name = SelectedOperator.MappingRule.Name;

                            rule.NewRule.FkOperatorId = (int)SelectedOperator.Id;
                            rule.NewRule.Id = 0;
                            rule.NewRule.OnDeserialized();

                            if (rule.OldRule != null)
                            {
                                var index = SelectedOperator.MappingRules.IndexOf(rule.OldRule);
                                SelectedOperator.MappingRules.RemoveAt(index);
                                SelectedOperator.MappingRules.Insert(index, rule.NewRule);
                            }                             
                            else
                                SelectedOperator.MappingRules.Add(rule.NewRule);
                        }

                        if (single && storedRules.Length == 1)
                            SelectedOperator.MappingRule = storedRules[0];
                        else
                            SelectedOperator.MappingRule = SelectedOperator.MappingRules.FirstOrDefault();
                        UpdateAllowedColumns();
                    }
                }
                catch (Exception ex)
                {
                    Log.Add(ex, "ImportViewModel.LoadRulesFromFile()");
                    MessageBox.Show(string.Format("Произошла ошибка при загрузке правил:{0}{1}", Environment.NewLine, ex.Message), "Ошибка при загрузке правил", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
        }

        private static ExelConvertionRule[] LoadRulesFromFile(string fileName)
        {
            bool IsXML = (System.IO.Path.GetExtension(fileName).ToLower() == ".xml");
            string text = (IsXML) 
                ? System.IO.File.ReadAllText(fileName)
                : System.IO.File.ReadAllBytes(fileName).DecompressFromBytes();

            List<ExelConvertionRule> storedRules;
            typeof(List<ExelConvertionRule>).DeserializeFromXML(text, out storedRules);
            return storedRules.ToArray();
        }

        private RelayCommand<DataRowView> addTagToHeaderCommand = null;
        public RelayCommand<DataRowView> AddTagToHeaderCommand { get { return addTagToHeaderCommand ?? (addTagToHeaderCommand = new RelayCommand<DataRowView>(AddTagToHeader)); } }
        private void AddTagToHeader(DataRowView prm)
        {
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();

            if (!SelectedOperator.MappingRule.SheetHeadersSearchTags.Select(t => t.Tag).Any(t => t.Like(tag)))
                SelectedOperator.MappingRule.SheetHeadersSearchTags.Add(new SearchTag() { Tag = tag });
        }

        private RelayCommand<DataRowView> delTagFromHeaderCommand = null;
        public RelayCommand<DataRowView> DelTagFromHeaderCommand { get { return delTagFromHeaderCommand ?? (delTagFromHeaderCommand = new RelayCommand<DataRowView>(DelTagFromHeader)); } }
        private void DelTagFromHeader(DataRowView prm)
        {
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();

            foreach (var fndTag in SelectedOperator.MappingRule.SheetHeadersSearchTags.Where(t => t.Tag.Like(tag)).ToArray())
                SelectedOperator.MappingRule.SheetHeadersSearchTags.Remove(fndTag);
        }

        private RelayCommand<DataRowView> excludeTagFromHeaderCommand = null;
        public RelayCommand<DataRowView> ExcludeTagFromHeaderCommand { get { return excludeTagFromHeaderCommand ?? (excludeTagFromHeaderCommand = new RelayCommand<DataRowView>(ExcludeTagFromHeader)); } }
        private void ExcludeTagFromHeader(DataRowView prm)
        {
            DelTagFromHeader(prm);
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();

            if (!SelectedOperator.MappingRule.SheetHeadersSearchTags.Select(t => t.Tag).Any(t => t.Like("-" + tag)))
                SelectedOperator.MappingRule.SheetHeadersSearchTags.Add(new SearchTag() { Tag = "-" + tag });
        }

        private RelayCommand<DataRowView> includeTagToHeaderCommand = null;
        public RelayCommand<DataRowView> IncludeTagToHeaderCommand { get { return includeTagToHeaderCommand ?? (includeTagToHeaderCommand = new RelayCommand<DataRowView>(IncludeTagToHeader)); } }
        private void IncludeTagToHeader(DataRowView prm)
        {
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();
            
            foreach (var fndTag in SelectedOperator.MappingRule.SheetHeadersSearchTags.Where(t => t.Tag.Like("-" + tag)).ToArray())
                SelectedOperator.MappingRule.SheetHeadersSearchTags.Remove(fndTag);
        }

        private RelayCommand<DataRowView> addTagToMainHeaderCommand = null;
        public RelayCommand<DataRowView> AddTagToMainHeaderCommand { get { return addTagToMainHeaderCommand ?? (addTagToMainHeaderCommand = new RelayCommand<DataRowView>(AddTagToMainHeader)); } }
        private void AddTagToMainHeader(DataRowView prm)
        {
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();

            if (!SelectedOperator.MappingRule.MainHeaderSearchTags.Select(t => t.Tag).Any(t => t.Like(tag)))
                SelectedOperator.MappingRule.MainHeaderSearchTags.Add(new SearchTag() { Tag = tag });
        }

        private RelayCommand<DataRowView> delTagFromMainHeaderCommand = null;
        public RelayCommand<DataRowView> DelTagFromMainHeaderCommand { get { return delTagFromMainHeaderCommand ?? (delTagFromMainHeaderCommand = new RelayCommand<DataRowView>(DelTagFromMainHeader)); } }
        private void DelTagFromMainHeader(DataRowView prm)
        {
            var tag = ((DataRowView)ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Item)
                        .Row[ExelConverterLite.View.ViewLocator.ImportView.MainDataGrid.CurrentCell.Column.Header.ToString()].ToString();

            foreach (var fndTag in SelectedOperator.MappingRule.MainHeaderSearchTags.Where(t => t.Tag.Like(tag)).ToArray())
                SelectedOperator.MappingRule.MainHeaderSearchTags.Remove(fndTag);
        }

        public RelayCommand SelectFileCommand { get; private set; }
        private void SelectFile()
        {
            try
            {
                var filePath = ExelConverterFileDialog.Show();
                if (filePath != null)
                    Document.Path = filePath;
            }
            catch(Exception e)
            {
                Document.Path = string.Empty;
                System.Windows.MessageBox.Show(App.Current.MainWindow, string.Format("Exception: {0}{2}{1}",e.Message,e.StackTrace, Environment.NewLine));
            }
        }

        //public RelayCommand LoadDocumentCommand { get; private set; }
        //private void LoadDocument()
        //{
        //    try
        //    {
        //        DocumentRows = new ObservableCollection<OutputRow>(SelectedOperator.MappingRule.Convert(SelectedSheet).Take(SettingsProvider.CurrentSettings.PreloadedRowsCount).ToArray());
        //    }
        //    catch
        //    {
        //        DocumentRows = new ObservableCollection<OutputRow>();
        //        System.Windows.MessageBox.Show("Произошла ошибка, проверьте еще раз\nправило и попробуйте снова...","Ошибка", 
        //            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        //    }
        //}

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
            if (Document.SelectedSheet != null)
                try                
                {
                    var data = SelectedOperator.MappingRule.ConvertionData.Where(f => f.FieldName == parameter).Single();
                    var initialRow = 0;
                    var sheet = Document.SelectedSheet;
                    initialRow = sheet.Rows.IndexOf(sheet.MainHeader) + sheet.MainHeaderRowCount;

                    if (sheet != null)
                    foreach (var i in 
                        sheet
                        .Rows
                        .AsParallel()
                        .Select(r => sheet.Rows.IndexOf(r))
                        .AsParallel()
                        .Where(i => 
                            i >= initialRow 
                            && !sheet.SheetHeaders.Subheaders.Select(s => s.RowNumber).Contains(i)
                            && !sheet.SheetHeaders.Headers.Select(s => s.RowNumber).Contains(i) 
                            )
                        .OrderBy(i => i)
                        .ToArray()
                        )
                    //for (var i = initialRow; i < sheet.Rows.Count; i++)
                    {
                        var cellResultContent = string.Empty;

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
                catch(Exception ex)
                {
                    Log.Add(ex, string.Format("ImportViewModel.UpdateMappingsTable('{0}')", parameter));
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

                var savedRules = _appSettingsDataAccess.GetExportRulesIdByOperator(SelectedOperator, Document.DocumentSheets == null ? null : Document.DocumentSheets.AsQueryable());
                foreach (var r in savedRules.Where(r2 => r2.Rule == null))
                    r.Rule = App.Locator.Import.NullRule;
                
                exportRules = new ObservableCollection<SheetRulePair>(savedRules);

                if (Document.DocumentSheets != null)
                    foreach (var sheet in Document.DocumentSheets)
                    {
                        if (!exportRules.Select(s => s.SheetName.ToLower().Trim()).Contains(sheet.Name.ToLower().Trim()))
                            exportRules.Add(
                                new SheetRulePair(Document.DocumentSheets.AsQueryable()) 
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
                    rule.AllowedSheets = Document.DocumentSheets.AsQueryable();
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

            if (!File.Exists(Document.Path))
                MessageBox.Show("Исходный файл перемещен или не существует", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                if ((dlg = new SaveFileDialog() { FileName = System.IO.Path.GetFileName(Document.Path), Filter = "Файлы экспорта (*" + System.IO.Path.GetExtension(Document.Path) + ")|*" + System.IO.Path.GetExtension(Document.Path) }).ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var dlgWnd = View.ViewLocator.ReExportProgressView;
                        File.Copy(Document.Path, dlg.FileName, true);
                        BackgroundWorker bw = ReExport.Start(dlg.FileName, SelectedOperator.Id, ExportRules.ToArray());
                        bw.RunWorkerCompleted += (s, e) => 
                        {
                            string errMessage = string.Empty;

                            if (e.Error != null)
                                errMessage = e.Error.Message;
                            else if (e.Result != null && !string.IsNullOrEmpty(e.Result.ToString()))
                                errMessage = e.Result.ToString();

                            if (!string.IsNullOrEmpty(errMessage) && !e.Cancelled)
                            {
                                MessageBox.Show(
                                    "При попытке реэкспорта файла произошла одна или несколько ошибок:" + Environment.NewLine + 
                                    errMessage + Environment.NewLine +
                                    "Подробности смотрите в log-файле." + Environment.NewLine + 
                                    (e.Error != null ? "Ошибка критическая. Реэкспорт не завершён." : "Ошибка не критическая. Реэкспорт завершен с учётом ошибок.")
                                    , "Ошибка", MessageBoxButtons.OK, (e.Error != null ? MessageBoxIcon.Error : MessageBoxIcon.Warning));
                            }
                            dlgWnd.DialogResult = false;
                        };
                        bw.ProgressChanged += (s, e) =>
                        {
                            App.Locator.ReExportProgress.ProgressValue = e.ProgressPercentage;
                        };
                        //show wait dialog
                        bw.RunWorkerAsync();
                        App.Locator.ReExportProgress.ProgressText = " Выполняется реэксопрт файла... ";
                        dlgWnd.ShowDialog(App.Current.MainWindow);
                    }
                    catch(Exception ex)
                    {
                        Log.Add(ex.GetExceptionText("ImportViewModel.ReExportFileCommand()"));
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
            SaveRules(false);
        }

        public RelayCommand UpdateOperatorCommand { get; private set; }
        private void UpdateOperator()
        {
            OperatorChanged();
        }

        public RelayCommand UpdateFoundedHeadersCommand { get; private set; }
        private void UpdateFoundedHeaders()
        {
            UpdateMainHeaderRow(true);
            UpdateSheetHeaderRow(true);
        }

        public RelayCommand<object> AddRuleCommand { get; private set; }
        private void AddRule(bool basedOnCurrentRule)
        {
            var rule = basedOnCurrentRule ? ExelConvertionRule.DeserializeFromB64String(SelectedOperator.MappingRule.Serialize()) : new ExelConvertionRule();
            rule.FkOperatorId = (int)SelectedOperator.Id;
            rule.Name = Document.SelectedSheet.Name;
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
            if (Document.SelectedSheet != null)
            {
                LoadPhotos();
                LoadMaps();
            }
            

            View.ViewLocator.ImageParsingView.ShowDialog(App.Current.MainWindow);
        }

        private void LoadPhotos()
        {
            if (Document.SelectedSheet != null)
            {
                var photoConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Photo_img");
                var mapConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Location_img");

                var i = Document.SelectedSheet.Rows.IndexOf(Document.SelectedSheet.MainHeader) + Document.SelectedSheet.MainHeaderRowCount;

                var url = photoConvertionData.Blocks.Run(Document.SelectedSheet, i, photoConvertionData);

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
            if (Document.SelectedSheet != null)
            {
                var mapConvertionData = SelectedOperator.MappingRule.ConvertionData.Single(cd => cd.PropertyId == "Location_img");
                var i = Document.SelectedSheet.Rows.IndexOf(Document.SelectedSheet.MainHeader) + Document.SelectedSheet.MainHeaderRowCount;
                var url = mapConvertionData.Blocks.Run(Document.SelectedSheet, i, mapConvertionData);

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
                    //LoadDocumentCommand.RaiseCanExecuteChanged();
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
                    //LoadDocumentCommand.RaiseCanExecuteChanged();
                    ToCsvCommand.RaiseCanExecuteChanged();
                    ReExportFileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        //private ObservableCollection<OutputRow> _documentRows;
        //public ObservableCollection<OutputRow> DocumentRows
        //{
        //    get { return _documentRows ?? (_documentRows = new ObservableCollection<OutputRow>()); }
        //    set
        //    {
        //        if (_documentRows != value)
        //        {
        //            _documentRows = value;
        //            RaisePropertyChanged("DocumentRows");
        //        }
        //    }
        //}

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
                if (_selectedOperator == value)
                    return;

                _repeatLocksUpdater.Stop();

                bool wasException = false;
                var logSession = Helpers.Log.SessionStart("ImportViewModel.SelectedOperator_set()", true);
                try
                {
                    if (_selectedOperator != null)
                    {
                        SaveRules(false);
                        _appSettingsDataAccess.SetOperatorLocker(_selectedOperator, CurrentUser, false);
                        SelectedOperator.LockedBy = null;
                    }

                    if (value != null && _appSettingsDataAccess.SetOperatorLocker(value, CurrentUser, true))
                    {
                        _selectedOperator = value;
                        _selectedOperator.LockedBy = currentUser;
                    }
                    else
                    {
                        _selectedOperator = null;
                        if (value != null)
                        {
                            UpdateLockedOperators();
                            User usr = _appSettingsDataAccess.GetOperatorLocker(value);
                            MessageBox.Show(string.Format("Изменение выбранного оператора '{0}' невозможно, так как в данный момент данный оператор редактируется в другом сеансе пользователем '{1}'", value.Name, usr != null ? usr.ToString() : "unknown"), "Изменение оператора", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    OperatorChanged();   
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

        private Operator oldOperator = null;
        public void StartSharpControl()
        {
            this.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "SelectedOperator")
                    {
                        if (oldOperator != null)
                        {
                            oldOperator.PropertyChanged -= SelectedOperator_PropertyChanged;
                            if (oldOperator.MappingRule != null)
                                oldOperator.MappingRule.PropertyChanged -= MappingRule_PropertyChanged;
                        }

                        if (SelectedOperator != null)
                        {
                            oldOperator = SelectedOperator;

                            SelectedOperator.PropertyChanged -= SelectedOperator_PropertyChanged;
                            SelectedOperator.PropertyChanged += SelectedOperator_PropertyChanged;    
                            if (SelectedOperator.MappingRule != null)
                            {
                                SelectedOperator.MappingRule.PropertyChanged -= MappingRule_PropertyChanged;
                                SelectedOperator.MappingRule.PropertyChanged += MappingRule_PropertyChanged;
                            }
                        }
                    }
                };
            RaisePropertyChanged("SelectedOperator");
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
                    SelectedOperator.MappingRule.PropertyChanged -= MappingRule_PropertyChanged;
                    SelectedOperator.MappingRule.PropertyChanged += MappingRule_PropertyChanged;
                }

                if (SelectedOperator.MappingRule != null)
                    SelectedField = SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();
            }
        }

        private void MappingRule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Like("MainHeaderSearchTags*") && Sharp != null)
            {
                UpdateMainHeaderRow(true);
            }
            if (e.PropertyName.Like("SheetHeadersSearchTags*") && Sharp != null)
            {
                UpdateSheetHeaderRow(true);
            }
        }



        private bool isSharpLoading = false;
        public bool IsSharpLoading
        {
            get
            {
                return isSharpLoading;
            }
            private set
            {
                if (isSharpLoading != value)
                {
                    isSharpLoading = value;
                    RaisePropertyChanged("IsSharpLoading");
                }
            }
        }

        private class UpdateSharpParams
        {
            public int[] Headers = new int[] { };
            public int[] SubHeaders = new int[] { };
            public int[] MainHeaders = new int[] { };
            public ExelSheet Sheet = null;
            public DataTable CurrentData = null;
        }

        private void UpdateSharp(bool reInit)
        {
            int[] headers = Document.SelectedSheet.SheetHeaders.Headers.Select(s => s.RowNumber).ToArray();
            int[] subHeaders = Document.SelectedSheet.SheetHeaders.Subheaders.Select(s => s.RowNumber).ToArray(); 
            int[] main = new int[Document.SelectedSheet.MainHeaderRowCount];   
            for(int i = 0; i< Document.SelectedSheet.MainHeaderRowCount; i++)
                main[i] = Document.SelectedSheet.Rows.IndexOf(Document.SelectedSheet.MainHeader) + i;

            var UpdateSharpWorker = new BackgroundWorker();
            UpdateSharpWorker.WorkerSupportsCancellation = false;
            UpdateSharpWorker.WorkerReportsProgress = false;
            UpdateSharpWorker.DoWork += (s, e) =>
                {
                    UpdateSharpParams prm = (UpdateSharpParams)e.Argument;
                    DataTable sharp = prm.CurrentData == null ? prm.Sheet.AsDataTable() : prm.CurrentData;
                    
                    var mainStart = prm.MainHeaders.OrderBy(i => i).FirstOrDefault();

                    for (int i = 0; i < sharp.Rows.Count; i++)
                    {
                        string rowType = string.Empty;
                        if (i < mainStart)
                            rowType = "D";
                        else if (prm.MainHeaders.Contains(i))
                            rowType = "M";
                        else if (prm.Headers.Contains(i))
                            rowType = "H";
                        else if (prm.SubHeaders.Contains(i))
                            rowType = "S";

                        if (sharp.Rows[i]["type"].ToString() != rowType)
                            sharp.Rows[i]["type"] = rowType;
                    }

                    e.Result = sharp;
                };
            UpdateSharpWorker.RunWorkerCompleted += (s, e) =>
                {
                    try
                    { 
                        Sharp = (DataTable)e.Result;
                    }
                    finally
                    { 
                        IsSharpLoading = false;
                    }
                };

            UpdateSharpParams prms = new UpdateSharpParams() 
            {
                Headers = headers,
                SubHeaders = subHeaders,
                MainHeaders = main,
                Sheet = Document.SelectedSheet,
                CurrentData = (reInit) ? null : Sharp
            };

            Sharp = null;
            IsSharpLoading = true;

            UpdateSharpWorker.RunWorkerAsync(prms);
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
