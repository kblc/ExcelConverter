using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.Converter.CommonTypes;
using ExelConverter.Core.DataWriter;
using System.Data;
using System.Collections.ObjectModel;
using ExelConverter.Core.Settings;
using System.ComponentModel;
using ExelConverter.Core.DataAccess;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Helpers;
using ExelConverter.Core.Converter.Functions;

namespace ExelConverter.Core.Converter
{
    public class SheetRulePair
    {
        public IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> AllowedSheets { get; set; }
        public SheetRulePair(IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> allowedSheets) : this()
        {
            this.AllowedSheets = allowedSheets;
        }
        public SheetRulePair() { }
        public ExelConverter.Core.ExelDataReader.ExelSheet Sheet
        {
            get
            {
                if (AllowedSheets == null)
                    return null;
                else
                    return AllowedSheets.FirstOrDefault(s => s.Name.ToLower().Trim() == SheetName);
            }
            set
            {
                SheetName = value == null ? string.Empty : value.Name;
            }
        }
        public ExelConverter.Core.Converter.ExelConvertionRule Rule { get; set; }
        public string SheetName { get; set; }
    }

    public interface ICopyFrom<T>
    {
        T CopyFrom(T source);
    }

    [Serializable]
    public class ExelConvertionRule : INotifyPropertyChanged, ICopyFrom<ExelConvertionRule>
    {
        [NonSerialized]
        public const string DefaultName = "По умолчанию";

        [NonSerialized]
        private IDataAccess _appDataAccess = new DataAccess.DataAccess(); 

        public ExelConvertionRule()
        {
            UpdateMappingTablesValues();
        }

        [NonSerialized]
        private ObservableCollection<ImageParsingData> _mapParsingData;
        public ObservableCollection<ImageParsingData> MapParsingData
        {
            get { return _mapParsingData ?? (_mapParsingData = new ObservableCollection<ImageParsingData>()); }
            set
            {
                if (_mapParsingData != value)
                {
                    _mapParsingData = value;
                    RaisePropertyChanged("MapParsingData");
                }
            }
        }

        [NonSerialized]
        private ObservableCollection<ImageParsingData> _photoParsingData;
        public ObservableCollection<ImageParsingData> PhotoParsingData
        {
            get { return _photoParsingData ?? (_photoParsingData = new ObservableCollection<ImageParsingData>()); }
            set
            {
                if (_photoParsingData != value)
                {
                    _photoParsingData = value;
                    RaisePropertyChanged("PhotoParsingData");
                }
            }
        }

        [NonSerialized]
        private ImageParsingData _selectedPhotoParsingData;
        public ImageParsingData SelectedPhotoParsingData
        {
            get { return _selectedPhotoParsingData ?? (_selectedPhotoParsingData = PhotoParsingData.FirstOrDefault()); }
            set
            {
                if (_selectedPhotoParsingData != value)
                {
                    _selectedPhotoParsingData = value;
                    RaisePropertyChanged("SelectedPhotoParsingData");
                }
            }
        }

        [NonSerialized]
        private ImageParsingData _selectedMapParsingData;
        public ImageParsingData SelectedMapParsingData
        {
            get { return _selectedPhotoParsingData ?? (_selectedPhotoParsingData = MapParsingData.FirstOrDefault()); }
            set
            {
                if (_selectedMapParsingData != value)
                {
                    _selectedMapParsingData = value;
                    RaisePropertyChanged("SelectedMapParsingData");
                }
            }
        }

