using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ExelConverter.Core.Converter.CommonTypes;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class DirectValueFunction : FunctionBase
    {
        public DirectValueFunction()
        {
            SelectedParameter = FunctionParameters.CellName;
            Name = "Значение";
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
            
            return str;
        }
    }
}
