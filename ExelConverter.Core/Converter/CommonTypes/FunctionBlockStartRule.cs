using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ExelConverter.Core.DataObjects;
using ExelConverter.Core.Converter.Functions;
using ExelConverter.Core.ExelDataReader;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("ExpectedValue")]
    public class ExpextedValue : ICopyFrom<ExpextedValue>
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }

        public ExpextedValue CopyFrom(ExpextedValue source)
        {
            this.Value = source.Value;
            return this;
        }
    }

    [Serializable]
    [XmlRoot("Condition")]
    [XmlInclude(typeof(ExpextedValue))]
    [XmlInclude(typeof(FunctionBase))]
    public class FunctionBlockStartRule : INotifyPropertyChanged, ICopyFrom<FunctionBlockStartRule>
    {
        public FunctionBlockStartRule() { }

        private Guid id = Guid.Empty;
        [XmlIgnore]
        public Guid Id 
        {
            get
            {
                return id == Guid.Empty ? (id = Guid.NewGuid()) : id;
            }
        }
       
        [NonSerialized]
        private ObservableCollection<FunctionBase> _supportedFunctions;
        [XmlIgnore]
        public ObservableCollection<FunctionBase> SupportedFunctions
        {
            get 
            {
                if (_supportedFunctions != null)
                    return _supportedFunctions;

                _supportedFunctions = FunctionBase.GetSupportedFunctions();
                if (_rule != null)
                {
                    var similaryItem = SupportedFunctions.Where(item => item.Name == _rule.Name).FirstOrDefault();
                    _supportedFunctions[SupportedFunctions.IndexOf(similaryItem)] = _rule;
                }

                return _supportedFunctions;
            }
        }

        private FunctionBase _rule;
        public FunctionBase Rule
        {
            get
            {
                return _rule ?? (_rule = SupportedFunctions.FirstOrDefault());
            }
            set
            {
                if (_rule != value)
                {
                    _rule = value;
                    if (SupportedFunctions.IndexOf(_rule) < 0)
                    {
                        var similaryItem = SupportedFunctions.Where(item => item.Name == _rule.Name).FirstOrDefault();
                        SupportedFunctions[SupportedFunctions.IndexOf(similaryItem)] = _rule;
                    }
                    RaisePropertyChanged("Rule");
                }
            }
        }

        private ObservableCollection<ExpextedValue> _expectedValues;
        [XmlArray("ExpectedValues")]
        public ObservableCollection<ExpextedValue> ExpectedValues
        {
            get { return _expectedValues ?? (_expectedValues = new ObservableCollection<ExpextedValue>()); }
            set
            {
                if (ExpectedValues == value)
                    return;

                ExpectedValues.Clear();
                if (value != null)
                    foreach (var b in value)
                        ExpectedValues.Add(b);

                RaisePropertyChanged("ExpectedValues");
            }
        }

        private bool _absoluteCoincidence;
        [XmlAttribute("AbsoluteCoincidence")]
        public bool AbsoluteCoincidence
        {
            get { return _absoluteCoincidence; }
            set
            {
                if (_absoluteCoincidence != value)
                {
                    _absoluteCoincidence = value;
                    RaisePropertyChanged("AbsoluteCoincidence");
                }
            }
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public FunctionBlockStartRule CopyFrom(FunctionBlockStartRule source)
        {
            this.AbsoluteCoincidence = source.AbsoluteCoincidence;
            ExpectedValues.Clear();
            foreach (var item in source.ExpectedValues)
                ExpectedValues.Add((new ExpextedValue()).CopyFrom(item));
            Rule = SupportedFunctions.Where( i=> i.Name == source.Rule.Name).FirstOrDefault();
            return this;
        }
    }
}
