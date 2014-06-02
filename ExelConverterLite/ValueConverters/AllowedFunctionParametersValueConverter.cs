using ExelConverter.Core.Converter.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ExelConverterLite.ValueConverters
{
    class AllowedFunctionParametersValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var collection = (ObservableCollection<FunctionParameters>)value;
            var result = new ObservableCollection<string>();
            foreach (var itm in collection)
            {
                switch (itm.ToString())
                {
                    case "CellName":
                        result.Add("от колонки(по заголовку)");
                        break;
                    case "CellNumber":
                        result.Add("от колонки(по номеру)");
                        break;
                    case "Header":
                        result.Add("от верхнего заголовка");
                        break;
                    case "Subheader":
                        result.Add("от заголовка");
                        break;
                    case "Sheet":
                        result.Add("от вкладки");
                        break;
                    case "PrewFunction":
                        result.Add("от предидущей функции");
                        break;
                }
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
