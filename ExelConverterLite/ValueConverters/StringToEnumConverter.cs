using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

using ExelConverter.Core.Converter.Functions;

namespace ExelConverterLite.ValueConverters
{
    class StringToEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                switch (value.ToString())
                {
                    case "CellName":
                        return "от колонки(по заголовку)";
                    case "CellNumber":
                        return "от колонки(по номеру)";
                    case "Header":
                        return "от верхнего заголовка";
                    case "Subheader":
                        return "от заголовка";
                    case "Sheet":
                        return "от вкладки";
                    case "PrewFunction":
                        return "от предидущей функции";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                switch (value.ToString())
                {
                    case "от колонки(по заголовку)":
                        return FunctionParameters.CellName;
                    case "от колонки(по номеру)":
                        return FunctionParameters.CellNumber;
                    case "от верхнего заголовка":
                        return FunctionParameters.Header;
                    case "от заголовка":
                        return FunctionParameters.Subheader;
                    case "от вкладки":
                        return FunctionParameters.Sheet;
                    case "от предидущей функции":
                        return FunctionParameters.PrewFunction;
                }
            }
            return null;
        }
    }
}
