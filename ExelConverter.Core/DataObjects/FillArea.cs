using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataObjects
{
    public class FillArea
    {
        public int ID { get; set; }

        public int FKOperatorID { get; set; }

        public int X1 { get; set; }

        public int Y1 { get; set; }

        public int X2 { get; set; }

        public int Y2 { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Type { get; set; }
    }
}
