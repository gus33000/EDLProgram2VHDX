using QualcommEDLProgramStream.XML;

namespace QualcommEDLProgramStream
{
    public class ParseProgramXML
    {
        public static Stream? GetStream(string inputXmlStr, uint sectorSize)
        {
            Data data = XmlReader.DeserializeDataXmlFile(inputXmlStr);

            foreach (XML.Program program in data.Program)
            {
                if (program.Label == "PrimaryGPT")
                {
                    GPT.GPT gpt = new(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(inputXmlStr)!, program.Filename)), sectorSize);
                    ulong diskSize = (gpt.LastUsableSector + 1) * sectorSize;

                    Console.WriteLine($"Disk size: {diskSize}");

                    List<FlashPart> flashingPartitions = [];

                    foreach (XML.Program dataProgram in data.Program)
                    {
                        if (string.IsNullOrEmpty(dataProgram.Filename))
                        {
                            continue;
                        }

                        string startSector = dataProgram.Start_sector;
                        startSector = startSector.Replace("NUM_DISK_SECTORS", (gpt.LastUsableSector + 1).ToString());
                        startSector = startSector.Trim('.');

                        Console.WriteLine($"STR: {startSector}");

                        ulong startSectorLong = 0;

                        if (startSector.EndsWith("-5"))
                        {
                            string newStr = startSector[..^2];
                            startSectorLong = ulong.Parse(newStr) - 5;
                        }
                        else
                        {
                            startSectorLong = ulong.Parse(startSector);
                        }

                        Console.WriteLine($"UL: {startSectorLong}");

                        FlashPart flashPart = new()
                        {
                            LocationOnDisk = startSectorLong * sectorSize,
                            Data = File.OpenRead(Path.Combine(Path.GetDirectoryName(inputXmlStr)!, dataProgram.Filename))
                        };

                        flashingPartitions.Add(flashPart);
                    }

                    Console.WriteLine($"FlashPart counts: {flashingPartitions.Count}");

                    FlashPartStream flashPartStream = new(diskSize, flashingPartitions);

                    return flashPartStream;
                }
            }

            return null;
        }
    }
}
