using System;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using ExelConverter.Core.DataWriter;

namespace ExelConverterLite.ViewModel
{
    public class ExportLogViewModel : ViewModelBase
    {
        public ExportLogViewModel()
        {
            ExportLog = new ObservableCollection<ExportedCsv>();
            DeleteEntryCommand = new RelayCommand<Guid>(DeleteEntry);
            ShowExportedCsvCommand = new RelayCommand<Guid>(ShowExportedCsv);
        }

        private ObservableCollection<ExportedCsv> _exportLog;
        public ObservableCollection<ExportedCsv> ExportLog
        {
            get { return _exportLog; }
            set
            {
                if (_exportLog != value)
                {
                    _exportLog = value;
                    RaisePropertyChanged("ExportLog");
                }
            }
        }

        public RelayCommand<Guid> DeleteEntryCommand { get; private set; }
        private void DeleteEntry(Guid entryId)
        {
            var entry = ExportLog.Where(l => l.Id == entryId).Single();
            ExportLog.Remove(entry);
        }

        public RelayCommand<Guid> ShowExportedCsvCommand { get; private set; }
        private void ShowExportedCsv(Guid entryId)
        {
            var entry = ExportLog.Where(l => l.Id == entryId).Single();
            Process.Start(entry.Path);
        }

        public void AddExportedCsv(ExportedCsv entry)
        {
            ExportLog.Add(entry);
        }
    }
}
