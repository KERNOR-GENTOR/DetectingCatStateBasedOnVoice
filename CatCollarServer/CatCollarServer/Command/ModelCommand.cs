using CatCollarServer.Audio;
using CatCollarServer.AudioModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CatCollarServer.Command
{
    public static class ModelCommand
    {
        public static void Add(ref Context context, in string modelName)
        {
            // Check the model's name
            if (string.IsNullOrEmpty(modelName))
            {
                Console.WriteLine("Model name is not specified");
                return;
            }

            Console.WriteLine("Adding the new sample");

            // Get a word to recognize
            Word word = GetWord(ref context);
            if (word == null)
            {
                return;
            }

            // Inin storage 
            context.Storage.Init();

            // Find the model
            Model model = null;
            Dictionary<uint, Model> models = new Dictionary<uint, Model>(context.Storage.Models);
            foreach (var m in models)
            {
                if(modelName == m.Value.Text)
                {
                    model = m.Value;
                    break;
                }
            }

            // Create the model if it does not exist
            if (model == null)
            {
                model = new Model(modelName);
                context.Storage.AddModel(model);
            }

            // Add the sample to the model
            context.Storage.AddSample(model.Id, word);

            Console.WriteLine("The new sample has been successfully added!");
        }
        
        public static string Recognize(ref Context context, in List<string> modelNames)
        {
            // Check the storage
            if(!context.Storage.Init())
            {
                return null;
            }
            if (context.Storage.Models.Count == 0)
            {
                Console.WriteLine("Models storage is empty! Add some model before starting recognition.");
                return null;
            }

            Console.WriteLine("Word recognition");

            // Get a word to recognize
            Word word = GetWord(ref context);

            //Get available models
            List<Model> modelsFiltered = new List<Model>();
            Dictionary<uint, Model> models = context.Storage.Models;

            foreach(var m in models)
            {
                string modelName = m.Value.Text;
                if(modelNames.Count == 0 || modelNames.FirstOrDefault(i => i == modelName) != modelNames.LastOrDefault())
                {
                    modelsFiltered.Add(m.Value);
                }
            }

            // Try to recognize
            Recognizer recognizer = new Recognizer(modelsFiltered);
            Model model = recognizer.Do(word);

            //Get result
            if(model != null)
            {
                return Regex.Replace(model.Text, "[0-9]", "");
            }

            return "No result";
        }

        private static Word GetWord(ref Context context)
        {
            //Check pre-requirements
            if(context.WavData == null)
            {
                Console.WriteLine("Input data is not specified");
                return null;
            }

            Console.WriteLine("Checking input data");

            // Create the Processor
            Processor processor = new Processor(context.WavData);
            processor.Init();
            context.Processor = processor;

            Console.WriteLine("Calculating MFCC for input data");

            // Calc & show mfcc
            Word word = processor.GetAsWholeWord();
            processor.InitMFCC(ref word);

            return word;
        }
    }
}
