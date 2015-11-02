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
using Helpers.Serialization;
using Helpers.WPF;
using ExelConverter.Core.Converter.Functions;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter
{
    public class SheetRulePair : INotifyPropertyChanged
    {
        private IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> allowedSheets = null;
        public IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> AllowedSheets
        {
            get { return allowedSheets; }
            set { if (allowedSheets == value) return; allowedSheets = value; RaisePropertyChanged(nameof(AllowedSheets)); }
        }

        public SheetRulePair(IQueryable<ExelConverter.Core.ExelDataReader.ExelSheet> allowedSheets): this()
        {
            this.AllowedSheets = allowedSheets;
        }
        public SheetRulePair()
        {

        }

        public ExelConverter.Core.ExelDataReader.ExelSheet Sheet
        {
            get
            {
                if (AllowedSheets == null)
                    return null;
                else
                    return AllowedSheets.FirstOrDefault(s => s.Name.ToLower().Trim() == SheetName.ToLower().Trim());
            }
            set
            {
                SheetName = value?.Name ?? string.Empty;
            }
        }
        private ExelConverter.Core.Converter.ExelConvertionRule rule = null;
        public ExelConverter.Core.Converter.ExelConvertionRule Rule { get { return rule; } set { if (rule == value) return; rule = value;  RaisePropertyChanged(nameof(Rule)); RaisePropertyChanged(nameof(Status)); } }

        private string sheetName = string.Empty;
        public string SheetName { get { return sheetName; } set { if (sheetName == value) return; sheetName = value;  RaisePropertyChanged(nameof(SheetName)); RaisePropertyChanged(nameof(Sheet)); RaisePropertyChanged(nameof(Status)); } }

        public string Status
        {
            get
            {
                if (Sheet == null && (Rule == null || string.IsNullOrWhiteSpace(Rule.Name)))
                    return string.Empty;
                if (Sheet == null)
                    return string.Format("Отсутствует лист в сетке для правила '{0}'", Rule.Name);
                if (Rule == null || string.IsNullOrWhiteSpace(Rule.Name))
                    return string.Format("Отсутствует правило для листа '{0}'", Sheet.Name);
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            var e = PropertyChanged;
            if (e != null)
                e(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface ICopyFrom<T>
    {
        T CopyFrom(T source);
    }

    [Serializable]
    [XmlRoot("Rule")]
    [XmlInclude(typeof(FieldConvertionData))]
    [XmlInclude(typeof(SearchTag))]
    public class ExelConvertionRule : INotifyPropertyChanged, Helpers.Serialization.ISerializable, ICopyFrom<ExelConvertionRule>
    {
        [NonSerialized]
        public const string DefaultName = "По умолчанию";

        [NonSerialized]
        private IDataAccess _appDataAccess = new DataAccess.DataAccess(); 

        public ExelConvertionRule()
        {
            UpdateMappingTablesValues();
            UpdateAfterDeserialize(this);
        }

        [NonSerialized]
        private bool isMapParsingDataLoaded = false;
        [XmlIgnore]
        public bool IsMapParsingDataLoaded { get { return isMapParsingDataLoaded; } }

        [NonSerialized]
        private ObservableCollection<ImageParsingData> _mapParsingData;
        [XmlIgnore]
        public ObservableCollection<ImageParsingData> MapParsingData
        {
            get 
            {
                isMapParsingDataLoaded = true;
                return _mapParsingData ?? (_mapParsingData = LoadMap());
            }
            set
            {
                if (_mapParsingData != value)
                {
                    _mapParsingData = value;
                    isMapParsingDataLoaded = true;
                    RaisePropertyChanged("MapParsingData");
                }
            }
        }

        [NonSerialized]
        private bool isPhotoParsingDataLoaded = false;
        [XmlIgnore]
        public bool IsPhotoParsingDataLoaded { get { return isPhotoParsingDataLoaded; } }

        [NonSerialized]
        private ObservableCollection<ImageParsingData> _photoParsingData;
        [XmlIgnore]
        public ObservableCollection<ImageParsingData> PhotoParsingData
        {
            get
            {
                isPhotoParsingDataLoaded = true;
                return _photoParsingData ?? (_photoParsingData = LoadPhoto()); 
            }
            set
            {
                if (_photoParsingData != value)
                {
                    _photoParsingData = value;
                    isPhotoParsingDataLoaded = true;
                    RaisePropertyChanged("PhotoParsingData");
                }
            }
        }

        private ObservableCollection<FieldConvertionData> _convertionData;

        [XmlArray("Convertions")]
        [XmlArrayItem(ElementName = "Convertion", Type = typeof(FieldConvertionData))]
        public ObservableCollection<FieldConvertionData> ConvertionData 
        {
            get
            {
                if (_convertionData == null)
                {
                    _convertionData = new ObservableCollection<FieldConvertionData>();
                    foreach (var cd in
                        new FieldConvertionData[] 
                            {
                                new FieldConvertionData{PropertyId="Code", Owner = this},
                                new FieldConvertionData{PropertyId="CodeDoors", Owner = this},
                                new FieldConvertionData{PropertyId="Type", Owner = this, IsCheckable = true },
                                new FieldConvertionData{PropertyId="Side", Owner = this},
                                new FieldConvertionData{PropertyId="Size", Owner = this},
                                new FieldConvertionData{PropertyId="Light", Owner = this},
                                new FieldConvertionData{PropertyId="Restricted", Owner = this},
                                new FieldConvertionData{PropertyId="City", Owner = this, IsCheckable = true},
                                new FieldConvertionData{PropertyId="Region", Owner = this},
                                new FieldConvertionData{PropertyId="Address", Owner = this},
                                new FieldConvertionData{PropertyId="Description", Owner = this},
                                new FieldConvertionData{PropertyId="Price", Owner = this},
                                new FieldConvertionData{PropertyId="Photo_img", Owner = this},
                                new FieldConvertionData{PropertyId="Location_img", Owner = this}
                            }
                        )
                        _convertionData.Add(cd);
                    _convertionData.CollectionChanged += _convertionData_CollectionChanged;
                }
                return _convertionData;
            }
            set
            {
                if (value == ConvertionData)
                    return;

                if (value != null)
                    foreach(var cd in value)
                        ConvertionData.Add(cd);
            }
        }

        [NonSerialized]
        private bool supressChangeEvent = false;
        private void _convertionData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (supressChangeEvent)
                return;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                supressChangeEvent = true;
                try
                {
                    foreach (var cd in 
                            e.NewItems
                            .Cast<FieldConvertionData>()
                            .Select(nw => new { NewValue = nw, OldValue = ConvertionData.FirstOrDefault(cd => cd.PropertyId == nw.PropertyId && cd != nw) })
                            .Where(i => i.OldValue != null)
                            .ToArray())
                        {
                            var oldInd = ConvertionData.IndexOf(cd.OldValue);
                            ConvertionData.Remove(cd.OldValue);
                            //ConvertionData.Insert(oldInd, cd.NewValue);
                            cd.NewValue.Owner = this;
                        }
                }
                finally
                {
                    supressChangeEvent = false;
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                supressChangeEvent = true;
                try
                {
                    foreach (var cd in e.OldItems.Cast<FieldConvertionData>())
                        ConvertionData.Add(cd);
                }
                finally
                {
                    supressChangeEvent = false;
                }
            }
        }

        private int _id;
        [XmlIgnore]
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private int _fkOperatorId;
        [XmlIgnore]
        public int FkOperatorId
        {
            get { return _fkOperatorId; }
            set { _fkOperatorId = value; }
        }

        private string _name;
        [XmlAttribute("Name")]
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

        private ObservableCollection<SearchTag> _mainHeaderSearchTags = null;

        [XmlArray(ElementName = "MainHeaderSearchTags")]
        [XmlArrayItem(ElementName="Tag", Type=typeof(SearchTag))]
        public ObservableCollection<SearchTag> MainHeaderSearchTags
        {
            get
            {
                return _mainHeaderSearchTags ?? (_mainHeaderSearchTags = new ObservableCollection<SearchTag>()); 
            }
            set
            {
                if (_mainHeaderSearchTags == value)
                    return;

                _mainHeaderSearchTags.Clear();
                if (value != null)
                {
                    foreach (var i in value)
                        _mainHeaderSearchTags.Add(new SearchTag().CopyFrom(i));
                    RaisePropertyChanged("MainHeaderSearchTags");
                }
            }
        }

        private ObservableCollection<SearchTag> _sheetHeadersSearchTags = null;

        [XmlArray(ElementName = "SheetHeadersSearchTags")]
        [XmlArrayItem(ElementName = "Tag", Type = typeof(SearchTag))]
        public ObservableCollection<SearchTag> SheetHeadersSearchTags
        {
            get { return _sheetHeadersSearchTags ?? (_sheetHeadersSearchTags = new ObservableCollection<SearchTag>()); }
            set
            {
                if (_sheetHeadersSearchTags == value)
                    return;

                _sheetHeadersSearchTags.Clear();
                if (value != null)
                {
                    foreach (var i in value)
                        _sheetHeadersSearchTags.Add(new SearchTag().CopyFrom(i));
                    RaisePropertyChanged("SheetHeadersSearchTags");
                }
            }
        }

        #region Some methods

        public List<OutputRow> Convert(ExelSheet sheet, string[] conversionDataLimiter = null, 
            Action<int> progressAction = null,
            Action<Exception, int> additionalErrorAction = null,
            Func<bool> isCanceled = null)
        {
            Guid logSession = Helpers.Old.Log.SessionStart("ExelConvertionRule.Convert()");
            var result = new List<OutputRow>();
            try
            {
                if (sheet.MainHeader != null)
                {
                    var initialRow = 0;
                    var headers = sheet.SheetHeaders.Headers;
                    var subheaders = sheet.SheetHeaders.Subheaders;

                    initialRow = sheet.Rows.IndexOf(sheet.MainHeader) + sheet.MainHeaderRowCount;
                    int excludeRow = 0;
                    bool hasError = false;

                    int logPart = 0;

                    var exceptions = new List<Exception>();

                    for (var i = initialRow; i < sheet.Rows.Count; i++)
                    {
                        if (isCanceled != null && isCanceled())
                            break;

                        var outputRow = new OutputRow();
                        try
                        {
                            exceptions.Clear();
                            hasError = false;

                            if (progressAction != null)
                                progressAction((int)(((decimal)i / (decimal)sheet.Rows.Count) * 100m));

                            logPart = 1;
                            if (headers.Any(h => h.RowNumber == i) || subheaders.Any(h => h.RowNumber == i))
                                continue;

                            outputRow.OriginalIndex = sheet.Rows[i].Index;
                            outputRow.OriginalSheet = sheet.Name;

                            logPart++;
                            foreach (var convertionData in (conversionDataLimiter == null ? ConvertionData : ConvertionData.Where(i2 => conversionDataLimiter.Contains(i2.PropertyId))))
                            {
                                var cellResultContent = string.Empty;
                                int subLogPart = 1;
                                try
                                {
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
                                                    var header = sheet.Rows[sheet.Rows.IndexOf(sheet.MainHeader)].HeaderCells.Select(c => c.Value).ToList();
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

                                    try
                                    {
                                        cellResultContent = convertionData.Blocks.Run(sheet, i, convertionData);
                                        //if (cellResultContent != null)
                                        //{
                                        //    cellResultContent = cellResultContent.Trim();
                                        //}
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception(string.Format("Сетка '{0}'. Ошибка для правил столбца '{1}' ('{2}')", sheet.Name, convertionData.FieldName, convertionData.PropertyId), ex);
                                    }

                                    var property = typeof(OutputRow).GetProperty(convertionData.PropertyId);
                                    if (property != null)
                                        property.SetValue(outputRow, cellResultContent, null);
                                }
                                catch (Exception ex)
                                {
                                    hasError = true;
                                    if (additionalErrorAction != null)
                                    {
                                        Helpers.Old.Log.Add(logSession, ex);
                                        exceptions.Add(ex);
                                    }
                                    else
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
                            if (!string.IsNullOrWhiteSpace(outputRow?.Code?.Trim()) || (hasError && additionalErrorAction != null))
                            {
                                if (additionalErrorAction != null)
                                {
                                    foreach (var ex in exceptions)
                                        additionalErrorAction(ex, i - initialRow - excludeRow);
                                    exceptions.Clear();
                                }
                                result.Add(outputRow);
                            }
                            else
                                excludeRow++;
                        }
                        catch (Exception ex)
                        {
                            if (additionalErrorAction != null)
                            {
                                Helpers.Old.Log.Add(logSession, ex);
                                if (!result.Contains(outputRow))
                                    result.Add(outputRow);
                                additionalErrorAction(ex, i - initialRow - excludeRow);
                            }
                            else
                                throw new Exception(string.Format("exception at row line: '{0}', log part: '{1}';", i, logPart), ex);
                        }
                    }
                }
                else
                    Log.Add(string.Format("sheet '{0}' has not header!", sheet.Name));
            }
            catch (Exception ex)
            {
                Helpers.Old.Log.Add(logSession, ex);
            }
            finally
            {
                RemoveRepeatingId(result);
                Helpers.Old.Log.SessionEnd(logSession);
            }
            
            return result;
        }

        public static void RemoveRepeatingId(List<OutputRow> list)
        {
            var sameCodes = list
                    .AsParallel()
                    .GroupBy(r => r.Code)
                    .Select(g => new { Code = g.First().Code, Count = g.Count(), Items = g.ToArray() })
                    .Where(r => r.Count > 1 && !string.IsNullOrWhiteSpace(r.Code));

            foreach (var code in sameCodes)
                for (var i = 0; i < code.Count; i++)
                    code.Items.ElementAt(i).Code = string.Format("{0}_{1}", code.Code, i + 1);
        }

        private void UpdateMappingTablesValues()
        {
            bool wasException = false;
            var logSession = Helpers.Old.Log.SessionStart("ExcelConvertionRule.UpdateMappingTablesValues()", true);
            try
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
            catch(Exception ex)
            {
                wasException = true;
                Helpers.Old.Log.Add(logSession, ex);
            }
            finally
            {
                Helpers.Old.Log.SessionEnd(logSession, wasException);
            }
        }

        public ObservableCollection<ImageParsingData> LoadMap()
        {
            _appDataAccess = new DataAccess.DataAccess();
            var result = new ObservableCollection<ImageParsingData>();
            #region Map
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
                result.Add(ipd);
            }
            #endregion
            return result;
        }

        public ObservableCollection<ImageParsingData> LoadPhoto()
        {
            _appDataAccess = new DataAccess.DataAccess();
            var result = new ObservableCollection<ImageParsingData>();
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
                result.Add(ipd);
            }
            #endregion
            return result;
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

        public byte[] SerializeToCompressedBytes()
        {
            return this.SerializeToXML(true).CompressToBytes();
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

        public static ExelConvertionRule DeserializeFromCompressedBytes(byte[] image)
        {
            ExelConvertionRule result;
            typeof(ExelConvertionRule).DeserializeFromXML(image.DecompressFromBytes(), out result);
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
                //if (convData.Blocks != null && convData.Blocks.Blocks != null)
                    foreach (var block in convData.Blocks.Blocks)
                        block.UpdateFunctionsList();

            rule.MainHeaderSearchTags.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (var i in e.NewItems.Cast<SearchTag>())
                        i.PropertyChanged += (s2, e2) =>
                            {
                                rule.RaisePropertyChanged("MainHeaderSearchTagsItem");
                            };
                }
                rule.RaisePropertyChanged("MainHeaderSearchTagsItems");
            };

            rule.SheetHeadersSearchTags.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (var i in e.NewItems.Cast<SearchTag>())
                        i.PropertyChanged += (s2, e2) =>
                        {
                            rule.RaisePropertyChanged("SheetHeadersSearchTagsItem");
                        };
                }
                rule.RaisePropertyChanged("SheetHeadersSearchTagsItems");
            };
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
            SheetHeadersSearchTags = source.SheetHeadersSearchTags;
            MainHeaderSearchTags = source.MainHeaderSearchTags;
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

            //FindSheetHeadersByTags = source.FindSheetHeadersByTags;
            //FindMainHeaderByTags = source.FindMainHeaderByTags;
            Name = source.Name;
            FkOperatorId = source.FkOperatorId;

            UpdateMappingTablesValues();

            return this;
        }

        public override string ToString()
        {
            return Name;
        }

        public void OnDeserialized()
        {
            UpdateAfterDeserialize(this);
        }
    }

    [Serializable]
    [XmlRoot("Tag")]
    public class SearchTag : INotifyPropertyChanged, ICopyFrom<SearchTag>
    {
        private string _tag;
        [XmlAttribute("Value")]
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

        [field: NonSerializedAttribute()]
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
