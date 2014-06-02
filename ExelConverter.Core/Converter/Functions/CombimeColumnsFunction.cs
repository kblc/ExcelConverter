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
    public class CombimeColumnsFunction : FunctionBase
    {
        public CombimeColumnsFunction()
        {
            Name = "Склеить столбцы";
            StringFormat = Name + "()";
            SelectedParameter = FunctionParameters.Sheet;
            Parameters = new ObservableCollection<Parameter>
            {
                new Parameter{Name="Названия", ExpectedValueType = typeof(string), ParsingExpected = true},
                new Parameter{Name="Разделитель", ExpectedValueType = typeof(string)}
            };
        }

        protected override ObservableCollection<FunctionParameters> GetAllowedParameters()
        {
            return null;
        }

        public override string Function(Dictionary<string, object> param)
        {
            var result = string.Empty;
            /*var index = 0;
            ExelSheet sheet = null;
            if (param.ContainsKey("sheet"))
            {
                sheet = ((ExelSheet)param["sheet"]);
            }
            if (param.ContainsKey("row"))
            {
                index = (int)param["row"];
            }
            

            var names = Parameters[0].Value.ToString().Split(new char[]{'|'});
            var header = sheet.Rows.ElementAt(sheet.HeaderRowNumber).Cells.Select(c=>((ExelCell)c).Value).ToList();
            var separator = Parameters[1].Value.ToString();
            var indexes = new List<int>();
            foreach(var name in names)
            {
                indexes.Add(header.IndexOf(name));
            }

            foreach (var i in indexes)
            {
                if (i >= 0 && i < indexes.Count)
                {
                    result += i != indexes.First() ? separator + ((ExelCell)sheet.Rows.ElementAt(index).Cells.ElementAt(i)).Value 
                        : ((ExelCell)sheet.Rows.ElementAt(index).Cells.ElementAt(i)).Value;
                }
            }*/

            
            return result;
        }
    }
}
