using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class FindStringFunction : FunctionBase
    {
        public FindStringFunction()
        {
            Name = "Поиск значений";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<CommonTypes.Parameter>
            {
                new Parameter{Name="Значения", ExpectedValueType = typeof(string), ParsingExpected = true},
                new Parameter{Name="По умолчанию", ExpectedValueType = typeof(string)}
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
            if (str.Length > 0)
            {
                var flag = false;
                var values = ((string)Parameters.Where(p => p.Name == "Значения").Single().Value).Split(new char[]{'|'});
                var defaultValue = (string)Parameters.Where(p => p.Name == "По умолчанию").Single().Value;

                foreach (var value in values)
                {
                    str = CheckValue(str, values);
                    if (str != null)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    str = defaultValue;
                }

            }
            else
            {
                str = "";
            }
            return str;
        }

        private string CheckValue(string value, string[] values)
        {
            foreach (var mapping in values)
            {
                if (value != null && value.ToLower().Trim().StartsWith(mapping.ToLower().Trim()))
                {
                    return mapping;
                }
            }
            if (value == null || value.Length <= 1)
            {
                return null;
            }
            return CheckValue(value.Substring(1), values);
        }
    }
}
