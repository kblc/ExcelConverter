﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("Parameter")]
    public class Parameter : INotifyPropertyChanged, IDataErrorInfo
    {
        public Parameter()
        {
            Value = string.Empty;
        }

        [XmlIgnore]
        public Type ExpectedValueType { get; set; }

        [XmlAttribute("ExpectedValueType")]
        public string ExpectedValueTypeString { get { return ExpectedValueType != null ? ExpectedValueType.ToString() : string.Empty; } }

        [XmlAttribute("ParsingExpected")]
        public bool ParsingExpected { get; set; }

        private string _name;
        [XmlAttribute("Name")]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        private object _value;
        [XmlIgnore]
        public object Value 
        {
            get { return _value; }
            set
            {
                _value = value;
                RaisePropertyChanged("Value");
                RaisePropertyChanged("Error");
                RaisePropertyChanged("StringValue");
            }
        }

        [XmlAttribute("Value")]
        public string StringValue
        {
            get
            {
                return Value == null ? string.Empty : Value.ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    Value = null; else
                    Value = Convert.ChangeType(value, ExpectedValueType ?? typeof(string));
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
            get { return this["Value"]; }
        }

        [XmlIgnore]
        public string this[string columnName]
        {
            get 
            {
                string msg = null;
                if (columnName == "Value")
                {
                    if (Value == null)
                    {
                        msg = "поле не может быть пустым";
                        return msg;
                    }
                    if (ParsingExpected)
                    {
                        if (string.IsNullOrEmpty((string)Value) || string.IsNullOrWhiteSpace((string)Value))
                        {
                            msg = "строка не может быть пустой";
                            return msg;
                        }

                        string[] values = null;
                        try
                        {
                            values = ((string)Value).Split(new char[] { ',' });
                        }
                        catch
                        {
                            msg = "невозможно распознать значения";
                            return msg;
                        }
                        if (values.Length == 0)
                        {
                            msg = "невозможно распознать значения";
                            return msg;
                        }
                        if (values.Any(s => string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)))
                        {
                            msg = "одно или несколько значений пусты";
                            return msg;
                        }
                        if (ExpectedValueType == typeof(int))
                        {
                            try
                            {
                                foreach (var value in values)
                                {
                                    int.Parse(value);
                                }
                                return null;
                            }
                            catch
                            {
                                msg = "невозможно распознать одно или несколько значений";
                                return msg;
                            }
                        }
                    }
                    if (ExpectedValueType == typeof(int))
                    {
                        if (string.IsNullOrEmpty(Value.ToString()) || string.IsNullOrWhiteSpace(Value.ToString()))
                        {
                            msg = "поле не може быть пустым";
                            return msg;
                        }
                        int o;
                        if (!int.TryParse((string)Value, out o))
                        {
                            msg = "введённый текст не является числом";
                            return msg;
                        }
                    }
                    else if (ExpectedValueType == typeof(string))
                    {
                        if (Value == null)
                        {
                            msg = "поле не може быть пустым";
                            return msg;
                        }
                    }
                }
                return msg;
            }
        }
    }
}
