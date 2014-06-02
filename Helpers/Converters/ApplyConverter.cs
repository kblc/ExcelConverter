using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Helpers.Converters
{
    public class ApplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double curVal = (double)value;
            string paramline = parameter as string;
            if (!string.IsNullOrEmpty(paramline))
            {
                var items = paramline.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                double add;
                if (items.Length > 0 && double.TryParse(items[0], out add))
                {
                    curVal += add;

                    double min;
                    if (items.Length > 1 && double.TryParse(items[1], out min))
                    {
                        curVal = Math.Max(curVal, min);

                        double max;
                        if (items.Length > 2 && double.TryParse(items[2], out max))
                        {
                            curVal = Math.Min(curVal, max);
                        }
                    }
                }
            }
            return System.Convert.ChangeType(curVal, targetType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
