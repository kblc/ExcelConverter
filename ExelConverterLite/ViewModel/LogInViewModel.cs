using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using ExelConverter.Core;
using ExelConverter.Core.DataAccess;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace ExelConverterLite.ViewModel
{


    public sealed class LogInViewModel : ViewModelBase
    {
        private IDataAccess _appSettingsDataAccess;
        public LogInViewModel(IDataAccess appSettingsDataAccess)
        {
            _appSettingsDataAccess = appSettingsDataAccess;
        }

    }
}
