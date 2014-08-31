using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ExcelConverter.Parser
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
                        if (items.Length > 1 && double.TryParse(items[2], out max))
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

    public class BoolToBoldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FinishedBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte isFinished = (byte)value;
            System.Windows.Media.Brush result = null;

            if (isFinished == 1)
                result = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen);
            else if (isFinished == 2)
                result = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightCoral);

            return result ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNotEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null)
                return false;

            if (value == null)
                return true;

            if (value is int)
            {
                return parameter != null ? value.ToString() != parameter.ToString() : true;
            }
            else if (value is bool && parameter is bool)
            {
                return ((bool)value) == ((bool)parameter);
            }

            if (value != null && parameter != null)
            {
                if (value.ToString().ToUpper() == parameter.ToString().ToUpper())
                    return false;
            }

            return !value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNotVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (System.Windows.Visibility)value != System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ImageFromUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();

                if (value is System.Drawing.Bitmap)
                {
                    System.Drawing.Bitmap bmp = value as System.Drawing.Bitmap;
                    IntPtr hBitmap = bmp.GetHbitmap();
                    var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    var encoder = new BmpBitmapEncoder();
                    using(var memoryStream = new MemoryStream())
                    {
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(memoryStream);
                        bitmap.StreamSource = new MemoryStream(memoryStream.ToArray());
                    }
                } else
                    bitmap.UriSource = new Uri(value as string, UriKind.Absolute);
                bitmap.EndInit();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
