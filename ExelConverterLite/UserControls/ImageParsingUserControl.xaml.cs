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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExelConverterLite.UserControls
{
    public partial class ImageParsingUserControl : UserControl
    {
        private bool _mouseCaptured = false;
        private bool _newRect = false;
        private Point _capturedPosition;

        public ImageParsingUserControl()
        {
            InitializeComponent();
        }

        private void drawingPanelMouseDown(object sender, MouseButtonEventArgs e)
        {
            _capturedPosition = e.GetPosition(drawingPanel);
            _mouseCaptured = true;
            _newRect = true;
            var rect = new Rectangle { Height = 1, Width = 1, Opacity = 0.2 };
            drawingPanel.Children.Add(rect);
            Canvas.SetLeft(rect, _capturedPosition.X);
            Canvas.SetTop(rect, _capturedPosition.Y);

            rect.CaptureMouse();
            rect.MouseMove += (_s,_e) =>
            {
                if (_mouseCaptured)
                {
                    if (_capturedPosition.Y < _e.GetPosition(drawingPanel).Y)
                    {
                        rect.Height = _e.GetPosition(drawingPanel).Y - _capturedPosition.Y;
                    }
                    else
                    {
                        rect.Height = _capturedPosition.Y - _e.GetPosition(drawingPanel).Y;
                        Canvas.SetTop(rect, _e.GetPosition(drawingPanel).Y);
                    }
                    if (_capturedPosition.X < _e.GetPosition(drawingPanel).X)
                    {
                        rect.Width = _e.GetPosition(drawingPanel).X - _capturedPosition.X;
                    }
                    else
                    {
                        rect.Width = _capturedPosition.X - _e.GetPosition(drawingPanel).X;
                        Canvas.SetLeft(rect, _e.GetPosition(drawingPanel).X);
                    }
                }
            };

            rect.MouseLeftButtonDown += (_s,_e) =>
            {

            };

            rect.MouseRightButtonDown += (_s, _e) =>
            {
                drawingPanel.Children.Remove(rect);
            };

            rect.MouseUp += (_s, _e) =>
            {
                _mouseCaptured = false;
                _newRect = false;
                rect.ReleaseMouseCapture();
                ReleaseMouseCapture();
            };
        }
    }
}
