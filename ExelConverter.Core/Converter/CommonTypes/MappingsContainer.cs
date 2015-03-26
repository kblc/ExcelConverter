using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Controls;
using ExelConverter.Core.DataObjects;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("MappingContainer")]
    public class MappingsContainer : ObservableCollection<Mapping>, INotifyPropertyChanged, ICopyFrom<MappingsContainer>
    {
        public MappingsContainer() : base() { }

        protected override void InsertItem(int index, Mapping item)
        {
            //item.AllowedValues = AllowedValues;
            item.Owner = this;
            base.InsertItem(index, item);       
        }

        [System.Xml.Serialization.XmlIgnore]
        public FieldConvertionData Owner { get; set; }

        private ObservableCollection<string> _allowedValues;
        [XmlIgnore]
        public ObservableCollection<string> AllowedValues
        {
            get { return _allowedValues ?? (_allowedValues = new ObservableCollection<string>()); }
            set
            {
                if (_allowedValues == value)
                    return;

                _allowedValues.Clear();
                if (value != null)
                    foreach (var v in value)
                        _allowedValues.Add(v);
                RaisePropertyChanged("AllowedValues");
            }
        }

        private bool _absoluteCoincidence;
        [XmlAttribute("AbsoluteCoincidence")]
        public bool AbsoluteCoincidence
        {
            get { return Owner == null ? _absoluteCoincidence : Owner.AbsoluteCoincidence; }
            set
            {
                if (Owner == null)
                { 
                    if (_absoluteCoincidence != value)
                    {
                        _absoluteCoincidence = value;
                        RaisePropertyChanged("AbsoluteCoincidence");
                    }
                } else
                {
                    Owner.AbsoluteCoincidence = value;
                    RaisePropertyChanged("AbsoluteCoincidence");
                }
            }
        }

        public void LoadAllowedValues(string[] values)
        {
            if (values == null)
            {
                return;
            }
            AllowedValues.Clear();
            if (Owner.FieldName == "Размер")
            {
                foreach (var value in values)
                {
                    var sizes = Settings.SettingsProvider.AllowedSizes.Where(s => s.Name == value).ToArray();
                    foreach (var size in sizes)
                    {
                        var type = Settings.SettingsProvider.AllowedTypes.Where(t => t.Id == size.FkTypeId).FirstOrDefault();
                        if (type != null)
                            AllowedValues.Add(value + " (" + type.Name + ")");
                    }
                }
            }
            else if (Owner.FieldName == "Район")
            {
                List<string> regionsAdded = new List<string>();

                var cities = Settings.SettingsProvider.AllowedCities.OrderBy(item => item.Name).ToArray();
                var regionsForCities = Settings.SettingsProvider.AllowedRegions.Where(r => values.Contains(r.Name) && cities.Any(city => r.FkCityId == city.Id)).ToArray();

                foreach (var city in cities)
                {
                    var regions = regionsForCities.Where(r => r.FkCityId == city.Id).ToArray();
                    foreach (var region in regions)
                    {
                        regionsAdded.Add(region.Name);
                        AllowedValues.Add(region.Name + " (" + city.Name + ")");
                    }
                }

                foreach (var value in values.Where(val => !regionsAdded.Contains(val) ))
                {
                    AllowedValues.Add(value);
                }
                //foreach (var value in values)
                //{
                //    var regions = Settings.SettingsProvider.AllowedRegions.Where(r => r.Name == value).ToArray();
                //    foreach (var region in regions)
                //    {
                //        var city = Settings.SettingsProvider.AllowedCities.Where(c => c.Id == region.FkCityId).FirstOrDefault();
                //        AllowedValues.Add(value + " (" + city.Name + ")");
                //    }
                //}
            }
            else
            {
                foreach (var value in values)
                {
                    AllowedValues.Add(value);
                }
            }
            
        }

        //[field: NonSerialized()]
        //public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

            //if (PropertyChanged != null)
            //{
            //    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //}
        }

        public MappingsContainer CopyFrom(MappingsContainer source)
        {
            this.Owner = source.Owner;
            this.AbsoluteCoincidence = source.AbsoluteCoincidence;

            this.Clear();
            foreach (var item in source)            
                Add((new Mapping()).CopyFrom(item));

            AllowedValues.Clear();
            foreach (var item in source.AllowedValues)
                AllowedValues.Add(item);

            return this;
        }
    }
}
