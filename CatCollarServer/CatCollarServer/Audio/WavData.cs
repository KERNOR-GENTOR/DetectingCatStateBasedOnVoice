using System;
using System.IO;
using System.Runtime.Serialization;

namespace CatCollarServer.Audio
{
    public static class Serialization
    {
        public static WavHeader DeserializeHeader(byte[] vs)
        {
            WavHeader header = new WavHeader();
            int startPoint = 0;

            byte[] bufferInt = new byte[sizeof(uint)];
            byte[] bufferShr = new byte[sizeof(ushort)];

            Array.Copy(vs, startPoint, header.RIFF, 0, 4);
            startPoint += 4;


            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.ChunkSize = BitConverter.ToUInt32(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, header.Wave, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, header.FMT, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.Subchunk1Size = BitConverter.ToUInt32(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.AudioFormat = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.NumberOfChan = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.SamplesPerSec = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.BytesPerSec = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.BlockAlign = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.BitsPerSample = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, header.Data, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.Subchunk2Size = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);

            return header;
        }
    }

    [Serializable()]
    public class WavHeader : ISerializable
    {
        public byte[] RIFF; // RIFF Header
        public uint ChunkSize; // RIFF Chunk Size
        public byte[] Wave; // WAVE Header

        public byte[] FMT; // FMT header
        public uint Subchunk1Size; // Size of the fmt chunk
        public ushort AudioFormat; // Audio format 1=PCM (Other formats are unsupported)
        public ushort NumberOfChan; // Number of channels 1=Mono, 2=Stereo
        public uint SamplesPerSec; // Sampling Frequency in Hz
        public uint BytesPerSec; // bytes per second
        public ushort BlockAlign; // 2=16-bit mono, 4=16-bit stereo
        public ushort BitsPerSample; // Number of bits per sample

        // The data below depends on audioFormat, but we work only with PCM cases
        public byte[] Data; // DATA header
        public uint Subchunk2Size; // Sampled data length

        public WavHeader()
        {
            RIFF = new byte[4];
            ChunkSize = new uint();
            Wave = new byte[4];
            FMT = new byte[4];
            Subchunk1Size = new uint();
            AudioFormat = new ushort();
            NumberOfChan = new ushort();
            SamplesPerSec = new uint();
            BytesPerSec = new uint();
            BlockAlign = new ushort();
            BitsPerSample = new ushort();
            Data = new byte[4];
            Subchunk2Size = new uint();
        }

        //Deserialization constructor.
        public WavHeader(SerializationInfo info, StreamingContext ctxt) : this()
        {
            //Get the values from info and assign them to the appropriate properties
            RIFF = (byte[])info.GetValue("RIFF", RIFF.GetType());
            ChunkSize = (uint)info.GetValue("ChunkSize", ChunkSize.GetType());
            Wave = (byte[])info.GetValue("Wave", Wave.GetType());
            FMT = (byte[])info.GetValue("FMT", FMT.GetType());
            Subchunk1Size = (uint)info.GetValue("Subchunk1Size", Subchunk1Size.GetType());
            AudioFormat = (ushort)info.GetValue("AudioFormat", AudioFormat.GetType());
            NumberOfChan = (ushort)info.GetValue("NumberOfChan", NumberOfChan.GetType());
            SamplesPerSec = (uint)info.GetValue("SamplesPerSec", SamplesPerSec.GetType());
            BytesPerSec = (uint)info.GetValue("BytesPerSec", BytesPerSec.GetType());
            BlockAlign = (ushort)info.GetValue("BlockAlign", BlockAlign.GetType());
            BitsPerSample = (ushort)info.GetValue("BitsPerSample", BlockAlign.GetType());
            Data = (byte[])info.GetValue("Data", Data.GetType());
            Subchunk2Size = (uint)info.GetValue("Subchunk2Size", Subchunk2Size.GetType());
        }

        //Serialization function.
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            //Add the values to info and name them
            info.AddValue("RIFF", RIFF);
            info.AddValue("ChunkSize", ChunkSize);
            info.AddValue("Wave", Wave);
            info.AddValue("FMT", FMT);
            info.AddValue("Subchunk1Size", Subchunk1Size);
            info.AddValue("AudioFormat", AudioFormat);
            info.AddValue("NumberOfChan", NumberOfChan);
            info.AddValue("SamplesPerSec", SamplesPerSec);
            info.AddValue("BytesPerSec", BytesPerSec);
            info.AddValue("BlockAlign", BlockAlign);
            info.AddValue("BitsPerSample", BitsPerSample);
            info.AddValue("Data", Data);
            info.AddValue("Subchunk2Size", Subchunk2Size);
        }
    }

