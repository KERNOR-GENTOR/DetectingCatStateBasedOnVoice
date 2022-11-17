using CatCollarServer.Command;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CatCollarServer
{
    public static class Recorder
    {
        private const string output_folder = "..\\CatCollarServer\\Output\\";
        private static List<string> sourceList;
        private static List<WaveInEvent> sourceStreams = new List<WaveInEvent>();
        private static List<WaveFileWriter> waveWriters = new List<WaveFileWriter>();
        private static List<System.Timers.Timer> timers = new List<System.Timers.Timer>();

        public static void Run()
        {
            List<WaveInCapabilities> sources = new List<WaveInCapabilities>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                Console.WriteLine("Found " + WaveIn.GetCapabilities(i).ProductName);
                sources.Add(WaveIn.GetCapabilities(i));
            }

            sourceList = new List<string>();
            sourceStreams = new List<WaveInEvent>();
            waveWriters = new List<WaveFileWriter>();
            timers = new List<System.Timers.Timer>();
            foreach (var source in sources)
            {
                sourceList.Add(source.ProductName);
                sourceStreams.Insert(sourceList.IndexOf(source.ProductName), new WaveInEvent());
                waveWriters.Insert(sourceList.IndexOf(source.ProductName), null);
                timers.Insert(sourceList.IndexOf(source.ProductName), new System.Timers.Timer(2000));
                Console.WriteLine($"Added {source.ProductName}.");
            }
            CommandFacad.Devices = sourceList;

            Parallel.ForEach(sourceList, RecordingLoop);
        }

        private static object audioLock = new object();

        private static void RecordingLoop(string device)
        {
            while (true)
            {
                lock (audioLock)
                {
                    StartRecording(device);
                }
                Thread.Sleep(1000);
            }
        }

        private static void StartRecording(string device)
        {
            Console.WriteLine($"Start {device}.");

            while (true)
            {
                if (sourceList != null)
                {
                    break;
                }
            }

            int deviceNumber = sourceList.IndexOf(device);

            try
            {
                if (sourceStreams[deviceNumber] == null)
                {
                    return;
                }
                sourceStreams[deviceNumber].DeviceNumber = deviceNumber;
                sourceStreams[deviceNumber].WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(deviceNumber).Channels);
                sourceStreams[deviceNumber].DataAvailable += new EventHandler<WaveInEventArgs>((sender, e) => sourceStream_Data(sender, e, deviceNumber));
            }
            catch
            {
                sourceList.Remove(device);
                CommandFacad.Devices.Remove(device);
                return;
            }

            Console.WriteLine("Start writing into " + device + ".wav");

            try
            {
                using (Stream stream = File.Open(output_folder + device + ".wav", FileMode.Create))
                {
                    waveWriters[deviceNumber] = new WaveFileWriter(stream, sourceStreams[deviceNumber].WaveFormat);
                }
                sourceStreams[deviceNumber].StartRecording();
            }
            catch
            {
                Thread.Sleep(1000);
            }

            timers[deviceNumber].AutoReset = true;
            timers[deviceNumber].Enabled = true;

            timers[deviceNumber].Elapsed += (sender, e) =>
            {
                Console.WriteLine("Stop writing into " + device + ".wav");
                StopTimer(sender, e, deviceNumber);
            };
        }
        private static void sourceStream_Data(object sender, WaveInEventArgs e, int deviceNumber)
        {
            if (waveWriters[deviceNumber] == null)
                return;
            try
            {
                //offset is 0 because written the entire array of data
                waveWriters[deviceNumber].WriteData(e.Buffer, 0, e.BytesRecorded);
                waveWriters[deviceNumber].Flush();
            }
            catch
            {
                return;
            }
        }

        private static void StopTimer(object sender, EventArgs e, int deviceNumber)
        {
            try
            {
                if (sourceStreams[deviceNumber] == null)
                {
                    return;
                }
                sourceStreams[deviceNumber].StopRecording();
                sourceStreams[deviceNumber].Dispose();
                sourceStreams[deviceNumber] = null;
                waveWriters[deviceNumber].Close();
                waveWriters[deviceNumber] = null;

                timers[deviceNumber].Enabled = false;
                timers[deviceNumber].Stop();
                timers[deviceNumber].Dispose();
            }
            catch { }
        }
    }
}
