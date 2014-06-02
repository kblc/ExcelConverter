using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

using ExelConverter.Core.Converter.CommonTypes;

namespace ExelConverterLite.ValueConverters
{
    class MappingTableSelectedItemValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (((string)value).Length > 0)
            {
                return ((string)value).Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries).First().Trim();
            }
            return string.Empty;
        }
    }
}
