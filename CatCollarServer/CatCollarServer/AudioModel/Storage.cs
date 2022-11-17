using CatCollarServer.Algorytm;
using CatCollarServer.Audio;
using System;
using System.Collections.Generic;
using System.IO;

namespace CatCollarServer.AudioModel
{
    public class Storage
    {
        private const string storage_examples_file = "..\\CatCollarServer\\Resources\\models.dat";
        private const string storage_header = "DATAS";

        public Dictionary<uint, Model> Models { get; private set; }
        private uint maxId;

        public Storage()
        {
            maxId = 0;
            Models = null;
        }

        public bool Init()
        {
            if (Models != null)
            {
                return true;
            }
            Models = new Dictionary<uint, Model>();

            Console.WriteLine("Loading models from the storage");
            if (File.Exists(storage_examples_file))
            {
                using (Stream file = File.Open(storage_examples_file, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(file);

                    if (reader == null)
                    {
                        Console.WriteLine("Can't access the model's storage");
                    }

                    byte[] header = new byte[(storage_header.Length)];
                    reader.Read(header, 0, (storage_header.Length));

                    if (System.Text.ASCIIEncoding.ASCII.GetString(header) != storage_header)
                    {
                        Console.WriteLine("Invalid storage");
                        return false;
                    }

                    byte[] maxIdBuffer = reader.ReadBytes(sizeof(uint));
                    maxId = BitConverter.ToUInt32(maxIdBuffer);

                    string tmpName = "";
                    for (uint i = 0; i < maxId; i++)
                    {
                        Model model = new Model(tmpName);
                        model.Read(ref reader);

                        Models.Add(model.Id, model);
                    }

                    reader.BaseStream.Close();
                    reader.Close();
                }
            }
            else
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(storage_examples_file, FileMode.Create)))
                {
                    Console.WriteLine("Storage not found, creating an empty one");
                    writer.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(storage_header));
                    writer.Write(BitConverter.GetBytes(maxId));
                    writer.BaseStream.Close();
                    writer.Close();
                }

                return false;
            }

            return true;
        }

        public uint AddModel(Model model)
        {
            model.Id = ++maxId;
            Models.Add(maxId, model);

            return maxId;
        }

        public void AddSample(uint modelId, Word word)
        {
            Models[modelId].Samples.Add(new MFCCSample() { data = word.MFCC, size = word.MFCCSize });
        }

        //Save models into the file
        public bool Persist()
        {
            using (Stream file = File.Open(storage_examples_file, FileMode.Create))
            {
                BinaryWriter writer = new BinaryWriter(file);
                if (writer == null)
                {
                    Console.WriteLine("Can't access the model's storage");
                }

                writer.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(storage_header));
                writer.Write(BitConverter.GetBytes(maxId));

                foreach (var model in Models)
                {
                    Model tmpModel = model.Value;
                    tmpModel.Write(ref writer);
                }
                writer.BaseStream.Close();
                writer.Close();
            }
            Console.WriteLine("Done!");
            return true;
        }
    }
}
