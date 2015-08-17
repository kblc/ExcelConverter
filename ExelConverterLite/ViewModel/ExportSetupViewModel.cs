using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ExelConverter.Core.Converter;
using ExelConverter.Core.DataObjects;
using ExelConverter.Core.DataAccess;
using ExelConverter.Core.Converter.CommonTypes;

namespace ExelConverterLite.ViewModel
{

    public class ExportSetupViewModel : ViewModelBase 
    {
        public ExportSetupViewModel()
        {
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            exportRules = App.Locator.Import.ExportRules;
            exportRules.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach(var i in e.NewItems.Cast<SheetRulePair>())
                    {
                        i.AllowedSheets = App.Locator.Import.Document.DocumentSheets.AsQueryable();
                    }
            };

        }

        private ObservableCollection<SheetRulePair> exportRules = null;
        public ObservableCollection<SheetRulePair> ExportRules
        {
            get
            {
                return exportRules ?? (exportRules = App.Locator.Import.ExportRules);
            }
        }

        public RelayCommand SaveCommand { get; private set; }
        private void Save()
        {
            for (int i = exportRules.Count - 1; i >= 0; i--)
            { 
                if (string.IsNullOrWhiteSpace(exportRules[i].SheetName) && string.IsNullOrWhiteSpace(exportRules[i].Rule?.Name))
                    exportRules.RemoveAt(i);
            }

            App.Locator.Import.ExportRules = exportRules;
            View.ViewLocator.ExportSetupView.DialogResult = true;
            View.ViewLocator.ExportSetupView.Close();
        }

        public RelayCommand CancelCommand { get; private set; }
        private void Cancel()
        {
            View.ViewLocator.ExportSetupView.Close();
        }
    }
}
