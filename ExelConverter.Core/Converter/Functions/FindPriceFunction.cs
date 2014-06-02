using ExelConverter.Core.Converter.CommonTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.Converter.Functions
{
    public class FindPriceFunction : FunctionBase
    {
        public FindPriceFunction()
        {
            Name = "Поиск цены";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>
            {
                new Parameter{Name="По умолчанию", ExpectedValueType = typeof(int)}
            };
        }

        protected override ObservableCollection<FunctionParameters> GetAllowedParameters()
        {
            return new ObservableCollection<FunctionParameters>
            {
                FunctionParameters.CellName,
                FunctionParameters.CellNumber,
                FunctionParameters.Header,
                FunctionParameters.PrewFunction,
                FunctionParameters.Sheet,
                FunctionParameters.Subheader
            };
        }

        public override string Function(Dictionary<string, object> param)
        {
            var str = GetStringValue(param);

            return str;
        }
    }
}
