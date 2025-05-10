namespace QualcommEDLProgramStream
{
    internal class FlashPartStream : Stream
    {
        private readonly long length;

        private long currentPosition = 0;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => currentPosition;
            set
            {
                if (currentPosition < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // Workaround for malformed MBRs
                /*if (currentPosition > Length)
                {
                    throw new EndOfStreamException();
                }*/

                currentPosition = value;
            }
        }

        private List<FlashPart> flashParts;

        public FlashPartStream(ulong totalLength, List<FlashPart> flashParts)
        {
            this.flashParts = flashParts;
            length = (long)totalLength;
        }

        public override void Flush()
        {
            // Nothing to do here
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = count;
            if (currentPosition + bytesToRead > length)
            {
                bytesToRead = (int)(length - currentPosition);
            }

            long i = currentPosition;
            while (i < currentPosition + bytesToRead)
            {
                bool foundPart = false;
                foreach (FlashPart flashPart in flashParts)
                {
                    if (flashPart.LocationOnDisk <= (ulong)i && (ulong)i < flashPart.LocationOnDisk + (ulong)flashPart.Data.Length)
                    {
                        long StreamPos = i - (long)flashPart.LocationOnDisk;
                        flashPart.Data.Seek(StreamPos, SeekOrigin.Begin);

                        long currentForProgressIndex = i - currentPosition;

                        int toRead = (int)(bytesToRead - currentForProgressIndex);
                        if (toRead > flashPart.Data.Length - StreamPos)
                        {
                            toRead = (int)(flashPart.Data.Length - StreamPos);
                        }

                        int bufferOffset = (int)(offset + currentForProgressIndex);
                        flashPart.Data.Read(buffer, bufferOffset, toRead);
                        i += toRead;

                        foundPart = true;
                    }
                }

                if (!foundPart)
                {
                    foreach (FlashPart flashPart in flashParts.OrderBy(t => t.LocationOnDisk))
                    {
                        if (flashPart.LocationOnDisk > (ulong)i)
                        {
                            i = (long)flashPart.LocationOnDisk;
                            break;
                        }
                    }
                }
            }

            currentPosition += bytesToRead;
            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        Position = offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        Position += offset;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        Position = Length + offset;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException(nameof(origin));
                    }
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        ~FlashPartStream()
        {
            // Cleanup
            foreach (FlashPart flashingPartition in flashParts)
            {
                flashingPartition.Data.Close();
            }
        }
    }
}
