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
    public class StringPositionFunction : FunctionBase
    {
        public StringPositionFunction()
        {
            Name = "Подстрока";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<Parameter>
            {
                new Parameter{Name="Начало", ExpectedValueType = typeof(int)},
                new Parameter{Name="Длинна", ExpectedValueType = typeof(int)}
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

            var startPos = Math.Max(int.Parse((string)Parameters.Where(p=>p.Name=="Начало").Single().Value), 1) - 1;
            var len = Math.Max(int.Parse((string)Parameters.Where(p => p.Name == "Длинна").Single().Value), (int)0);
            
            if (startPos < str.Length)
            {
                if (len > str.Length - startPos)
                    str = str.Substring(startPos); else
                    str = str.Substring(startPos, len);
            }

            return str;
        }
    }
}
