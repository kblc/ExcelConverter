using ExelConverter.Core.Converter.CommonTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class FindNumFunction : FunctionBase
    {
        public FindNumFunction()
        {
            Name = "Поиск числа";
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
            try
            {
                str = double.Parse(str).ToString();
            }
            catch
            {
                str = Parameters[0].Value.ToString();
            }
            return str;
        }
    }
}
