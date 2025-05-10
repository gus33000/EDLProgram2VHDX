using QualcommEDLProgramStream.XML;
using System.Xml.Serialization;

namespace QualcommEDLProgramStream
{
    internal static class XmlReader
    {
        public static Data DeserializeDataXmlFile(string filePath)
        {
            using StreamReader streamReader = new StreamReader(filePath);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Data));
            return (Data)xmlSerializer.Deserialize(streamReader)!;
        }
    }
}