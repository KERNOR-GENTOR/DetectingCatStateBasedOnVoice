namespace CatCollarServer.Audio
{
    public class Word
    {
        //Get frame's serial number
        public uint Id { get; private set; }
        public uint MFCCSize { get; private set; }
        public double[] MFCC { get; private set; }

        //Create a word based on set of frames
        public Word(uint id)
        {
            Id = id;

            MFCCSize = 0;
            MFCC = null;
        }

        public void SetMFCC(double[] mfcc, uint size)
        {
            MFCC = mfcc;
            MFCCSize = size;
        }
    }
}
