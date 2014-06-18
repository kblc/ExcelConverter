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
    public class OperatorSettingsViewModel : ViewModelBase 
    {
        private IDataAccess _appSettingsDataAccess;

        //private Operator _tempOperator;

        public OperatorSettingsViewModel(IDataAccess appSettingsDataAccess)
        {
            _appSettingsDataAccess = appSettingsDataAccess;
            
            Operator = App.Locator.Import.SelectedOperator;
            //_tempOperator = App.Locator.Import.SelectedOperator;
            OperatorName = Operator.Name;
            //Operator.MappingRule = Operator.MappingRules.FirstOrDefault();
            SaveChangesCommand = new RelayCommand(SaveChanges);
            CancelCommand = new RelayCommand(Cancel);
            ClosingCommand = new RelayCommand(CancelChanges);
            AddRuleCommand = new RelayCommand(AddRule);
        }

        private string _operatorName;
        public string OperatorName
        {
            get { return _operatorName; }
            set
            {
                if (_operatorName != value)
                {
                    _operatorName = value;
                    RaisePropertyChanged("OperatorName");
                }
            }
        }

        private Operator _operator;
        public Operator Operator
        {
            get { return _operator; }
            set
            {
                if (_operator != value)
                {
                    _operator = value;
                    RaisePropertyChanged("Operator");
                }
            }
        }

        public RelayCommand SaveChangesCommand { get; private set; }
        private void SaveChanges()
        {
            Operator.Name = OperatorName;

            //var storedRules = _appSettingsDataAccess.GetRulesByOperator(Operator);
            //foreach (var r in storedRules)
            //{
            //    if (!App.Locator.Import.SelectedOperator.MappingRules.Any(mr => mr.Id == r.Id))
            //    {
            //        _appSettingsDataAccess.RemoveOperatorRule(r);
            //    }
            //}

            //var newRules = App.Locator.Import.SelectedOperator.MappingRules.Where(mr => mr.Id == 0).ToArray();
            //_appSettingsDataAccess.AddOperatorRules(newRules);

            //var updatedRules = App.Locator.Import.SelectedOperator.MappingRules.Where(mr => mr.Id != 0).ToArray();
            //_appSettingsDataAccess.UpdateOperatorRules(updatedRules);

            //App.Locator.Import.SelectedOperator.MappingRules = 
            //    new ObservableCollection<ExelConvertionRule>(_appSettingsDataAccess.GetRulesByOperator(App.Locator.Import.SelectedOperator));
            //App.Locator.Import.SelectedOperator.MappingRule = App.Locator.Import.SelectedOperator.MappingRules.FirstOrDefault();
            ////App.Locator.Import.SelectedOperator.MappingRule.ConvertionData = new ObservableCollection<FieldConvertionData>(App.Locator.Import.SelectedOperator.MappingRule.ConvertionData);
            //App.Locator.Import.SelectedOperator.MappingRule.RaisePropertyChanged("ConvertionData");
            //App.Locator.Import.SelectedField = App.Locator.Import.SelectedOperator.MappingRule.ConvertionData.FirstOrDefault();

            App.Locator.Import.SaveRules(false);
            
            View.ViewLocator.OperatorSettingsView.Close();
        }

        public RelayCommand CancelCommand { get; private set; }
        private void Cancel()
        {
            CancelChanges();
            View.ViewLocator.OperatorSettingsView.Close();
        }

        public RelayCommand AddRuleCommand { get; private set; }
        private void AddRule()
        {
            var rule = new ExelConvertionRule();
            rule.FkOperatorId = (int)Operator.Id;
            Operator.MappingRules.Add(rule);
        }

        public RelayCommand ClosingCommand { get; private set; }
        private void CancelChanges()
        {
            App.Locator.Import.RealUpdateOperatorAndRulesFromDatabase();
        }
    }
}
