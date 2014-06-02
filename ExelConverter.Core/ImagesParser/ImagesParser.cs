using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Drawing;


namespace ExelConverter.Core.ImagesParser
{
    public class ImagesParser
    {
        private string _url;

        public ImagesParser(string url)
        {
            _url = url;
        }

        public BitmapImage GetImage()
        {
            BitmapImage result = null;
            if (ImageAllowed)
            {
                using (var browser = new System.Windows.Forms.WebBrowser())
                {
                    browser.ScriptErrorsSuppressed = true;
                    browser.ScrollBarsEnabled = false;
                    browser.DocumentCompleted += (s, e) =>
                    {

                        var width = browser.Document.InvokeScript("eval", new object[] { @"
function documentWidth(){
	return Math.max(
		document.documentElement['clientWidth'],
		document.body['scrollWidth'], document.documentElement['scrollWidth'],
		document.body['offsetWidth'], document.documentElement['offsetWidth']
	);
}
documentWidth();
"});
                        var height = browser.Document.InvokeScript("eval", new object[] { @"
function documentHeight(){
	return Math.max(
		document.documentElement['clientHeight'],
		document.body['scrollHeight'], document.documentElement['scrollHeight'],
		document.body['offsetHeight'], document.documentElement['offsetHeight']
	);
}
documentHeight();
"});

                        if (height != null && width != null)
                        {
                            browser.Height = int.Parse(height.ToString());
                            browser.Width = int.Parse(width.ToString());
                            using (var pic = new Bitmap(browser.Width, browser.Height))
                            {

                                browser.Focus();
                                browser.DrawToBitmap(pic, new System.Drawing.Rectangle(0, 0, pic.Width, pic.Height));
                                var strm = new MemoryStream();
                                pic.Save(strm, System.Drawing.Imaging.ImageFormat.Jpeg);
                                strm.Seek(0, SeekOrigin.Begin);
                                result = new BitmapImage();
                                result.BeginInit();
                                result.StreamSource = strm;
                                result.DecodePixelHeight = 300;
                                result.EndInit();
                            }
                        }
                        else
                        {
                            result = new System.Windows.Media.Imaging.BitmapImage(new Uri(_url));
                        }
                    };

                    browser.Navigate(_url);
                    while (browser.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            }
            else
            {
                result = new System.Windows.Media.Imaging.BitmapImage(new Uri(_url));
            }
            return result;
        }

        public bool ImageAllowed
        {
            get
            {
                return _url.EndsWith(".jpg") || _url.EndsWith(".png") || _url.EndsWith(".bmp") || _url.EndsWith(".gif");
            }
        }
    }
}
