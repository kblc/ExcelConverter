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
    public class GetColorFunction : FunctionBase
    {
        public GetColorFunction()
        {
            Name = "Цвет";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>();
        }

        public override string Function(Dictionary<string, object> param)
        {
            var str = string.Empty;
            if (param.ContainsKey("value"))
            {
                //TODO: To HEX from ARGB
                //str = ((ExelCell)param["value"]).Color.ToString();
                str = HexConverter(((ExelCell)param["value"]).Color);
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

        private static String HexConverter(System.Drawing.Color c)
        {
            String rtn = String.Empty;
            try
            {
                rtn = "#" + c.A.ToString("X2") + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
            }
            catch
            {
                //doing nothing
            }

            return rtn;
        }
    }
}
