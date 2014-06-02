using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Validation
{
    public class ValidationResult
    {
        public ValidationResult()
        {
            IsValidationSuccess = true;
            Error = null;
        }

        public bool IsValidationSuccess { get; set; }

        public string Error { get; set; }
    }
}
