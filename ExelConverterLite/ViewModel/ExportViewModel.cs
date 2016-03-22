using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.DataWriter;
using ExelConverter.Core.Settings;
using System.Windows.Controls;
using System.IO;
using Helpers;
using System.Threading.Tasks;
using System.Windows;
using ExcelConverter.Parser;
using ExcelConverter.Parser.Controls;
using ExelConverter.Core.DataAccess;
using ExelConverter.Core.Converter;
using ExelConverterLite.View;
using System.ComponentModel;
using System.Threading;

namespace ExelConverterLite.ViewModel
{
    [Serializable]
    [System.Xml.Serialization.XmlRoot("ParserCollection")]
    public class MyDBParserCollection : DBParserCollection 
    {
        protected IDataAccess DataAccess = new DataAccess();

        protected override void DBParserLoad()
        {
            Parsers.Clear();
            Parser[] parsersLoaded = DataAccess.ParsersGet();
            foreach (var p in parsersLoaded)
                Parsers.Add(p);
        }
        protected override void DBParserSave()
        {
            Parser[] parsersToUpdate = Parsers.Where(p => p.IsChanged && !p.IsDeleted).ToArray();
            Parser[] parsersToDelete = Parsers.Where(p => p.IsDeleted).ToArray();
            Parser[] parsersToInsert = Parsers.Where(p => p.Id == Guid.Empty && !p.IsDeleted).ToArray();

            DataAccess.ParsersInsert(parsersToInsert);
            DataAccess.ParsersUpdate(parsersToUpdate);
            DataAccess.ParsersRemove(parsersToDelete);
        }
        protected override void DBParserAdd(Parser p)
        {
            if (StoreDirect)
            {
                p.Id = Guid.NewGuid();
                DataAccess.ParsersInsert(new Parser[] { p });
                p.IsChanged = false;
            }
        }
        protected override void DBParserRemove(Parser p)
        {
            p.IsDeleted = true;
            if (StoreDirect)
            {
                DataAccess.ParsersRemove(new Parser[] { p });
                p.Id = Guid.Empty;
                p.IsChanged = false;
            }
        }
        protected override void DBParserUpdate(Parser p)
        {
            if (StoreDirect)
            {
                DataAccess.ParsersUpdate(new Parser[] { p });
                p.IsChanged = false;
            }
        }
    }

    public class UrlCollectionAdditional
    {
        public UrlCollection Collection { get; set; }
        public string Name { get; set; }
    }

