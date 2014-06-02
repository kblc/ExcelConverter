using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Validation
{
    public class StringIsNotNullOrEmptyValidationRule : IValidationRule<string>
    {
        public ValidationResult Validate(string obj)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(obj))
            {
                result.Error = "строка не должна быть пустой";
                result.IsValidationSuccess = false;
            }

            return result;
        }
    }
}
