using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.Converter.CommonTypes;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class GetRightCharFunction : FunctionBase
    {
        public GetRightCharFunction()
        {
            Name = "Строка справа";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<Parameter>
            {
                new Parameter{Name="Длинна", Value=0, ExpectedValueType = typeof(int)}
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
            var len = int.Parse((string)Parameters[0].Value);
            if (str.Length > len)
            {
                str = str.Substring(str.Length - len);
            }
            return str;
        }
    }
}
