using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverterLite.Model.Constructor
{
    public class SimpleSharpConstructorModel
    {
        public string Name { get; set; }

        public int ColumnNumber { get; set; }

        public bool IsAdvancedModeEnabled { get; set; }

        public bool IsFindReplaceEnabled { get; set; }

        public bool IsSubstrEnabled { get; set; }

        public string FirstTextFieldName { get; set; }

        public string FirstTextField { get; set; }

        public string SecondTextFieldName { get; set; }

        public string SecondTextField { get; set; }
    }
}
