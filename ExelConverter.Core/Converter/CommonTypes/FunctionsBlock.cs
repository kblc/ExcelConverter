using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;

using ExelConverter.Core.DataObjects;
using ExelConverter.Core.Converter.Functions;
using ExelConverter.Core.ExelDataReader;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("FunctionContainer")]
    public class FunctionContainer : ICopyFrom<FunctionContainer>
    {
        public FunctionContainer() { }

        [NonSerialized]
        private Guid id = Guid.Empty;
        [XmlIgnore]
        public Guid Id
        {
            get
            {
                return id == Guid.Empty ? id = Guid.NewGuid() : id;
            }
        }

        private FunctionBase _function;

        [XmlElement("Function")]
        [BindableAttribute(true)]
        public FunctionBase Function 
        {
            get 
            {
                return _function ?? (_function = SupportedFunctions.FirstOrDefault());
            }
            set
            {
                if (_function != value)
                {
                    _function = value;
                    if (SupportedFunctions.IndexOf(_function) < 0)
                    {
                        var similaryItem = SupportedFunctions.Where(item => item.Name == _function.Name).FirstOrDefault();
                        SupportedFunctions[SupportedFunctions.IndexOf(similaryItem)] = _function;
                    }
                }
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
                if (_function != null)
                {
                    var similaryItem = SupportedFunctions.Where(item => item.Name == _function.Name).FirstOrDefault();
                    _supportedFunctions[SupportedFunctions.IndexOf(similaryItem)] = _function;
                }

                return _supportedFunctions;
            }
        }

        public FunctionContainer CopyFrom(FunctionContainer source)
        {
            Function = SupportedFunctions.Where(i=> i.Name == source.Function.Name).FirstOrDefault();
            return this;
        }
    }

    [Serializable]
    [XmlRoot("FunctionalBlock")]
    [XmlInclude(typeof(FunctionBlockStartRule))]
    [XmlInclude(typeof(FunctionContainer))]
    public class FunctionsBlock : INotifyPropertyChanged, ICopyFrom<FunctionsBlock>
    {
        public FunctionsBlock() { }

        [NonSerialized]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        private ICommand deleteFunctionCommand = null;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ICommand DeleteFunctionCommand 
        {
            get
            {
                return deleteFunctionCommand ?? (deleteFunctionCommand = new FunctionBlockCommand(DeleteFunction));
            }
        }
        private void DeleteFunction(Guid id)
        {
            UsedFunctions.Remove(UsedFunctions.Where(uf => uf.Id == id).Single());
        }

        [NonSerialized]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        private ICommand deleteStartRuleCommand = null;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ICommand DeleteStartRuleCommand
        {
            get
            {
                return deleteStartRuleCommand ?? (deleteStartRuleCommand =  new FunctionBlockCommand(DeleteStartRule));
            }
        }
        private void DeleteStartRule(Guid id)
        {
            StartRules.Remove(StartRules.Where(sr => sr.Id == id).Single());
        }

        [NonSerialized]
        private Guid id = Guid.Empty;
        [XmlIgnore]
        public Guid Id 
        {
            get
            {
                return id == Guid.Empty ? (id = Guid.NewGuid()) : id;
            }
        }

        private bool _returnAfterExecute;
        [XmlAttribute("ReturnAfterExecute")]
        public bool ReturnAfterExecute
        {
            get { return _returnAfterExecute; }
            set
            {
                if (_returnAfterExecute != value)
                {
                    _returnAfterExecute = value;
                    RaisePropertyChanged("ReturnAfterExecute");
                }
            }
        }

        public void UpdateFunctionsList()
        {
            var functions = FunctionBase.GetSupportedFunctions();
            foreach (var func in functions)
            {
                foreach (var updFunc in UsedFunctions)
                {
                    if (!updFunc.SupportedFunctions.Any(f => f.Name == func.Name))
                    {
                        updFunc.SupportedFunctions.Add(func);
                    }
                }
                foreach (var updRule in StartRules)
                {
                    if (!updRule.SupportedFunctions.Any(f => f.Name == func.Name))
                    {
                        updRule.SupportedFunctions.Add(func);
                    }
                }
            }
        }

        private bool _isAllStartRulesNeeded;
        [XmlAttribute("CheckAllConditions")]
        public bool IsAllStartRulesNeeded
        {
            get { return _isAllStartRulesNeeded; }
            set
            {
                if (_isAllStartRulesNeeded != value)
                {
                    _isAllStartRulesNeeded = value;
                    RaisePropertyChanged("IsAllStartRulesNeeded");
                }
            }
        }

        private ObservableCollection<FunctionBlockStartRule> _startRules = null;
        [XmlArray("Conditions")]
        public ObservableCollection<FunctionBlockStartRule> StartRules
        {
            get { return _startRules ?? (_startRules = new ObservableCollection<FunctionBlockStartRule>()); }
            set
            {
                if (StartRules == value)
                    return;

                StartRules.Clear();
                if (value != null)
                    foreach (var b in value)
                        StartRules.Add(b);

                RaisePropertyChanged("StartRules");
            }
        }

        private ObservableCollection<FunctionContainer> _usedFunctions = null;
        [XmlArray("Functions")]
        public ObservableCollection<FunctionContainer> UsedFunctions
        {
            get { return _usedFunctions ?? (_usedFunctions = new ObservableCollection<FunctionContainer>()); }
            set
            {
                if (UsedFunctions == value)
                    return;

                UsedFunctions.Clear();
                if (value != null)
                    foreach (var b in value)
                        UsedFunctions.Add(b);

                RaisePropertyChanged("UsedFunctions");
            }
        }

        #region Executing

        public bool CheckCanExecute(ExelSheet sheet, int rowNumber, FieldConvertionData convertionData)
        {
            var result = IsAllStartRulesNeeded || StartRules.Count == 0;
            foreach (var rule in StartRules)
            {
                var parameters = new Dictionary<string, object>();
                var cellResultContent = string.Empty;
                if (convertionData.MappingNeeded)
                {
                    parameters.Add("mappings", convertionData.MappingsTable);
                }
                if (rule.Rule.SelectedParameter == FunctionParameters.CellName)
                {
                    if (sheet.MainHeader != null)
                    {
                        var header = sheet.Rows[sheet.Rows.IndexOf(sheet.MainHeader)].Cells.Select(c => c.Value).ToList();
                        var columnNumber = header.IndexOf(header.Where(s => s.Trim().ToLower() == rule.Rule.ColumnName.Trim().ToLower()).FirstOrDefault());
                        if (columnNumber >= 0 && sheet.Rows.ElementAt(rowNumber).Cells.Count > columnNumber)
                        {
                            parameters.Add("value", sheet.Rows.ElementAt(rowNumber).Cells.ElementAt(columnNumber));
                        }
                    }
                }
                else if (rule.Rule.SelectedParameter == FunctionParameters.CellNumber)
                {   
                    if (sheet.Rows.ElementAt(rowNumber).Cells.Count > rule.Rule.ColumnNumber)
                    {
                        parameters.Add("value", sheet.Rows.ElementAt(rowNumber).Cells.ElementAt(rule.Rule.ColumnNumber));
                    }
                }
                else
                {
                    parameters.Add("sheet", sheet);
                    parameters.Add("row", rowNumber);
                }
                cellResultContent = rule.Rule.Function(parameters);
                bool subResult = false;
                if (rule.AbsoluteCoincidence)
                {
                    subResult = 
                        rule
                        .ExpectedValues
                        .Any(s => 
                                s != null 
                                && 
                                (
                                    s.Value == cellResultContent 
                                    || string.IsNullOrWhiteSpace(s.Value) == string.IsNullOrWhiteSpace(cellResultContent) == true
                                )
                            );
                }
                else
                {
                    subResult = 
                        rule
                        .ExpectedValues
                        .Any(s => 
                                s != null 
                                && s.Value != null 
                                && cellResultContent.ToLower().Contains(s.Value.ToLower()
                        )
                    );
                }
                if (IsAllStartRulesNeeded)
                    result &= subResult;
                else
                    result |= subResult;

                if ((!IsAllStartRulesNeeded && result) || (IsAllStartRulesNeeded && !result))
                {
                    return result;
                }
            }
            return result;
        }

        public string Execute(ExelSheet sheet, int rowNumber, FieldConvertionData convertionData)
        {
            var result = string.Empty;
            var tempResults = new List<string>() { "" };
            foreach (var function in UsedFunctions)
            {
                var parameters = new Dictionary<string, object>();
                if (convertionData.MappingNeeded)
                {
                    parameters.Add("mappings", convertionData.MappingsTable);
                }

                if (function.Function.SelectedParameter == FunctionParameters.PrewFunction)
                {
                    parameters.Add("string", tempResults.LastOrDefault());
                }
                else if (function.Function.SelectedParameter == FunctionParameters.CellName)
                {
                    if (sheet.MainHeader != null)
                    {
                        var header = sheet.Rows[sheet.Rows.IndexOf(sheet.MainHeader)].Cells.Select(c => c.Value).ToList();
                        var columnNumber = header.IndexOf(header.Where(s => s.Trim().ToLower() == (function.Function.ColumnName ?? string.Empty).Trim().ToLower()).FirstOrDefault());
                        if (columnNumber >= 0 && sheet.Rows.ElementAt(rowNumber).Cells.Count > columnNumber)
                        {
                            parameters.Add("value", sheet.Rows.ElementAt(rowNumber).Cells.ElementAt(columnNumber));
                        }
                    }
                }
                else if (function.Function.SelectedParameter == FunctionParameters.CellNumber)
                {
                    if (sheet.Rows.ElementAt(rowNumber).Cells.Count > function.Function.ColumnNumber)
                    {
                        parameters.Add("value", sheet.Rows.ElementAt(rowNumber).Cells.ElementAt(function.Function.ColumnNumber));
                    }
                }
                else
                {
                    parameters.Add("sheet", sheet);
                    parameters.Add("row", rowNumber);
                }
                if (function.Function.SelectedParameter == FunctionParameters.PrewFunction)
                {
                    tempResults[tempResults.IndexOf(tempResults.LastOrDefault())] = function.Function.Function(parameters);
                }
                else
                {
                    tempResults.Add(function.Function.Function(parameters));
                }
            }
            foreach (var res in tempResults)
            {
                result += res;
            }
            return result;
        }

        #endregion
        #region INotifyPropertyChanged

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
        public FunctionsBlock CopyFrom(FunctionsBlock source)
        {
            ReturnAfterExecute = source.ReturnAfterExecute;
            IsAllStartRulesNeeded = source.IsAllStartRulesNeeded;

            UsedFunctions.Clear();
            foreach (var item in source.UsedFunctions)
                UsedFunctions.Add( (new FunctionContainer()).CopyFrom(item) );

            StartRules.Clear();
            foreach (var item in source.StartRules)
                StartRules.Add((new FunctionBlockStartRule()).CopyFrom(item));

            return this;
        }
    }

    [Serializable]
    public class FunctionBlockCommand : ICommand 
    {
        private Action<Guid> _action;

        public FunctionBlockCommand(Action<Guid> action)
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
            _action((Guid)parameter);
        }
    }


}
