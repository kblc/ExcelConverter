using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataWriter
{
    public class ExportedCsv
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string Path { get; set; }

        public DateTime ExportDate { get; set; }
    }
}
