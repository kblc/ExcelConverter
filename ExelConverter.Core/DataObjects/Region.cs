using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataObjects
{
    public class Region
    {
        public static string Unknown = "Не определен";

        public int? Id { get; set; }

        public int? FkCityId { get; set; }

        public string Name { get; set; }
    }
}
