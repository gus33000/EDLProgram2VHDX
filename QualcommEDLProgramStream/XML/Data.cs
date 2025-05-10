using System.Xml.Serialization;

namespace QualcommEDLProgramStream.XML
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