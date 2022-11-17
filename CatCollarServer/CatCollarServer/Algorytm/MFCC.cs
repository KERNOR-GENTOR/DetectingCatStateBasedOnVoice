using System;
using System.Numerics;

namespace CatCollarServer.Algorytm
{
    //MFCC sample
    public struct MFCCSample
    {
        public uint size;
        public double[] data;
    };

    public class MFCC
    {
        //Perform MFCC transformation
        public static double[] Transform(in double[] source, uint start, uint finish, byte mfccSize, uint frequency, uint freqMin, uint freqMax)
        {
            uint sampleLength = finish - start + 1;

            // Calc
            double[] fourierRaw = FourierTransform(source, start, sampleLength, true);
            double[][] melFilters = GetMelFilters(mfccSize, sampleLength, frequency, freqMin, freqMax);

            for (int i = 0; i < melFilters.Length; i++)
            {
                for (int j = 0; j < melFilters[i].Length; j++)
                {
                    if (melFilters[i][j] < 0)
                    {
                        melFilters[i][j] *= -1;
                    }
                }
            }

            double[] logPower = CalcPower(fourierRaw, sampleLength, melFilters, mfccSize);
            double[] dctRaw = DCTTransform(logPower, mfccSize);

            return dctRaw;
        }

        //Compute singnal's magnitude (short-time Fourier transform with Hamming window)
        public static double[] FourierTransform(in double[] source, uint start, uint length, bool useWindow)
        {
            Complex[] fourierCmplxRaw = new Complex[length];
            double[] fourierRaw = new double[length];


            for (uint k = 0; k < length; k++)
            {
                fourierCmplxRaw[k] = new Complex(0, 0);

                for (uint n = 0; n < length; n++)
                {
                    double sample = source[start + n];

                    // According Euler's formula
                    double x = -2.0 * Math.PI * k * n / (double)length;
                    Complex f = sample * new Complex(Math.Cos(x), Math.Sin(x));

                    double w = 1.0;
                    if (useWindow)
                    {
                        // Hamming window
                        w = 0.54 - 0.46 * Math.Cos(2 * Math.PI * n / (length - 1));
                    }

                    fourierCmplxRaw[k] += f * w;
                }

                // As for magnitude, let's use Euclid's distance for its calculation
                fourierRaw[k] = Math.Sqrt(fourierCmplxRaw[k].Magnitude);
            }

            return fourierRaw;
        }

        //Create triangular filters spaced on mel scale
        public static double[][] GetMelFilters(byte mfccSize, uint filterLength, uint frequency, uint freqMin, uint freqMax)
        {
            // Create points for filter banks
            double[] fb = new double[mfccSize + 2];

            double[] fbFloor = new double[mfccSize + 2];

            fb[0] = convertToMel(freqMin);
            fb[mfccSize + 1] = convertToMel(freqMax);

            // Create mel bin
            for (ushort m = 1; m < mfccSize + 1; m++)
            {
                fb[m] = fb[0] + m * (fb[mfccSize + 1] - fb[0]) / (mfccSize + 1);
            }

            for (ushort m = 0; m < mfccSize + 2; m++)
            {
                // Convert them from mel to frequency
                fb[m] = convertFromMel(fb[m]);

                // Map those frequencies to the nearest FT bin
                fb[m] = (filterLength + 1) * fb[m] / (double)frequency;

                fbFloor[m] = Math.Floor((filterLength + 1) * fb[m] / (double)frequency);
            }

            // Calc filter banks
            double[][] filterBanks = new double[mfccSize][];
            for (ushort m = 0; m < mfccSize; m++)
            {
                filterBanks[m] = new double[filterLength];
            }

            for (ushort m = 1; m < mfccSize + 1; m++)
            {
                for (uint k = 0; k < filterLength; k++)
                {

                    if (fbFloor[m - 1] <= k && k <= fbFloor[m])
                    {
                        filterBanks[m - 1][k] = (k - fb[m - 1]) / (fb[m] - fb[m - 1]);
                    }
                    else if (fbFloor[m] < k && k <= fbFloor[m + 1])
                    {
                        filterBanks[m - 1][k] = (fb[m + 1] - k) / (fb[m + 1] - fb[m]);
                    }
                    else
                    {
                        filterBanks[m - 1][k] = 0;
                    }
                }
            }

            return filterBanks;
        }

        //Apply mel filters to spectrum's magnitudes, take the logs of the powers
        public static double[] CalcPower(in double[] fourierRaw, uint fourierLength, double[][] melFilters, byte mfccCount)
        {
            double[] logPower = new double[mfccCount];

            for (ushort m = 0; m < mfccCount; m++)
            {
                logPower[m] = 0.0;

                for (uint k = 0; k < fourierLength; k++)
                {
                    logPower[m] += melFilters[m][k] * Math.Pow(fourierRaw[k], 2);
                }

                //Take logs since normalizing the input data
                logPower[m] = Math.Log(logPower[m]);
            }

            return logPower;
        }

        //Take the discrete cosine transform of the list of mel log powers
        public static double[] DCTTransform(in double[] data, uint length)
        {
            double[] dctTransform = new double[length];

            for (ushort n = 0; n < length; n++)
            {
                dctTransform[n] = 0;

                for (ushort m = 0; m < length; m++)
                {
                    dctTransform[n] += data[m] * Math.Cos(Math.PI * n * (m + 1.0 / 2.0) / length);
                }
            }

            return dctTransform;
        }

        // Mel convertors
        public static double convertToMel(double f)
        {
            return 1125.0 * Math.Log(1.0 + f / 700.0);
        }
        public static double convertFromMel(double m)
        {
            return 700.0 * (Math.Exp(m / 1125.0) - 1);
        }
    }
}
