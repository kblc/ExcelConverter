using System;
using System.IO;
using System.Data.OleDb;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Threading.Tasks;
using Helpers;

using Aspose.Cells;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ExelConverter.Core.ExelDataReader
{
    public class AsyncLoadResult
    {
        public Workbook WorkBook { get; set; }
        public ExelSheet[] WorkSheets { get; set; }
        public AsyncLoadInit StartSettings { get; set; }
    }

    public class AsyncLoadInit
    {
        public string WorkBookFilePath { get; set; }
        public bool DeleteEmptyRows { get; set; }
        public int PreloadCount { get; set; }
    }

    public class ExelDocument : INotifyPropertyChanged
    {
        public ExelDocument() { }

        private string path = string.Empty;
        public string Path { get { return path; } set { path = value; RaisePropertyChanged(nameof(Path)); RaisePropertyChanged(nameof(Name)); Load(); } }

        public string Name { get { return System.IO.Path.GetFileName(Path); } }

        private string error = string.Empty;
        public string Error { get { return error; } set { if (error == value) return; error = value; RaisePropertyChanged(nameof(Error)); RaisePropertyChanged(nameof(HasError)); } }

        public bool HasError { get { return !string.IsNullOrWhiteSpace(error); } }

        private bool deleteEmptyRows = true;
        public bool DeleteEmptyRows { get { return deleteEmptyRows; } set { if (deleteEmptyRows == value) return; deleteEmptyRows = value; RaisePropertyChanged(nameof(DeleteEmptyRows)); } }

        private Workbook workBook = null;
        public Workbook WorkBook { get { return workBook; } private set { if (workBook == value) return; workBook = value; RaisePropertyChanged(nameof(WorkBook)); } }

        private ObservableCollection<ExelSheet> documentSheets = new ObservableCollection<ExelSheet>();
        public ObservableCollection<ExelSheet> DocumentSheets { get { return documentSheets; } }

        private ExelSheet selectedSheet = null;
        public ExelSheet SelectedSheet
        {
            get
            {
                return selectedSheet;
            }
            set
            {
                if (selectedSheet == value)
                    return;
                selectedSheet = value;
                RaisePropertyChanged(nameof(SelectedSheet));
            }
        }

        private bool isDocumentLoaded = false;
        public bool IsDocumentLoaded { get { return isDocumentLoaded; } private set { if (isDocumentLoaded == value) return; isDocumentLoaded = value; RaisePropertyChanged(nameof(IsDocumentLoaded)); } }

        private bool isBusy = false;
        public bool IsBusy { get { return isBusy; } private set { if (isBusy == value) return; isBusy = value; RaisePropertyChanged(nameof(IsBusy)); } }

        private string status = string.Empty;
        public string Status
        {
            get { return status; }
            private set { if (status == value) return; status = value; RaisePropertyChanged(nameof(Status)); }
        }

        private int loadingProgress = 0;
        public int LoadingProgress { get { return loadingProgress; } set { if (loadingProgress == value) return; loadingProgress = value; RaisePropertyChanged(nameof(LoadingProgress)); } }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            var e = PropertyChanged;
            if (e != null)
                e(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private BackgroundWorker loadWorker = null;
        private void Load()
        {
            if (loadWorker != null)
                try
                {
                    loadWorker.CancelAsync();
                }
                catch { }

            LoadingProgress = 0;
            IsDocumentLoaded = false;
            Error = string.Empty;

            if (string.IsNullOrWhiteSpace(Path))
                return;

            IsBusy = true;
            loadWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            var init = new AsyncLoadInit() { DeleteEmptyRows = this.DeleteEmptyRows, WorkBookFilePath = this.Path, PreloadCount = Settings.SettingsProvider.CurrentSettings.PreloadedRowsCount };

            var applyRes = new Action<Workbook, ExelSheet[]>((w, s) =>
            {
                WorkBook = w;
                DocumentSheets.Clear();
                var oldSheetName = SelectedSheet?.Name ?? string.Empty;
                foreach (var s2 in s)
                    DocumentSheets.Add(s2);
                SelectedSheet = DocumentSheets.FirstOrDefault(ss => ss.Name == oldSheetName) ?? DocumentSheets.FirstOrDefault();
            });

            loadWorker.DoWork += (s, e) =>
            {
                var bw = (BackgroundWorker)s;
                var prms = (AsyncLoadInit)e.Argument;
                var res = new AsyncLoadResult() { StartSettings = prms };

                var pp = new Helpers.PercentageProgress();
                pp.Change += (sP, eP) => { bw.ReportProgress((int)eP.Value); };

                var pp0 = pp.GetChild();
                var pp1 = pp.GetChild();
                var pp2 = pp.GetChild();

                bw.ReportProgress((int)pp.Value, "Открытие документа...");

                res.WorkBook = new Workbook(prms.WorkBookFilePath);
                pp0.Value = 100;

                bw.ReportProgress((int)pp.Value, "Чтение первых записей на страницах...");

                var subRes = AsyncDocumentLoader.LoadSheets(res.WorkBook, prms.PreloadCount, (i) => { pp1.Value = i; }, prms.DeleteEmptyRows).ToArray();
                bw.ReportProgress((int)pp.Value, new AsyncLoadResult() { WorkBook = res.WorkBook, WorkSheets = subRes });

                bw.ReportProgress((int)pp.Value, "Чтение всех записей на страницах...");

                res.WorkSheets = AsyncDocumentLoader.LoadSheets(res.WorkBook, 0, (i) => { pp2.Value = i; }, prms.DeleteEmptyRows).ToArray();

                bw.ReportProgress((int)pp.Value, "Применение результата...");

                e.Result = res;
            };
            loadWorker.ProgressChanged += (s, e) =>
            {
                if (loadWorker != s)
                    return;

                LoadingProgress = e.ProgressPercentage;
                var res = e.UserState as AsyncLoadResult;
                if (res != null)
                    applyRes(res.WorkBook, res.WorkSheets);
                else
                {
                    var status = e.UserState as string;
                    if (status != null)
                        Status = status;
                }
            };
            loadWorker.RunWorkerCompleted += (s, e) =>
            {
                if (loadWorker == s)
                {
                    if (e.Cancelled)
                        Error = "Задание отменено пользователем";

                    if (e.Error != null)
                    { 
                        Error = e.Error.GetExceptionText();
                    } else
                    { 
                        var res = e.Result as AsyncLoadResult;
                        if (res != null)
                        {
                            if (res.WorkSheets.Count() == 0)
                                Error = "В документе не найдено ни одного листа";
                            applyRes(res.WorkBook, res.WorkSheets);
                        }
                        else
                            Error = "Внутренняя ошибка приложения";
                    }
                    Status = "Загрузка документа завершена" + (string.IsNullOrWhiteSpace(Error) ? string.Empty : " с ошибками");

                    LoadingProgress = 100;
                    IsDocumentLoaded = true;
                    IsBusy = false;
                    loadWorker = null;
                }
                ((BackgroundWorker)s).Dispose();
            };
            loadWorker.RunWorkerAsync(init);
        }
    }
}
