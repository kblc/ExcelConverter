using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Validation
{
    public class IsNumberValidationRule : IValidationRule<string>
    {
        public ValidationResult Validate(string obj)
        {
            var result = new ValidationResult();
            try
            {
                int.Parse(obj);
            }
            catch(Exception ex)
            {
                result.Error = "введённая строка не является числом";
                result.IsValidationSuccess = false;
            }
            return result;
        }
    }
}
