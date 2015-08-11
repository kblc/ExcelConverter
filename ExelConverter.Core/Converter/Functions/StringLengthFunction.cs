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
    public class StringLengthFunction : FunctionBase
    {
        public static string FunctionName = "Длинна строки";

        public StringLengthFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<Parameter>
            {
                new Parameter{Name="Больше", ExpectedValueType = typeof(int)},
                new Parameter{Name="Меньше", ExpectedValueType = typeof(int)},
                new Parameter{Name="Равно",  ExpectedValueType = typeof(int)},
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
            var res = true;
            var str = GetStringValue(param) ?? string.Empty;

            var morePrm = Parameters.Where(p => p.Name == "Больше").Single();
            if (morePrm != null && !string.IsNullOrWhiteSpace(morePrm.StringValue))
                res = res & (str.Length > int.Parse(morePrm.StringValue));

            var lessPrm = Parameters.Where(p => p.Name == "Меньше").Single();
            if (lessPrm != null && !string.IsNullOrWhiteSpace(lessPrm.StringValue))
                res = res & (str.Length < int.Parse(lessPrm.StringValue));

            var quaPrm = Parameters.Where(p => p.Name == "Равно").Single();
            if (lessPrm != null && !string.IsNullOrWhiteSpace(quaPrm.StringValue))
                res = res & (str.Length == int.Parse(quaPrm.StringValue));

            return res ? "+" : "-";
        }
    }
}
