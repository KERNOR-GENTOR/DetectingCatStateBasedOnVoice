using System;

namespace CatCollarServer.Algorytm
{
    public static class Basic
    {
        public static double RMS (in short[] source, uint start, uint finish)
        {
            double value = 0;

            for(uint i = start; i <= finish; i++)
            {
                value += Math.Pow(source[i], 2);
            }

            value /= finish - start + 1;

            return Math.Sqrt(value);
        }

        public static double Entropy(in double[] source, uint start, uint finish, byte binsCount, double minRaw, double maxRaw)
		{
			double entropy = 0;

			double binSize = Math.Abs(maxRaw - minRaw) / (double)binsCount;
			if (Math.Abs(binSize) < double.Epsilon)
			{
				return 0;
			}

			double[] p = new double[binsCount];
			for (byte i = 0; i < binsCount; i++)
			{
				p[i] = 0.0;
			}

			// Calculate probabilities
			byte index;
			for (uint i = start; i <= finish; i++)
			{
				double value = source[i];
				index = (byte)Math.Floor((value - minRaw) / binSize);

				if (index >= binsCount)
				{
					index = (byte)(binsCount - 1);
				}

				p[index] += 1.0;
			}

			// Normalize probabilities
			byte size = (byte)(finish - start + 1);
			for (byte i = 0; i < binsCount; i++)
			{
				p[i] /= size;
			}

			// Calculate entropy
			for (byte i = 0; i < binsCount; i++)
			{
				if (p[i] > double.Epsilon)
				{
					entropy += p[i] * Math.Log2(p[i]);
				}
			}

			entropy = -entropy;
			return entropy;
		}
	}
}
