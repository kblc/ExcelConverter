using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class ReplaceStringFunction : FunctionBase
    {
        public ReplaceStringFunction()
        {
            Name = "Замена подстроки в строке";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>
            {
                new Parameter{Name="Найти", ExpectedValueType = typeof(string)},
                new Parameter{Name="Заменить на", ExpectedValueType = typeof(string)}
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
            var find = Parameters.Where(p => p.Name == "Найти").Single().Value.ToString();
            var replace = Parameters.Where(p => p.Name == "Заменить на").Single().Value.ToString();

            return string.IsNullOrWhiteSpace(str) 
                ? string.Empty 
                : (string.IsNullOrEmpty(find) ? str : str.Replace(find, replace));
        }
    }
}
