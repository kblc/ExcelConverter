using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using System.Windows.Media;
using System.Windows.Controls;

namespace ExelConverterLite.ValueConverters
{
    public class MappingValueConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Brush result = Brushes.White;
            try
            {
                if (value != null)
                {
                    var conv = new BrushConverter();
                    result = (SolidColorBrush)conv.ConvertFromString((string)value);
                }
            }
            catch (FormatException e)
            { }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }
    }
}
