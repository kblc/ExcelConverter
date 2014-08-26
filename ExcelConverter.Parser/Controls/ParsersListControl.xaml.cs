using Helpers.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExcelConverter.Parser.Controls
{
    public class UrlResultWrapper: INotifyPropertyChanged
    {
        private string value = string.Empty;
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException();

                this.value = value;
                RaisePropertyChanged("Value");
            }
        }

        private readonly ObservableCollection<ParseImageResult> parseResult = new ObservableCollection<ParseImageResult>();
        public ObservableCollection<ParseImageResult> ParseResult
        {
            get
            {
                return parseResult;
            }
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;

    }

    public partial class ParsersListControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ParsersProperty = DependencyProperty.Register("Parsers", typeof(ParserCollection), typeof(ParsersListControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ParsersProperty)
            {
                ParsersListControl p = dobj as ParsersListControl;
                if (p != null)
                {
                    p.RaisePropertyChanged(e.Property.Name);
                    //p.ParsersGenerateRuleControl.Parser = e.NewValue as Parser;
                }
            }
        }

        public ParserCollection Parsers
        {
            get
            {
                return this.GetValue(ParsersProperty) as ParserCollection;
            }
            set
            {
                this.SetValue(ParsersProperty, value);
            }
        }

        public ParsersListControl()
        {
            InitializeComponent();
        }

        private DelegateCommand parserDeleteCommand = null;
        public ICommand ParserDeleteCommand
        {
            get
            {
                return parserDeleteCommand ?? (parserDeleteCommand = new DelegateCommand((o) =>
                {
                    var parsers = o as IList<object>;
                    if (parsers != null)
                        foreach(var parser in parsers.Cast<Parser>().ToArray())
                            Parsers.Parsers.Remove(parser);
                }));
            }
        }

        //private DelegateCommand parserChangeCommand = null;
        //public ICommand ParserChangeCommand
        //{
        //    get
        //    {
        //        return parserChangeCommand ?? (parserChangeCommand = new DelegateCommand((o) =>
        //        {
        //            var parser = o as Parser;
        //            if (parser != null)
        //            {
                        
        //            }
        //        }));
        //    }
        //}

        private DelegateCommand parserNewCommand = null;
        public ICommand ParserNewCommand
        {
            get
            {
                return parserNewCommand ?? (parserNewCommand = new DelegateCommand((o) =>
                {
                    Parser parser = new Parser() { Url = "<new>" };
                    Parsers.Parsers.Add(parser);

                    ParsersListBox.SelectedItem = parser;
                }));
            }
        }


        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion;

        private void ParsersEditParserControl_AddModeChange(object sender, BoolRoutedEventArgs e)
        {
            ParsersListGrid.IsEnabled = !e.Value;
        }

    }


}
