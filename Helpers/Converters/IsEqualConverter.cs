using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Helpers.Converters
{
    public class IsEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null)
                return true;

            if (value == null)
                return false;

            if (value is int)
            {
                return parameter != null ? value.ToString() == parameter.ToString() : false;
            }
            else if (value is bool && parameter is bool)
            {
                return ((bool)value) == ((bool)parameter);
            }

            if (value != null && parameter != null)
            {
                if (value.ToString().ToUpper() == parameter.ToString().ToUpper())
                    return true;
            }

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
