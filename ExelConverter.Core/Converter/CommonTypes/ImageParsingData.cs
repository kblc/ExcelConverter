using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ExelConverter.Core.ExelDataReader;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Data;

namespace ExelConverter.Core.Converter.CommonTypes
{

    [Serializable]
    public class Area : INotifyPropertyChanged
    {
        private double _x;
        public double X
        {
            get { return _x; }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    RaisePropertiChanged("X");
                }
            }
        }

        private double _y;
        public double Y 
        {
            get { return _y; }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    RaisePropertiChanged("Y");
                }
            }
        }

        private double _width;
        public double Width 
        {
            get { return _width; }
            set
            {
                if (value != _width)
                {
                    _width = value;
                    RaisePropertiChanged("Width");
                }
            }
        }

        private double _height;
        public double Height 
        {
            get { return _height; }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    RaisePropertiChanged("Height");
                }
            }
        }

        public bool IsForFill
        {
            get;
            set;
        }

        private void RaisePropertiChanged(string propertyName)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }
    }

    [Serializable]
    public class ImageParsingData : INotifyPropertyChanged, ICopyFrom<ImageParsingData>
    {
        private bool _mouseCaptured = false;
        private bool _newRect = false;
        private Point _capturedPosition;

        public ImageParsingData() { }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        private Canvas _drawingArea;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public Canvas DrawingArea
        {
            get 
            {
                if (_drawingArea != null)
                    return _drawingArea;

                _drawingArea = new Canvas();
                _drawingArea.Background = Brushes.Transparent;
                _drawingArea.MouseDown += drawingPanelMouseDown;
                _drawingArea.VerticalAlignment = VerticalAlignment.Center;
                _drawingArea.HorizontalAlignment = HorizontalAlignment.Center;

                return _drawingArea;
            }
            set
            {
                if (_drawingArea != value)
                {
                    _drawingArea = value;
                    RaisePropertiChanged("DrawingArea");
                }
            }
        }

        private string _imageUrl;
        public string ImageUrl
        {
            get { return _imageUrl; }
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value;
                    RaisePropertiChanged("ImageUrl");
                }
            }
        }

        private double _height;
        public double Height
        {
            get { return _height; }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    DrawingArea.Height = Height;
                    RaisePropertiChanged("Height");
                }
            }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    DrawingArea.Width = Width;
                    RaisePropertiChanged("Width");
                }
            }
        }

        public string Size
        {
            get { return Height+"x"+Width; }
        }

        public void RefreshImage()
        {
            /*var convertionData = ConvertionRule.ConvertionData.Single(cd => cd.PropertyId == "Photo_img");
            var url = convertionData.Blocks.Run(CurrentSheet, CurrentSheet.Rows.IndexOf(CurrentSheet.MainHeader) + 1, convertionData);
            var imParser = new ExelConverter.Core.ImagesParser.ImagesParser(url);
            ImageSource = imParser.GetImage();
            Height = ImageSource.PixelHeight;
            Width = ImageSource.PixelWidth;*/
        }

        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertiChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void drawingPanelMouseDown(object sender, MouseButtonEventArgs e)
        {
            _capturedPosition = e.GetPosition(DrawingArea);
            _mouseCaptured = true;
            _newRect = true;
            var rect = new Rectangle { Height = 1, Width = 1, Opacity = 0.3, Fill = Brushes.Green };
            DrawingArea.Children.Add(rect);
            Canvas.SetLeft(rect, _capturedPosition.X);
            Canvas.SetTop(rect, _capturedPosition.Y);

            rect.CaptureMouse();
            rect.MouseMove += (_s, _e) =>
            {
                if (_mouseCaptured)
                {
                    if (_capturedPosition.Y < _e.GetPosition(DrawingArea).Y)
                    {
                        rect.Height = _e.GetPosition(DrawingArea).Y - _capturedPosition.Y;
                    }
                    else
                    {
                        rect.Height = _capturedPosition.Y - _e.GetPosition(DrawingArea).Y;
                        Canvas.SetTop(rect, _e.GetPosition(DrawingArea).Y);
                    }
                    if (_capturedPosition.X < _e.GetPosition(DrawingArea).X)
                    {
                        rect.Width = _e.GetPosition(DrawingArea).X - _capturedPosition.X;
                    }
                    else
                    {
                        rect.Width = _capturedPosition.X - _e.GetPosition(DrawingArea).X;
                        Canvas.SetLeft(rect, _e.GetPosition(DrawingArea).X);
                    }
                }
            };

            rect.MouseLeftButtonDown += (_s, _e) =>
            {

            };

            rect.MouseRightButtonDown += (_s, _e) =>
            {
                DrawingArea.Children.Remove(rect);
            };

            rect.MouseUp += (_s, _e) =>
            {
                _mouseCaptured = false;
                _newRect = false;
                rect.ReleaseMouseCapture();
                DrawingArea.ReleaseMouseCapture();
            };
        }

        public ImageParsingData CopyFrom(ImageParsingData source)
        {
            this.ImageUrl = source.ImageUrl;
            this.Height = source.Height;
            this.Width = source.Width;
            this._drawingArea = source._drawingArea;
            return this;
        }
    }
}
