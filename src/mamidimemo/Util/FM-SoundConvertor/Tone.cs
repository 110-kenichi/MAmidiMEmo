


using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System;
using zanac.MAmidiMEmo.Util.FITOM;
using zanac.MAmidiMEmo.Util.Syx;
using System.Windows.Forms;
using zanac.MAmidiMEmo.Properties;

namespace FM_SoundConvertor
{
    public class Op
    {
        public int AR;
        public int DR;
        public int SR;
        public int RR;
        public int SL;
        public int TL;
        public int KS;
        public int ML;
        public int DT;
        public int DT2;
        public int OSCW;
        public int FINE;
        public int AM;
        public int EGSF;
        public int REV;
        public int FIX;
        public int FIXR;
        public int FIXF;

        public int KSR;
        public int VIB;
        public int EG;
        public int WS;
        public int SSG;

        public int LS;
        public int EBS;
        public int KVS = -1;

        public Op()
        {
        }


        public Op(Op op)
        {
            AR = op.AR;
            DR = op.DR;
            SR = op.SR;
            RR = op.RR;
            SL = op.SL;
            TL = op.TL;
            KS = op.KS;
            ML = op.ML;
            DT = op.DT;
            DT2 = op.DT2;
            OSCW = op.OSCW;
            FINE = op.FINE;
            AM = op.AM;
            EGSF = op.EGSF;
            REV = op.REV;
            FIX = op.FIX;
            FIXR = op.FIXR;
            FIXF = op.FIXF;

            KSR = op.KSR;
            VIB = op.VIB;
            EG = op.EG;
            WS = op.WS;
            SSG = op.SSG;

            LS = op.LS;
            EBS = op.EBS;
            KVS = op.KVS;
        }
    }



    public class Tone
    {
        public string[] MML;

        public string Name;
        public int Number;
        public int FB;
        public int FB2;
        public int AL;
        public int CNT = -1;

        public int REV;

        public int PMS;
        public int AMS;
        public int PMS2;
        public int AMS2;

        public int AMD;
        public int PMD;
        public int AMD2;
        public int PMD2;

        public int? NE;
        public int? NF;
        public int? SY;
        public int? SY2;
        public int? LFOE;
        public int? LFRQ;
        public int? LFRQ2;
        public int? LFOW;
        public int? LFOW2;

        public int? LFD;
        public int? LFD2;

        public Op[] aOp;

        public int KeyShift;
        public int PitchShift;
        public int PitchShift2;
        public int KeyOnDelay;
        public int KeyOffDelay;

        public Tone()
        {
            Name = "";
            Number = -1;
            aOp = new Op[4];
            aOp[0] = new Op();
            aOp[1] = new Op();
            aOp[2] = new Op();
            aOp[3] = new Op();
        }

        public Tone(Tone tone)
        {
            Name = tone.Name;
            Number = tone.Number;
            FB = tone.FB;
            FB2 = tone.FB2;
            AL = tone.AL;
            CNT = tone.CNT;

            REV = tone.REV;

            AMD = tone.AMD;
            PMD = tone.PMD;
            AMD2 = tone.AMD2;
            PMD2 = tone.PMD2;

            PMS = tone.PMS;
            AMS = tone.AMS;
            PMS2 = tone.PMS2;
            AMS2 = tone.AMS2;

            NE = tone.NE;
            NF = tone.NF;
            SY = tone.SY;
            SY2 = tone.SY2;
            LFOE = tone.LFOE;
            LFRQ = tone.LFRQ;
            LFRQ2 = tone.LFRQ2;
            LFOW = tone.LFOW;
            LFOW2 = tone.LFOW2;

            LFD = tone.LFD;
            LFD2 = tone.LFD2;

            KeyShift = tone.KeyShift;
            PitchShift = tone.PitchShift;
            PitchShift2 = tone.PitchShift2;
            KeyOnDelay = tone.KeyOnDelay;
            KeyOffDelay = tone.KeyOffDelay;

            aOp = new Op[4];
            aOp[0] = new Op(tone.aOp[0]);
            aOp[1] = new Op(tone.aOp[1]);
            aOp[2] = new Op(tone.aOp[2]);
            aOp[3] = new Op(tone.aOp[3]);
        }

        public bool IsValid()
        {
            return (Number >= 0x00 && Number <= 0xff);  // && aOp[0].AR > 0 && aOp[1].AR > 0 && aOp[2].AR > 0 && aOp[3].AR > 0);
        }

        public bool IsValid2Op()
        {
            return (Number >= 0x00 && Number <= 0xff);  // && aOp[0].AR > 0 && aOp[1].AR > 0);
        }

        public override string ToString()
        {
            return "(" + Number + ")" + Name;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string SupportedExts
        {
            get
            {
                return "*.muc;*.dat;*.mwi;*.mml;*.fxb;*.gwi;*.bnk;*.syx;*.ff;*.ffopm;*.vgi;*.tfi";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IEnumerable<Tone> ImportToneFile(string file)
        {
            string ext = System.IO.Path.GetExtension(file);
            IEnumerable<Tone> tones = null;
            try
            {
                switch (ext.ToUpper(CultureInfo.InvariantCulture))
                {
                    case ".MUC":
                        tones = Muc.Reader(file);
                        break;
                    case ".DAT":
                        tones = Dat.Reader(file);
                        break;
                    case ".MWI":
                        tones = Fmp.Reader(file);
                        break;
                    case ".MML":
                        tones = Pmd.Reader(file);
                        break;
                    case ".FXB":
                        tones = Vopm.Reader(file);
                        break;
                    case ".GWI":
                        tones = Gwi.Reader(file);
                        break;
                    case ".BNK":
                        tones = BankReader.Read(file);
                        break;
                    case ".SYX":
                        tones = SyxReaderTX81Z.Read(file);
                        break;
                    case ".FF":
                        tones = FF.Reader(file);
                        break;
                    case ".FFOPM":
                        tones = FF.Reader(file);
                        break;
                    case ".VGI":
                        tones = Vgi.Reader(file);
                        break;
                    case ".TFI":
                        tones = Tfi.Reader(file);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                MessageBox.Show(Resources.FailedLoadFile + "\r\n" + ex.Message);
            }
            return tones;
        }
    }
}
