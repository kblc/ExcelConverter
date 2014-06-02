using System;
using System.Windows;

namespace ExcelConverter.Parser.Test
{
    [Serializable]
    [System.Xml.Serialization.XmlRoot("ParserCollection")]
    public class MyDBParserCollection : ExcelConverter.Parser.DBParserCollection { }

    public partial class MainWindow : Window
    {
        private MyDBParserCollection parsers = null;
        public MyDBParserCollection Parsers 
        {
            get
            {
                return parsers ?? (parsers = new MyDBParserCollection() { StoreLocal = true, StoreDirect = false });
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { Parsers.Load(); };
            this.Closed += (s, e) => { Parsers.Save(); };
        }
    }
}
