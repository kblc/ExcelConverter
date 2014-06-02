using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Microsoft.Win32;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ExelConverter.Core.Settings
{
    [Serializable]
    public class SettingsObject : INotifyPropertyChanged
    {
        private int _preloadedRowsCount;
        public int PreloadedRowsCount
        {
            get { return _preloadedRowsCount; }
            set
            {
                if (_preloadedRowsCount != value)
                {
                    _preloadedRowsCount = value;
                    RaisePropertyChanged("PreloadedRowsCount");
                }
            }
        }

        private string _heaaderSearchTags;
        public string HeaderSearchTags 
        {
            get { return _heaaderSearchTags; }
            set
            {
                if (_heaaderSearchTags != value)
                {
                    _heaaderSearchTags = value;
                    RaisePropertyChanged("HeaderSearchTags");
                }
            }
        }

        private string _csvFilesDirectory;
        public string CsvFilesDirectory
        {
            get { return _csvFilesDirectory; }
            set
            {
                if (_csvFilesDirectory != value)
                {
                    _csvFilesDirectory = value;
                    RaisePropertyChanged("CsvFilesDirectory");
                }
            }
        }

        public static SettingsObject Deserialize(string obj)
        {
            SettingsObject result = null;
            using (var stream = new MemoryStream(System.Convert.FromBase64String(obj)))
            {
                var formatter = new BinaryFormatter();
                result = (SettingsObject)formatter.Deserialize(stream);
            }
            return result;
        }

        public static string GetDefaultSettings()
        {
            return new SettingsObject()
            {
                CsvFilesDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                HeaderSearchTags = "фото,локализация,номер,код,район,схема,сторона",
                PreloadedRowsCount = 100
            }.Serialize();
        }

        public static SettingsObject GetStoredSettings()
        {
            var result = SettingsObject.Deserialize((string)Registry.GetValue("HKEY_CURRENT_USER", "Settings", SettingsObject.GetDefaultSettings()));
            return result;
        }

        public string Serialize()
        {
            var settings = string.Empty;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                var bytes = stream.ToArray();
                settings = System.Convert.ToBase64String(bytes);
            }
            return settings;
        }

        public void SaveToRegistry()
        {
            Registry.SetValue("HKEY_CURRENT_USER", "Settings", Serialize());
        }

        [NonSerialized]
        private PropertyChangedEventHandler _propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
