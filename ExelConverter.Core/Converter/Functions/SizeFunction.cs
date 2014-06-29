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
    public class SizeFunction : FunctionBase
    {
        public SizeFunction()
        {
            Name = "Поиск размера";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new System.Collections.ObjectModel.ObservableCollection<CommonTypes.Parameter>();
            //StringFormat = Name + "()";
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

            var strData = str.Split(new char[] { ' ' });
            for (var i = 0; i < strData.Length; i++)
            {
                var strdata = strData[i];
                if (strdata.ToLower().Contains('x') || strdata.ToLower().Contains('х') || strdata.ToLower().Contains('*'))
                {
                    if (strdata.Length > 1)
                    {
                        str = strdata;
                    }
                    else
                    {
                        str = strData[i - 1] + strdata + strData[i + 1];
                    }
                    break;
                }
            }

            return str;
        }
    }
}
