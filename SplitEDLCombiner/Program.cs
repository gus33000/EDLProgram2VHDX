using DiscUtils;
using DiscUtils.Containers;
using DiscUtils.Streams;
using QualcommEDLProgramStream;

namespace SplitEDLCombiner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputXmlStr = args[0];
            string outputVHDXStr = args[1];
            string sectorSizeStr = args[2];

            Console.WriteLine($"Input XML Programming File: {inputXmlStr}");
            Console.WriteLine($"Output VHDX Disk File: {outputVHDXStr}");
            Console.WriteLine($"Sector Size: {sectorSizeStr}");

            if (!File.Exists(inputXmlStr))
            {
                Console.WriteLine("ERROR: XML Does not exist");
                return;
            }

            if (File.Exists(outputVHDXStr))
            {
                Console.WriteLine("ERROR: VHDX Already exists");
                return;
            }

            uint sectorSize = uint.Parse(sectorSizeStr);

            Stream? flashPartStream = ParseProgramXML.GetStream(inputXmlStr, sectorSize);

            if (flashPartStream == null)
            {
                Console.WriteLine("ERROR: Could not build stream out of XML file");
                return;
            }

            ConvertDD2VHD(flashPartStream, outputVHDXStr, sectorSize);

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
