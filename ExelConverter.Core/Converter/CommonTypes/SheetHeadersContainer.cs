using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.Converter.CommonTypes
{
    public class SheetHeadersContainer : INotifyPropertyChanged
    {

        public SheetHeadersContainer()
        {
            Headers = new ObservableCollection<SheetHeader>();
            Subheaders = new ObservableCollection<SheetHeader>();
        }

        private ObservableCollection<SheetHeader> _headers;
        public ObservableCollection<SheetHeader> Headers
        {
            get { return _headers; }
            set
            {
                if (_headers != value)
                {
                    _headers = value;
                    RaisePropertyChanged("Headers");
                }
            }
        }


        private ObservableCollection<SheetHeader> _subheaders;
        public ObservableCollection<SheetHeader> Subheaders
        {
            get { return _subheaders; }
            set
            {
                if (_subheaders != value)
                {
                    _subheaders = value;
                    RaisePropertyChanged("Subheaders");
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
