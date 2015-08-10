using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class CutRightAndLeftCharsFunction : FunctionBase
    {
        public CutRightAndLeftCharsFunction()
        {
            Name = "Обрезать";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<CommonTypes.Parameter>
            {
                new Parameter{Name="Слева", Value="0", ExpectedValueType = typeof(int)},
                new Parameter{Name="Справа", Value="0", ExpectedValueType = typeof(int)}
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
                var left = int.Parse((string)Parameters.Where(p => p.Name == "Слева").Single().Value);
                var right = int.Parse((string)Parameters.Where(p => p.Name == "Справа").Single().Value);
                if (str.Length > left + right)
                {
                    if (right > 0)
                        str = str.Remove(str.Length - right);

                    if (left > 0)
                        str = str.Remove(0, left);
                }
                else
                    str = string.Empty;
            }
            else
            {
                str = string.Empty;
            }
            return str;
        }
    }
}
