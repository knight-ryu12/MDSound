﻿namespace MDSound
{
    public class ym2151 : Instrument
    {
        private fmgen.OPM[] chip = new fmgen.OPM[2];
        private const uint DefaultYM2151ClockValue = 3579545;

        public override void Reset(byte ChipID)
        {
            chip[ChipID].Reset();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            chip[ChipID] = new fmgen.OPM();
            chip[ChipID].Init(DefaultYM2151ClockValue,clock);

            return clock;
        }

        public uint Start(byte ChipID, uint clock, uint FMClockValue, params object[] option)
        {
            chip[ChipID] = new fmgen.OPM();
            chip[ChipID].Init(FMClockValue,clock);

            return clock;
        }

        public override void Stop(byte ChipID)
        {
            chip[ChipID] = null;
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            int[] buffer = new int[2];
            buffer[0] = 0;
            buffer[1] = 0;
            chip[ChipID].Mix(buffer, 1);
            for (int i = 0; i < 1; i++)
            {
                outputs[0][i] = buffer[i * 2 + 0];
                outputs[1][i] = buffer[i * 2 + 1];
                //Console.Write("[{0:d8}] : [{1:d8}] [{2}]\r\n", outputs[0][i], outputs[1][i],i);
            }
        }

        public int YM2151_Write(byte ChipID, byte adr, byte data)
        {
            chip[ChipID].SetReg(adr, data);
            return 0;
        }
    }
}