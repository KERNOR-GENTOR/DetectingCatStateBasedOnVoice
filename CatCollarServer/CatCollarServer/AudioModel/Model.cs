using CatCollarServer.Algorytm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CatCollarServer.AudioModel
{
    public class Model
    {
        public uint Id { get; set; }
        public string Text { get; private set; }
        public List<MFCCSample> Samples { get; set; }

        public Model(string t)
        {
            Text = t;
            Samples = new List<MFCCSample>();
        }

        public void Write(ref BinaryWriter writer)
        {
            writer.Write(BitConverter.GetBytes(Id));

            uint textSize = (uint)Text.Length;
            writer.Write(BitConverter.GetBytes(textSize));
            writer.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(Text));

            uint sampleCount = (uint)Samples.Count;
            writer.Write(BitConverter.GetBytes(sampleCount));

            foreach (MFCCSample sample in Samples)
            {
                writer.Write(BitConverter.GetBytes(sample.size));
                writer.Write(sample.data.SelectMany(value => BitConverter.GetBytes(value)).ToArray());
            }
        }

        public void Read(ref BinaryReader reader)
        {
            byte[] bufferInt = new byte[sizeof(uint)];

            reader.Read(bufferInt, 0, sizeof(uint));
            Id = BitConverter.ToUInt32(bufferInt);

            reader.Read(bufferInt, 0, sizeof(uint));
            uint textSize = BitConverter.ToUInt32(bufferInt);

            byte[] textChars = new byte[textSize];
            reader.Read(textChars, 0, (int)textSize);
            Text = System.Text.ASCIIEncoding.ASCII.GetString(textChars);

            reader.Read(bufferInt, 0, sizeof(uint));
            uint samplesCount = BitConverter.ToUInt32(bufferInt);

            MFCCSample sample;
            for (uint i = 0; i < samplesCount; i++)
            {
                reader.Read(bufferInt, 0, sizeof(uint));
                sample.size = BitConverter.ToUInt32(bufferInt);

                byte[] sampleDataBuffer = new byte[sample.size * sizeof(double)];
                reader.Read(sampleDataBuffer, 0, (int)(sample.size * sizeof(double)));
                sample.data = Enumerable.Range(0, sampleDataBuffer.Length / sizeof(double))
                    .Select(offset => BitConverter.ToDouble(sampleDataBuffer, offset * sizeof(double)))
                    .ToArray();

                Samples.Add(sample);
            }
        }
    }
}
