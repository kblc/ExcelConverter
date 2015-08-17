using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ExelConverter.Core.ExelDataReader;
using System.Xml.Serialization;

namespace ExelConverter.Core.Converter.CommonTypes
{
    [Serializable]
    [XmlInclude(typeof(FunctionsBlock))]
    [XmlRoot("FunctionalContainer")]
    public class FunctionsBlocksContainer : INotifyPropertyChanged, ICopyFrom<FunctionsBlocksContainer>
    {
        public FunctionsBlocksContainer() { }

        private ObservableCollection<FunctionsBlock> _blocks;
        [XmlArray("FunctionalBlocks")]
        public ObservableCollection<FunctionsBlock> Blocks
        {
            get { return _blocks ?? (_blocks = new ObservableCollection<FunctionsBlock>()); }
            set
            {
                if (Blocks == value)
                    return;

                Blocks.Clear();
                if (value != null)
                    foreach (var b in value)
                        Blocks.Add(b);

                RaisePropertyChanged("Blocks");
            }
        }

        #region Run

        public string Run(ExelSheet sheet, int rowNumber, FieldConvertionData convertionData)
        {
            var result = string.Empty;
            foreach (var block in Blocks)
                try
                {
                    if (block.CheckCanExecute(sheet, rowNumber, convertionData))
                    {
                        result += block.Execute(sheet, rowNumber, convertionData);
                        if (block.ReturnAfterExecute)
                        {
                            if (convertionData.MappingNeeded)
                            {
                                return MappingTable(result, convertionData);
                            }
                            return result;
                        }
                    }
                }
                catch
                {
                    
                    throw;
                }
            if (convertionData.MappingNeeded)
            {
                return MappingTable(result, convertionData);
            }
            return result;
        }

        private string MappingTable(string value, FieldConvertionData convertinoData)
        {
            foreach (var mapping in convertinoData.MappingsTable)
            {
                if (convertinoData.MappingsTable.AbsoluteCoincidence)
                {
                    if (value.Trim() == mapping.From.Trim())
                    {
                        return mapping.To;
                    }
                }
                else
                {
                    var res = CheckMapping(value, convertinoData.MappingsTable);
                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        return res;
                    }
                }
            }
            return value;
        }

        private string CheckMapping(string value, MappingsContainer mappings)
        {  
            foreach (var mapping in mappings)
            {
                if (value.Trim().StartsWith(mapping.From.Trim()) || value.Trim() == mapping.From.Trim())
                {
                    var result = mapping.To;
                    if (result != null)
                    {
                        return result;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            if (value.Length <= 1)
            {
                return null;
            }
            return CheckMapping(value.Substring(1), mappings);
        }

        #endregion
        #region INotifyPropertyChanged

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public FunctionsBlocksContainer CopyFrom(FunctionsBlocksContainer source)
        {
            Blocks.Clear();
            foreach (var item in source.Blocks)
            {
                Blocks.Add( (new FunctionsBlock()).CopyFrom(item) );
            }
            return this;
        }
    }
}
