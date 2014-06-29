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

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlRoot("Convertion")]
    [XmlInclude(typeof(FunctionsBlocksContainer))]
    [XmlInclude(typeof(MappingsContainer))]
    public class FieldConvertionData : INotifyPropertyChanged, ICopyFrom<FieldConvertionData>
    {
        public FieldConvertionData() { }

        [XmlAttribute("SystemName")]
        public string PropertyId { get; set; }

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public ExelConvertionRule Owner { get; set; }

        [XmlAttribute("IsCheckable")]
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
        [XmlAttribute("Name")]
        public string FieldName
        {
            get
            {
                return _fieldName; 
            }
            set
            {
                if (_fieldName != value)
                {
                    _fieldName = value;
                    RaisePropertyChanged("FieldName");
                }
            }
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
            FieldName = source.FieldName;
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