    public class WavData : ICloneable
    {
        public WavHeader Header { get; private set; }
        public short[] RawData { get; private set; }
        public double[] NormalizedData { get; private set; }

        public short MaxValue { get; private set; }
        public short MinValue { get; private set; }
        public uint NumberOfSamples { get; private set; }
        public WavData(WavHeader header)
        {
            Header = header;
            RawData = null;
            NormalizedData = null;

            MaxValue = 0;
            MinValue = 0;
            NumberOfSamples = 0;
        }

        public const int WavHeaderSizeOf = 44;

        public static WavData ReadFromFile(in string filePathName)
        {
            WavHeader wavHeader = new WavHeader();

            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, filePathName);

            try
            {
                //Read file
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                //Deserialize header
                wavHeader = Serialization.DeserializeHeader(reader.ReadBytes(WavHeaderSizeOf));

                WavData wavData = new WavData(wavHeader);

                ReadData(ref reader, wavHeader, ref wavData);
                reader.BaseStream.Close();
                reader.Close();

                return wavData;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }
        }

        public bool CheckHeader(in WavHeader header)
        {
            string riff = System.Text.ASCIIEncoding.ASCII.GetString(header.RIFF),
                wave = System.Text.ASCIIEncoding.ASCII.GetString(header.Wave);
            if (String.Compare(riff, "RIFF") != 0
                || String.Compare(wave, "WAVE") != 0)
            {
                Console.WriteLine("Invalid RIFF/WAVE format");
                return false;
            }

            if (header.AudioFormat != 1)
            {
                Console.WriteLine("nvalid WAV format: only PCM audio format is supported");
                return false;
            }

            if (header.NumberOfChan > 2)
            {
                Console.WriteLine("Invalid WAV format: only 1 or 2 channels audio is supported");
                return false;
            }

            ulong bitsPerChannel = (ulong)(header.BitsPerSample / header.NumberOfChan);
            if (bitsPerChannel != 16)
            {
                Console.WriteLine("Invalid WAV format: only 16 - bit per channel is supported");
                return false;
            }

            if (header.Subchunk2Size > 0)
            {
                Console.WriteLine("File too big");
                return false;
            }

            return true;
        }

        public static void ReadData(ref BinaryReader reader, WavHeader header, ref WavData wavFile)
        {
            short value, minValue = 0, maxValue = 0;
            short value16, valueLeft16, valueRight16;

            var bytesPerSample = header.BitsPerSample / 8;
            ulong numberOfSamplesXChannels = (ulong)(header.Subchunk2Size / (header.NumberOfChan * bytesPerSample));

            wavFile.RawData = new short[numberOfSamplesXChannels];

            uint sampleNumber = 0;
            for (; sampleNumber < numberOfSamplesXChannels && reader.BaseStream.CanRead; sampleNumber++)
            {
                try
                {
                    if (header.NumberOfChan == 1)
                    {
                        value16 = BitConverter.ToInt16(reader.ReadBytes(sizeof(short)));
                        value = value16;
                    }
                    else
                    {
                        byte[] left = reader.ReadBytes(sizeof(short));
                        if (left.Length == 0)
                        {
                            break;
                        }
                        valueLeft16 = BitConverter.ToInt16(left);
                        
                        byte[] right = reader.ReadBytes(sizeof(short));
                        if (right.Length == 0)
                        {
                            break;
                        }
                        valueRight16 = BitConverter.ToInt16(right);

                        value = (short)((Math.Abs(valueLeft16) + Math.Abs(valueRight16)) / 2);
                    }

                    if (maxValue < value)
                    {
                        maxValue = value;
                    }

                    if (minValue > value)
                    {
                        minValue = value;
                    }
                    wavFile.RawData[sampleNumber] = value;
                }
                catch { }
            }

            // Normalization
            wavFile.NormalizedData = new double[sampleNumber];
            double maxAbs = Math.Max(MathF.Abs(minValue), MathF.Abs(maxValue));

            for (int i = 0; i < sampleNumber; i++)
            {
                wavFile.NormalizedData[i] = wavFile.RawData[i] / maxAbs;
            }

            // Update values
            wavFile.MinValue = minValue;
            wavFile.MaxValue = maxValue;
            wavFile.NumberOfSamples = sampleNumber;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
