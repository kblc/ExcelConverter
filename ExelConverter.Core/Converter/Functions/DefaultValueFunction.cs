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
    public class DefaultValueFunction : FunctionBase
    {
        public DefaultValueFunction()
        {
            Name = "По Умолчанию";
            StringFormat = Name + "()";
            SelectedParameter = null;
            Parameters = new ObservableCollection<Parameter>()
            {
                new Parameter{Name="Значение", ExpectedValueType = typeof(string)}
            };
        }

        protected override ObservableCollection<FunctionParameters> GetAllowedParameters()
        {
            return new ObservableCollection<FunctionParameters>();
        }

        public override string Function(Dictionary<string, object> param)
        {
            var str = (string)Parameters[0].Value;
            
            return str;
        }
    }
}
