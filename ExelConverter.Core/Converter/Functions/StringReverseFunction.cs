using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class StringReverseFunction : FunctionBase
    {
        public StringReverseFunction()
        {
            Name = "Реверс строки";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<CommonTypes.Parameter>();
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

            var revesedString = str.Reverse();
            var result = string.Empty;
            foreach(var c in revesedString)
            {
                result += c;
            }
            return result;
        }
    }
}
