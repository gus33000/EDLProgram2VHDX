using DiscUtils;
using DiscUtils.Containers;
using DiscUtils.Streams;
using SplitEDLCombiner.XML;

namespace SplitEDLCombiner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Input XML Programming File: {args[0]}");
            Console.WriteLine($"Output VHDX Disk File: {args[1]}");
            Console.WriteLine($"Sector Size: {args[2]}");

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("ERROR: XML Does not exist");
                return;
            }

            if (File.Exists(args[1]))
            {
                Console.WriteLine("ERROR: VHDX Already exists");
                return;
            }

            uint sectorSize = uint.Parse(args[2]);

            Data data = XmlReader.DeserializeDataXmlFile(args[0]);

            foreach (XML.Program program in data.Program)
            {
                if (program.Label == "PrimaryGPT")
                {
                    GPT.GPT gpt = new(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(args[0])!, program.Filename)), sectorSize);
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
                            Data = File.OpenRead(Path.Combine(Path.GetDirectoryName(args[0])!, dataProgram.Filename))
                        };

                        flashingPartitions.Add(flashPart);
                    }

                    Console.WriteLine($"FlashPart counts: {flashingPartitions.Count}");

                    FlashPartStream flashPartStream = new(diskSize, flashingPartitions);

                    ConvertDD2VHD(flashPartStream, args[1], sectorSize);

                    // Cleanup
                    foreach (FlashPart flashingPartition in flashingPartitions)
                    {
                        flashingPartition.Data.Close();
                    }

                    break;
                }
            }

            Console.WriteLine("The end.");
        }



        /// <summary>
        ///     Coverts a raw DD image into a VHD file suitable for FFU imaging.
        /// </summary>
        /// <param name="ddfile">The path to the DD file.</param>
        /// <param name="vhdfile">The path to the output VHD file.</param>
        /// <returns></returns>
        public static void ConvertDD2VHD(Stream inputStream, string vhdfile, uint SectorSize)
        {
            SetupHelper.SetupContainers();

            using DiscUtils.Raw.Disk inDisk = new(inputStream, Ownership.Dispose);

            long diskCapacity = inputStream.Length;
            using Stream fs = new FileStream(vhdfile, FileMode.CreateNew, FileAccess.ReadWrite);
            using DiscUtils.Vhdx.Disk outDisk = DiscUtils.Vhdx.Disk.InitializeDynamic(fs, Ownership.None, diskCapacity, Geometry.FromCapacity(diskCapacity, (int)SectorSize));
            SparseStream contentStream = inDisk.Content;

            StreamPump pump = new()
            {
                InputStream = contentStream,
                OutputStream = outDisk.Content,
                SparseCopy = true,
                SparseChunkSize = (int)SectorSize,
                BufferSize = (int)SectorSize * 1024
            };

            long totalBytes = contentStream.Length;

            DateTime now = DateTime.Now;
            pump.ProgressEvent += (o, e) => { ShowProgress((ulong)e.BytesRead, (ulong)totalBytes, now); };

            Logging.Log("Converting RAW to VHDX");
            pump.Run();
            Console.WriteLine();
        }

        protected static void ShowProgress(ulong readBytes, ulong totalBytes, DateTime startTime)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining =
                TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / readBytes * (totalBytes - readBytes));

            double speed = Math.Round(readBytes / 1024L / 1024L / timeSoFar.TotalSeconds);

            Logging.Log(
                $"{Logging.GetDISMLikeProgressBar((uint)(readBytes * 100 / totalBytes))} {speed}MB/s {remaining:hh\\:mm\\:ss\\.f}",
                returnLine: false);
        }
    }
}
