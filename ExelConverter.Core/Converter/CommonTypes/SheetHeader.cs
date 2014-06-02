using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.Converter.CommonTypes
{
    public class SheetHeader : INotifyPropertyChanged
    {

        private int _rowNumber;
        public int RowNumber
        {
            get { return _rowNumber; }
            set
            {
                if (_rowNumber != value)
                {
                    _rowNumber = value;
                    RaisePropertyChanged("RowNumber");
                }
            }
        }

        private string _header;
        public string Header
        {
            get { return _header; }
            set
            {
                if (_header != value)
                {
                    _header = value;
                    RaisePropertyChanged("Header");
                }
            }
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
