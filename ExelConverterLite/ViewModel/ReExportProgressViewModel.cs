using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverterLite.ViewModel
{
    public class ReExportProgressViewModel : ViewModelBase
    {
        private string progressText = string.Empty;
        public string ProgressText
        {
            get { return progressText; }
            set { progressText = value; RaisePropertyChanged("ProgressText"); }
        }

        private int progressValue = 0;
        public int ProgressValue
        {
            get { return progressValue; }
            set { progressValue = value; RaisePropertyChanged("ProgressValue"); }
        }
    }
}