        //[NonSerialized]
        private ObservableCollection<FieldConvertionData> _convertionData;
        public ObservableCollection<FieldConvertionData> ConvertionData 
        {
            get
            {
                return _convertionData ??
                    (
                        _convertionData = new ObservableCollection<FieldConvertionData>
                        {
                            new FieldConvertionData{FieldName = "Код", PropertyId="Code", Owner = this},
                            new FieldConvertionData{FieldName = "Код DOORS", PropertyId="CodeDoors", Owner = this},
                            new FieldConvertionData{FieldName = "Тип", PropertyId="Type", Owner = this, IsCheckable = true },
                            new FieldConvertionData{FieldName = "Сторона", PropertyId="Side", Owner = this},
                            new FieldConvertionData{FieldName = "Размер", PropertyId="Size", Owner = this},
                            new FieldConvertionData{FieldName = "Освещение", PropertyId="Light", Owner = this},
                            new FieldConvertionData{FieldName = "Ограничения", PropertyId="Restricted", Owner = this},
                            new FieldConvertionData{FieldName = "Город", PropertyId="City", Owner = this, IsCheckable = true},
                            new FieldConvertionData{FieldName = "Район", PropertyId="Region", Owner = this},
                            new FieldConvertionData{FieldName = "Адрес", PropertyId="Address", Owner = this},
                            new FieldConvertionData{FieldName = "Описание", PropertyId="Description", Owner = this},
                            new FieldConvertionData{FieldName = "Цена", PropertyId="Price", Owner = this},
                            new FieldConvertionData{FieldName = "Фото", PropertyId="Photo_img", Owner = this},
                            new FieldConvertionData{FieldName = "Фото расп.", PropertyId="Location_img", Owner = this}
                        }
                );
            }
            //set
            //{
            //    if (_convertionData != value)
            //    {
            //        _convertionData = value;
            //        RaisePropertyChanged("ConvertionData");
            //    }
            //}
        }

        private int _id;
        [field: NonSerializedAttribute()]
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private int _fkOperatorId;
        [field: NonSerializedAttribute()]
        public int FkOperatorId
        {
            get { return _fkOperatorId; }
            set { _fkOperatorId = value; }
        }

        private string _name;
        [field: NonSerializedAttribute()]
        public string Name 
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        private bool _findMainHeaderByTags;
        [field: NonSerializedAttribute()]
        public bool FindMainHeaderByTags
        {
            get { return _findMainHeaderByTags; }
            set
            {
                if (_findMainHeaderByTags != value)
                {
                    _findMainHeaderByTags = value;
                    RaisePropertyChanged("FindMainHeaderByTags");
                }
            }
        }

        private bool _findSheetHeadersByTags;
        [field: NonSerializedAttribute()]
        public bool FindSheetHeadersByTags
        {
            get { return _findSheetHeadersByTags; }
            set
            {
                if (_findSheetHeadersByTags != value)
                {
                    _findSheetHeadersByTags = value;
                    RaisePropertyChanged("FindSheetHeadersByTags");
                }
            }
        }

        private ObservableCollection<SearchTag> _mainHeaderSearchTags;
        public ObservableCollection<SearchTag> MainHeaderSearchTags
        {
            get { return _mainHeaderSearchTags ?? (_mainHeaderSearchTags = new ObservableCollection<SearchTag>()); }
            set
            {
                if (_mainHeaderSearchTags != value)
                {
                    _mainHeaderSearchTags = value;
                    RaisePropertyChanged("");
                }
            }
        }

        private ObservableCollection<SearchTag> _sheetHeadersSearchTags;
        public ObservableCollection<SearchTag> SheetHeadersSearchTags
        {
            get { return _sheetHeadersSearchTags ?? (_sheetHeadersSearchTags = new ObservableCollection<SearchTag>()); }
            set
            {
                if (_sheetHeadersSearchTags != value)
                {
                    _sheetHeadersSearchTags = value;
                    RaisePropertyChanged("SheetHeadersSearchTags");
                }
            }
        }

        #region Some methods

