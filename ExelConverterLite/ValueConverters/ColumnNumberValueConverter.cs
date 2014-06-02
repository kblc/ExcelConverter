using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;


namespace ExelConverterLite.ValueConverters
{
    public class ColumnNumberValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var collection = (ObservableCollection<string>)value;
            var result = new ObservableCollection<string>();
            for (var i = 0; i < collection.Count; i++)
            {
                result.Add(collection[i] + " (" + (int)(i + 1) + ")");
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
