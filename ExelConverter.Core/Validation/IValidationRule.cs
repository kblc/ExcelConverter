using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.Validation
{
    public interface IValidationRule<T>
    {
        ValidationResult Validate(T obj);
    }
}
