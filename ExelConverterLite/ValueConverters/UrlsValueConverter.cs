using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using ExelConverter.Core.ImagesParser;

namespace ExelConverterLite.ValueConverters
{
    public class ImageUrl
    {
        public string Url { get; set; }
    }

    public class ImageSource
    {
        public BitmapImage Source { get; set; }

        public string ImageUrl { get; set; }
    }

    public class UrlsValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = new ObservableCollection<ImageSource>();
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var row = (DataRowView)value;
                var urls = row.Row.ItemArray.Where(itm => itm != null && ((string)itm).Contains("http")).Select(u => new ImageUrl { Url = (string)u }).ToArray();
                foreach (var url in urls)
                {
                    var imParser = new ImagesParser(url.Url);
                    result.Add(new ImageSource
                    {
                        Source = imParser.GetImage(),
                        ImageUrl = url.Url
                    });
                }   
            }));
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
