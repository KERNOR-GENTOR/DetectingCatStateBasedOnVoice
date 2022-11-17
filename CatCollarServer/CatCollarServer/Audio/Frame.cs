using CatCollarServer.Algorytm;
using System;

namespace CatCollarServer.Audio
{
	public static class AudioParameters
    {
		//Length of frame(ms)
		public const double FRAME_LENGTH = 10;

		//Percentage of overlap for frames (0 <= x < 1)
		public const double FRAME_OVERLAP = 0.5;

		//Minimal size of word (in frames)
		//According my experiments average length of the words in my dictionary is 500ms.
		//So put the minimal length of word is 200ms.
		public const ushort WORD_MIN_SIZE = (ushort)((200 / FRAME_LENGTH) / (1 - FRAME_OVERLAP));

		// Minimal amount of framer between two words. 50%
		public const ushort WORDS_MIN_DISTANCE = (ushort)(WORD_MIN_SIZE * 0.5);

		//MFCC vector size
		public const ushort MFCC_SIZE = 12;

		//Frequency bounds
		public const short MFCC_FREQ_MIN = 300;
		public const short MFCC_FREQ_MAX = 4000;

		//Entropy parameters
		public const short ENTROPY_BINS = 75;
		public const double ENTROPY_THRESHOLD = 0.1;
	}
    public class Frame : ICloneable
    {
		public uint Id { get; private set; }
		public double RMS { get; private set; }
		public double Entropy { get; private set; }
		public double[] MFCC { get; private set; }

		//Сreate frame
		public Frame(uint id)
		{
			Id = id;
			RMS = 0;
			Entropy = 0;
			MFCC = new double[AudioParameters.MFCC_SIZE];
		}

		//Init the frame using a part of wave data
		public void Init(short[] source, in double[] sourceNormalized, uint start, uint finish)
		{
			RMS = Basic.RMS(source, start, finish);
			Entropy = Basic.Entropy(sourceNormalized, start, finish, (byte)AudioParameters.ENTROPY_BINS, -1, 1);
		}

		public double[] InitMFCC(in double[] source, uint start, uint finish, uint freq)
		{
			MFCC = Algorytm.MFCC.Transform(source, start, finish, (byte)AudioParameters.MFCC_SIZE, freq, (uint)AudioParameters.MFCC_FREQ_MIN, (uint)AudioParameters.MFCC_FREQ_MAX);
			return MFCC;
		}
		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}
