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
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("Mapping")]
    public class Mapping : INotifyPropertyChanged, IDataErrorInfo, ICopyFrom<Mapping>
    {
        public Mapping() { }

        [XmlIgnore]
        public MappingsContainer Owner { get; set; }

        private ObservableCollection<string> _allowedValues;
        [XmlIgnore]
        public ObservableCollection<string> AllowedValues
        {
            get
            {
                return Owner.AllowedValues;
                //return _allowedValues ?? (_allowedValues = new ObservableCollection<string>());
            }
            //set
            //{
            //    if (_allowedValues == value)
            //        return;

            //    _allowedValues.Clear();
            //    if (value != null)
            //        foreach (var v in value)
            //            _allowedValues.Add(v);
            //    RaisePropertyChanged("AllowedValues");
            //}
        }

        private string _from = string.Empty;
        [XmlAttribute("From")]
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
        [XmlAttribute("IsChecked")]
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
        [XmlAttribute("To")]
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

        [XmlIgnore]
        public string Error
        {
            get { return this["To"]; }
        }

        [XmlIgnore]
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
}
