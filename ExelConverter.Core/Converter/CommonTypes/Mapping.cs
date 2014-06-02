using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Controls;

using ExelConverter.Core.ExelDataReader;
using System.Data;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    public class Mapping : INotifyPropertyChanged, IDataErrorInfo, ICopyFrom<Mapping>
    {
        //private bool _newValueHandled = false;

        public Mapping() { }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public MappingsContainer Owner { get; set; }

        private ObservableCollection<string> _allowedValues;
        public ObservableCollection<string> AllowedValues
        {
            get { return _allowedValues ?? (_allowedValues = new ObservableCollection<string>()); }
            set
            {
                if (_allowedValues != value)
                {
                    _allowedValues = value;
                    RaisePropertyChanged("AllowedValues");
                }
            }
        }

        private string _from = string.Empty;
        public string From
        {
            get { return _from; }
            set
            {
                if (_from != value)
                {
                    _from = value;
                    RaisePropertyChanged("From");
                }
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    RaisePropertyChanged("IsChecked");
                    IsCheckedChanged();
                }
            }
        }

        private string _to = string.Empty;
        public string To
        {
            get { return _to; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && _to != value)
                {
                    _to = value;
                    RaisePropertyChanged("To");
                    RaisePropertyChanged("Error");
                    IsCheckedChanged();
                }
            }
        }

        private void IsCheckedChanged()
        {
            if (Owner != null && Owner.Owner != null)
            {
                if (Owner.Owner.PropertyId == "Type")
                {
                    var checkedTypes = Owner.Where(m => m.IsChecked && m.To != null).Select(m => m.To).ToArray();
                    if (checkedTypes.Count() != 0)
                    {
                        var allowedTypes = Settings.SettingsProvider.AllowedTypes.Where(t => checkedTypes.Any(at => at.Contains(t.Name))).ToArray();
                        var allowedSizes = Settings.SettingsProvider.AllowedSizes.Where(s => allowedTypes.Any(t => t.Id == s.FkTypeId)).ToArray();
                        var sizeMappings = Owner.Owner.Owner.ConvertionData.Where(cd => cd.PropertyId == "Size").Single();
                        sizeMappings.MappingsTable.LoadAllowedValues(allowedSizes.Select(v => v.Name).ToArray());
                    }
                    else
                    {
                        var sizeMappings = Owner.Owner.Owner.ConvertionData.Where(cd => cd.PropertyId == "Size").Single();
                        sizeMappings.MappingsTable.LoadAllowedValues(Settings.SettingsProvider.AllowedSizes.Select(v => v.Name).ToArray());
                    }
                }
                else if (Owner.Owner.PropertyId == "City")
                {
                    var checkedCities = Owner.Where(m => m.IsChecked && m.To != null).Select(m => m.To).ToArray();
                    if (checkedCities.Count() != 0)
                    {
                        var allowedCities = Settings.SettingsProvider.AllowedCities.Where(t => checkedCities.Any(at => at.Contains(t.Name))).ToArray();
                        var allowedRegions = Settings.SettingsProvider.AllowedRegions.Where(r => allowedCities.Any(c => c.Id == r.FkCityId)).ToArray();
                        var regionMappings = Owner.Owner.Owner.ConvertionData.Where(cd => cd.PropertyId == "Region").Single();
                        regionMappings.MappingsTable.LoadAllowedValues(allowedRegions.Select(v => v.Name).ToArray());
                    }
                    else
                    {
                        var regionMappings = Owner.Owner.Owner.ConvertionData.Where(cd => cd.PropertyId == "Region").Single();
                        regionMappings.MappingsTable.LoadAllowedValues(Settings.SettingsProvider.AllowedRegions.Select(r => r.Name).ToArray());
                    }
                }
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Error
        {
            get { return this["To"]; }
        }

        public string this[string columnName]
        {
            get 
            {
                string msg = null;

                if (string.IsNullOrEmpty(To) || string.IsNullOrWhiteSpace(To))
                {
                    msg = "соответствие не может быть пустым";
                }
                else if (AllowedValues.Count > 0 && !AllowedValues.Contains(To))
                {
                    msg = "введено нестандартное значение";
                }
                return msg;
            }
        }

        public Mapping CopyFrom(Mapping source)
        {
            Owner = source.Owner;
            From = source.From;
            To = source.To;
            IsChecked = source.IsChecked;
            return this;
        }
    }

    [Serializable]
    public class MappingCommand : ICommand
    {

        private Action<object> _action;
        public MappingCommand(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action(parameter);
        }
    }
}
