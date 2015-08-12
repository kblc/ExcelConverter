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
    public class GetCommentFunction : FunctionBase
    {
        public static string FunctionName = "Комментарий из ячейки";

        public GetCommentFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>();
        }

        public override string Function(Dictionary<string, object> param)
        {
            var str = string.Empty;
            if (param.ContainsKey("value"))
            {
                var val = param["value"] as ExelCell;
                if (val != null)
                    str = val.Comment;
            }
            return str;
        }

        protected override ObservableCollection<FunctionParameters> GetAllowedParameters()
        {
            return new ObservableCollection<FunctionParameters>
            {
                FunctionParameters.CellName,
                FunctionParameters.CellNumber
            };
        }
    }
}
