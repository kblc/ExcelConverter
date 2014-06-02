using ExelConverter.Core.Converter.CommonTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExelConverterLite.View
{
    /// <summary>
    /// Interaction logic for ImageParsingView.xaml
    /// </summary>
    public partial class ImageParsingView : Window
    {
        private Point _mouseDownPoint;
        private bool _mousePressed = false;

        public ImageParsingView()
        {
            InitializeComponent();
            Closing += (s, e) =>
            {
                IsClosed = true;
            };
        }

        public bool IsClosed { get; private set; }

        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }

        private void PhotoDrop(object sender, DragEventArgs e)
        {
           var files = (e.Data.GetData(DataFormats.FileDrop, true)) as string[];
           foreach (var file in files)
           {
               var bitmap = new BitmapImage(new Uri(file));
               if (!App.Locator.Import.SelectedOperator.MappingRule.PhotoParsingData.Any(ppd => ppd.Height == bitmap.PixelHeight
                   && ppd.Width == bitmap.PixelWidth))
               {
                   var ipd = new ImageParsingData
                   {
                       Height = bitmap.PixelHeight,
                       Width = bitmap.PixelWidth,
                   };
                   ipd.DrawingArea.Background = new ImageBrush(bitmap);
                   App.Locator.Import.SelectedOperator.MappingRule.PhotoParsingData.Add(ipd);
               }
               else
               {
                   var ipd = App.Locator.Import.SelectedOperator.MappingRule.PhotoParsingData.FirstOrDefault(ppd => ppd.Height == bitmap.PixelHeight && ppd.Width == bitmap.PixelWidth);
                   ipd.DrawingArea.Background = new ImageBrush(bitmap);
               }
           }
        }

        private void MapDrop(object sender, DragEventArgs e)
        {
            var files = (e.Data.GetData(DataFormats.FileDrop, true)) as string[];
            foreach (var file in files)
            {
                var bitmap = new BitmapImage(new Uri(file));
                if (!App.Locator.Import.SelectedOperator.MappingRule.MapParsingData.Any(ppd => ppd.Height == bitmap.PixelHeight
                && ppd.Width == bitmap.PixelWidth))
                {
                    var ipd = new ImageParsingData
                    {
                        Height = bitmap.PixelHeight,
                        Width = bitmap.PixelWidth,
                    };
                    ipd.DrawingArea.Background = new ImageBrush(bitmap);
                    App.Locator.Import.SelectedOperator.MappingRule.MapParsingData.Add(ipd);
                }
                else
                {
                    var ipd = App.Locator.Import.SelectedOperator.MappingRule.MapParsingData.FirstOrDefault(ppd => ppd.Height == bitmap.PixelHeight && ppd.Width == bitmap.PixelWidth);
                    ipd.DrawingArea.Background = new ImageBrush(bitmap);
                }
            }
        }
    }
}