    public class ExportViewModel : ViewModelBase
    {
        private static MyDBParserCollection _DBParsers = null;
        private static MyDBParserCollection DBParsers
        {
            get
            {
                if (_DBParsers == null)
                {
                    if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject())) 
                    {
                        _DBParsers = new MyDBParserCollection() { StoreLocal = true, StoreDirect = false };
                    } else
                        _DBParsers = new MyDBParserCollection() { StoreLocal = false, StoreDirect = true };
                    _DBParsers.Load();
                }
                return _DBParsers;
            }
        }


        public ExportViewModel() { }

        private string initializeError = string.Empty;
        public string InitializeError
        {
            get
            {
                return initializeError;
            }
            private set
            {
                if (initializeError == value)
                    return;
                initializeError = value;
                RaisePropertyChanged(nameof(InitializeError));
                RaisePropertyChanged(nameof(HasError));
            }
        }

        public bool HasError { get { return !string.IsNullOrWhiteSpace(InitializeError); } }

        private bool isLoading = false;
        public bool IsLoading {
            get { return isLoading; }
            private set { if (isLoading == value) return; isLoading = value; RaisePropertyChanged(nameof(IsLoading)); }
        }

        private int loadProgress = 0;
        public int LoadProgress
        {
            get { return loadProgress; }
            private set { if (loadProgress == value) return; loadProgress = value; RaisePropertyChanged(nameof(LoadProgress)); }
        }

        private BackgroundWorker loadWorker = null;

        private RelayCommand cancelCommand = null;
        public RelayCommand CancelCommand
        {
            get { return (cancelCommand ?? (cancelCommand = new RelayCommand(() => 
            {
                if (loadWorker != null)
                    loadWorker.CancelAsync();                
            }, () => { return IsLoading; }))); }
        }

        public void Initialize()
        {
            InitializeError = string.Empty;
            if (Export2CsvCommand == null)
                Export2CsvCommand = new RelayCommand(() => Export2Csv());
            if (Export2DbCommand == null)
                Export2DbCommand = new RelayCommand(Export2Db);
            if (UpdateErrorsCommand == null)
                UpdateErrorsCommand = new RelayCommand(UpdateErrors);
            if (UpdateSelectedErrorCommand == null)
                UpdateSelectedErrorCommand = new RelayCommand(UpdateSelectedError);
            if (UpdateSelectedWarningCommand == null)
                UpdateSelectedWarningCommand = new RelayCommand(UpdateSelectedWarning);

            loadWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            loadWorker.DoWork += (s, prm) => 
            {
                var Disp = prm.Argument as System.Windows.Threading.Dispatcher;

                ObservableCollection<OutputRow> rowsToExport = new ObservableCollection<OutputRow>();
                Guid logSession = Helpers.Old.Log.SessionStart("ExportViewModel.Initialize()");
                try
                {
                    Log.Add(string.Format("total sheets count: '{0}'", App.Locator.Import.Document.DocumentSheets.Count));
                    var addErr = new List<Error>();
                    var addGErr = new List<GlobalError>();

                    var progress = new PercentageProgress();

                    foreach (var item in App.Locator.Import
                        .ExportRules
                        .Where(r => r.Rule != App.Locator.Import.NullRule)
                        .Select(r => new { Rule = r, Progress = progress.GetChild() })
                        .ToArray()
                        )
                    {
                        if (((BackgroundWorker)s).CancellationPending || s != loadWorker)
                            break;

                        var mappingRule = item.Rule.Rule;
                        var ds = item.Rule.Sheet;

                        if (mappingRule == null || ds == null)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Rule.Status))
                                addGErr.Add(new GlobalError() { Description = item.Rule.Status });
                            item.Progress.Value = 100;
                            continue;
                        }
                        else
                        {
                            if (ds.MainHeader == null)
                            {
                                Log.Add(string.Format("should update main header row..."));

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

                            var oc = new ObservableCollection<OutputRow>(mappingRule.Convert(ds, 
                                progressAction: (i) => 
                                {
                                    item.Progress.Value = i;
                                    ((BackgroundWorker)s).ReportProgress((int)progress.Value);
                                },
                                isCanceled: () => { return ((BackgroundWorker)s).CancellationPending; },
                                additionalErrorAction: (e, r) =>
                                {
                                    addErr.Add(new Error() { Description = e.GetExceptionText(includeStackTrace: false, clearText: true).Trim(), RowNumber = r });
                                }));
                            Log.Add(string.Format("row count on sheet '{0}' : '{1}'", ds.Name, oc.Count));
                            rowsToExport = new ObservableCollection<OutputRow>(rowsToExport.Union(oc));
                            Log.Add(string.Format("subtotal row count on sheets: '{0}'", rowsToExport.Count));
                        }
                    }

                    ExelConvertionRule.RemoveRepeatingId(rowsToExport.ToList());

                    var UrlsPhoto = new UrlCollection();
                    var UrlsSchema = new UrlCollection();
                    var UrlsAll = new UrlCollection();

                    var photos = rowsToExport.Select(r => r.Photo_img).Where(r => Helper.IsWellFormedUriString(r, UriKind.Absolute)).Distinct();
                    var schemas = rowsToExport.Select(r => r.Location_img).Where(r => Helper.IsWellFormedUriString(r, UriKind.Absolute)).Distinct();
                    var all = photos.Union(schemas).Distinct();

                    foreach (var p in photos)
                        UrlsPhoto.Add(new StringUrlWithResultWrapper(p));
                    foreach (var p in schemas)
                        UrlsSchema.Add(new StringUrlWithResultWrapper(p));
                    foreach (var p in all)
                        UrlsAll.Add(new StringUrlWithResultWrapper(p));

                    if (!((BackgroundWorker)s).CancellationPending && s == loadWorker)
                        Disp.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                        {
                            UrlCollection.Clear();
                            if (UrlsPhoto.Count > 0)
                                UrlCollection.Add(new UrlCollectionAdditional() { Name = DBParsers.Labels.ElementAt(0), Collection = UrlsPhoto });
                            if (UrlsSchema.Count > 0)
                                UrlCollection.Add(new UrlCollectionAdditional() { Name = DBParsers.Labels.ElementAt(1), Collection = UrlsSchema });
                            if (UrlsPhoto.Count > 0 && UrlsSchema.Count > 0 && UrlsAll.Count > 0)
                                UrlCollection.Add(new UrlCollectionAdditional() { Name = "Все", Collection = UrlsAll });
                            UrlCollectionSelectedIndex = UrlCollection.Count - 1;
                            RowsToExport = rowsToExport;
                            UpdateErrors(addErr, addGErr);
                        }));
                    else
                        prm.Cancel = true;
                }
                catch (Exception ex)
                {
                    Helpers.Old.Log.Add(logSession, ex.GetExceptionText());
                    throw ex;
                }
                finally
                {
                    Log.Add(string.Format("total row count to export: '{0}'", rowsToExport.Count));
                    Helpers.Old.Log.SessionEnd(logSession);
                }
            };

            loadWorker.ProgressChanged += (s, e) =>
            {
                if (s == loadWorker)
                    LoadProgress = e.ProgressPercentage;
            };

            loadWorker.RunWorkerCompleted += (s, e) => 
            {
                if (s == loadWorker)
                try
                { 
                    if (e.Cancelled)
                        throw new Exception("Загрузка отменена пользователем");
                    if (e.Error != null)
                        throw e.Error;
                }
                catch (Exception ex)
                {
                    InitializeError = ex.GetExceptionText();
                }
                finally
                {
                    if (s == loadWorker)
                    { 
                        IsLoading = false;
                        loadWorker = null;
                    }
                }
                ((BackgroundWorker)s).Dispose();
            };

            LoadProgress = 0;
            IsLoading = true;
            CancelCommand.RaiseCanExecuteChanged();
            loadWorker.RunWorkerAsync(System.Windows.Threading.Dispatcher.CurrentDispatcher);
        }

        private ObservableCollection<UrlCollectionAdditional> urlCollection = null;
        public ObservableCollection<UrlCollectionAdditional> UrlCollection
        {
            get
            {
                return urlCollection ?? (urlCollection = new ObservableCollection<UrlCollectionAdditional>());
            }
        }

        private int urlCollectionSelectedIndex = -1;
        public int UrlCollectionSelectedIndex
        {
            get
            {
                return urlCollectionSelectedIndex < 0 ? (urlCollectionSelectedIndex = Math.Max(urlCollection.Count - 1, 0)) : urlCollectionSelectedIndex;
            }
            set
            {
                if (urlCollectionSelectedIndex == value)
                    return;
                urlCollectionSelectedIndex = value;
                RaisePropertyChanged(nameof(UrlCollectionSelectedIndex));
            }
        }

        public MyDBParserCollection Parsers
        {
            get
            {
                return DBParsers;
            }
        }

        ~ExportViewModel()
        {
            //Parsers.Save();
        }

        private ObservableCollection<OutputRow> _rowsToExport = new ObservableCollection<OutputRow>();
        public ObservableCollection<OutputRow> RowsToExport
        {
            get { return _rowsToExport; }
            set
            {
                if (_rowsToExport != value)
                {
                    RowsToExport.Clear();
                    if (value != null)
                        foreach (var i in value)
                            RowsToExport.Add(i);
                    RaisePropertyChanged("RowsToExport");
                }
            }
        }

        private Error _selectedError;
        public Error SelectedError
        {
            get { return _selectedError; }
            set
            {
                if (_selectedError != value)
                {
                    _selectedError = value;
                    RaisePropertyChanged("SelectedError");
                }
                if (SelectedError != null)
                {
                    View.ViewLocator.ExportView.ScrollToRow(SelectedError.RowNumber);
                }
            }
        }

        private Error _selectedWarning;
        public Error SelectedWarning
        {
            get { return _selectedWarning; }
            set
            {
                if (_selectedWarning != value)
                {
                    _selectedWarning = value;
                    RaisePropertyChanged("SelectedWarning");
                }
            }
        }

        private ObservableCollection<Error> _errors = new ObservableCollection<Error>();
        public ObservableCollection<Error> Errors
        {
            get { return _errors; }
            set
            {
                if (_errors != value)
                {
                    Errors.Clear();
                    if (value != null)
                        foreach (var i in value)
                            Errors.Add(i);
                    RaisePropertyChanged("Errors");
                }
            }
        }

        private ObservableCollection<GlobalError> _globalErrors = new ObservableCollection<GlobalError>();
        public ObservableCollection<GlobalError> GlobalErrors
        {
            get { return _globalErrors; }
            set
            {
                if (_globalErrors != value)
                {
                    _globalErrors.Clear();
                    if (value != null)
                        foreach (var i in value)
                            _globalErrors.Add(i);
                    RaisePropertyChanged("GlobalErrors");
                }
            }
        }

        private ObservableCollection<Error> _warnings = new ObservableCollection<Error>();
        public ObservableCollection<Error> Warnings
        {
            get { return _warnings; }
            set
            {
                if (_warnings != value)
                {
                    Warnings.Clear();
                    if (value != null)
                        foreach (var i in value)
                            Warnings.Add(i);
                    RaisePropertyChanged("Warnings");
                }
            }
        }

        private RelayCommand<object> exportToQueueCommand = null;
        public RelayCommand<object> ExportToQueueCommand
        {
            get
            {
                return exportToQueueCommand ?? (exportToQueueCommand = new RelayCommand<object>(ExportToQueue));
            }
        }

        private void ExportToQueue(object param)
        {
            ExelConverter.Core.DataAccess.HttpDataAccessQueueParameters prm = param as ExelConverter.Core.DataAccess.HttpDataAccessQueueParameters;
            if (prm != null)
            {
                prm.OperatorID = App.Locator.Import.SelectedOperator.Id;
                prm.FilePath = App.Locator.Settings.Settings.CsvFilesDirectory + Path.DirectorySeparatorChar + App.Locator.Import.Document.Name + ".csv";
                if (Export2Csv())
                try
                {
                    string res = ExelConverter.Core.DataAccess.HttpDataClient.Default.UploadFileToQueue(prm);
                    MessageBox.Show(string.Format("Файл добавлен в очередь. Его ID в очереди: '{0}'", res), "Очередь", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch(Exception ex)
                {
                    Log.Add(ex, "ExportViewModel.ExportToQueue()");
                    MessageBox.Show(string.Format("Произошла ошибка при отправке файла для постановки в очередь:{0}{1}", Environment.NewLine, ex.GetExceptionText(includeData: false)), "Ошибка при отправке файла", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private RelayCommand<UrlCollection> applyParsingCommand = null;
        public RelayCommand<UrlCollection> ApplyParsingCommand 
        {
            get
            {
                return applyParsingCommand ?? (applyParsingCommand = new RelayCommand<UrlCollection>((collection) =>
                    {
                        if (collection != null)
                        {
                            foreach(var row in RowsToExport)
                            {
                                //string photo_img = string.IsNullOrWhiteSpace(row.Photo_img) ? row.Location_img : row.Photo_img;
                                //string location_img = string.IsNullOrWhiteSpace(row.Location_img) ? row.Photo_img : row.Location_img; ;

                                //row.Photo_img = ReplaceUrlFromData(photo_img, DBParsers.Labels[0], collection);
                                //row.Location_img = ReplaceUrlFromData(location_img, DBParsers.Labels[1], collection);

                                row.Photo_img = ReplaceUrlFromData(row.Photo_img, DBParsers.Labels[0], collection);
                                row.Location_img = ReplaceUrlFromData(row.Location_img, DBParsers.Labels[1], collection);

                            }
                            RaisePropertyChanged("RowsToExport");
                        }
                    }));
            }
        }

        private string ReplaceUrlFromData(string url, string label, UrlCollection collection)
        {
            string result = url;
            var colRes = collection.FirstOrDefault(i => i.Value == url);
            if (colRes != null)
            {
                result = string.Empty;
                if (colRes.Result != null && colRes.Result.Data != null && colRes.Result.Data.ContainsLabel(label))
                    result = colRes.Result.Data[label];
            }
            return result;
        }

        public RelayCommand Export2CsvCommand { get; private set; }
        private bool Export2Csv()
        {
            try
            {
                var fileName = App.Locator.Import.Document.Name + ".csv";
                var filePath = Path.Combine(App.Locator.Settings.Settings.CsvFilesDirectory, fileName);

                var allowedFields = App.Locator.Import.ExportRules
                    .Where(r => r.Rule != null && r.Rule != App.Locator.Import.NullRule && r.Sheet != null)
                    .Select(er => er.Rule)
                    .SelectMany(r => r.ConvertionData)
                    .Where(cd => cd.Blocks != null && cd.Blocks.Blocks.Count > 0)
                    .Select(cd => cd.PropertyId)
                    .Distinct()
                    .ToArray();

                var props = OutputRow.ColumnOrder
                    .Join(allowedFields, i => i.Value, a => a, (i, a) => i)
                    .OrderBy(i => i.Key)
                    .Select(i => i.Value)
                    .ToArray();

                string headerLine = (props.Length > 0) 
                    ? props.Select(p => $"\"{p}\"").Aggregate((s0, s1) => $"{s0};{s1}")
                    : string.Empty;

                var rows = RowsToExport.Select(r => r.ToCsvString(props)).ToArray();

                var allLines = string.IsNullOrEmpty(headerLine)
                    ? rows
                    : new[] { headerLine }.Union(rows).ToArray();

                File.WriteAllLines(filePath, allLines);

                var exportedCsv = new ExportedCsv
                {
                    ExportDate = DateTime.Now,
                    FileName = fileName,
                    Path = filePath,
                    Id = Guid.NewGuid()
                };
                App.Locator.ExportLog.AddExportedCsv(exportedCsv);
                return true;
            }
            catch
            {
                System.Windows.MessageBox.Show(@"Произошла ошибка, проверьте еще раз правило, 
корректность загрузки сетки, правильность пути к 
папке с файлами и попробуйте снова...", "Ошибка",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            return false;
        }

        public RelayCommand Export2DbCommand { get; private set; }
        private void Export2Db()
        {

        }

        public RelayCommand UpdateSelectedErrorCommand { get; private set; }
        private void UpdateSelectedError()
        {
            if (SelectedError != null)
            {
                View.ViewLocator.ExportView.ScrollToRow(SelectedError.RowNumber);
            }
        }

        public RelayCommand UpdateSelectedWarningCommand { get; private set; }
        private void UpdateSelectedWarning()
        {
            if (SelectedWarning != null)
            {
                View.ViewLocator.ExportView.ScrollToRow(SelectedWarning.RowNumber);
            }
        }

        public RelayCommand UpdateErrorsCommand { get; private set; }

        private void UpdateErrors()
        {
            UpdateErrors(null);
        }
        private void UpdateErrors(List<Error> addThisErrors = null, List<GlobalError> addThisGlobalErrors = null)
        {
            Errors.Clear();
            GlobalErrors.Clear();

            if (addThisGlobalErrors != null)
                foreach (var e in addThisGlobalErrors)
                    GlobalErrors.Add(e);

            var allErrors = addThisErrors ?? new List<Error>();

            foreach (var row in RowsToExport)
            {
                var errors = CheckRowErrors(row);
                if (errors.Length != 0)
                {
                    foreach (var err in errors)
                        allErrors.Add(err);
                }
            }

            foreach (var e in allErrors.OrderBy(e => e.RowNumber))
                Errors.Add(e);
        }

        private Error[] CheckRowErrors(OutputRow row)
        {
            var result = new List<Error>();

            var totalErrors = new List<Error[]>
            {
                CheckCity(row),
                CheckRegion(row),
                CheckType(row),
                CheckSize(row),
                CheckLight(row),
                CheckSide(row),
                CheckCodeDoors(row)
            };
            foreach (var errArray in totalErrors)
            {
                foreach (var err in errArray)
                {
                    result.Add(err);
                }
            }
            
            return result.ToArray();
        }

        private Error[] CheckCodeDoors(OutputRow row)
        {
            var result = new List<Error>();
            //if (string.IsNullOrWhiteSpace(row.CodeDoors))
            //{
            //    var error = new Error
            //    {
            //        RowNumber = RowsToExport.IndexOf(row),
            //        Description = "Колонка <Код DOORS> содержит пустое значение."
            //    };
            //    result.Add(error);
            //}
            //else
            //{
                int res;
                if (!string.IsNullOrWhiteSpace(row.CodeDoors) && row.CodeDoors.Any(c => !(new char[] {'1','2','3','4','5','6','7','8','9','0'}.Contains(c))))
                {
                    var error = new Error
                    {
                        RowNumber = RowsToExport.IndexOf(row),
                        Description = "Значение \"" + row.CodeDoors + "\" в колонке <Код DOORS> неверное. Должны содержаться только арабские цифры."
                    };
                    result.Add(error);
                }
            //}
            return result.ToArray();
        }

        private Error[] CheckCity(OutputRow row)
        {
            var result = new List<Error>();
            if (string.IsNullOrEmpty(row.City)||string.IsNullOrWhiteSpace(row.City))
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Колонка <Город> содержит пустое значение"
                };
                result.Add(error);
            }
            else if (!SettingsProvider.AllowedCities.Any(c => c.Name == row.City) && row.City != string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение \"" + row.City + "\" в колонке <Город> не является стандартным"
                };
                result.Add(error);
            }
            return result.ToArray();
        }

        private Error[] CheckRegion(OutputRow row)
        {
            var result = new List<Error>();
            if (!SettingsProvider.AllowedRegions.Any(r => r.Name == row.Region) && row.City != string.Empty && row.Region!=string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение \"" + row.Region + "\" в колонке <Район> не является стандартным"
                };
                result.Add(error);
            }
            else if (row.Region != string.Empty)
            {
                var city = SettingsProvider.AllowedCities.Where(c => c.Name == row.City).FirstOrDefault();
                if (city != null && !SettingsProvider.AllowedRegions.Where(r => r.FkCityId == city.Id || r.FkCityId == null).ToArray().Any(r => r.Name == row.Region))
                {
                    var error = new Error
                    {
                        RowNumber = RowsToExport.IndexOf(row),
                        Description = "Район \"" + row.Region + "\" не принадлежит городу \" " + row.City + "\""
                    };
                    result.Add(error);
                }
            }
            return result.ToArray();
        }

        private Error[] CheckType(OutputRow row)
        {
            var result = new List<Error>();
            if (string.IsNullOrEmpty(row.Type) || string.IsNullOrWhiteSpace(row.Type))
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Колонка <Тип> содержит пустое значение"
                };
                result.Add(error);
            }
            else if (!SettingsProvider.AllowedTypes.Any(t => t.Name == row.Type) && row.Type != string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение в колонке <Тип> не является стандартным"
                };
                result.Add(error);
            }
            return result.ToArray();
        }

        private Error[] CheckSize(OutputRow row)
        {
            var result = new List<Error>();
            if (!SettingsProvider.AllowedSizes.Any(s => s.Name == row.Size) && row.Size != string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение \"" + row.Size + "\" в колонке <Размер> не является стандартным"
                };
                result.Add(error);
            }
            else if (string.IsNullOrWhiteSpace(row.Size) || string.IsNullOrEmpty(row.Size))
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Колонка <Размер> содержит пустое значение"
                };
                result.Add(error);
            }
            else
            {
                var type = SettingsProvider.AllowedTypes.Where(c => c.Name == row.Type).FirstOrDefault();
                if (type != null && !SettingsProvider.AllowedSizes.Where(r => r.FkTypeId == type.Id).ToArray().Any(s => s.Name == row.Size))
                {
                    var error = new Error
                    {
                        RowNumber = RowsToExport.IndexOf(row),
                        Description = "Размер \"" + row.Size + "\" не принадлежит типу \" " + row.Type + "\""
                    };
                    result.Add(error);
                }
            }
            return result.ToArray();
        }

        private Error[] CheckLight(OutputRow row)
        {
            var result = new List<Error>();

            if (!SettingsProvider.AllowedLights.Contains(row.Light) && row.Light != string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение в колонке <Свет> не является стандартным"
                };
                result.Add(error);
            }
            else if (string.IsNullOrEmpty(row.Light) || string.IsNullOrWhiteSpace(row.Light))
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Колонка <Свет> содержит пустое значение"
                };
                result.Add(error);
            }

            return result.ToArray();
        }

        private Error[] CheckSide(OutputRow row)
        {
            var result = new List<Error>();

            if (!SettingsProvider.AllowedSides.Contains(row.Side) && row.Side != string.Empty)
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Значение в колонке <Сторона> не является стандартным"
                };
                result.Add(error);
            }
            else if (string.IsNullOrEmpty(row.Side) || string.IsNullOrWhiteSpace(row.Side))
            {
                var error = new Error
                {
                    RowNumber = RowsToExport.IndexOf(row),
                    Description = "Колонка <Сторона> содержит пустое значение"
                };
                result.Add(error);
            }
            
            return result.ToArray();
        }

        private Error[] CheckRowWarnings(OutputRow row)
        {
            var result = new List<Error>();
            var props = row.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (((string)prop.GetValue(row, null)) == string.Empty)
                {
                    result.Add(new Error
                    {
                        Description = "Колонка <"+prop.Name+"> содержит пустое значение",
                        RowNumber = RowsToExport.IndexOf(row)
                    });
                }
                else if (string.IsNullOrEmpty(row.Light) || string.IsNullOrWhiteSpace(row.Light))
                {
                    var error = new Error
                    {
                        RowNumber = RowsToExport.IndexOf(row),
                        Description = "Колонка <Свет> содержит пустое значение"
                    };
                    result.Add(error);
                }
            }
            return result.ToArray();
        }

        internal void Closing()
        {
            Parsers.Save();
            if (loadWorker != null)
            { 
                loadWorker.CancelAsync();
                loadWorker = null;
            }
        }
    }
}
