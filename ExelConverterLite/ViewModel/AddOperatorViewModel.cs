using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using ExelConverter.Core.DataAccess;
using ExelConverter.Core.Converter;
using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.DataObjects;

namespace ExelConverterLite.ViewModel
{
    public class AddOperatorViewModel : ViewModelBase
    {
        private readonly IDataAccess _appSettingsDataAccess;

        public AddOperatorViewModel(IDataAccess appSettingsDataAccess)
        {
            _appSettingsDataAccess = appSettingsDataAccess;
            InitializeComands();
        }

        private void InitializeComands()
        {
            AddOperatorCommand = new RelayCommand(AddOperator);
        }

        public static RelayCommand AddOperatorCommand { get; private set; }
        private void AddOperator()
        {

            var op = new Operator
            {
                Name = OperatorName
            };
            
            App.Locator.Import.OperatorsList.Add(op);
            App.Locator.Import.Operators.Add(op);
            App.Locator.Import.SelectedOperator = op;

            View.ViewLocator.AddOperatorView.Visibility = System.Windows.Visibility.Hidden;
            OperatorName = string.Empty;
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
    }
}
