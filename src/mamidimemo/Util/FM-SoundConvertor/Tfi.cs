


using System;

using static FM_SoundConvertor.Path;
using static FM_SoundConvertor.File;
using System.Collections.Generic;
using System.Text;

namespace FM_SoundConvertor
{
    class Tfi
    {
        enum ePut
        {
            AL,
            FB,
            //AMD_FMD,
            ML0,
            DT0,
            TL0,
            RS0,
            AR0,
            DR0_AM,
            SR0,
            RR0,
            SL0,
            SSGEG0,
            ML2,
            DT2,
            TL2,
            RS2,
            AR2,
            DR2_AM,
            SR2,
            RR2,
            SL2,
            SSGEG2,
            ML1,
            DT1,
            TL1,
            RS1,
            AR1,
            DR1_AM,
            SR1,
            RR1,
            SL1,
            SSGEG1,
            ML3,
            DT3,
            TL3,
            RS3,
            AR3,
            DR3_AM,
            SR3,
            RR3,
            SL3,
            SSGEG3,
        }



        static int ToneLength()
        {
            return 1;
        }

        static int PutLength()
        {
            return System.Enum.GetNames(typeof(ePut)).Length;
        }

        static int DatLength()
        {
            return ToneLength() * PutLength();
        }



        public static byte[] New()
        {
            return new byte[DatLength()];
        }

        static int[] DT_Table = new int[]
        {
            //TFI
            // 0, 1, 2, 3, 4, 5, 6,
            //-3,-2,-1, 0, 1, 2, 3,

            // Yamaha Reg
            // 0, 1, 2, 3, 4, 5, 6, 7,
            // 0, 1, 2, 3, 0,-1,-2,-3,

             7, 6, 5, 0, 1, 2, 3, 4,
        };

        static void Get(ref Tone @Tone, byte[] Buffer, int i)
        {
            var o = i * PutLength();

            Tone.AL = Buffer[o + (int)ePut.AL] & 0x7;
            Tone.FB = Buffer[o + (int)ePut.FB] & 0x7;
            //Tone.AMS = (Buffer[o + (int)ePut.AMD_FMD] >> 4) & 0x3;
            //Tone.PMS = Buffer[o + (int)ePut.AMD_FMD] & 0x7;

            Tone.aOp[0].AR = Buffer[o + (int)ePut.AR0] & 0x1f;
            Tone.aOp[1].AR = Buffer[o + (int)ePut.AR1] & 0x1f;
            Tone.aOp[2].AR = Buffer[o + (int)ePut.AR2] & 0x1f;
            Tone.aOp[3].AR = Buffer[o + (int)ePut.AR3] & 0x1f;
            Tone.aOp[0].DR = Buffer[o + (int)ePut.DR0_AM] & 0x1f;
            Tone.aOp[1].DR = Buffer[o + (int)ePut.DR1_AM] & 0x1f;
            Tone.aOp[2].DR = Buffer[o + (int)ePut.DR2_AM] & 0x1f;
            Tone.aOp[3].DR = Buffer[o + (int)ePut.DR3_AM] & 0x1f;
            Tone.aOp[0].SR = Buffer[o + (int)ePut.SR0] & 0x1f;
            Tone.aOp[1].SR = Buffer[o + (int)ePut.SR1] & 0x1f;
            Tone.aOp[2].SR = Buffer[o + (int)ePut.SR2] & 0x1f;
            Tone.aOp[3].SR = Buffer[o + (int)ePut.SR3] & 0x1f;
            Tone.aOp[0].RR = Buffer[o + (int)ePut.RR0] & 0xf;
            Tone.aOp[1].RR = Buffer[o + (int)ePut.RR1] & 0xf;
            Tone.aOp[2].RR = Buffer[o + (int)ePut.RR2] & 0xf;
            Tone.aOp[3].RR = Buffer[o + (int)ePut.RR3] & 0xf;
            Tone.aOp[0].SL = Buffer[o + (int)ePut.SL0] & 0xf;
            Tone.aOp[1].SL = Buffer[o + (int)ePut.SL1] & 0xf;
            Tone.aOp[2].SL = Buffer[o + (int)ePut.SL2] & 0xf;
            Tone.aOp[3].SL = Buffer[o + (int)ePut.SL3] & 0xf;
            Tone.aOp[0].TL = Buffer[o + (int)ePut.TL0] & 0x7f;
            Tone.aOp[1].TL = Buffer[o + (int)ePut.TL1] & 0x7f;
            Tone.aOp[2].TL = Buffer[o + (int)ePut.TL2] & 0x7f;
            Tone.aOp[3].TL = Buffer[o + (int)ePut.TL3] & 0x7f;
            Tone.aOp[0].KS = Buffer[o + (int)ePut.RS0] & 0x3;
            Tone.aOp[1].KS = Buffer[o + (int)ePut.RS1] & 0x3;
            Tone.aOp[2].KS = Buffer[o + (int)ePut.RS2] & 0x3;
            Tone.aOp[3].KS = Buffer[o + (int)ePut.RS3] & 0x3;
            Tone.aOp[0].ML = Buffer[o + (int)ePut.ML0] & 0xf;
            Tone.aOp[1].ML = Buffer[o + (int)ePut.ML1] & 0xf;
            Tone.aOp[2].ML = Buffer[o + (int)ePut.ML2] & 0xf;
            Tone.aOp[3].ML = Buffer[o + (int)ePut.ML3] & 0xf;
            Tone.aOp[0].DT = DT_Table[Buffer[o + (int)ePut.DT0] & 0x7];
            Tone.aOp[1].DT = DT_Table[Buffer[o + (int)ePut.DT1] & 0x7];
            Tone.aOp[2].DT = DT_Table[Buffer[o + (int)ePut.DT2] & 0x7];
            Tone.aOp[3].DT = DT_Table[Buffer[o + (int)ePut.DT3] & 0x7];
            //Tone.aOp[0].AM = (Buffer[o + (int)ePut.DR0_AM] >> 7) & 0x1;
            //Tone.aOp[1].AM = (Buffer[o + (int)ePut.DR1_AM] >> 7) & 0x1;
            //Tone.aOp[2].AM = (Buffer[o + (int)ePut.DR2_AM] >> 7) & 0x1;
            //Tone.aOp[3].AM = (Buffer[o + (int)ePut.DR3_AM] >> 7) & 0x1;
            Tone.aOp[0].SSG = Buffer[o + (int)ePut.SSGEG0] & 0xf;
            Tone.aOp[1].SSG = Buffer[o + (int)ePut.SSGEG1] & 0xf;
            Tone.aOp[2].SSG = Buffer[o + (int)ePut.SSGEG2] & 0xf;
            Tone.aOp[3].SSG = Buffer[o + (int)ePut.SSGEG3] & 0xf;

            Tone.Name = "";

            Tone.Number = i;
        }

        public static IEnumerable<Tone> Reader(string Path)
        {
            List<Tone> tones = new List<Tone>();

            var Buffer = ReadByte(Path);
            if (Buffer.Length == DatLength())
            {
                Tone vTone = new Tone();

                for (int i = 0; i < ToneLength(); ++i)
                {
                    Get(ref vTone, Buffer, i);

                    if (vTone.IsValid())
                        tones.Add(new Tone(vTone));
                }

            }

            return tones;
        }

    }
}
