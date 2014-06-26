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
    public class GetFormatedValueFunction : FunctionBase
    {
        public static string FunctionName = "Форматированное значение";

        public GetFormatedValueFunction()
        {
            SelectedParameter = FunctionParameters.CellName;
            Name = FunctionName;
            Parameters = new ObservableCollection<Parameter>();
        }

        protected override ObservableCollection<FunctionParameters> GetAllowedParameters()
        {
            return new ObservableCollection<FunctionParameters>
            {
                FunctionParameters.CellName,
                FunctionParameters.CellNumber
            };
        }

        public override string Function(Dictionary<string, object> param)
        {
            var str = string.Empty;
            if(param.ContainsKey("value"))
            {
                str = ((ExelCell)param["value"]).FormatedValue;
            }
            return str;
        }
    }
}
