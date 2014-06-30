using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading.Tasks;

using ExelConverter.Core.ExelDataReader;
using ExelConverter.Core.Converter.Functions;
using ExelConverter.Core.Converter.CommonTypes;
using System.Xml.Serialization;

using Helpers;
using System.Reflection;

namespace ExelConverter.Core.Converter.CommonTypes
{
    public enum FieldConvertionType
    {
        [Description("Код")]
        Code,
        [Description("Код DOORS")]
        CodeDoors,
        [Description("Тип")]
        Type,
        [Description("Сторона")]
        Side,
        [Description("Размер")]
        Size,
        [Description("Освещение")]
        Light,
        [Description("Ограничения")]
        Restricted,
        [Description("Город")]
        City,
        [Description("Район")]
        Region,
        [Description("Адрес")]
        Address,
        [Description("Описание")]
        Description,
        [Description("Цена")]
        Price,
        [Description("Фото")]
        Photo_img,
        [Description("Фото расп.")]
        Location_img
    };

    [Serializable]
    [XmlRoot("Convertion")]
    [XmlInclude(typeof(FunctionsBlocksContainer))]
    [XmlInclude(typeof(MappingsContainer))]
    public class FieldConvertionData : INotifyPropertyChanged, ICopyFrom<FieldConvertionData>
    {
        public FieldConvertionData()
        {
        }

        [XmlAttribute("SystemName")]
        public string PropertyId { get; set; }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ExelConvertionRule Owner { get; set; }

        //[XmlAttribute("IsCheckable")]
        [XmlIgnore]
        public bool IsCheckable { get; set; }

        private string _separatorName;
        [XmlIgnore]
        public string SeparatorName
        {
            get { return _separatorName; }
            set
            {
                if (_separatorName != value)
                {
                    _separatorName = value;
                    RaisePropertyChanged("SeparatorName");
                }
            }
        }

        private string _fieldName;
        [XmlIgnore]
        public string FieldName
        {
            get
            {
                string fieldName = string.Empty;
                foreach(var v in typeof(FieldConvertionType).GetEnumValues())
                    if (v.ToString().Like(PropertyId))
                    {
                        fieldName = GetEnumDescription(v as Enum);
                        break;
                    }

                return fieldName; 
            }
        }

        private static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        private string _stringFunction;
        [XmlIgnore]
        public string StringFunction
        {
            get { return _stringFunction; }
            set
            {
                if (_stringFunction != value)
                {
                    _stringFunction = value;
                    RaisePropertyChanged("StringFunction");
                }
            }
        }

        private bool _mappingNeeded;
        [XmlAttribute("MappingNeeded")]
        public bool MappingNeeded
        {
            get { return _mappingNeeded; }
            set
            {
                if (_mappingNeeded != value)
                {
                    _mappingNeeded = value;
                    RaisePropertyChanged("MappingNeeded");
                }
            }
        }

        private FunctionsBlocksContainer _blocks;

        [XmlElement(ElementName = "FunctionalContainer")]
        public FunctionsBlocksContainer Blocks
        {
            get { return _blocks ?? (_blocks = new FunctionsBlocksContainer()); }
            set
            {
                if (Blocks == value)
                    return;

                if (value != null)
                    value.CopyObject(Blocks);

                RaisePropertyChanged("Blocks");
            }
        }

        private bool _absoluteCoincidence;
        [XmlIgnore]
        public bool AbsoluteCoincidence
        {
            get { return _absoluteCoincidence; }
            set
            {
                if (_absoluteCoincidence != value)
                {
                    _absoluteCoincidence = value;
                    RaisePropertyChanged("AbsoluteCoincidence");
                }
            }
        }

        private MappingsContainer _mappingsTable = null;
        [XmlArray("MappingContainer")]
        [XmlArrayItem("Mapping", Type = typeof(Mapping))]
        public MappingsContainer MappingsTable
        {
            get { return _mappingsTable ?? (_mappingsTable = new MappingsContainer() { Owner = this }); }
        }

        #region INotifyPropertyChanged

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        #endregion

        public FieldConvertionData CopyFrom(FieldConvertionData source)
        {
            PropertyId = source.PropertyId;
            IsCheckable = source.IsCheckable;
            SeparatorName = source.SeparatorName;
            //FieldName = source.FieldName;
            StringFunction = source.StringFunction;
            MappingNeeded = source.MappingNeeded;
            AbsoluteCoincidence = source.AbsoluteCoincidence;
            Owner = source.Owner;

            MappingsTable.CopyFrom(source.MappingsTable);
            MappingsTable.Owner = this;

            Blocks.CopyFrom(source.Blocks);

            return this;
        }
    }
}
