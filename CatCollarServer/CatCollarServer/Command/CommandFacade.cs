using CatCollarServer.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CatCollarServer.Command
{
    public static class CommandFacad
    {
        public static Context context = new Context();
        public static string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).FullName + "\\CatCollarServer\\";
        private const int amountOfCatsExamples = 3;
        public static Dictionary<string, string> EmotionColorPair = CreatePair();
        private const int RawAmount = 1, colorAmount = 4;

        public static List<string> Devices { get; set; }
        public static string Result { get; private set; }

        private static Dictionary<string, string> CreatePair()
        {
            string[] s = File.ReadAllLines(projectDirectory + "Resources\\colors.txt"), subS;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            for (int i = 0; i <= (RawAmount + 1) * (colorAmount - 1); i += RawAmount + 1)
            {
                for (int j = i + 1; j <= i + RawAmount; j++)
                {
                    subS = s[j].Trim().Split(' ');
                    foreach (string ss in subS)
                    {
                        pairs.Add(ss, s[i]);
                    }
                }
            }
            return pairs;
        }
        public static void CreateModels()
        {
            for (int c = 0; c < amountOfCatsExamples; c++)
            {
                foreach (var pair in EmotionColorPair)
                {
                    context.WavData = WavData.ReadFromFile(projectDirectory + $"Resources\\cat{c}\\{pair.Key}{c}.wav");
                    ModelCommand.Add(ref context, $"{pair.Key}{c}");
                    if (c == amountOfCatsExamples - 1 && pair.Key == EmotionColorPair.Last().Key)
                    {
                        context.Storage.Persist();
                    }
                }
            }
        }
        public static void Recognize(string device)
        {
            context.WavData = WavData.ReadFromFile(projectDirectory + $"Output\\{device}.wav");
            if(context.WavData.NumberOfSamples == 0)
            {
                Result = "No sound";
            }
            else
            {
                Result = ModelCommand.Recognize(ref context, EmotionColorPair.Keys.ToList());
                Result = char.ToUpper(Result[0]) + Result.Substring(1);
            }
        }
    }
}
