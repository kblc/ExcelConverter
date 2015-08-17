using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;
using Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class StringContainsFunction : FunctionBase
    {
        public static string FunctionName = "Строка содержит";

        public StringContainsFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<Parameter>
            {
                new Parameter{Name="Текст", ExpectedValueType = typeof(string)}
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
            var res = false;
            var str = GetStringValue(param) ?? string.Empty;
            var textPrm = Parameters.Where(p => p.Name == "Текст").Single();
            if (textPrm != null && !string.IsNullOrWhiteSpace(textPrm.StringValue))
                foreach(var prm in textPrm
                    .StringValue
                    .Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => Tag.ClearStringFromDoubleChars("*" + i.Replace(" ", "*") + "*", '*')))
                        res |= str.Like(prm);
            return res ? "+" : "-";
        }
    }
}
