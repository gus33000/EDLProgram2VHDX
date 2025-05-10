using System.Xml.Serialization;

namespace SplitEDLCombiner.XML
{
    [XmlRoot(ElementName = "data")]
    public class Data
    {
        [XmlElement(ElementName = "program")]
        public List<Program> Program
        {
            get; set;
        }
    }
}