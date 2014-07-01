using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Helpers.Serialization;
using Helpers.WPF;

namespace ExcelConverter.Parser.Controls
{
    /// <summary>
    /// Interaction logic for Parsers.xaml
    /// </summary>
    public partial class ParsersControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency propertyes
        
        public static readonly DependencyProperty ParsersProperty = DependencyProperty.Register("Parsers", typeof(ParserCollection), typeof(ParsersControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowOpenSaveParsersProperty = DependencyProperty.Register("AllowOpenSaveParsers", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportFileImportProperty = DependencyProperty.Register("AllowImportFileImport", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowFileImportDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportFileExportProperty = DependencyProperty.Register("AllowImportFileExport", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowFileExportDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty ImportThreadCountProperty = DependencyProperty.Register("ImportThreadCount", typeof(byte), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.ThreadCountDefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty UrlsProperty = DependencyProperty.Register("Urls", typeof(UrlCollection), typeof(ParsersControl), new FrameworkPropertyMetadata(new UrlCollection(), FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty ShowEditTabProperty = DependencyProperty.Register("ShowEditTab", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty ShowImportTabProperty = DependencyProperty.Register("ShowImportTab", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        public static readonly DependencyProperty AllowImportAllProperty = DependencyProperty.Register("AllowImportAll", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowImportAllProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportPhotoProperty = DependencyProperty.Register("AllowImportPhoto", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowImportPhotoProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportSchemaProperty = DependencyProperty.Register("AllowImportSchema", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowImportSchemaProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));
        public static readonly DependencyProperty AllowImportClearUrlsAfterImportProperty = DependencyProperty.Register("AllowImportClearUrlsAfterImport", typeof(bool), typeof(ParsersControl), new FrameworkPropertyMetadata(ParsersImportControl.AllowClearUrlsAfterImportProperty.DefaultMetadata.DefaultValue, FrameworkPropertyMetadataOptions.Inherits, new PropertyChangedCallback(MyCustom_PropertyChanged)));

        private static void MyCustom_PropertyChanged(DependencyObject dobj, DependencyPropertyChangedEventArgs e)
        {
            ParsersControl p = dobj as ParsersControl;
            if (p != null)
            {
                p.RaisePropertyChanged(e.Property.Name);

                if (e.Property == AllowImportFileExportProperty)
                {
                    p.ParsersImportControl.AllowFileExport = (bool)e.NewValue;
                }
                if (e.Property == AllowImportFileImportProperty)
                {
                    p.ParsersImportControl.AllowFileImport = (bool)e.NewValue;
                }
                if (e.Property == ImportThreadCountProperty)
                {
                    p.ParsersImportControl.ThreadCount = (byte)e.NewValue;
                }
                if (e.Property == UrlsProperty)
                {
                    p.ParsersImportControl.Urls = (UrlCollection)e.NewValue;
                }
                if (e.Property == AllowImportAllProperty)
                {
                    p.ParsersImportControl.AllowImportAll = (bool)e.NewValue;
                }
                if (e.Property == AllowImportPhotoProperty)
                {
                    p.ParsersImportControl.AllowImportPhoto = (bool)e.NewValue;
                }
                if (e.Property == AllowImportSchemaProperty)
                {
                    p.ParsersImportControl.AllowImportSchema = (bool)e.NewValue;
                }
                if (e.Property == AllowImportClearUrlsAfterImportProperty)
                {
                    p.ParsersImportControl.AllowClearUrlsAfterImport = (bool)e.NewValue;
                }
            }
        }
        public bool AllowOpenSaveParsers
        {
            get { return (bool)this.GetValue(AllowOpenSaveParsersProperty); }
            set { this.SetValue(AllowOpenSaveParsersProperty, value); }
        }
        public ParserCollection Parsers
        {
            get { return this.GetValue(ParsersProperty) as ParserCollection; }
            set { this.SetValue(ParsersProperty, value); }
        }
        public bool AllowImportFileImport
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowFileImportProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowFileImportProperty, value); }
        }
        public bool AllowImportFileExport
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowFileExportProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowFileExportProperty, value); }
        }
        public byte ImportThreadCount
        {
            get { return (byte)ParsersImportControl.GetValue(ParsersImportControl.ThreadCountProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.ThreadCountProperty, value); }
        }
        public UrlCollection Urls
        {
            get { return (UrlCollection)ParsersImportControl.GetValue(ParsersImportControl.UrlsProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.UrlsProperty, value); }
        }
        public bool ShowEditTab
        {
            get { return (bool)this.GetValue(ShowEditTabProperty); }
            set { this.SetValue(ShowEditTabProperty, value); }
        }
        public bool ShowImportTab
        {
            get { return (bool)this.GetValue(ShowImportTabProperty); }
            set { this.SetValue(ShowImportTabProperty, value); }
        }
        public bool AllowImportAll
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowImportAllProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowImportAllProperty, value); }
        }
        public bool AllowImportPhoto
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowImportPhotoProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowImportPhotoProperty, value); }
        }
        public bool AllowImportSchema
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowImportSchemaProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowImportSchemaProperty, value); }
        }
        public bool AllowImportClearUrlsAfterImport
        {
            get { return (bool)ParsersImportControl.GetValue(ParsersImportControl.AllowClearUrlsAfterImportProperty); }
            set { ParsersImportControl.SetValue(ParsersImportControl.AllowClearUrlsAfterImportProperty, value); }
        }

        #endregion

        private DelegateCommand saveParsersCommand = null;
        public ICommand SaveParsersCommand
        {
            get
            {
                return saveParsersCommand ?? (saveParsersCommand = new DelegateCommand((o) =>
                {
                    var saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Parsers files (*.parsers,*.xml)|*.parsers;*.xml|All files (*.*)|*.*";
                    saveFileDialog.DefaultExt = "xml";
                    saveFileDialog.AddExtension = true;
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() ?? false)
                        SaveParsers(saveFileDialog.FileName);
                }));
            }
        }

        private DelegateCommand loadParsersCommand = null;
        public ICommand LoadParsersCommand
        {
            get
            {
                return loadParsersCommand ?? (loadParsersCommand = new DelegateCommand((o) =>
                {
                    var openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "Parsers files (*.parsers,*.xml)|*.parsers;*.xml|All files (*.*)|*.*";
                    openFileDialog.RestoreDirectory = true;
                    if (openFileDialog.ShowDialog() ?? false)
                        LoadParsers(openFileDialog.FileName);
                }));
            }
        }

        private void SaveParsers(string filePath)
        {
            SaveParsers(Parsers, filePath);
        }
        public static void SaveParsers(ParserCollection Parsers, string filePath)
        {
            try
            {
                string text = (System.IO.Path.GetExtension(filePath).ToLower() == ".xml")
                    ? Parsers.SerializeXML()
                    : Parsers.Serialize();
                System.IO.File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                string exError = Helpers.Log.GetExceptionText(ex, "ParsersControl.SaveParsers()");
                MessageBox.Show(string.Format("При сохранении произошла ошибка:{0}{1}", Environment.NewLine, exError), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadParsers(string filePath)
        {
            LoadParsers(Parsers, filePath);
        }
        public static void LoadParsers(ParserCollection Parsers, string filePath)
        {
            try
            {
                string text = System.IO.File.ReadAllText(filePath);

                var parsers = (System.IO.Path.GetExtension(filePath).ToLower() == ".xml")
                    ? ParserCollection.DeserilizeXML(text)
                    : ParserCollection.Deserilize(text);

                Parsers.Parsers.Clear();
                foreach (var parser in parsers.Parsers)
                    Parsers.Parsers.Add(parser);
            }
            catch (Exception ex)
            {
                if (!ex.IsDesignMode())
                { 
                    string exError = Helpers.Log.GetExceptionText(ex, "ParsersControl.LoadParsers()");
                    MessageBox.Show(string.Format("При загрузке произошла ошибка:{0}{1}", Environment.NewLine, exError), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public ParsersControl()
        {
            InitializeComponent();
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