        public List<OutputRow> Convert(ExelSheet sheet, string[] conversionDataLimiter = null)
        {
            Guid logSession = Log.SessionStart("ExelConvertionRule.Convert()");
            var result = new List<OutputRow>();
            try
            {
                if (sheet.MainHeader != null)
                {
                    var initialRow = 0;
                    var headers = sheet.SheetHeaders.Headers;
                    var subheaders = sheet.SheetHeaders.Subheaders;

                    initialRow = sheet.Rows.IndexOf(sheet.MainHeader) + sheet.MainHeaderRowCount;

                    int logPart = 0;
                    for (var i = initialRow; i < sheet.Rows.Count; i++)
                        try
                        {
                            logPart = 1;
                            if (headers.Any(h => h.RowNumber == i) || subheaders.Any(h => h.RowNumber == i))
                            {
                                continue;
                            }
                            logPart++;
                            var outputRow = new OutputRow();
                            logPart++;
                            foreach (var convertionData in (conversionDataLimiter == null ? ConvertionData : ConvertionData.Where(i2 => conversionDataLimiter.Contains(i2.PropertyId))))
                            {
                                var cellResultContent = string.Empty;
                                int subLogPart = 1;
                                try
                                {
                                    subLogPart = 1;
                                    var propertyOld = typeof(OutputRow).GetProperty("Original" + convertionData.PropertyId);
                                    if (propertyOld != null)
                                    {
                                        string oldValue = null;
                                        subLogPart++;
                                        var blocks = convertionData.Blocks.Blocks.FirstOrDefault();
                                        if (blocks != null)
                                        {
                                            subLogPart++;
                                            var startFunc = blocks.UsedFunctions.FirstOrDefault();
                                            if (startFunc != null)
                                            {
                                                subLogPart++;
                                                if (startFunc.Function.SelectedParameter == FunctionParameters.CellName)
                                                {
                                                    subLogPart++;
                                                    var header = sheet.Rows[sheet.Rows.IndexOf(sheet.MainHeader)].Cells.Select(c => c.Value).ToList();
                                                    var columnNumber = header.IndexOf(header.Where(s => s.Trim().ToLower() == startFunc.Function.ColumnName.Trim().ToLower()).FirstOrDefault());
                                                    if (columnNumber >= 0 && sheet.Rows.ElementAt(i).Cells.Count > columnNumber)
                                                    {
                                                        oldValue = sheet.Rows.ElementAt(i).Cells.ElementAt(columnNumber).Value;
                                                    }
                                                }
                                                else if (startFunc.Function.SelectedParameter == FunctionParameters.CellNumber)
                                                {
                                                    subLogPart++;
                                                    if (sheet.Rows.ElementAt(i).Cells.Count > startFunc.Function.ColumnNumber)
                                                    {
                                                        oldValue = sheet.Rows.ElementAt(i).Cells.ElementAt(startFunc.Function.ColumnNumber).Value;
                                                    }
                                                }
                                            }
                                        }
                                        if (oldValue != null)
                                            propertyOld.SetValue(outputRow, oldValue.Trim(), null);
                                    }

                                    cellResultContent = convertionData.Blocks.Run(sheet, i, convertionData);
                                    if (cellResultContent != null)
                                    {
                                        cellResultContent = cellResultContent.Trim();
                                    }

                                    var property = typeof(OutputRow).GetProperty(convertionData.PropertyId);
                                    if (property != null)
                                        property.SetValue(outputRow, cellResultContent, null);
                                }
                                catch(Exception ex)
                                {
                                    throw new Exception(string.Format("exception on update field '{0}' at sub step '{1}';", convertionData.PropertyId, subLogPart), ex);
                                }
                            }
                            logPart++;
                            if (!string.IsNullOrWhiteSpace(outputRow.Region) && outputRow.Region.Contains('(') && outputRow.Region.Contains(')'))
                            {
                                //outputRow.Region = outputRow.Region.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                                outputRow.Region = outputRow.Region.Substring(0, outputRow.Region.LastIndexOf("(") - 1).Trim();
                            }
                            logPart++;
                            if (!string.IsNullOrWhiteSpace(outputRow.Size) && outputRow.Size.Contains('(') && outputRow.Size.Contains(')'))
                            {
                                //outputRow.Size = outputRow.Size.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                                outputRow.Size = outputRow.Size.Substring(0, outputRow.Size.LastIndexOf("(") - 1).Trim();
                            }
                            logPart++;
                            if (!string.IsNullOrWhiteSpace(outputRow.Code) && !string.IsNullOrEmpty(outputRow.Code.Trim()) || !string.IsNullOrWhiteSpace(outputRow.Code.Trim()))
                            {
                                result.Add(outputRow);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("exception at row line: '{0}', log part: '{1}';", i, logPart), ex);
                        }
                }
                else
                    Log.Add(string.Format("sheet '{0}' has not header!", sheet.Name));
            }
            catch (Exception ex)
            {
                Log.Add(logSession, Helpers.Log.GetExceptionText(ex));
            }
            finally
            {
                RemoveRepeatingId(result);
                Log.SessionEnd(logSession);
            }
            
            return result;
        }

        //private string RenameRepeatingId(List<OutputRow> list ,string value, int repeatNumber = 1)
        //{
        //    var result = string.Empty;
        //    var temp = value;
        //    for (; ; )
        //    {
        //        if (list.Any(r => r.Code == temp))
        //        {
        //            temp = value + "_" + repeatNumber;
        //            repeatNumber++;
        //        }
        //        else
        //        {
        //            result = temp;
        //            break;
        //        }
        //    }

        //    return result;
        //}

