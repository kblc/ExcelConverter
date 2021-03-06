﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ExelConverter.Core.Settings;
using System.Xml.Serialization;

namespace ExelConverter.Core.DataObjects
{
    [Serializable]
    public class Operator : INotifyPropertyChanged
    {
        public Operator()
        {
            //MappingRules = new ObservableCollection<ExelConvertionRule>();
            //MappingRules.Add(new ExelConvertionRule { Name = "По умолчанию" });
            //MappingRule = MappingRules.First();
            IsNew = true;
        }

        [NonSerialized]
        private long _id;
        public long Id 
        {
            get { return _id; }
            set { _id = value; }
        }

        [NonSerialized]
        private bool _isNew;
        public bool IsNew 
        {
            get { return _isNew; }
            set { _isNew = value; } 
        }

        private string _name;
        public string Name 
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        private ExelConvertionRule _mappingRule;
        public ExelConvertionRule MappingRule 
        {
            get
            {
                if (_mappingRule == null || !MappingRules.Contains(_mappingRule))
                    _mappingRule = MappingRules.FirstOrDefault();
                return _mappingRule; 
            }
            set
            {
                if (_mappingRule != value && (value == null || MappingRules.Contains(value)))
                {
                    _mappingRule = value;
                    RaisePropertyChanged("MappingRule");
                    RaisePropertyChanged("ConvertionData");
                }
            }
        }

        private ObservableCollection<ExelConvertionRule> _mappingRules;
        [XmlArray(ElementName = "Rule")]
        [XmlArrayItem(ElementName = "Rule", Type = typeof(ExelConvertionRule))]
        public ObservableCollection<ExelConvertionRule> MappingRules 
        {
            get 
            {
                if (_mappingRules == null)
                {
                    _mappingRules = new ObservableCollection<ExelConvertionRule>();
                    _mappingRules.Add(new ExelConvertionRule { Name = ExelConvertionRule.DefaultName });
                    _mappingRules.CollectionChanged += (s, e) => { RaisePropertyChanged("MappingRule"); };
                }
                return _mappingRules;
            }
            set
            {
                MappingRuleSavedData.Clear();
                MappingRules.Clear();
                if (value != null && value.Count > 0)
                    foreach (var r in value)
                    {
                        //r.InitializeImageParsingData();
                        MappingRules.Add(r);
                    }
                    else
                        MappingRules.Add(new ExelConvertionRule { Name = ExelConvertionRule.DefaultName });

                foreach (var r in
                            MappingRules
                                .AsParallel()
                                .Select(r => new { Id = r.Id, Data = r.SerializeToBytes() })
                                .ToArray())
                    MappingRuleSavedData.Add(r.Id, r.Data);

                RaisePropertyChanged("MappingRules");
            }
        }

        [NonSerialized]
        private string notes = string.Empty;
        [field: NonSerialized]
        public string Notes { get { return notes; } set { if (notes == value) return; notes = value; RaisePropertyChanged(nameof(Notes)); RaisePropertyChanged(nameof(HasNotes)); } }

        [field: NonSerialized]
        public bool HasNotes { get { return !string.IsNullOrWhiteSpace(notes); } }
        
        [NonSerialized]
        private Dictionary<int, byte[]> mappingRuleSavedData = null;
        [field: NonSerialized]
        public Dictionary<int, byte[]> MappingRuleSavedData
        {
            get
            {
                return mappingRuleSavedData ?? (mappingRuleSavedData = new Dictionary<int, byte[]>());
            }
        }

        [NonSerialized]
        private User lockedBy = null;
        public User LockedBy
        {
            get
            {
                return lockedBy;
            }
            set
            {
                if (lockedBy != value)
                {
                    lockedBy = value;
                    RaisePropertyChanged("LockedBy");
                    RaisePropertyChanged("IsLocked");
                    RaisePropertyChanged("IsNotLocked");
                }
            }
        }

        public bool IsLocked { get { return lockedBy != null; } }
        public bool IsNotLocked { get { return !(lockedBy != null); } }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /*public string Serialize()
        {
            var stringOperator = string.Empty;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                var bytes = stream.ToArray();
                stringOperator = System.Convert.ToBase64String(bytes);
            }
            return stringOperator;
        }*/

        /*public void UpdateMappingTablesValues()
        {
            for (var i = 0; i < MappingRules.Count; i++)
            {
                for (var j = 0; j < MappingRules[i].ConvertionData.Count; j++)
                {
                    MappingRules[i].ConvertionData[j].UpdateFunctions(ExelConverter.Core.Converter.Functions.FunctionBase.GetSupportedFunctions());
                    if (MappingRules[i].ConvertionData[j].PropertyId == "Type")
                    {
                        MappingRules[i].ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedTypes.Select(t=>t.Name).ToArray());
                    }
                    if (MappingRules[i].ConvertionData[j].PropertyId == "Side")
                    {
                        MappingRules[i].ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedSides);
                    }
                    if (MappingRules[i].ConvertionData[j].PropertyId == "Size")
                    {
                        MappingRules[i].ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedSizes.Select.);
                    }
                    if (MappingRules[i].ConvertionData[j].PropertyId == "Region")
                    {
                        MappingRules[i].ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedRegions);
                    }
                    if (MappingRules[i].ConvertionData[j].PropertyId == "City")
                    {
                        MappingRules[i].ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedCities);
                    }
                }
            }
        }*/

        /*public static Operator Deserialize(string obj)
        {
            Operator result = null;
            using (var stream = new MemoryStream(System.Convert.FromBase64String(obj)))
            {
                var formatter = new BinaryFormatter();
                result = (Operator)formatter.Deserialize(stream);
            }
            result.UpdateMappingTablesValues();
            return result;
        }*/

    }
}
