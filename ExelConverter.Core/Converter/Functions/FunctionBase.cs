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
    [XmlInclude(typeof(GetFormatedValueFunction))]
    [XmlInclude(typeof(FindNumFunction))]
    [XmlInclude(typeof(StringReverseFunction))]
    [XmlInclude(typeof(ReplaceStringFunction))]
    [XmlInclude(typeof(FunctionBase))]
    [XmlInclude(typeof(Parameter))]
    [XmlInclude(typeof(FunctionParameters))]
    [XmlRoot("Function")]
    public abstract class FunctionBase : INotifyPropertyChanged
    {
        public FunctionBase() { }

        private ObservableCollection<Parameter> _parameters;
        [XmlArray("Parameters")]
        public ObservableCollection<Parameter> Parameters
        {
            get
            { 
                if (_parameters == null)
                {
                    _parameters = new ObservableCollection<Parameter>();
                    _parameters.CollectionChanged += _parameters_CollectionChanged;
                }
                return _parameters;
            }
            set
            {
                if (Parameters == value)
                    return;

                Parameters.Clear();
                if (value != null)
                    foreach (var b in value)
                        Parameters.Add(b);

                RaisePropertyChanged("Parameters");
            }
        }

        [NonSerialized]
        private bool supressChangeEvent = false;
        private void _parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (supressChangeEvent)
                return;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                supressChangeEvent = true;
                try
                {
                    foreach (var cd in
                            e.NewItems
                            .Cast<Parameter>()
                            .Select(nw => new { NewValue = nw, OldValue = Parameters.FirstOrDefault(cd => cd.Name == nw.Name && cd != nw) })
                            .Where(i => i.OldValue != null)
                            .ToArray())
                    {
                        var oldInd = Parameters.IndexOf(cd.OldValue);
                        Parameters.Remove(cd.OldValue);
                    }
                }
                finally
                {
                    supressChangeEvent = false;
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                supressChangeEvent = true;
                try
                {
                    foreach (var cd in e.OldItems.Cast<Parameter>())
                        Parameters.Add(cd);
                }
                finally
                {
                    supressChangeEvent = false;
                }
            }
        }

        private string _name;
        [XmlIgnore]
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                    RaisePropertyChanged("Description");
                }
            }
        }

        [XmlIgnore]
        public string Description
        {
            get { return Settings.SettingsProvider.FunctionDescriptions[Name]; }
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

        private string _stringFormat;
        [XmlIgnore]        
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

        [NonSerialized]
        private ObservableCollection<FunctionParameters> _allowedParameters;
        [XmlIgnore]
        public ObservableCollection<FunctionParameters> AllowedParameters
        {
            get 
            {
                return _allowedParameters ?? (_allowedParameters = GetAllowedParameters());
            }
        }

        protected abstract ObservableCollection<FunctionParameters> GetAllowedParameters();

        private FunctionParameters? _selectedParameter;
        [XmlIgnore]
        public FunctionParameters? SelectedParameter
        {
            get { return _selectedParameter; }
            set
            {
                if (_selectedParameter != value)
                {
                    _selectedParameter = value;
                    RaisePropertyChanged("SelectedParameter");
                    RaisePropertyChanged("SelectedParameterString");
                }
            }
        }

        [XmlAttribute("GetValuesBy")]
        public string SelectedParameterString
        {
            get { return SelectedParameter == null ? string.Empty : SelectedParameter.ToString(); }
            set 
            {
                foreach(var i in typeof(FunctionParameters).GetEnumValues())
                    if (i.ToString() == value)
                    {
                        SelectedParameter = (FunctionParameters)i;
                        break;
                    }
            }
        }

        private int _columnNumber;
        [XmlAttribute("ColumnNumber")]
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
        [XmlAttribute("ColumnName")]
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
                new GetFormatedValueFunction(),
                new FindNumFunction(),
                new StringReverseFunction(),
                new ReplaceStringFunction()
            };
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
