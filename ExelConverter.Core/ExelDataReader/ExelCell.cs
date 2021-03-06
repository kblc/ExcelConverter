﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ExelConverter.Core.ExelDataReader
{

    public class ExelCell
    {
        public ExelCell()
        {
            IsMerged = false;
        }

        public string FormatedValue { get; set; }

        public string Value { get; set; }

        public string Comment { get; set; }

        public System.Drawing.Color Color { get; set; }

        public int OriginalIndex { get; set; }

        private static string DecodeUrlString(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;
            return newUrl;
        }

        private string hyperLink = string.Empty;
        public string HyperLink 
        {
            get
            {
                return hyperLink;
            }
            set
            {
                if (hyperLink == value)
                    return;

                hyperLink = System.Web.HttpUtility.UrlPathEncode(value ?? string.Empty);

                //var val = System.Web.HttpUtility.UrlEncode(value ?? string.Empty);// DecodeUrlString(value ?? string.Empty);
                //hyperLink = System.Web.HttpUtility.UrlEncode(val); //Uri.EscapeUriString(val);
            }
        }

        public bool IsMerged { get; set; }

        public Aspose.Cells.Style CellStyle { get; set; }

        public double UniqueWeight
        {
            get
            {
                double result = 0;
                if (CellStyle != null)
                {
                    result += CellStyle.BackgroundColor != null && !AsyncDocumentLoader.DefColors.Any(clr => AsyncDocumentLoader.ColorsEqual(clr, CellStyle.BackgroundColor)) ? (double)0.2 : (double)0.0;
                    result += CellStyle.ForegroundColor != null && !AsyncDocumentLoader.DefColors.Any(clr => AsyncDocumentLoader.ColorsEqual(clr, CellStyle.ForegroundColor)) ? (double)0.2 : (double)0.0;
                    result += CellStyle.Font != null && CellStyle.Font.IsBold ? 0.2 : 0.0;
                    result += CellStyle.Font != null && CellStyle.Font.IsItalic ? 0.2 : 0.0;
                    result += CellStyle.Font != null && CellStyle.Font.Color != null && !(AsyncDocumentLoader.ColorsEqual(CellStyle.Font.Color, System.Drawing.Color.FromArgb(byte.MaxValue, byte.MinValue, byte.MinValue, byte.MinValue))) ? 0.2 : 0.0;
                }
                return result;
            }
        }

        public override string ToString()
        {
            return (string)Value;
        }
    }
   
}
