using CatCollarServer.Algorytm;
using CatCollarServer.Audio;
using System;
using System.Collections.Generic;

namespace CatCollarServer.AudioModel
{
    public class Recognizer
    {
        private List<Model> models;

        public Recognizer(List<Model> models)
        {
            this.models = models;
        }

        public Model Do(in Word word)
        {
            Model bestModel = null;
            double minDistance = 0;

            foreach(Model model in models)
            {
                double distance = 0;
                foreach(MFCCSample sample in model.Samples)
                {
                    if (sample.size == AudioParameters.MFCC_SIZE)
                        distance += DTW.CalcDistanceVector(word.MFCC, word.MFCCSize, sample.data, sample.size, (byte)AudioParameters.MFCC_SIZE);
                    else distance = double.MaxValue;
                }
                distance /= model.Samples.Count;

                Console.WriteLine("Distance for model \'{0}\' is {1}", model.Text, distance);

                if (bestModel == null  || distance < minDistance)
                {
                    minDistance = distance;
                    bestModel = model;
                }
            }
            Console.WriteLine("The best model is \'{0}\' with {1} distance", bestModel.Text, minDistance);

            return bestModel;
        }
    }
}
