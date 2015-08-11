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
    public class UpperCaseFunction : FunctionBase
    {
        public static string FunctionName = "Верхний регистр";

        public UpperCaseFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
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
            return (GetStringValue(param) ?? string.Empty).ToUpperInvariant();
        }
    }

    [Serializable]
    public class LowerCaseFunction : FunctionBase
    {
        public static string FunctionName = "Нижний регистр";

        public LowerCaseFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
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
            return (GetStringValue(param) ?? string.Empty).ToLowerInvariant();
        }
    }

    [Serializable]
    public class CamelCaseFunction : FunctionBase
    {
        public static string FunctionName = "С заглавной все слова регистр";

        public CamelCaseFunction()
        {
            Name = FunctionName;
            SelectedParameter = FunctionParameters.CellName;
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
            var str = GetStringValue(param) ?? string.Empty;
            var camelCaseWords = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new { OldWord = s, NewWord = s.Substring(0, 1).ToUpperInvariant() + (s.Length > 1 ? s.Substring(1) : string.Empty) });
            foreach (var i in camelCaseWords)
                str = str.Replace(i.OldWord, i.NewWord);
            return str;
        }
    }
}
