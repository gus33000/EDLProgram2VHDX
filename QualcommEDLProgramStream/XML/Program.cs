using System.Xml.Serialization;

namespace QualcommEDLProgramStream.XML
{
    [XmlRoot(ElementName = "program")]
    public class Program
    {
        [XmlAttribute(AttributeName = "filename")]
        public string Filename
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "label")]
        public string Label
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "physical_partition_number")]
        public string Physical_partition_number
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "start_sector")]
        public string Start_sector
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "num_partition_sectors")]
        public string Num_partition_sectors
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "SECTOR_SIZE_IN_BYTES")]
        public string SECTOR_SIZE_IN_BYTES
        {
            get; set;
        }
    }
}