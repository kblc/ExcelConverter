using System;
using System.Collections.Generic;
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
    public class BoolRoutedEventArgs : RoutedEventArgs
    {
        public BoolRoutedEventArgs() { }
        public BoolRoutedEventArgs(RoutedEvent routedEvent) : base(routedEvent) { }
        public BoolRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }
        public bool Value { get; set; }
    }

    public delegate void BoolRoutedEventHandler(object sender, BoolRoutedEventArgs e);

    public partial class ParsersEditParserControl : UserControl, INotifyPropertyChanged
    {
        public event BoolRoutedEventHandler IsAddModeChange
        {
            add { AddHandler(IsAddModeChangeEvent, value); }
            remove { RemoveHandler(IsAddModeChangeEvent, value); }
        }

        public static readonly RoutedEvent IsAddModeChangeEvent = EventManager.RegisterRoutedEvent("IsAddModeChange", RoutingStrategy.Bubble, typeof(BoolRoutedEventHandler), typeof(ParsersEditParserControl));
        public static readonly DependencyProperty ParserProperty = DependencyProperty.Register("Parser", typeof(Parser), typeof(ParsersEditParserControl), new FrameworkPropertyMetadata(new PropertyChangedCallback(MyCustom_PropertyChanged)));

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ParserProperty)
            {
                ParsersEditParserControl p = dobj as ParsersEditParserControl;
                if (p != null)
                {
                    p.RaisePropertyChanged("Parser");
                    //p.ParsersGenerateRuleControl.Parser = e.NewValue as Parser;
                }
            }
        }

        public Parser Parser
        {
            get
            {
                return this.GetValue(ParserProperty) as Parser;
            }
            set
            {
                this.SetValue(ParserProperty, value);
                RaisePropertyChanged("Parser");
            }
        }

        public ParsersEditParserControl()
        {
            InitializeComponent();
        }

        private DelegateCommand showParsersGenerateRuleControlCommand = null;
        public ICommand ShowParsersGenerateRuleControlCommand
        {
            get
            {
                return showParsersGenerateRuleControlCommand ?? 
                    (showParsersGenerateRuleControlCommand = new DelegateCommand((o) => { ChangeAddMode(true); }));
            }
        }

        private void ParsersGenerateRuleControl_Hide(object sender, RuleRoutedEventArgs e)
        {
            ChangeAddMode(false);
        }

        private void ChangeAddMode(bool isAddMode)
        {
            ParsersGenerateRuleControl.Visibility = !isAddMode ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

            if (!isAddMode)
            {
                Grid mainGrid = this.Content as Grid;
                mainGrid.RowDefinitions[1].Height = GridLength.Auto;
            }

            RaiseEvent(new BoolRoutedEventArgs(IsAddModeChangeEvent) { Value = isAddMode });
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
}
