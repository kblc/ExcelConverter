using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataObjects
{
    public class Size
    {
        public int Id { get; set; }

        public int FkTypeId { get; set; }

        public string Name { get; set; }
    }
}
