using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.ExelDataReader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Converter.Functions
{
    [Serializable]
    public class AddTextFunction : FunctionBase, IDataErrorInfo
    {
        public AddTextFunction()
        {
            Name = "Добавить текст";
            SelectedParameter = FunctionParameters.CellName;
            Parameters = new ObservableCollection<Parameter>()
            {
                new Parameter{ Name = "Текст слева", ExpectedValueType = typeof(string) },
                new Parameter{ Name = "Текст справа", ExpectedValueType = typeof(string) },
                new Parameter{ Name = "Разделитель", ExpectedValueType = typeof(string) }
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

            var leftText = (string)Parameters[0].Value;
            var sep = (string)Parameters[2].Value;
            var rightText = (string)Parameters[1].Value;

            if (!String.IsNullOrEmpty(leftText))
            {
                str = leftText + sep + str;
            }
            if (!String.IsNullOrEmpty(rightText))
            {
                str += sep + rightText;
            }

            return str;
        }

        public string Error
        {
            get { return null; }
        }

        public string this[string columnName]
        {
            get 
            {
                var msg = string.Empty;


                return msg;
            }
        }
    }
}
