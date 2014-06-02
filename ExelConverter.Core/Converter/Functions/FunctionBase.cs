using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public enum FunctionParameters
    {
        CellName,
        CellNumber,
        Header,
        Subheader,
        Sheet,
        PrewFunction
    }

    [Serializable]
    [XmlInclude(typeof(DefaultValueFunction))]
    [XmlInclude(typeof(DirectValueFunction))]
    [XmlInclude(typeof(CutRightAndLeftCharsFunction))]
    [XmlInclude(typeof(StringPositionFunction))]
    [XmlInclude(typeof(GetLeftCharFunction))]
    [XmlInclude(typeof(GetRightCharFunction))]
    [XmlInclude(typeof(FindStringFunction))]
    [XmlInclude(typeof(SizeFunction))]
    [XmlInclude(typeof(SplitFunction))]
    [XmlInclude(typeof(AddTextFunction))]
    [XmlInclude(typeof(GetColorFunction))]
    [XmlInclude(typeof(GetHyperlinkFunction))]
    [XmlInclude(typeof(FindNumFunction))]
    [XmlInclude(typeof(StringReverseFunction))]
    [XmlInclude(typeof(ReplaceStringFunction))]
    [XmlInclude(typeof(FunctionBase))]
    public abstract class FunctionBase : INotifyPropertyChanged
    {
        public FunctionBase()
        {
            
        }

        private ObservableCollection<Parameter> _parameters;
        public ObservableCollection<Parameter> Parameters
        {
            get { return _parameters; }
            set
            {
                if (_parameters != value)
                {
                    _parameters = value;
                    RaisePropertyChanged("Parameters");
                }
            }
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

        public string Description
        {
            get { return Settings.SettingsProvider.FunctionDescriptions[Name]; }
        }

        private bool _absoluteCoincidence;
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

        private string _stringFormat;
        public string StringFormat
        {
            get { return _stringFormat; }
            set
            {
                if (_stringFormat != value)
                {
                    _stringFormat = value;
                    RaisePropertyChanged("StringFormat");
                }
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        [NonSerialized]
        private ObservableCollection<FunctionParameters> _allowedParameters;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ObservableCollection<FunctionParameters> AllowedParameters
        {
            get 
            {
                return _allowedParameters ?? (_allowedParameters = GetAllowedParameters());
            }
            //set
            //{
            //    if (_allowedParameters != value)
            //    {
            //        _allowedParameters = value;
            //        RaisePropertyChanged("AllowedParameters");
            //    }
            //}
        }

        protected abstract ObservableCollection<FunctionParameters> GetAllowedParameters();

        private FunctionParameters? _selectedParameter;
        public FunctionParameters? SelectedParameter
        {
            get { return _selectedParameter; }
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    RaisePropertyChanged("SelectedParameter");
                }
            }
        }

        private int _columnNumber;
        public int ColumnNumber
        {
            get { return _columnNumber; }
            set
            {
                if (_columnNumber != value)
                {
                    _columnNumber = value;
                    RaisePropertyChanged("ColumnNumber");
                }
            }
        }

        private string _columnName;
        public string ColumnName
        {
            get
            {
                return _columnName; 
            }
            set
            {
                _columnName = value;
                RaisePropertyChanged("ColumnName");
            }
        }

        protected string GetNearestHeader(ExelSheet sheet, int rowIndex)
        {
            var result = string.Empty;
            if (sheet.SheetHeaders.Headers.Count > 0)
            {
                var header = sheet.SheetHeaders.Headers.Where(h => h.RowNumber <= rowIndex).LastOrDefault();
                if (header != null)
                {
                    result = header.Header;
                }
            }
            return result;
        }

        protected string GetNearestSubheader(ExelSheet sheet, int rowIndex)
        {
            var result = string.Empty;
            if (sheet.SheetHeaders.Subheaders.Count > 0)
            {
                var header = sheet.SheetHeaders.Subheaders.Where(h => h.RowNumber <= rowIndex).LastOrDefault();
                if (header != null)
                {
                    result = header.Header;
                }
            }
            return result;
        }

        protected string GetStringValue(Dictionary<string, object> param)
        {
            var str = string.Empty;
            if (param.ContainsKey("sheet") && param.ContainsKey("row"))
            {
                var sheet = (ExelSheet)param["sheet"];
                var row = (int)param["row"];
                if (SelectedParameter == FunctionParameters.Header)
                {
                    str = GetNearestHeader(sheet, row);
                }
                else if (SelectedParameter == FunctionParameters.Subheader)
                {
                    str = GetNearestSubheader(sheet, row);
                }
                else
                {
                    str = ((ExelSheet)param["sheet"]).Name;
                }
            }
            else if (param.ContainsKey("value"))
            {
                str = ((ExelCell)param["value"]).Value;
            }
            else if (param.ContainsKey("string"))
            {
                str = (string)param["string"];
            }
            return str;
        }

        public virtual string Function(Dictionary<string, object> param)
        {
            throw new NotImplementedException();
        }


        public static ObservableCollection<FunctionBase> GetSupportedFunctions()
        {
            return new ObservableCollection<FunctionBase>
            {
                new DefaultValueFunction(),
                new DirectValueFunction(),
                new CutRightAndLeftCharsFunction(),
                new StringPositionFunction(),
                new GetLeftCharFunction(),
                new GetRightCharFunction(),
                new FindStringFunction(),
                new SizeFunction(),
                new SplitFunction(),
                new AddTextFunction(),
                new GetColorFunction(),
                new GetHyperlinkFunction(),
                new FindNumFunction(),
                new StringReverseFunction(),
                new ReplaceStringFunction()
            };
        }

        public void UpdateSupportedFunctions()
        {

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
    }
}
