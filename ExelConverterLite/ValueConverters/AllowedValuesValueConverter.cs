using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.Settings;

namespace ExelConverterLite.ValueConverters
{
    public class AllowedValuesValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var convertionData = (FieldConvertionData)value;

            var updatedAllowedValues = new List<string>();
            
            if (convertionData.FieldName == "Район")
            {
                foreach (var allowedValue in convertionData.MappingsTable.AllowedValues)
                {
                    var regions = SettingsProvider.AllowedRegions.Where(r=>r.Name == allowedValue).ToArray();
                    var cities = SettingsProvider.AllowedCities.Where(c=> regions.Any(r=>r.FkCityId == c.Id)).ToArray();
                    foreach (var city in cities)
                    {
                        var str = allowedValue + " -> " + city.Name;
                        updatedAllowedValues.Add(str);
                    }
                    
                }
                return updatedAllowedValues;
            }
            else if (convertionData.FieldName == "Размер")
            {
                foreach (var allowedValue in convertionData.MappingsTable.AllowedValues)
                {
                    var sizes = SettingsProvider.AllowedSizes.Where(r => r.Name == allowedValue).ToArray();
                    var types = SettingsProvider.AllowedTypes.Where(t => sizes.Any(s=>s.FkTypeId==t.Id)).ToArray();
                    
                    foreach (var type in types)
                    {
                        var str = allowedValue + " -> "+type.Name;
                        updatedAllowedValues.Add(str);
                    }
                }
                return updatedAllowedValues;
            }

            return convertionData.MappingsTable.AllowedValues;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