        public static void RemoveRepeatingId(List<OutputRow> list)
        {
            foreach (var row in list)
            {
                var sameIds = list.Where(r => r.Code == row.Code && row != r).ToArray();
                for (var i = 0; i < sameIds.Length; i++)
                    sameIds[i].Code = string.Format("{0}_{1}", sameIds[i].Code, i + 1);
            }
        }

        private void UpdateMappingTablesValues()
        {
            if (!SettingsProvider.IsInitialized)
                throw new Exception("SettingsProvider is not initialized!");

            for (var j = 0; j < ConvertionData.Count; j++)
            {
                //ConvertionData[j].UpdateFunctions(ExelConverter.Core.Converter.Functions.FunctionBase.GetSupportedFunctions());
                if (ConvertionData[j].PropertyId == "Type")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedTypes.Where(t => t != null).Select(t => t.Name).ToArray());
                }
                if (ConvertionData[j].PropertyId == "Side")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedSides);
                }
                if (ConvertionData[j].PropertyId == "Size")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedSizes.Select(s => s.Name).ToArray());
                }
                if (ConvertionData[j].PropertyId == "Region")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedRegions.Select(r => r.Name).ToArray());
                }
                if (ConvertionData[j].PropertyId == "City")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedCities.Select(c => c.Name).ToArray());
                }
                if (ConvertionData[j].PropertyId == "Light")
                {
                    ConvertionData[j].MappingsTable.LoadAllowedValues(SettingsProvider.AllowedLights);
                }
                if (ConvertionData[j].PropertyId == "Adress")
                {
                    ConvertionData[j].PropertyId = "Address";
                }
            }
        }

        public void InitializeImageParsingData()
        {
            _appDataAccess = new DataAccess.DataAccess();
            #region Map
            MapParsingData = new ObservableCollection<ImageParsingData>();
            var mapSizes = 
                _appDataAccess
                .GetFillRects(FkOperatorId, "location")
                .Select(r => new { Width = r.Width, Height = r.Height })
                .Distinct()
                .ToArray();

            foreach (var size in mapSizes)
            {
                var ipd = new ImageParsingData
                {
                    Height = size.Height,
                    Width = size.Width,
                    DrawingArea = new Canvas
                    {
                        Height = size.Height,
                        Width = size.Width,
                    }

                };
                ipd.DrawingArea.MouseLeftButtonDown += ipd.drawingPanelMouseDown;
                var sizeRects = _appDataAccess.GetFillRects(FkOperatorId).Where(r => r.Height == size.Height && r.Width == size.Width && r.Type == "location").ToArray();
                foreach (var rect in sizeRects)
                {
                    var rectangle = new Rectangle
                    {
                        Fill = Brushes.Green,
                        Opacity = 0.3,
                        Height = Math.Abs(rect.Y1 - rect.Y2),
                        Width = Math.Abs(rect.X1 - rect.X2)
                    };
                    ipd.DrawingArea.Children.Add(rectangle);
                    Canvas.SetTop(rectangle, rect.Y1);
                    Canvas.SetLeft(rectangle, rect.X1);
                    rectangle.MouseRightButtonDown += (s, e) =>
                    {
                        ipd.DrawingArea.Children.Remove(rectangle);
                        _appDataAccess.RemoveFillRectangle(rect.ID);
                    };
                }
                MapParsingData.Add(ipd);
            }
            #endregion
            #region Photo
            PhotoParsingData = new ObservableCollection<ImageParsingData>();
            var photoSizes = 
                _appDataAccess
                .GetFillRects(FkOperatorId, "photo")
                .Select(r => new { Width = r.Width, Height = r.Height })
                .Distinct()
                .ToArray();
            foreach (var size in photoSizes)
            {
                var ipd = new ImageParsingData
                {
                    Height = size.Height,
                    Width = size.Width,
                    DrawingArea = new Canvas
                    {
                        Height = size.Height,
                        Width = size.Width,
                    }

                };
                ipd.DrawingArea.MouseLeftButtonDown += ipd.drawingPanelMouseDown;
                var sizeRects = _appDataAccess.GetFillRects(FkOperatorId).Where(r => r.Height == size.Height && r.Width == size.Width && r.Type == "photo").ToArray();
                foreach (var rect in sizeRects)
                {
                    var rectangle = new Rectangle
                    {
                        Fill = Brushes.Green,
                        Opacity = 0.3,
                        Height = Math.Abs(rect.Y1 - rect.Y2),
                        Width = Math.Abs(rect.X1 - rect.X2)
                    };
                    ipd.DrawingArea.Children.Add(rectangle);
                    Canvas.SetTop(rectangle, rect.Y1);
                    Canvas.SetLeft(rectangle, rect.X1);
                    rectangle.MouseRightButtonDown += (s, e) =>
                    {
                        ipd.DrawingArea.Children.Remove(rectangle);
                        _appDataAccess.RemoveFillRectangle(rect.ID);
                    };
                }
                PhotoParsingData.Add(ipd);
            }
            #endregion
        }

        #endregion
        #region Serialization

        public string Serialize()
        {
            var stringRule = string.Empty;
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                var bytes = stream.ToArray();
                stringRule = System.Convert.ToBase64String(bytes);
            }
            return stringRule;
        }

        public byte[] SerializeToBytes()
        {
            var byteRule = new byte[] {};
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(compressionStream);
                    }
                    byteRule = compressedStream.ToArray();
                }
            }
            return byteRule;
        }

        public string SerializeXML()
        {
            string result = string.Empty;

            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(ExelConvertionRule));
            using (MemoryStream stream = new MemoryStream())
            {
                s.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                result = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }
            return result;
        }

        public static ExelConvertionRule DeserializeFromBytes(byte[] image)
        {
            ExelConvertionRule result = null;

            if (image != null && image.Length > 0)
                using (var compressedStream = new MemoryStream(image))
                using (var decompressStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    var formatter = new BinaryFormatter();
                    result = (ExelConvertionRule)formatter.Deserialize(decompressStream);
                }
            else
                result = new ExelConvertionRule() { Name = DefaultName };
            UpdateAfterDeserialize(result);
            return result;
        }

        public static ExelConvertionRule DeserializeFromB64String(string obj)
        {
            ExelConvertionRule result = null;
            if (!string.IsNullOrWhiteSpace(obj))
            {
                byte[] bytes = System.Convert.FromBase64String(obj);
                using (var stream = new MemoryStream(bytes))
                {
                    var formatter = new BinaryFormatter();
                    result = (ExelConvertionRule)formatter.Deserialize(stream);
                }
            }
            else
                result = new ExelConvertionRule() { Name = DefaultName };
            UpdateAfterDeserialize(result);
            return result;
        }

        private static void UpdateAfterDeserialize(ExelConvertionRule rule)
        {
            rule.UpdateMappingTablesValues();
            foreach (var convData in rule.ConvertionData)
                foreach (var block in convData.Blocks.Blocks)
                    block.UpdateFunctionsList();
        }

        #endregion
        #region INotifyPropertyChanged

        [field: NonSerializedAttribute()] //ADDED
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public ExelConvertionRule CopyFrom(ExelConvertionRule source)
        {
            SheetHeadersSearchTags.Clear();
            foreach (var item in source.SheetHeadersSearchTags)
                SheetHeadersSearchTags.Add((new SearchTag()).CopyFrom(item));
            MainHeaderSearchTags.Clear();
            foreach (var item in source.MainHeaderSearchTags)
                MainHeaderSearchTags.Add((new SearchTag()).CopyFrom(item));
            MapParsingData.Clear();
            foreach (var item in source.MapParsingData)
                MapParsingData.Add((new ImageParsingData()).CopyFrom(item));
            PhotoParsingData.Clear();
            foreach (var item in source.PhotoParsingData)
                PhotoParsingData.Add((new ImageParsingData()).CopyFrom(item));
            ConvertionData.Clear();
            foreach (var item in source.ConvertionData)
            {
                var cd = (new FieldConvertionData()).CopyFrom(item);
                cd.Owner = this;
                ConvertionData.Add(cd);
            }

            FindSheetHeadersByTags = source.FindSheetHeadersByTags;
            FindMainHeaderByTags = source.FindMainHeaderByTags;
            Name = source.Name;
            FkOperatorId = source.FkOperatorId;
            _selectedPhotoParsingData = null;
            _selectedMapParsingData = null;

            UpdateMappingTablesValues();

            return this;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public class SearchTag : INotifyPropertyChanged, ICopyFrom<SearchTag>
    {
        private string _tag;
        public string Tag
        {
            get { return _tag; }
            set
            {
                if (_tag != value)
                {
                    _tag = value;
                    RaisePropertyChanged("Tag");
                }
            }
        }

        [field: NonSerializedAttribute()] //ADDED
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SearchTag CopyFrom(SearchTag source)
        {
            this.Tag = source.Tag;
            return this;
        }
    }
}
