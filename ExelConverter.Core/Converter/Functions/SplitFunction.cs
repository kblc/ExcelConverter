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
    public class SplitFunction : FunctionBase
    {
        public SplitFunction()
        {
            Name = "Разбиение строки";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>
            {
                new Parameter{Name="Разделитель", ExpectedValueType = typeof(string)},
                new Parameter{Name="Индексы", ExpectedValueType = typeof(int), ParsingExpected = true}
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
            var indexes = Parameters.Where(p => p.Name == "Индексы").Single().Value.ToString().Split(new char[]{'|'});
            var separator = Parameters.Where(p => p.Name == "Разделитель").Single().Value.ToString();

            var values = str.Split(new string[] { separator }, StringSplitOptions.None);

            var result = string.Empty;
            foreach (var index in indexes)
            {
                var i = int.Parse(index) - 1;
                if (values.Length > i)
                {
                    result += values[i];
                }
            }
            str = result;
            
            return str;
        }
    }
}
