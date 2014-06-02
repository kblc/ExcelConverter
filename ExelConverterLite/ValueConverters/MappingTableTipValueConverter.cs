using ExelConverter.Core.Converter.CommonTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ExelConverterLite.ValueConverters
{
    class SearchResult
    {
        public string Result { get; set; }
    }

    class MappingTableTipValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DataTable result = null;
            if (App.Locator.Import.SelectedSheet != null)
            {
                var expectedValueFrom = (string)values[0];
                var convertionData = (FieldConvertionData)values[1];
                var tempRes = new List<SearchResult>();

                for (var i = 0; i < App.Locator.Import.SelectedSheet.Rows.Count; i++)
                {
                    var str = string.Empty;
                    foreach (var block in convertionData.Blocks.Blocks)
                    {
                        if (block.CheckCanExecute(App.Locator.Import.SelectedSheet, i, convertionData))
                        {
                            str += block.Execute(App.Locator.Import.SelectedSheet, i, convertionData);
                            if (block.ReturnAfterExecute)
                            {
                                break;
                            }
                        }
                    }
                    tempRes.Add(new SearchResult { Result = str });
                }
                var indexes = tempRes.Where(s => s.Result == expectedValueFrom).Select(s => tempRes.IndexOf(s)).ToArray();

                result = App.Locator.Import.SelectedSheet.AsDataTable(indexes);
            }

            return result != null ? result.Rows.Count > 0 ? result.DefaultView : null : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
