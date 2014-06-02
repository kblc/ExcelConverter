using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.DataWriter
{
    public class OutputRow : INotifyPropertyChanged
    {
        //public static string[] ColumnOrder = new string[] { "Code", "CodeDoors", "Type", "City", "Side", "Size", "Light", "Restricted", "Region", "Address", "Description", "Price", "Photo_img", "Location_img" };

        private static Dictionary<int, string> columnOrder = null;
        public static Dictionary<int, string> ColumnOrder
        {
            get
            {
                if (columnOrder != null)
                    return columnOrder;

                columnOrder = new Dictionary<int, string>();

                columnOrder.Add(0, "Code");
                columnOrder.Add(1, "CodeDoors");
                columnOrder.Add(2, "Type");
                columnOrder.Add(3, "City");
                columnOrder.Add(4, "Side");
                columnOrder.Add(5, "Size");
                columnOrder.Add(6, "Light");
                columnOrder.Add(7, "Restricted");
                columnOrder.Add(8, "Region");
                columnOrder.Add(9, "Address");
                columnOrder.Add(10, "Description");
                columnOrder.Add(11, "Price");
                columnOrder.Add(12, "Photo_img");
                columnOrder.Add(13, "Location_img");

                int i = 100;

                foreach (var propertyName in typeof(OutputRow).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Select(pi => pi.Name))
                {

                    if (!columnOrder.ContainsValue(propertyName))
                    {
                        columnOrder.Add(i, propertyName);
                        i++;
                    }
                }
                return columnOrder;
            }
        }

        private string code = string.Empty;
        public string Code { get { return code; } set { code = value; RaisePropertyChanged("Code"); } }

        private string originalCode = string.Empty;
        public string OriginalCode { get { return originalCode; } set { originalCode = value; RaisePropertyChanged("OriginalCode"); } }

        public string CodeDoors { get; set; }

        public string Type { get; set; }

        public string City { get; set; }

        public string Side { get; set; }

        public string Size { get; set; }

        public string Light { get; set; }

        public string Restricted { get; set; }

        public string Region { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public string Price { get; set; }

        private string photo_img = string.Empty;
        public string Photo_img { get { return photo_img; } set { photo_img = value; RaisePropertyChanged("Photo_img"); } }

        private string location_img = string.Empty;
        public string Location_img { get { return location_img; } set { location_img = value; RaisePropertyChanged("Location_img"); } }

        public string ToCsvString(IEnumerable<string> exportedPropertyes)
        {
            var result = string.Empty;
            foreach (string propertyName in exportedPropertyes)
                {
                    object val = this.GetType().GetProperty(propertyName).GetValue(this, null);
                    result += (result.Length > 0 ? ";" : string.Empty) + string.Format("\"{0}\"", val == null || val.ToString() == ExelConverter.Core.DataObjects.Region.Unknown ? string.Empty : val.ToString().Replace("\"", "\"\""));
                }
            return result;
        }

        public List<string> ToItemsList(IEnumerable<string> exportedPropertyes)
        {
            List<string> result = new List<string>();
            foreach (string propertyName in exportedPropertyes)
            {
                object val = this.GetType().GetProperty(propertyName).GetValue(this, null);
                result.Add(val == null || val == ExelConverter.Core.DataObjects.Region.Unknown ? string.Empty : val.ToString());
            }
            return result;
        }

        #region INotifyPropertyChanged

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
