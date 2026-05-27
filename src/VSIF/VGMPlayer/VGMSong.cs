using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using zanac.VGMPlayer.Properties;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using zanac.VGMPlayer;
using static zanac.VGMPlayer.FormMain;
using System.Diagnostics;
using System.Xml.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using zanac.MAmidiMEmo.Gimic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Net;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Windows.Media;
using System.IO.Ports;
using System.Windows;

//Sega Genesis VGM player. Player written and emulators ported by Landon Podbielski. 
namespace zanac.VGMPlayer
{
    public class VGMSong : SongBase
    {
        private const uint FCC_VGM = 0x206D6756;    // 'Vgm '

        private byte[] vgmData;
        private int vgmDataOffset;
        private int vgmDataCurrentOffset;
        private uint vgmDataLen;
        private (VGM_HEADER cur, VGM_HEADER raw) vgmHead;

        private VsifClient comPortDCSG;
        private VsifClient comPortOPLL;
        private VsifClient comPortOPN2;
        private VsifClient comPortSCC;
        private VsifClient comPortY8910;
        private VsifClient comPortOPM;
        private VsifClient comPortOPL3;
        private VsifClient comPortOPNA;
        private VsifClient comPortOPNB;
        private VsifClient comPortY8950;
        private VsifClient comPortOPN;
        private VsifClient comPortNES;
        private VsifClient comPortMCD;
        private VsifClient comPortSAA;
        private VsifClient comPortPCE;
        private VsifClient comPortPWM;

        private VsifClient comPortTurboRProxy;

        private BinaryReader vgmReader;

        private List<byte> dacData = new List<byte>();
        private List<int> dacDataOffset = new List<int>();
        private List<int> dacDataLength = new List<int>();

        private byte[] dpcmData;
        private static int[] dpcmFreqTable = new int[] { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 85, 72, 54 };

        private int dacOffset = 0;

        private Dictionary<int, StreamData> streamTable = new Dictionary<int, StreamData>();

        private SegaPcm segaPcm;

        private K053260 k053260;

        private OKIM6295 okim6295;

        private NesDpcm nesDpcm;

        private OpnbPcm opnbPcm;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public VGMSong(string fileName) : base(fileName)
        {
            OpenFile(fileName);
        }

        protected override void StopAllSounds(bool volumeOff)
        {
            abort();

            if (comPortTurboRProxy != null)
            {
                comPortTurboRProxy.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)(decimal)comPortTurboRProxy.BitBangWait.GetValue(Settings.Default));
            }

            if (comPortDCSG != null)
            {
                switch (comPortDCSG.SoundModuleType)
                {
                    case VsifSoundModuleType.Genesis:
                    case VsifSoundModuleType.Genesis_Low:
                    case VsifSoundModuleType.Genesis_FTDI:
                        for (int i = 0; i < 3; i++)
                            comPortDCSG.DeferredWriteData(0, 5 << 2, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        comPortDCSG.DeferredWriteData(0, 5 << 2, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        break;
                    case VsifSoundModuleType.SMS:
                        for (int i = 0; i < 3; i++)
                            comPortDCSG.DeferredWriteData(0, 0xFF, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        comPortDCSG.DeferredWriteData(0, 0xFF, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        break;
                    case VsifSoundModuleType.SMS_FTDI:
                        for (int i = 0; i < 3; i++)
                            comPortDCSG.DeferredWriteData(0, 0x00, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        comPortDCSG.DeferredWriteData(0, 0x00, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        break;
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        for (int i = 0; i < 3; i++)
                            comPortDCSG.DeferredWriteData(0xF, 0, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        comPortDCSG.DeferredWriteData(0xF, 0, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        break;
                    case VsifSoundModuleType.NanoDrive:
                        for (int i = 0; i < 3; i++)
                        {
                            comPortDCSG.DeferredWriteData(0x50, 0xFF, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                            comPortDCSG.DeferredWriteData(0x30, 0xFF, (byte)(0x80 | i << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        }
                        comPortDCSG.DeferredWriteData(0x50, 0xFF, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        comPortDCSG.DeferredWriteData(0x30, 0xFF, (byte)(0x80 | 3 << 5 | 0x1f), (int)Program.Default.BitBangWaitDCSG);
                        break;
                }
            }

            if (comPortOPLL != null)
            {
                //KOFF
                for (int i = 0x20; i <= 0x28; i++)
                {
                    deferredWriteOPLL(i, 0x00);
                }

                deferredWriteOPLL(0xe, 0x00);

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x30; i <= 0x38; i++)
                    {
                        deferredWriteOPLL(i, 0x0F);
                    }
                    //RR
                    for (int i = 0x6; i <= 0x7; i++)
                    {
                        deferredWriteOPLL(i, 0xFF);
                    }
                }
            }

            if (comPortOPN2 != null)
            {
                switch (comPortOPN2.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        // ALL KEY OFF
                        for (int i = 0; i < 3; i++)
                        {
                            deferredWriteOPN2_P0(comPortOPN2, 0x28, i);
                            deferredWriteOPN2_P0(comPortOPN2, 0x28, 0x4 | i);
                        }
                        break;
                }

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPN2_P0(comPortOPN2, i, 0xFF);
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPN2_P1(comPortOPN2, i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPN2_P0(comPortOPN2, i, 0xFF);
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPN2_P1(comPortOPN2, i, 0xFF);
                }
            }

            if (comPortOPNA != null)
            {
                switch (comPortOPNA.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                    case VsifSoundModuleType.PC88_FTDI:
                        // ALL KEY OFF
                        for (int i = 0; i < 3; i++)
                        {
                            deferredWriteOPNA_P0(comPortOPNA, 0x28, i);
                            deferredWriteOPNA_P0(comPortOPNA, 0x28, 0x4 | i);
                        }
                        //RHYTHM
                        deferredWriteOPNA_P0(comPortOPNA, 0x10, 0x80);
                        //SSG
                        deferredWriteOPNA_P0(comPortOPNA, 0x07, 0x3F);
                        //ADPCM
                        deferredWriteOPNA_P1(comPortOPNA, 0x00, 1);
                        break;
                }

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPNA_P0(comPortOPNA, i, 0xFF);
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPNA_P1(comPortOPNA, i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPNA_P0(comPortOPNA, i, 0xFF);
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPNA_P1(comPortOPNA, i, 0xFF);
                    //RHYTHM
                    deferredWriteOPNA_P0(comPortOPNA, 0x11, 0xFF);
                    //SSG
                    deferredWriteOPNA_P0(comPortOPNA, 0x08, 0);
                    deferredWriteOPNA_P0(comPortOPNA, 0x09, 0);
                    deferredWriteOPNA_P0(comPortOPNA, 0x0A, 0);
                    //ADPCM
                    //deferredWriteOPNA_P1(comPortOPNA, 0x0B, 0);    //VOLUME 0
                    deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x01);  //RESET
                    //deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x00);  //STOP
                }

                EnableDacYM2608(comPortOPNA, false);
            }

            if (comPortOPNB != null)
            {
                switch (comPortOPNB.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        // ALL KEY OFF
                        for (int i = 0; i < 3; i++)
                        {
                            deferredWriteOPNB_P0(comPortOPNB, 0x28, i);
                            deferredWriteOPNB_P0(comPortOPNB, 0x28, 0x4 | i);
                        }
                        //ADPCM-B
                        deferredWriteOPNB_P0(comPortOPNB, 0x10, 0x01);
                        //SSG
                        deferredWriteOPNB_P0(comPortOPNB, 0x07, 0x3F);
                        //ADPCM-A
                        deferredWriteOPNB_P1(comPortOPNB, 0x00, 0xFF);
                        break;
                }

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPNB_P0(comPortOPNB, i, 0xFF);
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPNB_P1(comPortOPNB, i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPNB_P0(comPortOPNB, i, 0xFF);
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPNB_P1(comPortOPNB, i, 0xFF);
                    //ADPCM-B
                    deferredWriteOPNB_P0(comPortOPNB, 0x10, 0x01);
                    //SSG
                    deferredWriteOPNB_P0(comPortOPNB, 0x08, 0);
                    deferredWriteOPNB_P0(comPortOPNB, 0x09, 0);
                    deferredWriteOPNB_P0(comPortOPNB, 0x0A, 0);
                    //ADPCM-A
                    deferredWriteOPNB_P1(comPortOPNB, 0x00, 0xFF);
                }
            }

            if (comPortOPN != null)
            {
                switch (comPortOPN.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                    case VsifSoundModuleType.PC88_FTDI:
                        // ALL KEY OFF
                        for (int i = 0; i < 3; i++)
                        {
                            deferredWriteOPN_P0(0x28, i);
                        }
                        //SSG
                        deferredWriteOPN_P0(0x07, 0x3F);
                        break;
                }

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x4F; i++)
                        deferredWriteOPN_P0(i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPN_P0(i, 0xFF);
                    //SSG
                    deferredWriteOPN_P0(0x08, 0);
                    deferredWriteOPN_P0(0x09, 0);
                    deferredWriteOPN_P0(0x0A, 0);
                }
            }

            if (comPortOPM != null)
            {
                //KOFF
                for (int i = 0; i < 8; i++)
                    deferredWriteOPM(0x08, i);

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x60; i <= 0x7f; i++)
                        deferredWriteOPM(i, 0xFF);
                    //RR
                    for (int i = 0xE0; i <= 0xFF; i++)
                        deferredWriteOPM(i, 0xFF);
                }
            }
            if (comPortOPL3 != null)
            {
                //KOFF
                for (int i = 0; i < 9; i++)
                    deferredWriteOPL3_P0(0xB0 + i, 0);
                for (int i = 0; i < 9; i++)
                    deferredWriteOPL3_P1(0xB0 + i, 0);

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x55; i++)
                        deferredWriteOPL3_P0(i, 0xFF);
                    for (int i = 0x40; i <= 0x55; i++)
                        deferredWriteOPL3_P1(i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x95; i++)
                        deferredWriteOPL3_P0(i, 0xFF);
                    for (int i = 0x80; i <= 0x95; i++)
                        deferredWriteOPL3_P1(i, 0xFF);
                }
            }

            if (comPortY8950 != null)
            {
                //KOFF
                for (int i = 0; i < 9; i++)
                    deferredWriteY8950(0xB0 + i, 0);

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x55; i++)
                        deferredWriteY8950(i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x95; i++)
                        deferredWriteY8950(i, 0xFF);
                }
            }

            //SCC
            if (comPortSCC != null)
            {
                switch (comPortSCC.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        deferredWriteSCC(3, 0xf, 00);

                        if (volumeOff)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                deferredWriteSCC(2, i, 00);
                            }
                        }
                        break;
                }
            }

            //Y8910
            if (comPortY8910 != null)
            {
                //comPortY8910?.DeferredWriteData(0x17, (byte)0x07, (byte)0xff, (int)Program.Default.BitBangWaitAY8910);
                deferredWriteY8910(0x8, 0x00, false);
                deferredWriteY8910(0x9, 0x00, false);
                deferredWriteY8910(0xa, 0x00, false);
                if ((vgmHead.raw.lngHzAY8910 & 0x40000000) == 0x40000000)
                {
                    deferredWriteY8910(0x8, 0x00, true);
                    deferredWriteY8910(0x9, 0x00, true);
                    deferredWriteY8910(0xa, 0x00, true);
                }
            }
            //NES
            if (comPortNES != null)
            {
                comPortNES.DeferredWriteData(0, 0x15, 0, (int)Program.Default.BitBangWaitNES);
                comPortNES.DeferredWriteData(0, 0x83, 0xc0, (int)Program.Default.BitBangWaitNES);

                if (comPortNES.SoundModuleType == VsifSoundModuleType.NES_FTDI_INDIRECT)
                {
                    //KOFF
                    for (int i = 0x20; i <= 0x28; i++)
                    {
                        //deferredWriteOPLL(i, 0x00);
                        DeferredWriteNES(36 + 1, i);
                        DeferredWriteNES(36 + 3, 0);
                    }

                    //deferredWriteOPLL(0xe, 0x00);
                    DeferredWriteNES(36 + 1, 0xe);
                    DeferredWriteNES(36 + 3, 0);

                    if (volumeOff)
                    {
                        //TL
                        for (int i = 0x30; i <= 0x38; i++)
                        {
                            //deferredWriteOPLL(i, 0x0F);
                            DeferredWriteNES(36 + 1, i);
                            DeferredWriteNES(36 + 3, 0xF);
                        }
                        //RR
                        for (int i = 0x6; i <= 0x7; i++)
                        {
                            //deferredWriteOPLL(i, 0xFF);
                            DeferredWriteNES(36 + 1, i);
                            DeferredWriteNES(36 + 3, 0xFF);
                        }
                    }
                }
            }
            //MCD RF5C164
            if (comPortMCD != null)
            {
                DeferredWriteMCDReg(0x08, 0xFF); // koff all
            }
            //SAA1099
            if (comPortSAA != null)
            {
                for (byte ch = 0; ch < 6; ch++)
                {
                    deferredWriteSAAReg(ch, 0, 0);
                }
            }
            //PCE
            if (comPortPCE != null)
            {
                //https://nicotakuya.hatenablog.com/entry/2020/12/04/010457
                for (byte ch = 0; ch < 6; ch++)
                {
                    DeferredWritePCE(0x00, ch);
                    DeferredWritePCE(0x04, 0);
                }
            }
            //PWM
            if (comPortPWM != null)
            {
                DeferredWritePWMReg(0, 0);
            }

            flushDeferredWriteDataAndWait();
            Thread.Sleep(250);
        }

        private static byte[] chAddressOffset = new byte[] { 0x00, 0x01, 0x02, 0x08, 0x09, 0x0a, 0x10, 0x11, 0x12 };

        private static byte[,] op2chConselTable = new byte[,] {
            { 00, 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16 ,17 },
            { 01, 02, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16 ,17 ,00 ,00 },
            { 02, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16 ,17 ,00, 00, 00, 00 },
            { 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16 ,17 ,00, 00, 00, 00, 00, 00 },
            { 06, 07, 08, 10, 11, 13, 14, 15, 16 ,17 ,00, 00, 00, 00, 00, 00, 00, 00 },
            { 06, 07, 08, 11, 14, 15, 16 ,17 ,00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
            { 06, 07, 08, 15, 16 ,17 ,00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
        };

        /// <summary>
        /// 
        /// </summary>
        private void Y8950WriteData(VsifClient comPort, uint address, int op, int slot, byte opmode, int consel, byte data, bool defferred, int wait)
        {
            //useCache = false;
            var adrL = address & 0xff;
            var adrH = (address & 0x100) >> 7;  // 0 or 2
            switch (opmode)
            {
                //Channel        0   1   2   3   4   5   6   7   8
                //Operator 1    00  01  02  06  07  09  0C  0D  0E
                //Operator 2    03  04  05  09  0A  0B  0F  10  11
                case 0:
                case 1:
                    slot = op2chConselTable[consel, slot];
                    if (slot >= 9)
                    {
                        adrH = 2;
                        slot = slot - 9;
                    }
                    break;
                //Channel        0   1   2    
                //Operator 1    00  01  02  
                //Operator 2    03  04  05  
                //Operator 3    07  08  09  
                //Operator 4    0A  0B  0C  
                default:
                    if (slot >= 3)
                    {
                        adrH = 2;
                        slot -= 3;
                    }
                    if (op >= 2)
                    {
                        op -= 2;
                        slot += 3;
                    }
                    break;
            }

            byte chofst = 0;
            switch (adrL)
            {
                case 0x20:
                case 0x40:
                case 0x60:
                case 0x80:
                case 0xe0:
                    chofst = chAddressOffset[slot];
                    break;
                case 0xa0:
                case 0xb0:
                case 0xc0:
                    chofst = (byte)slot;
                    break;
            }

            var adr = (byte)(adrL + (op * 3) + chofst);
            address = (adrH << 8) | adr;

            if (defferred)
            {
                switch (adrH)
                {
                    case 0:
                        comPort?.DeferredWriteData(10, adr, data, wait);
                        break;
                    case 2:
                        comPort?.DeferredWriteData(11, adr, data, wait);
                        break;
                }
            }
            else
            {
                switch (adrH)
                {
                    case 0:
                        comPort?.WriteData(10, adr, data, wait);
                        break;
                    case 2:
                        comPort?.WriteData(11, adr, data, wait);
                        break;
                }
            }
        }


        private (VGM_HEADER, VGM_HEADER) readVGMHeader(BinaryReader hFile)
        {
            VGM_HEADER rawHead = new VGM_HEADER();
            VGM_HEADER curHead = new VGM_HEADER();
            curHead.lngDataOffset = 0x100;
            FieldInfo[] fields = typeof(VGM_HEADER).GetFields();
            int position = 4;
            long lngDataOffset = 0x40;
            bool vrc7 = false;

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(uint))
                {
                    uint val = hFile.ReadUInt32();
                    //if (curHead.lngDataOffset < 0x100 && position >= curHead.lngDataOffset)
                    //    val = 0;
                    position += 4;
                    field.SetValue(rawHead, val);
                    if (field.Name.StartsWith("lngHz"))
                        val = (uint)(val & ~0x40000000);
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(ushort))
                {
                    ushort val = hFile.ReadUInt16();
                    //if (curHead.lngDataOffset < 0x100 && position >= curHead.lngDataOffset)
                    //    val = 0;
                    position += 2;
                    field.SetValue(rawHead, val);
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(sbyte))
                {
                    sbyte val = hFile.ReadSByte();
                    //if (curHead.lngDataOffset < 0x100 && position >= curHead.lngDataOffset)
                    //    val = 0;
                    position += 1;
                    field.SetValue(rawHead, val);
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(byte))
                {
                    byte val = hFile.ReadByte();
                    //if (curHead.lngDataOffset < 0x100 && position >= curHead.lngDataOffset)
                    //    val = 0;
                    position += 1;
                    field.SetValue(rawHead, val);
                    field.SetValue(curHead, val);
                }
                if (field.Name == "lngDataOffset")
                {
                    if (curHead.lngVersion >= 0x0150)
                        lngDataOffset = 0x34 + curHead.lngDataOffset;
                }
                if (position >= lngDataOffset)
                    break;
            }

            // Header preperations
            if (curHead.lngVersion < 0x00000101)
            {
                curHead.lngRate = 0;
            }
            if (curHead.lngVersion < 0x00000110)
            {
                curHead.shtPSG_Feedback = 0x0000;
                curHead.bytPSG_SRWidth = 0x00;
                curHead.lngHzYM2612 = curHead.lngHzYM2413;
                curHead.lngHzYM2151 = curHead.lngHzYM2413;
            }

            if (curHead.lngHzDCSG != 0)
            {
                if (curHead.shtPSG_Feedback == 0)
                    curHead.shtPSG_Feedback = 0x0009;
                if (curHead.bytPSG_SRWidth == 0)
                    curHead.bytPSG_SRWidth = 0x10;

                SongChipInformation += $"DCSG@{curHead.lngHzDCSG / 1000000f}MHz ";

                if (Program.Default.DCSG_Enable)
                {
                    coonectToDCSG();
                }
            }
            if (curHead.lngHzYM2413 != 0)
            {
                String chipName = "OPLL";
                if ((curHead.lngHzYM2413 & 0x80000000) != 0)
                {
                    chipName = "VRC7";
                    vrc7 = true;
                }

                uint clk = (uint)(curHead.lngHzYM2413 & ~0x80000000);
                SongChipInformation += $"{chipName}@{clk / 1000000f}MHz ";

                if (!vrc7)
                {
                    if (Program.Default.OPLL_Enable)
                    {
                        connectToOPLL();
                    }
                }
            }
            if (curHead.lngHzYM2612 != 0 && curHead.lngVersion >= 0x00000110)
            {
                String chipName = "OPN2";
                if ((curHead.lngHzYM2612 & 0x80000000) != 0)
                    chipName = "YM3438";
                curHead.lngHzYM2612 = (uint)(curHead.lngHzYM2612 & ~0x80000000);
                SongChipInformation += $"{chipName}@{curHead.lngHzYM2612 / 1000000f}MHz ";

                if (Program.Default.OPN2_Enable)
                {
                    connectToOPN2(0, false);
                }
                else
                {
                    if (Program.Default.OPNA_Enable && connectToOPNA(curHead.lngHzYM2612, false))
                    {
                        comPortOPNA.Tag["ProxyOPN2"] = true;
                        //Force OPNA mode
                        deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                        //Enable Pseudo DAC
                        EnablePseudoDacYM2608(comPortOPNA, true);
                    }
                }
            }
            if (curHead.lngHzYM2151 != 0 && curHead.lngVersion >= 0x00000110)
            {
                SongChipInformation += $"OPM@{curHead.lngHzYM2151 / 1000000f}MHz ";

                if (Program.Default.OPM_Enable)
                {
                    connectToOPM(curHead.lngHzYM2151);
                }
            }
            if (curHead.lngHzYM3526 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"OPL@{curHead.lngHzYM3526 / 1000000f}MHz ";

                if (Program.Default.OPL3_Enable)
                {
                    connectToOPL3();
                }
                else
                {
                    if (Program.Default.Y8950_Enable && connectToMsxAudio())
                    {
                        comPortY8950.Tag["ProxyOPL"] = true;
                    }
                }
            }
            if (curHead.lngHzYM3812 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"OPL2@{curHead.lngHzYM3812 / 1000000f}MHz ";

                if (Program.Default.OPL3_Enable)
                {
                    connectToOPL3();
                }
                else
                {
                    if (Program.Default.Y8950_Enable && connectToMsxAudio())
                    {
                        comPortY8950.Tag["ProxyOPL2"] = true;
                    }
                }
            }
            if (curHead.lngHzYMF262 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"OPL3@{curHead.lngHzYMF262 / 1000000f}MHz ";

                if (Program.Default.OPL3_Enable)
                {
                    connectToOPL3();
                }
            }
            if (curHead.lngHzYM2203 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"OPN@{curHead.lngHzYM2203 / 1000000f}MHz ";

                if (Program.Default.OPN_Enable)
                {
                    connectToOPN();
                }
                else
                {
                    if (Program.Default.OPNA_Enable && connectToOPNA(curHead.lngHzYM2203 * 2, false))
                    {
                        comPortOPNA.Tag["ProxyOPN"] = true;
                        //Force OPNA mode
                        deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                    }
                    else if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                    {
                        comPortOPN2.Tag["ProxyOPN"] = true;
                        if (Program.Default.Y8910_Enable && connectToPSG())
                        {
                            comPortY8910.Tag["ProxyOPN"] = true;
                        }
                    }
                }
            }
            if (curHead.lngHzYM2608 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"OPNA@{curHead.lngHzYM2608 / 1000000f}MHz ";

                if (Program.Default.OPNA_Enable)
                {
                    connectToOPNA(curHead.lngHzYM2608, false);
                }
                else
                {
                    if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                    {
                        comPortOPN2.Tag["ProxyOPNA"] = true;
                        if (Program.Default.Y8910_Enable && connectToPSG())
                        {
                            comPortY8910.Tag["ProxyOPNA"] = true;
                        }
                    }
                }
            }
            if (curHead.lngHzYM2610 != 0 && curHead.lngVersion >= 0x00000151)
            {
                bool opnbb = false;
                String chipName = "OPNB";
                if ((curHead.lngHzYM2610 & 0x80000000) != 0)
                {
                    chipName = "OPNB-B";
                    opnbb = true;
                }
                curHead.lngHzYM2610 = (uint)(curHead.lngHzYM2610 & ~0x80000000);
                SongChipInformation += $"{chipName}@{curHead.lngHzYM2610 / 1000000f}MHz ";

                if (Program.Default.OPNB_Enable)
                {
                    if (connectToOPNB(curHead.lngHzYM2610, true))
                    {
                        comPortOPNB.Tag["ProxyOPNB"] = true;
                    }
                }
                else if (Program.Default.OPNA_Enable)
                {
                    if (connectToOPNA(curHead.lngHzYM2610, true))
                    {
                        comPortOPNA.Tag["ProxyOPNB"] = true;
                        //Force OPNA mode
                        deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                        //Enable Pseudo DAC
                        EnablePseudoDacYM2608(comPortOPNA, true);
                    }
                }
                else
                {
                    if (Program.Default.OPN2_Enable && connectToOPN2(curHead.lngHzYM2610, true))
                    {
                        comPortOPN2.Tag["ProxyOPNB"] = true;
                        if (!opnbb)
                        {
                            //Enable Dac
                            deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);
                            comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                            UseChipInformation += $"w/ DAC";
                        }
                        if (Program.Default.Y8910_Enable && connectToPSG())
                        {
                            comPortY8910.Tag["ProxyOPNB"] = true;
                        }
                    }
                }
            }
            if (curHead.lngHzY8950 != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"Y8950@{curHead.lngHzY8950 / 1000000f}MHz ";

                if (Program.Default.Y8950_Enable)
                {
                    connectToMsxAudio();
                }
                else
                {
                    if (Program.Default.OPL3_Enable && connectToOPL3())
                    {
                        comPortOPL3.Tag["ProxyY8950"] = true;
                    }
                    if (Program.Default.OPNA_Enable && connectToOPNA(curHead.lngHzY8950, false))
                    {
                        comPortOPNA.Tag["ProxyY8950"] = true;
                    }
                }
            }
            if (curHead.lngHzK051649 != 0 && curHead.lngVersion >= 0x00000161)
            {
                SongChipInformation += $"SCC@{curHead.lngHzK051649 / 1000000f}MHz ";

                if (Program.Default.SCC_Enable)
                {
                    connectToSCC();
                }
            }
            if (curHead.lngHzAY8910 != 0 && curHead.lngVersion >= 0x00000151)
            {
                if ((curHead.bytAYFlag & 0x10) == 0x10)
                    curHead.lngHzAY8910 = curHead.lngHzAY8910 / 2;
                SongChipInformation += $"PSG@{curHead.lngHzAY8910 / 1000000f}MHz ";

                if (Program.Default.Y8910_Enable)
                {
                    connectToPSG();
                }
                else if (Program.Default.OPNA_Enable && connectToOPNA(curHead.lngHzAY8910 * 4, false))
                {
                    comPortOPNA.Tag["ProxyY8910"] = true;
                }
                else if (Program.Default.OPN_Enable && connectToOPN())
                {
                    comPortOPN.Tag["ProxyY8910"] = true;
                }
            }
            if (curHead.lngHzOKIM6258 != 0 && curHead.lngVersion >= 0x00000161)
            {
                uint[] dividers = new uint[] { 1024, 768, 512, 512 };

                SongChipInformation += $"OKIM6258@{curHead.lngHzOKIM6258 / 1000000f}MHz ";
                oki6258_master_clock = curHead.lngHzOKIM6258;
                oki6258_divider = dividers[curHead.bytOKI6258Flags & 0x3];
                oki6258_sample_rate = oki6258_master_clock / oki6258_divider;

                oki6258_clock_buffer[0x00] = (oki6258_master_clock & 0x000000FF) >> 0;
                oki6258_clock_buffer[0x01] = (oki6258_master_clock & 0x0000FF00) >> 8;
                oki6258_clock_buffer[0x02] = (oki6258_master_clock & 0x00FF0000) >> 16;
                oki6258_clock_buffer[0x03] = (oki6258_master_clock & 0xFF000000) >> 24;

                if (comPortTurboRProxy != null)
                {
                    comPortTurboRProxy.Tag["ProxyOKIM6258"] = true;
                    UseChipInformation += $"tR DAC@{curHead.lngHzOKIM6258 / 1000000f}MHz ";
                }
                else if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                {
                    comPortOPN2.Tag["ProxyOKIM6258"] = true;
                    //Enable Dac
                    deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);
                    UseChipInformation += $"OPN2 DAC@{curHead.lngHzOKIM6258 / 1000000f}MHz ";
                }
                else if (Program.Default.OPNA_Enable && connectToOPNA(7987200, false))
                {
                    comPortOPNA.Tag["ProxyOKIM6258"] = true;
                    //Force OPNA mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                    //Enable Dac
                    EnableDacYM2608(comPortOPNA, true);
                    UseChipInformation += $"OPNA DAC@{curHead.lngHzOKIM6258 / 1000000f}MHz ";
                }
            }
            if (curHead.lngHzOKIM6295 != 0 && curHead.lngVersion >= 0x00000161)
            {
                SongChipInformation += $"OKIM6295@{(curHead.lngHzOKIM6295 & 0x7FFFFFFFu) / 1000000f}MHz ";
                if (comPortTurboRProxy != null)
                {
                    comPortTurboRProxy.Tag["ProxyOKIM6295"] = true;

                    okim6295 = new OKIM6295(this);
                    okim6295.device_start_okim6295(0, (int)curHead.lngHzOKIM6295, comPortTurboRProxy);
                    UseChipInformation += $"tR DAC@{(curHead.lngHzOKIM6295 & 0x7FFFFFFFu) / 1000000f}MHz ";
                }
                else if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                {
                    comPortOPN2.Tag["ProxyOKIM6295"] = true;
                    //Enable Dac
                    deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);

                    okim6295 = new OKIM6295(this);
                    okim6295.device_start_okim6295(0, (int)curHead.lngHzOKIM6295, comPortOPN2);
                    UseChipInformation += $"OPN2 DAC@{(curHead.lngHzOKIM6295 & 0x7FFFFFFFu) / 1000000f}MHz ";
                }
                else if (Program.Default.OPNA_Enable && connectToOPNA(7987200, false))
                {
                    comPortOPNA.Tag["ProxyOKIM6295"] = true;
                    //Force OPNA mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                    //Enable Dac
                    EnableDacYM2608(comPortOPNA, true);

                    okim6295 = new OKIM6295(this);
                    okim6295.device_start_okim6295(0, (int)curHead.lngHzOKIM6295, comPortOPNA);
                    UseChipInformation += $"OPNA DAC@{(curHead.lngHzOKIM6295 & 0x7FFFFFFFu) / 1000000f}MHz ";
                }
            }
            if (curHead.lngHzSPCM != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"SEGA PCM@{curHead.lngHzSPCM / 1000000f}MHz ";
                if (comPortTurboRProxy != null)
                {
                    comPortTurboRProxy.Tag["ProxySegaPcm"] = true;

                    segaPcm = new SegaPcm(this);
                    segaPcm.device_start_segapcm(0, (int)curHead.lngHzSPCM, (int)curHead.lngSPCMIntf, comPortTurboRProxy);
                    UseChipInformation += $"tR DAC@@{curHead.lngHzSPCM / 1000000f}MHz ";
                }
                else if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                {
                    comPortOPN2.Tag["ProxySegaPcm"] = true;
                    //Enable Dac
                    deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);

                    segaPcm = new SegaPcm(this);
                    segaPcm.device_start_segapcm(0, (int)curHead.lngHzSPCM, (int)curHead.lngSPCMIntf, comPortOPN2);
                    UseChipInformation += $"OPN2 DAC@{curHead.lngHzSPCM / 1000000f}MHz ";
                }
                else if (Program.Default.OPNA_Enable && connectToOPNA(7987200, false))
                {
                    comPortOPNA.Tag["ProxySegaPcm"] = true;
                    //Force OPNA mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                    //Enable Dac
                    EnableDacYM2608(comPortOPNA, true);

                    segaPcm = new SegaPcm(this);
                    segaPcm.device_start_segapcm(0, (int)curHead.lngHzSPCM, (int)curHead.lngSPCMIntf, comPortOPNA);
                    UseChipInformation += $"OPNA DAC@{curHead.lngHzSPCM / 1000000f}MHz ";
                }
            }
            if (curHead.lngHzK053260 != 0 && curHead.lngVersion >= 0x00000161)
            {
                SongChipInformation += $"K053260@{curHead.lngHzK053260 / 1000000f}MHz ";
                if (comPortTurboRProxy != null)
                {
                    comPortTurboRProxy.Tag["ProxyK053260"] = true;

                    k053260 = new K053260(this);
                    k053260.device_start_k053260(0, (int)curHead.lngHzK053260, comPortTurboRProxy);
                    UseChipInformation += $"tR DAC@{curHead.lngHzK053260 / 1000000f}MHz ";
                }
                else if (Program.Default.OPN2_Enable && connectToOPN2(0, false))
                {
                    comPortOPN2.Tag["ProxyK053260"] = true;
                    //Enable Dac
                    deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);

                    k053260 = new K053260(this);
                    k053260.device_start_k053260(0, (int)curHead.lngHzK053260, comPortOPN2);
                    UseChipInformation += $"OPN2 DAC@{curHead.lngHzK053260 / 1000000f}MHz ";
                }
                else if (Program.Default.OPNA_Enable && connectToOPNA(7987200, false))
                {
                    comPortOPNA.Tag["ProxyK053260"] = true;
                    //Force OPNA mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                    //Enable Dac
                    EnableDacYM2608(comPortOPNA, true);

                    k053260 = new K053260(this);
                    k053260.device_start_k053260(0, (int)curHead.lngHzK053260, comPortOPNA);
                    UseChipInformation += $"OPNA DAC@{curHead.lngHzK053260 / 1000000f}MHz ";
                }
            }
            if (curHead.lngHzNESAPU != 0 && curHead.lngVersion >= 0x00000161)
            {
                curHead.lngHzNESAPU = (uint)(curHead.lngHzNESAPU & ~0x80000000);    //FDS
                SongChipInformation += $"NES APU@{curHead.lngHzNESAPU / 1000000f}MHz ";

                if (Program.Default.NES_Enable)
                {
                    if (connectToNES(vrc7))
                    {
                        nesDpcm = new NesDpcm(curHead.lngHzNESAPU, this);
                        nesDpcm.StartEngine();
                        //HACK:
                        DeferredWriteNES(0x15, 0x0f);
                    }
                }
            }
            if (curHead.lngHzRF5C164 != 0 && curHead.lngVersion >= 0x00000151)
            {
                curHead.lngHzRF5C164 = (uint)(curHead.lngHzRF5C164 & ~0x80000000);
                SongChipInformation += $"RF5C164@{curHead.lngHzRF5C164 / 1000000f}MHz ";

                if (Program.Default.MCD_Enable)
                {
                    connectToMCD();
                }
            }
            if (curHead.lngHzRF5C68 != 0 && curHead.lngVersion >= 0x00000151)
            {
                curHead.lngHzRF5C68 = (uint)(curHead.lngHzRF5C68 & ~0x80000000);
                SongChipInformation += $"RF5C68@{curHead.lngHzRF5C68 / 1000000f}MHz ";

                if (Program.Default.MCD_Enable && connectToMCD())
                {
                    comPortMCD.Tag["ProxyRF5C68"] = true;
                }
            }
            if (curHead.lngHzSAA1099 != 0 && curHead.lngVersion >= 0x00000171)
            {
                curHead.lngHzSAA1099 = (uint)(curHead.lngHzSAA1099 & ~0x80000000);
                SongChipInformation += $"SAA1099@{curHead.lngHzSAA1099 / 1000000f}MHz ";

                if (Program.Default.SAA_Enable)
                {
                    connectToSAA();
                }
            }
            if (curHead.lngHzHuC6280 != 0 && curHead.lngVersion >= 0x00000161)
            {
                curHead.lngHzHuC6280 = (uint)(curHead.lngHzHuC6280 & ~0x80000000);
                SongChipInformation += $"HuC6280@{curHead.lngHzHuC6280 / 1000000f}MHz ";

                if (Program.Default.PCE_Enable)
                {
                    connectToPCE();
                }
            }
            if (curHead.lngHzPWM != 0 && curHead.lngVersion >= 0x00000151)
            {
                SongChipInformation += $"PWM@{curHead.lngHzPWM / 1000000f}MHz ";

                if (Program.Default.PWM_Enable)
                {
                    connectToPWM();
                }
            }
            return (curHead, rawHead);
        }

        private bool connectToPWM()
        {
            if (comPortPWM == null)
            {
                switch (Program.Default.PWM_IF)
                {
                    case 0:
                        if (comPortPWM == null)
                        {
                            comPortPWM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                                (PortId)Program.Default.PWM_Port);
                            if (comPortPWM != null)
                            {
                                comPortPWM.ChipClockHz["PWM"] = 23011361;
                                comPortPWM.ChipClockHz["PWM_org"] = 23011361;
                                UseChipInformation += "PWM@23.011361MHz ";
                            }
                        }
                        break;
                }
                if (comPortPWM != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }

        private bool connectToPCE()
        {
            if (comPortPCE == null)
            {
                switch (Program.Default.PCE_IF)
                {
                    case 0:
                        if (comPortPCE == null)
                        {
                            comPortPCE = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboEverDrive,
                                (PortId)Program.Default.PCE_Port);
                            if (comPortPCE != null)
                            {
                                comPortPCE.ChipClockHz["PCE"] = 3579545;
                                comPortPCE.ChipClockHz["PCE_org"] = 3579545;
                                UseChipInformation += "PCE@3.579545MHz ";
                                comPortPCE.BitBangWait = typeof(Settings).GetProperty("BitBangWaitPCE");
                            }
                        }
                        break;
                }
                if (comPortPCE != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }

        private bool connectToSAA()
        {
            if (comPortSAA == null)
            {
                switch (Program.Default.SAA_IF)
                {
                    case 0:
                        if (comPortSAA == null)
                        {
                            comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.SAA_Port);

                            if (comPortSAA != null)
                            {
                                comPortSAA.ChipClockHz["SAA1099"] = 8000000;
                                comPortSAA.ChipClockHz["SAA1099_org"] = 8000000;
                                UseChipInformation += "SAA1099@8MHz ";
                                CanLoadCoverArt = true;
                                comPortSAA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitSAA");
                            }
                        }
                        break;
                    case 1:
                        if (comPortSAA == null)
                        {
                            comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.SAA_Port);

                            if (comPortSAA != null)
                            {
                                comPortSAA.ChipClockHz["SAA1099"] = 8000000;
                                comPortSAA.ChipClockHz["SAA1099_org"] = 8000000;
                                UseChipInformation += "SAA1099@8MHz ";
                                CanLoadCoverArt = true;
                                comPortSAA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitSAA");
                            }
                            if (comPortTurboRProxy == null && comPortSAA != null)
                                comPortTurboRProxy = comPortSAA;
                        }
                        break;
                    case 2:
                        if (comPortSAA == null)
                        {
                            comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.SAA_Port);

                            if (comPortSAA != null)
                            {
                                comPortSAA.ChipClockHz["SAA1099"] = 8000000;
                                comPortSAA.ChipClockHz["SAA1099_org"] = 8000000;
                                UseChipInformation += "SAA1099@8MHz ";
                                CanLoadCoverArt = true;
                                comPortSAA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitSAA");
                            }
                        }
                        break;
                    case 3:
                        if (comPortSAA == null)
                        {
                            comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.SAA_Port);

                            if (comPortSAA != null)
                            {
                                comPortSAA.ChipClockHz["SAA1099"] = 8000000;
                                comPortSAA.ChipClockHz["SAA1099_org"] = 8000000;
                                UseChipInformation += "SAA1099@8MHz ";
                                CanLoadCoverArt = true;
                                comPortSAA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitSAA");
                            }
                            if (comPortTurboRProxy == null && comPortSAA != null)
                                comPortTurboRProxy = comPortSAA;
                        }
                        break;
                }
                if (comPortSAA != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }

        private bool connectToMCD()
        {
            if (comPortMCD == null)
            {
                switch (Program.Default.MCD_IF)
                {
                    case 0:
                        if (comPortMCD == null)
                        {
                            comPortMCD = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                                (PortId)Program.Default.MCD_Port);
                            if (comPortMCD != null)
                            {
                                comPortMCD.ChipClockHz["RF5C164"] = 12500000;
                                comPortMCD.ChipClockHz["RF5C164_org"] = 12500000;
                                UseChipInformation += "RF5C164@12.5MHz ";
                            }
                        }
                        break;
                }
                if (comPortMCD != null)
                {
                    Accepted = true;

                    //HACK: sync clk
                    DeferredWriteMCDReg(8, (byte)0xff); // dummy key off
                    DeferredWriteMCDReg(8, (byte)0xff); // dummy key off

                    //debug
                    //DeferredWriteMCDReg(0x0F, 0xc0); // SON ch
                    //DeferredWriteMCDReg(0x0D, 0x00); // PCM ADR
                    //DeferredWriteMCDReg(0x0B, 0x00); // LOOP
                    //DeferredWriteMCDReg(0x09, 0x00); // LOOP
                    //DeferredWriteMCDReg(0x07, 0x08); // pitch hi
                    //DeferredWriteMCDReg(0x05, 0x08); // pitch lo
                    //DeferredWriteMCDReg(0x03, 0xff); // pan
                    //DeferredWriteMCDReg(0x01, 0xff); // vol
                    //DeferredWriteMCDReg(0x01, 0xFF); // vol

                    //DeferredWriteMCDReg(0x08, 0xfe); // kon ch0
                    //DeferredWriteMCDReg(0x08, 0xfe); // kon ch0

                    //DeferredWriteMCDReg(0x08, 0xff); // kon ch0
                    //DeferredWriteMCDReg(0x08, 0xff); // kon ch0

                    return true;
                }
            }

            return false;
        }

        private bool connectToNES(bool vrc7)
        {
            if (comPortNES == null)
            {
                switch (Program.Default.NES_IF)
                {
                    case 0:
                        if (comPortNES == null)
                        {
                            comPortNES = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NES_FTDI_DIRECT,
                                (PortId)Program.Default.NES_Port);

                            if (comPortNES != null)
                            {
                                comPortNES.ChipClockHz["NES"] = 1789772;
                                comPortNES.ChipClockHz["NES_org"] = 1789772;
                                UseChipInformation += "NES APU@1.789772MHz ";
                            }
                        }
                        break;
                    case 1:
                        if (comPortNES == null)
                        {
                            comPortNES = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NES_FTDI_INDIRECT,
                                (PortId)Program.Default.NES_Port);

                            if (comPortNES != null)
                            {
                                comPortNES.ChipClockHz["NES"] = 1789772;
                                comPortNES.ChipClockHz["NES_org"] = 1789772;
                                if (vrc7)
                                    UseChipInformation += "NES APU + VRC7@1.789772MHz ";
                                else
                                    UseChipInformation += "NES APU@1.789772MHz ";
                            }
                        }
                        break;
                }
                if (comPortNES != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }


        private bool connectToPSG()
        {
            if (comPortY8910 == null)
            {
                switch (Program.Default.Y8910_IF)
                {
                    case 0:
                        if (comPortY8910 == null)
                        {
                            comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.Y8910_Port);

                            if (comPortY8910 != null)
                            {
                                comPortY8910.ChipClockHz["Y8910"] = 1789773;
                                comPortY8910.ChipClockHz["Y8910_org"] = 1789773;
                                UseChipInformation += "PSG@1.789773MHz ";
                                CanLoadCoverArt = true;
                                comPortY8910.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                        }
                        break;
                    case 1:
                        if (comPortY8910 == null)
                        {
                            comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.Y8910_Port);

                            if (comPortY8910 != null)
                            {
                                comPortY8910.ChipClockHz["Y8910"] = 1789773;
                                comPortY8910.ChipClockHz["Y8910_org"] = 1789773;
                                UseChipInformation += "PSG@1.789773MHz ";
                                CanLoadCoverArt = true;
                                comPortY8910.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                            if (comPortTurboRProxy == null && comPortY8910 != null)
                                comPortTurboRProxy = comPortY8910;
                        }
                        break;
                    case 2:
                        if (comPortY8910 == null)
                        {
                            comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.Y8910_Port);

                            if (comPortY8910 != null)
                            {
                                comPortY8910.ChipClockHz["Y8910"] = 1789773;
                                comPortY8910.ChipClockHz["Y8910_org"] = 1789773;
                                UseChipInformation += "PSG@1.789773MHz ";
                                CanLoadCoverArt = true;
                                comPortY8910.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                        }
                        break;
                    case 3:
                        if (comPortY8910 == null)
                        {
                            comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.Y8910_Port);

                            if (comPortY8910 != null)
                            {
                                comPortY8910.ChipClockHz["Y8910"] = 1789773;
                                comPortY8910.ChipClockHz["Y8910_org"] = 1789773;
                                UseChipInformation += "PSG@1.789773MHz ";
                                CanLoadCoverArt = true;
                                comPortY8910.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                            if (comPortTurboRProxy == null && comPortY8910 != null)
                                comPortTurboRProxy = comPortY8910;
                        }
                        break;
                }
                if (comPortY8910 != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }


        private bool connectToOPLL()
        {
            if (comPortOPLL == null)
            {
                switch (Program.Default.OPLL_IF)
                {
                    case 0:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPLL.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPLL");
                            }
                        }
                        break;
                    case 2:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS_FTDI,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                            }
                        }
                        break;
                    case 3:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPLL.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPLL");
                            }
                            if (comPortTurboRProxy == null && comPortOPLL != null)
                                comPortTurboRProxy = comPortOPLL;
                        }
                        break;
                    case 4:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPLL.BitBangWait = typeof(Settings).GetProperty("BitBangWaitDCSG");
                            }
                        }
                        break;
                    case 5:
                        if (comPortOPLL == null)
                        {
                            comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPLL_Port);
                            if (comPortOPLL != null)
                            {
                                comPortOPLL.ChipClockHz["OPLL"] = 3579545;
                                comPortOPLL.ChipClockHz["OPLL_org"] = 3579545;
                                UseChipInformation += "OPLL@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPLL.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPLL");
                            }
                        }
                        break;
                }
                if (comPortOPLL != null)
                {
                    Accepted = true;

                    //To avoid pop noise
                    for (int i = 0x10; i <= 0x28; i++)
                        deferredWriteOPLL(i, 0);
                    //for (int i = 0x30; i <= 0x38; i++)
                    //    deferredWriteOPLL(i, 0xff);

                    return true;
                }
            }
            return false;
        }

        private bool coonectToDCSG()
        {
            if (comPortDCSG == null)
            {
                switch (Program.Default.DCSG_IF)
                {
                    case 0:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 1:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 2:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 3:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_Low,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 4:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                               (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortDCSG.BitBangWait = typeof(Settings).GetProperty("BitBangWaitDCSG");
                            }
                        }
                        break;
                    case 5:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS_FTDI,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 6:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                               (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortDCSG.BitBangWait = typeof(Settings).GetProperty("BitBangWaitDCSG");
                            }
                            if (comPortTurboRProxy == null && comPortDCSG != null)
                                comPortTurboRProxy = comPortDCSG;
                        }
                        break;
                    case 7:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NanoDrive,
                                (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                            }
                        }
                        break;
                    case 8:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                               (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortDCSG.BitBangWait = typeof(Settings).GetProperty("BitBangWaitDCSG");
                            }
                        }
                        break;
                    case 9:
                        if (comPortDCSG == null)
                        {
                            comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                               (PortId)Program.Default.DCSG_Port);
                            if (comPortDCSG != null)
                            {
                                comPortDCSG.ChipClockHz["DCSG"] = 3579545;
                                comPortDCSG.ChipClockHz["DCSG_org"] = 3579545;
                                UseChipInformation += "DCSG@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortDCSG.BitBangWait = typeof(Settings).GetProperty("BitBangWaitDCSG");
                            }
                            if (comPortTurboRProxy == null && comPortDCSG != null)
                                comPortTurboRProxy = comPortDCSG;
                        }
                        break;
                }
                if (comPortDCSG != null)
                {
                    Accepted = true;

                    return true;
                }
            }
            return false;
        }

        private bool connectToSCC()
        {
            if (comPortSCC == null)
            {
                switch (Program.Default.SCC_IF)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        if (comPortSCC == null)
                        {
                            switch (Program.Default.SCC_IF)
                            {
                                case 0:
                                    comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI, (PortId)Program.Default.SCC_Port);
                                    break;
                                case 1:
                                    comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI, (PortId)Program.Default.SCC_Port);
                                    break;
                                case 2:
                                    comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi, (PortId)Program.Default.SCC_Port);
                                    break;
                                case 3:
                                    comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR, (PortId)Program.Default.SCC_Port);
                                    break;
                            }
                            if (comPortTurboRProxy == null && comPortSCC != null)
                                comPortTurboRProxy = comPortSCC;

                            if (comPortSCC != null)
                            {
                                Accepted = true;
                                CanLoadCoverArt = true;

                                comPortSCC.ChipClockHz["SCC"] = 3.579545 * 1000 * 1000;
                                comPortSCC.ChipClockHz["SCC_org"] = 3.579545 * 1000 * 1000;
                                UseChipInformation += "SCC@3.579545MHz ";
                                comPortSCC.BitBangWait = typeof(Settings).GetProperty("BitBangWaitSCC");

                                switch (comPortSCC.SoundModuleType)
                                {
                                    case VsifSoundModuleType.MSX_FTDI:
                                    case VsifSoundModuleType.TurboR_FTDI:
                                    case VsifSoundModuleType.MSX_Pi:
                                    case VsifSoundModuleType.MSX_PiTR:
                                        SCCType type = (SCCType)comPortSCC.Tag["SCC.Type"];
                                        var slot = (int)comPortSCC.Tag["SCC.Slot"];
                                        if ((int)slot < 0)
                                            //自動選択方式
                                            comPortSCC.DeferredWriteData(3, (byte)type,
                                                (byte)(-((int)slot + 1)), (int)Program.Default.BitBangWaitSCC);
                                        else
                                            //従来方式
                                            comPortSCC.DeferredWriteData(3, (byte)(type + 4),
                                                (byte)slot, (int)Program.Default.BitBangWaitSCC);

                                        for (int i = 0; i < 0xFF; i++)
                                        {
                                            switch (type)
                                            {
                                                case SCCType.SCC1:
                                                    comPortSCC.DeferredWriteData(4, (byte)i, (byte)0, (int)Program.Default.BitBangWaitSCC);
                                                    if (comPortSCC != null)
                                                    {
                                                        comPortSCC.ChipClockHz["SCC"] = 3579545;
                                                        comPortSCC.ChipClockHz["SCC_org"] = 3579545;
                                                    }
                                                    break;
                                                case SCCType.SCC1_Compat:
                                                case SCCType.SCC:
                                                    comPortSCC.DeferredWriteData(5, (byte)i, (byte)0, (int)Program.Default.BitBangWaitSCC);
                                                    if (comPortSCC != null)
                                                    {
                                                        comPortSCC.ChipClockHz["SCC"] = 3579545;
                                                        comPortSCC.ChipClockHz["SCC_org"] = 3579545;
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                }

                                return true;
                            }
                        }
                        break;
                }
            }
            return false;
        }

        private bool connectToMsxAudio()
        {
            if (comPortY8950 == null)
            {
                switch (Program.Default.Y8950_IF)
                {
                    case 0:
                        if (comPortY8950 == null)
                        {
                            comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.Y8950_Port);
                            if (comPortY8950 != null)
                            {
                                comPortY8950.ChipClockHz["Y8950"] = 3579545;
                                comPortY8950.ChipClockHz["Y8950_org"] = 3579545;
                                UseChipInformation += "Y8950@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortY8950.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                        }
                        break;
                    case 1:
                        if (comPortY8950 == null)
                        {
                            comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.Y8950_Port);
                            if (comPortY8950 != null)
                            {
                                comPortY8950.ChipClockHz["Y8950"] = 3579545;
                                comPortY8950.ChipClockHz["Y8950_org"] = 3579545;
                                UseChipInformation += "Y8950@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortY8950.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                            if (comPortTurboRProxy == null && comPortY8950 != null)
                                comPortTurboRProxy = comPortY8950;
                        }
                        break;
                    case 2:
                        if (comPortY8950 == null)
                        {
                            comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.Y8950_Port);
                            if (comPortY8950 != null)
                            {
                                comPortY8950.ChipClockHz["Y8950"] = 3579545;
                                comPortY8950.ChipClockHz["Y8950_org"] = 3579545;
                                UseChipInformation += "Y8950@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortY8950.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                        }
                        break;
                    case 3:
                        if (comPortY8950 == null)
                        {
                            comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.Y8950_Port);
                            if (comPortY8950 != null)
                            {
                                comPortY8950.ChipClockHz["Y8950"] = 3579545;
                                comPortY8950.ChipClockHz["Y8950_org"] = 3579545;
                                UseChipInformation += "Y8950@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortY8950.BitBangWait = typeof(Settings).GetProperty("BitBangWaitY8950");
                            }
                            if (comPortTurboRProxy == null && comPortY8950 != null)
                                comPortTurboRProxy = comPortY8950;
                        }
                        break;
                }
                if (comPortY8950 != null)
                {
                    Accepted = true;

                    for (int i = 0x20; i <= 0x3F; i++)
                        deferredWriteY8950(i, 0);
                    //for (int i = 0x40; i <= 0x5F; i++)
                    //    deferredWriteY8950(i, 0xff);
                    for (int i = 0x60; i <= 0xC8; i++)
                        deferredWriteY8950(i, 0);

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPL3()
        {
            if (comPortOPL3 == null)
            {
                switch (Program.Default.OPL3_IF)
                {
                    case 0:
                        if (comPortOPL3 == null)
                            comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.OPL3_Port);
                        if (comPortOPL3 != null)
                        {
                            comPortOPL3.ChipClockHz["OPL3"] = 14318180;
                            comPortOPL3.ChipClockHz["OPL3_org"] = 14318180;
                            UseChipInformation += "OPL3@14.318180MHz ";
                            CanLoadCoverArt = true;
                            comPortOPL3.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPL3");
                        }
                        break;
                    case 1:
                        if (comPortOPL3 == null)
                            comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.OPL3_Port);
                        if (comPortOPL3 != null)
                        {
                            comPortOPL3.ChipClockHz["OPL3"] = 14318180;
                            comPortOPL3.ChipClockHz["OPL3_org"] = 14318180;
                            UseChipInformation += "OPL3@14.318180MHz ";
                            CanLoadCoverArt = true;
                            comPortOPL3.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPL3");
                        }
                        if (comPortTurboRProxy == null && comPortOPL3 != null)
                            comPortTurboRProxy = comPortOPL3;
                        break;
                    case 2:
                        if (comPortOPL3 == null)
                            comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPL3_Port);
                        if (comPortOPL3 != null)
                        {
                            comPortOPL3.ChipClockHz["OPL3"] = 14318180;
                            comPortOPL3.ChipClockHz["OPL3_org"] = 14318180;
                            UseChipInformation += "OPL3@14.318180MHz ";
                            CanLoadCoverArt = true;
                            comPortOPL3.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPL3");
                        }
                        break;
                    case 3:
                        if (comPortOPL3 == null)
                            comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPL3_Port);
                        if (comPortOPL3 != null)
                        {
                            comPortOPL3.ChipClockHz["OPL3"] = 14318180;
                            comPortOPL3.ChipClockHz["OPL3_org"] = 14318180;
                            UseChipInformation += "OPL3@14.318180MHz ";
                            CanLoadCoverArt = true;
                            comPortOPL3.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPL3");
                        }
                        if (comPortTurboRProxy == null && comPortOPL3 != null)
                            comPortTurboRProxy = comPortOPL3;
                        break;
                }
                if (comPortOPL3 != null)
                {
                    Accepted = true;

                    //WSE Disable
                    deferredWriteOPL3_P0(0x0, 0);
                    //Force 2op mode
                    deferredWriteOPL3_P1(0x4, 0);
                    //Force OPL mode
                    deferredWriteOPL3_P1(0x5, 0);

                    for (int i = 0x20; i <= 0x3F; i++)
                        deferredWriteOPL3_P0(i, 0);
                    //for (int i = 0x40; i <= 0x5F; i++)
                    //    deferredWriteOPL3_P0(i, 0xFF);
                    for (int i = 0x60; i <= 0xF5; i++)
                        deferredWriteOPL3_P0(i, 0);

                    for (int i = 0x20; i <= 0x3F; i++)
                        deferredWriteOPL3_P1(i, 0);
                    //for (int i = 0x40; i <= 0x5F; i++)
                    //    deferredWriteOPL3_P1(i, 0xFF);
                    for (int i = 0x60; i <= 0xF5; i++)
                        deferredWriteOPL3_P1(i, 0);

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPM(uint clock)
        {
            if (comPortOPM == null)
            {
                switch (Program.Default.OPM_IF)
                {
                    case 0:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPM.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPM");
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SpfmLight,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                            }
                        }
                        break;
                    case 2:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Spfm,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                            }
                        }
                        break;
                    case 3:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Gimic);
                            if (comPortOPM != null)
                            {
                                var gimmic = (PortWriterGimic)comPortOPM.DataWriter;
                                if (gimmic.OpmIndex >= 0)
                                {
                                    GimicManager.Reset(gimmic.OpmIndex);
                                    clock = GimicManager.SetClock(gimmic.OpmIndex, clock);
                                    comPortOPM.ChipClockHz["OPM"] = clock;
                                    comPortOPM.ChipClockHz["OPM_org"] = clock;
                                    UseChipInformation += $"OPM@{(double)clock / (double)1000000}MHz ";
                                }
                                else
                                {
                                    comPortOPM?.Dispose();
                                    comPortOPM = null;
                                }
                            }
                        }
                        break;
                    case 4:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPM.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPM");
                            }
                            if (comPortTurboRProxy == null && comPortOPM != null)
                                comPortTurboRProxy = comPortOPM;
                        }
                        break;
                    case 5:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPM.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPM");
                            }
                        }
                        break;
                    case 6:
                        if (comPortOPM == null)
                        {
                            comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                               (PortId)Program.Default.OPM_Port);
                            if (comPortOPM != null)
                            {
                                comPortOPM.ChipClockHz["OPM"] = 3579545;
                                comPortOPM.ChipClockHz["OPM_org"] = 3579545;
                                UseChipInformation += "OPM@3.579545MHz ";
                                CanLoadCoverArt = true;
                                comPortOPM.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPM");
                            }
                            if (comPortTurboRProxy == null && comPortOPM != null)
                                comPortTurboRProxy = comPortOPM;
                        }
                        break;
                }
                if (comPortOPM != null)
                {
                    Accepted = true;

                    for (int i = 0x00; i <= 0x1F; i++)
                        deferredWriteOPM(0x8, 0x00);
                    deferredWriteOPM(0xF, 0x00);
                    deferredWriteOPM(0x18, 0x00);
                    deferredWriteOPM(0x19, 0x00);
                    comPortOPM.FlushDeferredWriteDataAndWait();

                    for (int i = 0x20; i <= 0x3F; i++)
                        deferredWriteOPM(i, 0x00);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0x40; i <= 0x5F; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0x60; i <= 0x7F; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0x80; i <= 0x9F; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0xA0; i <= 0xBF; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0xC0; i <= 0xDF; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();
                    for (int i = 0xE0; i <= 0xFF; i++)
                        deferredWriteOPM(i, 0);
                    comPortOPM.FlushDeferredWriteDataAndWait();

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPNB(uint opnb_clock, bool opnb)
        {
            if (comPortOPNB == null)
            {
                switch (Program.Default.OPNB_IF)
                {
                    case 0:
                        if (comPortOPNB == null)
                        {
                            comPortOPNB = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPNB_Port);
                            if (comPortOPNB != null)
                            {
                                comPortOPNB.ChipClockHz["OPNB"] = 8000000;
                                comPortOPNB.ChipClockHz["OPNB_SSG"] = 8000000;
                                comPortOPNB.ChipClockHz["OPNB_org"] = 8000000;
                                UseChipInformation += "OPNB@8.000000MHz ";
                                CanLoadCoverArt = true;
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPNB == null)
                        {
                            comPortOPNB = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPNB_Port);
                            if (comPortOPNB != null)
                            {
                                comPortOPNB.ChipClockHz["OPNB"] = 8000000;
                                comPortOPNB.ChipClockHz["OPNB_SSG"] = 8000000;
                                comPortOPNB.ChipClockHz["OPNB_org"] = 8000000;
                                UseChipInformation += "OPNB@8.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPNB.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");  //dummy
                            }
                            if (comPortTurboRProxy == null && comPortOPNB != null)
                            {
                                comPortTurboRProxy = comPortOPNB;
                            }
                        }
                        break;
                }
                if (comPortOPNB != null)
                {
                    Accepted = true;

                    //LFO
                    deferredWriteOPNB_P0(comPortOPNB, 0x22, 0x00);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPNB_P0(comPortOPNB, i, 0);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPNB_P0(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPNB_P0(comPortOPNB, i, 0);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPNB_P0(comPortOPNB, i, 0xC0);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPNB_P1(comPortOPNB, i, 0);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPNB_P1(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPNB_P1(comPortOPNB, i, 0);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPNB_P1(comPortOPNB, i, 0xC0);

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPNA(uint opnb_clock, bool opnb)
        {
            if (comPortOPNA == null)
            {
                switch (Program.Default.OPNA_IF)
                {
                    case 0:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 8000000;
                                UseChipInformation += "OPNA@8.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPNA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SpfmLight,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 7987200;
                                UseChipInformation += "OPNA@7.987200MHz ";
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 2:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Spfm,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 7987200;
                                UseChipInformation += "OPNA@7.987200MHz ";
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 3:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Gimic);
                            if (comPortOPNA != null)
                            {
                                var gimmic = (PortWriterGimic)comPortOPNA.DataWriter;
                                if (gimmic.OpnaIndex >= 0)
                                {
                                    GimicManager.Reset(gimmic.OpnaIndex);
                                    opnb_clock = GimicManager.SetClock(gimmic.OpnaIndex, opnb_clock);
                                    comPortOPNA.ChipClockHz["OPNA"] = opnb_clock;
                                    comPortOPNA.ChipClockHz["OPNA_SSG"] = opnb_clock;
                                    comPortOPNA.ChipClockHz["OPNA_org"] = opnb_clock;
                                    UseChipInformation += $"OPNA@{(double)opnb_clock / (double)1000000}MHz ";
                                }
                                else
                                {
                                    //HACK:
                                    if (gimmic.Opn3lIndex >= 0)
                                    {
                                        GimicManager.Reset(gimmic.Opn3lIndex);
                                        opnb_clock = GimicManager.SetClock(gimmic.Opn3lIndex, opnb_clock * 2);
                                        comPortOPNA.ChipClockHz["OPNA"] = opnb_clock;
                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = opnb_clock;
                                        comPortOPNA.ChipClockHz["OPNA_org"] = opnb_clock;
                                        comPortOPNA.Tag["OPN3L"] = true;
                                        UseChipInformation += $"OPN3L@{(double)opnb_clock / (double)1000000}MHz ";
                                    }
                                    else
                                    {
                                        comPortOPNA?.Dispose();
                                        comPortOPNA = null;
                                    }
                                }
                            }
                            //if (opnb && comPortOPNA != null)
                            //{
                            //    opnbPcm = new OpnbPcm(this);
                            //    opnbPcm.device_start_opnb(0, (int)clock, comPortOPNA);
                            //    comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            //}
                        }
                        break;
                    case 4:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.PC88_FTDI,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 7987200;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 7987200;
                                UseChipInformation += "OPNA@7.987200MHz ";
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 5:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 8000000;
                                UseChipInformation += "OPNA@8.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPNA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                                UseChipInformation += $"w/ tR DAC";
                            }
                            else if (comPortTurboRProxy == null && comPortOPNA != null)
                            {
                                comPortTurboRProxy = comPortOPNA;
                            }
                        }
                        break;
                    case 6:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 8000000;
                                UseChipInformation += "OPNA@8.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPNA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 7:
                        if (comPortOPNA == null)
                        {
                            comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPNA_Port);
                            if (comPortOPNA != null)
                            {
                                comPortOPNA.ChipClockHz["OPNA"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_SSG"] = 8000000;
                                comPortOPNA.ChipClockHz["OPNA_org"] = 8000000;
                                UseChipInformation += "OPNA@8.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPNA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");
                            }
                            if (opnb && comPortOPNA != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPNA);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPNA);
                                comPortOPNA.Tag["ProxyOPNB_ADPCM"] = true;
                                UseChipInformation += $"w/ tR DAC";
                            }
                            else if (comPortTurboRProxy == null && comPortOPNA != null)
                            {
                                comPortTurboRProxy = comPortOPNA;
                            }
                        }
                        break;
                }
                if (comPortOPNA != null)
                {
                    Accepted = true;

                    //LFO
                    deferredWriteOPNA_P0(comPortOPNA, 0x22, 0x00);
                    //channel 3 mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x27, 0x00);
                    //Force OPN mode
                    deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x00);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPNA_P0(comPortOPNA, i, 0);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPNA_P0(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPNA_P0(comPortOPNA, i, 0);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPNA_P0(comPortOPNA, i, 0xC0);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPNA_P1(comPortOPNA, i, 0);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPNA_P1(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPNA_P1(comPortOPNA, i, 0);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPNA_P1(comPortOPNA, i, 0xC0);

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPN()
        {
            if (comPortOPN == null)
            {
                switch (Program.Default.OPN_IF)
                {
                    case 0:
                        if (comPortOPN == null)
                        {
                            comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.OPN_Port);
                            if (comPortOPN != null)
                            {
                                comPortOPN.ChipClockHz["OPN"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_SSG"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_org"] = 4000000;
                                UseChipInformation += "OPN@4.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN");
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPN == null)
                        {
                            comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.PC88_FTDI,
                                (PortId)Program.Default.OPN_Port);
                            if (comPortOPN != null)
                            {
                                comPortOPN.ChipClockHz["OPN"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_SSG"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_org"] = 4000000;
                                UseChipInformation += "OPN@4.000000MHz ";
                            }
                        }
                        break;
                    case 2:
                        if (comPortOPN == null)
                        {
                            comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.OPN_Port);
                            if (comPortOPN != null)
                            {
                                comPortOPN.ChipClockHz["OPN"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_SSG"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_org"] = 4000000;
                                UseChipInformation += "OPN@4.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN");
                            }
                            if (comPortTurboRProxy == null && comPortOPN != null)
                                comPortTurboRProxy = comPortOPN;
                        }
                        break;
                    case 3:
                        if (comPortOPN == null)
                        {
                            comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPN_Port);
                            if (comPortOPN != null)
                            {
                                comPortOPN.ChipClockHz["OPN"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_SSG"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_org"] = 4000000;
                                UseChipInformation += "OPN@4.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN");
                            }
                        }
                        break;
                    case 4:
                        if (comPortOPN == null)
                        {
                            comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPN_Port);
                            if (comPortOPN != null)
                            {
                                comPortOPN.ChipClockHz["OPN"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_SSG"] = 4000000;
                                comPortOPN.ChipClockHz["OPN_org"] = 4000000;
                                UseChipInformation += "OPN@4.000000MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN");
                            }
                            if (comPortTurboRProxy == null && comPortOPN != null)
                                comPortTurboRProxy = comPortOPN;
                        }
                        break;
                }
                if (comPortOPN != null)
                {
                    Accepted = true;

                    //LFO
                    deferredWriteOPN_P0(0x22, 0x00);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPN_P0(i, 0);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPNA_P0(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPN_P0(i, 0);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPN_P0(i, 0xC0);

                    return true;
                }
            }
            return false;
        }

        private bool connectToOPN2(uint opnb_clock, bool opnb)
        {
            if (comPortOPN2 == null)
            {
                switch (Program.Default.OPN2_IF)
                {
                    case 0:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                            }
                        }
                        break;
                    case 1:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                            }
                            if (opnb && comPortOPN2 != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPN2);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPN2);
                                comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 2:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_Low,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                            }
                        }
                        break;
                    case 3:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                                comPortOPN2.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN2");
                            }
                            if (opnb && comPortOPN2 != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPN2);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPN2);
                                comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 4:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN2.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN2");
                            }
                            if (opnb && comPortOPN2 != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPN2);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPN2);
                                comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                                UseChipInformation += $"w/ tR DAC";
                            }
                            else if (comPortTurboRProxy == null && comPortOPN2 != null)
                            {
                                comPortTurboRProxy = comPortOPN2;
                            }
                        }
                        break;
                    case 5:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NanoDrive,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                            }
                        }
                        break;
                    case 6:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN2.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN2");
                            }
                            if (opnb && comPortOPN2 != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPN2);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPN2);
                                comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                            }
                        }
                        break;
                    case 7:
                        if (comPortOPN2 == null)
                        {
                            comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                                (PortId)Program.Default.OPN2_Port);
                            if (comPortOPN2 != null)
                            {
                                comPortOPN2.ChipClockHz["OPN2"] = 7670453;
                                comPortOPN2.ChipClockHz["OPN2_org"] = 7670453;
                                UseChipInformation += "OPN2@7.670453MHz ";
                                CanLoadCoverArt = true;
                                comPortOPN2.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN2");
                            }
                            if (opnb && comPortOPN2 != null)
                            {
                                opnbPcm = new OpnbPcm(this, ProxyOPNType.OPN2);
                                opnbPcm.device_start_opnb(0, (int)opnb_clock, comPortOPN2);
                                comPortOPN2.Tag["ProxyOPNB_ADPCM"] = true;
                                UseChipInformation += $"w/ tR DAC";
                            }
                            else if (comPortTurboRProxy == null && comPortOPN2 != null)
                            {
                                comPortTurboRProxy = comPortOPN2;
                            }
                        }
                        break;
                }
                if (comPortOPN2 != null)
                {
                    Accepted = true;

                    //LFO
                    deferredWriteOPN2_P0(comPortOPN2, 0x22, 0x00);
                    //channel 3 mode
                    deferredWriteOPN2_P0(comPortOPN2, 0x27, 0x00);
                    //DAC OFF
                    deferredWriteOPN2_P0(comPortOPN2, 0x2B, 0x00);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPN2_P0(comPortOPN2, i, 0x00);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPN2_P0(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPN2_P0(comPortOPN2, i, 0x00);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPN2_P0(comPortOPN2, i, 0xC0);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPN2_P1(comPortOPN2, i, 0x00);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPN2_P1(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPN2_P1(comPortOPN2, i, 0x00);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPN2_P1(comPortOPN2, i, 0xC0);

                    return true;
                }
            }
            return false;
        }

        bool checkIfZip(string filepath, int signatureSize, string expectedSignature)
        {
            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length < signatureSize)
                    return false;
                byte[] signature = new byte[signatureSize];
                int bytesRequired = signatureSize;
                int index = 0;
                while (bytesRequired > 0)
                {
                    int bytesRead = fs.Read(signature, index, bytesRequired);
                    bytesRequired -= bytesRead;
                    index += bytesRead;
                }
                string actualSignature = BitConverter.ToString(signature);
                if (actualSignature == expectedSignature)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected virtual void OpenFile(string fileName)
        {
            bool zipped = checkIfZip(fileName, 3, "1F-8B-08");

            //Read size
            using (Stream vgmFile = File.Open(fileName, FileMode.Open))
            {
                ReadVgmFile(zipped, vgmFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zipped"></param>
        /// <param name="vgmFile"></param>
        /// <returns></returns>
        protected void ReadVgmFile(bool zipped, Stream vgmFile)
        {
            uint fileSize = 0;
            int offset = 0;

            if (zipped)
            {
                vgmFile.Position = vgmFile.Length - 4;
                byte[] b = new byte[4];
                vgmFile.Read(b, 0, 4);
                fileSize = BitConverter.ToUInt32(b, 0);
                vgmFile.Position = 0;

                GZipStream stream = new GZipStream(vgmFile, CompressionMode.Decompress);
                vgmReader = new BinaryReader(stream);
                zipped = true;
            }
            else
            {
                fileSize = (uint)vgmFile.Length;
                vgmReader = new BinaryReader(vgmFile);
            }

            uint fccHeader;
            fccHeader = (uint)vgmReader.ReadUInt32();
            if (fccHeader != FCC_VGM)
            {
                throw new IOException("VGM file error");
            }

            vgmDataLen = fileSize;
            vgmHead = readVGMHeader(vgmReader);


            //Figure out header offset
            offset = (int)vgmHead.cur.lngDataOffset + 0x34;
            if (offset == 0 || offset == 0x0000000C || vgmHead.cur.lngVersion < 0x150)
                offset = 0x40;
            vgmDataOffset = offset;
            vgmDataCurrentOffset = vgmDataOffset;
            //}
            //using (FileStream vgmFile = File.Open(fileName, FileMode.Open))
            //{
            vgmFile.Seek(0, SeekOrigin.Begin);
            if (zipped)
            {
                GZipStream stream = new GZipStream(vgmFile, CompressionMode.Decompress);
                vgmReader = new BinaryReader(stream);
            }
            else
            {
                vgmReader = new BinaryReader(vgmFile);
                vgmReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

#if DEBUG && OUT_VGM
            vgmLogFile = File.Create(Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "vgmplog.vgm"));
            List<byte> logData = new List<byte>();
            logData.AddRange(vgmReader.ReadBytes(offset));
            logData[0xc8] = 0x00;
            logData[0xc9] = 0x12;
            logData[0xca] = 0x7a;
            vgmLogFile?.Write(logData.ToArray(), 0, logData.Count);
#else
            vgmReader.ReadBytes(offset);
#endif
            vgmData = vgmReader.ReadBytes((int)(fileSize - offset));

            ym2608_adpcmbit8 = searchOpnaRamType(vgmData);

            y8950_adpcmbit64k = searchY8950RamType(vgmData);

            vgmReader = new BinaryReader(new MemoryStream(vgmData));
        }

#if DEBUG
        private FileStream vgmLogFile;
#endif

        [Conditional("DEBUG")]
        private void writeVgmLogFile(params byte[] data)
        {
#if DEBUG
            foreach (byte dt in data)
            {
                vgmLogFile?.WriteByte(dt);
            }
#endif
        }

        [Conditional("DEBUG")]
        private void writeVgmLogFile(bool close, params byte[] data)
        {
#if DEBUG
            foreach (byte dt in data)
            {
                vgmLogFile?.WriteByte(dt);
            }
            if (close)
            {
                vgmLogFile?.Close();
                vgmLogFile = null;
            }
#endif
        }

        //https://github.com/kuma4649/MDPlayer/blob/stable/LICENSE.txt
        //https://github.com/kuma4649/MDPlayer/blob/stable/MDPlayer/MDPlayer/Driver/vgm.cs

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vgmBuf"></param>
        /// <param name="adr"></param>
        /// <returns></returns>
        private UInt32 getLE32(byte[] vgmBuf, UInt32 adr)
        {
            UInt32 dat;
            dat = (UInt32)vgmBuf[adr] + (UInt32)vgmBuf[adr + 1] * 0x100 + (UInt32)vgmBuf[adr + 2] * 0x10000 + (UInt32)vgmBuf[adr + 3] * 0x1000000;

            return dat;
        }

        /// <summary>
        /// OPNAのRAMTypeをデータから調べる
        /// </summary>
        /// <returns>true:x8bit false:x1bit</returns>
        private bool searchOpnaRamType(byte[] vgmBuf)
        {
            try
            {
                long adr = 0;

                while (adr < vgmBuf.Length && vgmBuf[adr] != 0x66)
                {
                    byte dat = vgmBuf[adr];
                    if (dat <= 0x31) adr++;
                    else if (0x4f <= dat && dat < 0x51) adr += 2;
                    else if (dat < 0x57) adr += 3;
                    else if (dat == 0x57)
                    {
                        byte reg = vgmBuf[adr + 1];
                        byte val = vgmBuf[adr + 2];
                        adr += 3;
                        if (reg == 1)
                        {
                            if ((val & 2) != 0)
                            {
                                return true;
                            }
                        }
                    }
                    else if (dat < 0x62) adr += 3;
                    else if (dat < 0x64) adr++;
                    else if (dat == 0x64) adr += 4;
                    else if (dat == 0x66) adr++;
                    else if (dat == 0x67)
                    {
                        uint bLen = getLE32(vgmBuf, (uint)(adr + 3));
                        bLen &= 0x7fffffff;
                        adr += bLen + 7;
                    }
                    else if (dat == 0x68)
                    {
                        adr += 12;
                    }
                    else if (dat < 0x90) adr++;
                    else if (dat == 0x90) adr += 5;
                    else if (dat == 0x91) adr += 5;
                    else if (dat == 0x92) adr += 6;
                    else if (dat == 0x93) adr += 11;
                    else if (dat == 0x94) adr += 2;
                    else if (dat == 0x95) adr += 5;
                    else if (dat < 0xc0) adr += 3;
                    else if (dat < 0xe0) adr += 4;
                    else if (dat == 0xe0) adr += 5;
                    else if (dat == 0xe1) adr += 4;
                    else adr++;
                }
            }
            catch
            {
                ;
            }

            return false;
        }


        /// <summary>
        /// Y8950のRAMTypeをデータから調べる
        /// </summary>
        /// <returns>true:64kbit false:256kbit</returns>
        private bool searchY8950RamType(byte[] vgmBuf)
        {
            try
            {
                long adr = 0;

                while (adr < vgmBuf.Length && vgmBuf[adr] != 0x66)
                {
                    byte dat = vgmBuf[adr];
                    if (dat <= 0x31) adr++;
                    else if (0x4f <= dat && dat < 0x51) adr += 2;
                    else if (dat < 0x5c) adr += 3;
                    else if (dat == 0x5c)
                    {
                        byte reg = vgmBuf[adr + 1];
                        byte val = vgmBuf[adr + 2];
                        adr += 3;
                        if (reg == 8)
                        {
                            if ((val & 2) != 0)
                            {
                                return true;
                            }
                        }
                    }
                    else if (dat < 0x62) adr += 3;
                    else if (dat < 0x64) adr++;
                    else if (dat == 0x64) adr += 4;
                    else if (dat == 0x66) adr++;
                    else if (dat == 0x67)
                    {
                        uint bLen = getLE32(vgmBuf, (uint)(adr + 3));
                        bLen &= 0x7fffffff;
                        adr += bLen + 7;
                    }
                    else if (dat == 0x68)
                    {
                        adr += 12;
                    }
                    else if (dat < 0x90) adr++;
                    else if (dat == 0x90) adr += 5;
                    else if (dat == 0x91) adr += 5;
                    else if (dat == 0x92) adr += 6;
                    else if (dat == 0x93) adr += 11;
                    else if (dat == 0x94) adr += 2;
                    else if (dat == 0x95) adr += 5;
                    else if (dat < 0xc0) adr += 3;
                    else if (dat < 0xe0) adr += 4;
                    else if (dat == 0xe0) adr += 5;
                    else if (dat == 0xe1) adr += 4;
                    else adr++;
                }
            }
            catch
            {
                ;
            }

            return false;
        }

        private int readByte()
        {
            vgmDataCurrentOffset++;

            if (vgmReader.BaseStream == null)
                return -1;
            if (vgmReader.BaseStream.Position == vgmReader.BaseStream.Length)
                return -1;

            byte data = vgmReader.ReadByte();
            return data;
        }

        private bool ym2608_adpcmbit8;
        private int ym2608_pcm_start;
        private int ym2608_pcm_stop;
#if DEBUG
        private string y8950_adpcmbit64kString;
#endif
        private bool y8950_adpcmbit64k;
        private bool y8950_adpcm_ram;
        private int y8950_pcm_start;
        private int y8950_pcm_stop;

        private int lastOPN2DacEn;

        protected override void StreamSong()
        {
            vgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
            double wait = 0;
            double vgmWaitDelta = 0;
            double streamWaitDelta = 0;
            bool looped = false;
            //double lastDiff = 0;
            {
                //bool firstKeyon = false;    //TODO: true

                bool oki6285Adpcm2ndNibble = false;
                int streamChipType = 0;
                OKIM6258 okim6258 = null;

                if (segaPcm != null)
                {
                    Thread t = new Thread(new ThreadStart(segaPcm.StreamSong));
                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                }
                if (k053260 != null)
                {
                    Thread t = new Thread(new ThreadStart(k053260.StreamSong));
                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                }
                if (okim6295 != null)
                {
                    Thread t = new Thread(new ThreadStart(okim6295.StreamSong));
                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                }
                if (opnbPcm != null)
                {
                    Thread t = new Thread(new ThreadStart(opnbPcm.StreamSong));
                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                }

                long freq, before, after;
                double dbefore;
                QueryPerformanceFrequency(out freq);
                QueryPerformanceCounter(out before);
                dbefore = before;
                while (true)
                {

                    if (RequestedStat == SoundState.Stopped)
                    {
                        break;
                    }
                    else if (RequestedStat == SoundState.Paused)
                    {
                        if (State != SoundState.Paused)
                        {
                            State = SoundState.Paused;
                            StopAllSounds(false);
                        }
                        Thread.Sleep(1);
                        QueryPerformanceCounter(out before);
                        dbefore = before;
                        continue;
                    }
                    else if (RequestedStat == SoundState.Freezed)
                    {
                        if (State != SoundState.Freezed)
                            State = SoundState.Freezed;
                        Thread.Sleep(1);
                        QueryPerformanceCounter(out before);
                        dbefore = before;
                        continue;
                    }
                    State = SoundState.Playing;
                    try
                    {
                        if (vgmWaitDelta <= 0)
                        {
                            int command = readByte();
                            if (command != -1)
                            {
                                switch (command)
                                {
                                    case -1:
                                        vgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                        looped = true;
                                        break;

                                    case 0x30:
                                        {
                                            uint dclk = vgmHead.cur.lngHzDCSG;

                                            var data = readByte();
                                            if (data < 0)
                                                break;

                                            if (comPortDCSG != null)
                                            {
                                                switch (comPortDCSG.SoundModuleType)
                                                {
                                                    case VsifSoundModuleType.NanoDrive:
                                                        deferredWriteDCSG(data, dclk, true);
                                                        break;
                                                }
                                            }
                                        }
                                        break;

                                    case 0x31:  //AY8910 stereo mask
                                        {
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                        }
                                        break;

                                    case int cmd when 0x32 <= cmd && cmd <= 0x3F:
                                        {
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                        }
                                        break;

                                    case int cmd when 0x40 <= cmd && cmd <= 0x4E:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;

                                            if (vgmHead.cur.lngVersion < 0x00000160)
                                                break;

                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0x4F:
                                        {
                                            var data = readByte();
                                            if (data < 0)
                                                break;
                                        }
                                        break;

                                    case 0x50:  //DCSG
                                        {
                                            uint dclk = vgmHead.cur.lngHzDCSG;

                                            var data = readByte();
                                            if (data < 0)
                                                break;

                                            if (comPortDCSG != null)
                                                deferredWriteDCSG(data, dclk);
                                        }
                                        break;

                                    case 0x51: //YM2413
                                        {
                                            bool vrc7 = false;
                                            if ((vgmHead.cur.lngHzYM2413 & 0x80000000) != 0)
                                                vrc7 = true;
                                            uint dclk = (vgmHead.cur.lngHzYM2413 & ~0x80000000);

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (!vrc7)
                                            {
                                                if (comPortOPLL != null)
                                                    deferredWriteOPLL(dclk, adrs, dt);
                                            }
                                            else
                                            {
                                                if (comPortNES != null)
                                                {
                                                    DeferredWriteNES(36 + 1, adrs);
                                                    DeferredWriteNES(36 + 3, dt);
                                                }
                                            }
                                        }
                                        break;

                                    case 0x52: //YM2612 Write Port 0
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2612;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            //ignore test and unknown registers
                                            if (adrs < 0x2a && adrs != 0x22 && adrs != 0x27 && adrs != 0x28)
                                                break;
                                            if (adrs > 0xb6)
                                                break;

                                            if (comPortOPN2 != null)
                                            {
                                                switch (adrs)
                                                {
                                                    case 0x2a:
                                                        //output DAC
                                                        dt = (int)Math.Round((double)dt * (double)PcmMixer.DacVolume / 100d);
                                                        DeferredWriteOPN2_DAC(comPortOPN2, dt);
                                                        break;
                                                    case 0x2b:
                                                        //Enable DAC
                                                        if (lastOPN2DacEn != (dt & 0x80))
                                                            deferredWriteOPN2_P0(comPortOPN2, adrs, dt, dclk);
                                                        lastOPN2DacEn = (dt & 0x80);
                                                        break;
                                                    default:
                                                        deferredWriteOPN2_P0(comPortOPN2, adrs, dt, dclk);
                                                        break;
                                                }
                                            }
                                            else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPN2"))
                                            {
                                                switch (adrs)
                                                {
                                                    case 0x2a:
                                                        //output DAC
                                                        dt = (int)Math.Round((double)dt * (double)PcmMixer.DacVolume / 100d);
                                                        DeferredWriteOPNA_PseudoDAC(comPortOPNA, dt);
                                                        break;
                                                    case 0x2b:
                                                        //Enable DAC
                                                        break;
                                                    default:
                                                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt, dclk);
                                                        break;
                                                }
                                            }
                                        }
                                        break;

                                    case 0x53: //YM2612 Write Port 1
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2612;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            //ignore test and unknown registers
                                            if (adrs < 0x30 || adrs > 0xb6)
                                                break;

                                            if (comPortOPN2 != null)
                                            {
                                                deferredWriteOPN2_P1(comPortOPN2, adrs, dt, dclk);
                                            }
                                            else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPN2"))
                                            {
                                                deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                                if (adrs == 0xb6)
                                                {
                                                    deferredWriteOPNA_P1(comPortOPNA, 0x01, (byte)(dt & 0xC0));   //LR
                                                }
                                            }
                                        }
                                        break;

                                    case 0x54: //YM2151
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2151;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPM != null)
                                            {
                                                dt = deferredWriteOPM(adrs, dt, dclk);
                                            }
                                        }
                                        break;

                                    case 0x55: //YM2203
                                        {
                                            if (comPortOPN != null)
                                            {
                                                uint dclk = vgmHead.cur.lngHzYM2203;

                                                var adrs = readByte();
                                                if (adrs < 0)
                                                    break;
                                                var dt = readByte();
                                                if (dt < 0)
                                                    break;

                                                if (comPortOPN != null)
                                                {
                                                    switch (adrs)
                                                    {
                                                        case 0x2d:
                                                            comPortOPN.ChipClockHz["OPN"] = (int)comPortOPN.ChipClockHz["OPN_org"];
                                                            comPortOPN.ChipClockHz["OPN_SSG"] = (int)comPortOPN.ChipClockHz["OPN_org"];
                                                            break;
                                                        case 0x2e:
                                                            comPortOPN.ChipClockHz["OPN"] = (int)comPortOPN.ChipClockHz["OPN_org"] / 2;
                                                            comPortOPN.ChipClockHz["OPN_SSG"] = (int)comPortOPN.ChipClockHz["OPN_org"] / 2;
                                                            break;
                                                        case 0x2f:
                                                            comPortOPN.ChipClockHz["OPN"] = (int)comPortOPN.ChipClockHz["OPN_org"] / 3;
                                                            comPortOPN.ChipClockHz["OPN_SSG"] = (int)comPortOPN.ChipClockHz["OPN_org"] / 4;
                                                            break;
                                                    }
                                                }

                                                if (comPortOPN != null)
                                                {
                                                    deferredWriteOPN_P0(adrs, dt, dclk);
                                                }
                                            }
                                            else
                                            {
                                                if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPN"))
                                                    goto case 0x56;
                                                else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOPN"))
                                                    goto case 0x56;
                                            }
                                            break;
                                        }
                                    case 0x56: //YM2608 Write Port 0
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2608;
                                            if (command == 0x55)
                                                dclk = vgmHead.cur.lngHzYM2203 * 2;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPNA != null)
                                            {
                                                switch (adrs)
                                                {
                                                    case 0x2d:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"];
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"];
                                                        break;
                                                    case 0x2e:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 2;
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 2;
                                                        break;
                                                    case 0x2f:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 3;
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 4;
                                                        break;
                                                }
                                            }

                                            if (command == 0x55)
                                            {
                                                //ignore test and unknown registers
                                                if (adrs == 0xe || adrs == 0xf)
                                                    break;
                                                if (adrs > 0x20 && adrs < 0x30 && adrs != 0x27 && adrs != 0x28)
                                                    break;
                                                if (adrs > 0xb2)
                                                    break;
                                            }

                                            if (comPortOPNA != null)
                                            {
                                                deferredWriteOPNA_P0(comPortOPNA, adrs, dt, dclk);
                                            }
                                            else if (comPortOPN2 != null)
                                            {
                                                if (adrs <= 0xd)
                                                {
                                                    if (comPortY8910 != null && comPortY8910.Tag.ContainsKey("ProxyOPN"))
                                                        deferredWriteY8910(adrs, dt, dclk / 3);
                                                    break;
                                                }

                                                deferredWriteOPN2_P0(comPortOPN2, adrs, dt, dclk);
                                            }
                                        }
                                        break;

                                    case 0x58: //YM2610 Write Port 0
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2610;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            //ignore test and unknown registers
                                            if (adrs == 0xe || adrs == 0xf)
                                                break;
                                            if (0x16 <= adrs && adrs <= 0x18)
                                                break;
                                            if (0x1d <= adrs && adrs <= 0x1f)
                                                break;
                                            if (adrs > 0x20 && adrs < 0x30 && adrs != 0x27 && adrs != 0x28)
                                                break;
                                            if (adrs > 0xb2)
                                                break;

#if DEBUG
                                            if (adrs >= 0x10 && adrs <= 0x1c)
                                                Console.WriteLine("YM2610: ADPCM B write " + (adrs).ToString("x") + "=" + (dt).ToString("x"));
#endif

                                            if (comPortOPNB != null)
                                            {
                                                switch (adrs)
                                                {
                                                    case 0x2d:
                                                        comPortOPNB.ChipClockHz["OPNB"] = (int)comPortOPNB.ChipClockHz["OPNB_org"];
                                                        comPortOPNB.ChipClockHz["OPNB_SSG"] = (int)comPortOPNB.ChipClockHz["OPNB_org"];
                                                        break;
                                                    case 0x2e:
                                                        comPortOPNB.ChipClockHz["OPNB"] = (int)comPortOPNB.ChipClockHz["OPNB_org"] / 2;
                                                        comPortOPNB.ChipClockHz["OPNB_SSG"] = (int)comPortOPNB.ChipClockHz["OPNB_org"] / 2;
                                                        break;
                                                    case 0x2f:
                                                        comPortOPNB.ChipClockHz["OPNB"] = (int)comPortOPNB.ChipClockHz["OPNB_org"] / 3;
                                                        comPortOPNB.ChipClockHz["OPNB_SSG"] = (int)comPortOPNB.ChipClockHz["OPNB_org"] / 4;
                                                        break;
                                                }
                                            }
                                            else if (comPortOPNA != null)
                                            {
                                                switch (adrs)
                                                {
                                                    case 0x2d:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"];
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"];
                                                        break;
                                                    case 0x2e:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 2;
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 2;
                                                        break;
                                                    case 0x2f:
                                                        comPortOPNA.ChipClockHz["OPNA"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 3;
                                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = (int)comPortOPNA.ChipClockHz["OPNA_org"] / 4;
                                                        break;
                                                }
                                            }

                                            if (comPortOPNB != null)
                                            {
                                                deferredWriteOPNB_P0(comPortOPNB, adrs, dt, dclk);
                                            }
                                            else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPNB"))
                                            {
                                                if (!(0x10 <= adrs && adrs <= 0x1f))    //ignore ADPCM adrs
                                                {
                                                    if (!(0x29 <= adrs && adrs <= 0x2f))    //ignore 
                                                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt, dclk);
                                                }
                                                else if (opnbPcm != null)
                                                {
                                                    if (0x10 <= adrs && adrs <= 0x1b)
                                                        opnbPcm.WritRegisterADPCM_B((byte)(adrs - 0x10), (byte)dt);
                                                }
                                            }
                                            else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOPNB"))
                                            {
                                                if (adrs <= 0xd)
                                                {
                                                    if (comPortY8910 != null && comPortY8910.Tag.ContainsKey("ProxyOPN"))
                                                        deferredWriteY8910(adrs, dt, dclk / 3);
                                                    break;
                                                }

                                                if (!(0x10 <= adrs && adrs <= 0x1f))    //ignore ADPCM adrs
                                                {
                                                    if (!(0x29 <= adrs && adrs <= 0x2f))    //ignore
                                                    {
                                                        if (comPortOPN2.SoundModuleType != VsifSoundModuleType.TurboR_FTDI &&
                                                            comPortOPN2.SoundModuleType != VsifSoundModuleType.MSX_PiTR)
                                                            if (adrs >= 0x30)
                                                                adrs--;
                                                        deferredWriteOPN2_P0(comPortOPN2, adrs, dt, dclk);
                                                    }
                                                }
                                                else if (opnbPcm != null)
                                                {
                                                    if (0x10 <= adrs && adrs <= 0x1b)
                                                        opnbPcm.WritRegisterADPCM_B((byte)(adrs - 0x10), (byte)dt);
                                                }
                                            }
                                        }
                                        break;

                                    case 0x57: //YM2608 Write Port 1
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2608;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPNB != null)
                                            {
                                                deferredWriteOPNB_P1(comPortOPNB, adrs, dt, dclk);
                                            }
                                            else
                                            {
                                                if (adrs >= 0 && adrs <= 0x10)
                                                {
                                                    if (adrs == 0x1)
                                                    {
                                                        if ((dt & 1) == 0)
                                                        {
                                                            ym2608_adpcmbit8 = (dt & 2) == 2;

                                                            if (ym2608_adpcmbit8)
                                                            {
                                                                //FormMain.TopForm.SetStatusText("YM2608: Set 8bit ADPCM mode");
#if DEBUG
                                                                Console.WriteLine("YM2608: Set 8bit ADPCM mode");
#endif
                                                            }
                                                            else
                                                            {
                                                                //FormMain.TopForm.SetStatusText("YM2608: Set 1bit ADPCM mode");
#if DEBUG
                                                                Console.WriteLine("YM2608: Set 1bit ADPCM mode");
#endif
                                                            }
                                                        }
                                                    }
                                                    else if (adrs == 0x2)
                                                    {
                                                        ym2608_pcm_start = (ym2608_pcm_start & 0xff00) | dt;
                                                    }
                                                    else if (adrs == 0x3)
                                                    {
                                                        ym2608_pcm_start = (ym2608_pcm_start & 0xff) | (dt << 8);
                                                    }
                                                    else if (adrs == 0x4)
                                                    {
                                                        ym2608_pcm_stop = (ym2608_pcm_stop & 0xff00) | dt;
                                                    }
                                                    else if (adrs == 0x5)
                                                    {
                                                        ym2608_pcm_stop = (ym2608_pcm_stop & 0xff) | (dt << 8);
                                                        if (ym2608_adpcmbit8)
                                                        {
#if DEBUG
                                                            Console.WriteLine("YM2608: Play 8bit ADPCM(" + (ym2608_pcm_start << 5).ToString("x") + " - " + ((ym2608_pcm_stop << 5) | 0x1f).ToString("x") + ")");
#endif
                                                        }
                                                        else
                                                        {
#if DEBUG
                                                            Console.WriteLine("YM2608: Play 1bit ADPCM mode(" + (ym2608_pcm_start << 2).ToString("x") + " - " + ((ym2608_pcm_stop << 2) | 0x3).ToString("x") + ")");
#endif
                                                        }
                                                    }
                                                }
                                                if (adrs != 0x8)    //ignore ADPCM adrs
                                                {
                                                    if (comPortOPNA != null)
                                                    {
                                                        deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                                    }
                                                    else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOPNA"))
                                                    {
                                                        deferredWriteOPN2_P1(comPortOPN2, adrs, dt, dclk);
                                                    }
                                                }
                                                else
                                                {
                                                    if (comPortOPNA != null)
                                                    {
                                                        deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                                        comPortOPNA.FlushDeferredWriteDataAndWait();
                                                        //HACK:
                                                        QueryPerformanceCounter(out before);
                                                        dbefore = before;
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    case 0x59: //YM2610 Write Port 1
                                        {
                                            uint dclk = vgmHead.cur.lngHzYM2610;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (adrs >= 0x30)    //ignore ADPCM adrs
                                            {
                                                if (comPortOPNB != null)
                                                {
                                                    deferredWriteOPNB_P1(comPortOPNB, adrs, dt, dclk);
                                                }
                                                else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPNB"))
                                                {
                                                    deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                                }
                                                else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOPNB"))
                                                {
                                                    if (comPortOPN2.SoundModuleType != VsifSoundModuleType.TurboR_FTDI &&
                                                        comPortOPN2.SoundModuleType != VsifSoundModuleType.MSX_PiTR)
                                                        if (adrs >= 0x30)
                                                            adrs--;
                                                    deferredWriteOPN2_P1(comPortOPN2, adrs, dt, dclk);
                                                }
                                            }
                                            else
                                            {
                                                if (comPortOPNB != null)
                                                {
                                                    if (adrs != 0x02)
                                                        deferredWriteOPNB_P1(comPortOPNB, adrs, dt, dclk);
                                                }
                                                else if (opnbPcm != null)
                                                {
                                                    opnbPcm.WritRegisterADPCM_A((byte)(adrs), (byte)dt);
                                                }
                                            }
                                        }
                                        break;

                                    case 0x5A: //YM3812
                                    case 0x5B: //YM3526
                                        if (comPortOPL3 != null)
                                            goto case 0x5E;
                                        else if (comPortY8950 != null)
                                            goto case 0x5C;
                                        else
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0x5C: //Y8950
                                        {
                                            uint dclk = vgmHead.cur.lngHzY8950;
                                            if (command == 0x5A)
                                                dclk = vgmHead.cur.lngHzYM3812;
                                            else if (command == 0x5B)
                                                dclk = vgmHead.cur.lngHzYM3526;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (adrs == 0x8)
                                            {
                                                y8950_adpcmbit64k = (dt & 2) == 2;
                                                if ((dt & 1) == 1)
                                                {
                                                    //HACK: ROM -> RAM
                                                    if (comPortY8950?.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                                                        comPortY8950?.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                                                        comPortY8950?.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                                                        comPortY8950?.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                                                    {
                                                        dt -= 1;
#if DEBUG
                                                        if (y8950_adpcmbit64k)
                                                            y8950_adpcmbit64kString = "64Kbit Fake RAM mode";
                                                        else
                                                            y8950_adpcmbit64kString = "256Kbit Fake RAM mode";
#endif
                                                    }
                                                    else
                                                    {
#if DEBUG
                                                        if ((dt & 2) == 2)
                                                            y8950_adpcmbit64kString = "64Kbit ROM mode";
                                                        else
                                                            y8950_adpcmbit64kString = "256Kbit ROM mode";
#endif
                                                    }
                                                    y8950_adpcm_ram = false;
                                                }
                                                else
                                                {
#if DEBUG
                                                    if (y8950_adpcmbit64k)
                                                        y8950_adpcmbit64kString = "64Kbit RAM mode";
                                                    else
                                                        y8950_adpcmbit64kString = "256Kbit RAM mode";
#endif
                                                    y8950_adpcm_ram = true;
                                                }
#if DEBUG
                                                Console.WriteLine("Y8950: Set " + y8950_adpcmbit64kString);
#endif
                                            }

                                            if (adrs >= 0 && adrs <= 0x10)
                                            {
                                                if (adrs == 0x9)
                                                {
                                                    y8950_pcm_start = (y8950_pcm_start & 0xff00) | dt;
                                                    if (!y8950_adpcm_ram)
                                                    {
                                                        if (comPortY8950 != null)
                                                        {
                                                            if (comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                                                            {
                                                                var dt2 = ((y8950_pcm_start << 3) & 0xff00) >> 8;
                                                                writeY8950PcmAddressData(0xa, dt2);

                                                                dt = (y8950_pcm_start << 3) & 0xff;
                                                            }
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950"))
                                                        {
                                                            if (!y8950_adpcmbit64k)
                                                            {
                                                                switch (comPortOPNA.SoundModuleType)
                                                                {
                                                                    case VsifSoundModuleType.MSX_FTDI:
                                                                    case VsifSoundModuleType.TurboR_FTDI:
                                                                    case VsifSoundModuleType.MSX_Pi:
                                                                    case VsifSoundModuleType.MSX_PiTR:
                                                                    case VsifSoundModuleType.PC88_FTDI:
                                                                        var dt2 = ((y8950_pcm_start << 3) & 0xff00) >> 8;
                                                                        deferredWriteOPNA_P1(comPortOPNA, 0x03, dt2, dclk);

                                                                        dt = (y8950_pcm_start << 3) & 0xff;
                                                                        break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (adrs == 0xa)
                                                {
                                                    y8950_pcm_start = (y8950_pcm_start & 0xff) | (dt << 8);
                                                    if (!y8950_adpcm_ram)
                                                    {
                                                        if (comPortY8950 != null)
                                                        {
                                                            if (comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                                                            {
                                                                dt = ((y8950_pcm_start << 3) & 0xff00) >> 8;
                                                            }
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950"))
                                                        {
                                                            if (!y8950_adpcmbit64k)
                                                            {
                                                                switch (comPortOPNA.SoundModuleType)
                                                                {
                                                                    case VsifSoundModuleType.MSX_FTDI:
                                                                    case VsifSoundModuleType.TurboR_FTDI:
                                                                    case VsifSoundModuleType.MSX_Pi:
                                                                    case VsifSoundModuleType.MSX_PiTR:
                                                                    case VsifSoundModuleType.PC88_FTDI:
                                                                        dt = ((y8950_pcm_start << 3) & 0xff00) >> 8;
                                                                        break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (adrs == 0xb)
                                                {
                                                    y8950_pcm_stop = (y8950_pcm_stop & 0xff00) | dt;

                                                    if (!y8950_adpcm_ram)
                                                    {
                                                        if (comPortY8950 != null)
                                                        {
                                                            if (comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                                                            {
                                                                var dt2 = (((y8950_pcm_stop << 3) | 0b111) & 0xff00) >> 8;
                                                                writeY8950PcmAddressData(0xc, dt2);

                                                                dt = ((y8950_pcm_stop << 3) | 0b111) & 0xff;
                                                            }
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950"))
                                                        {
                                                            if (!y8950_adpcmbit64k)
                                                            {
                                                                switch (comPortOPNA.SoundModuleType)
                                                                {
                                                                    case VsifSoundModuleType.MSX_FTDI:
                                                                    case VsifSoundModuleType.TurboR_FTDI:
                                                                    case VsifSoundModuleType.MSX_Pi:
                                                                    case VsifSoundModuleType.MSX_PiTR:
                                                                    case VsifSoundModuleType.PC88_FTDI:
                                                                        {
                                                                            var dt2 = (((y8950_pcm_stop << 3) | 0b111) & 0xff00) >> 8;
                                                                            deferredWriteOPNA_P1(comPortOPNA, 0x05, dt2, dclk);
                                                                            deferredWriteOPNA_P1(comPortOPNA, 0x0d, dt2, dclk);

                                                                            dt = ((y8950_pcm_stop << 3) | 0b111) & 0xff;
                                                                            break;
                                                                        }
                                                                }
                                                            }

                                                        }
                                                    }
                                                }
                                                else if (adrs == 0xc)
                                                {
                                                    y8950_pcm_stop = (y8950_pcm_stop & 0xff) | (dt << 8);

                                                    if (!y8950_adpcm_ram)
                                                    {
                                                        if (comPortY8950 != null)
                                                        {
                                                            if (comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                                                                comPortY8950.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                                                            {
                                                                dt = (((y8950_pcm_stop << 3) | 0b111) & 0xff00) >> 8;
                                                            }
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950"))
                                                        {
                                                            switch (comPortOPNA.SoundModuleType)
                                                            {
                                                                case VsifSoundModuleType.MSX_FTDI:
                                                                case VsifSoundModuleType.TurboR_FTDI:
                                                                case VsifSoundModuleType.MSX_Pi:
                                                                case VsifSoundModuleType.MSX_PiTR:
                                                                case VsifSoundModuleType.PC88_FTDI:
                                                                    {
                                                                        dt = (((y8950_pcm_stop << 3) | 0b111) & 0xff00) >> 8;
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }
#if DEBUG
                                                    Console.WriteLine("Y8950: Play " + y8950_adpcmbit64kString + "(" + (y8950_pcm_start << 5).ToString("x") + " - " + ((y8950_pcm_stop << 5) | 0x1f).ToString("x") + ")");
#endif
                                                }
                                            }
                                            if (adrs != 0xf)   //ignore ADPCM adrs
                                            {
                                                if (comPortY8950 != null)
                                                {
                                                    deferredWriteY8950(adrs, dt, dclk);
                                                }
                                                else
                                                {
                                                    if (comPortOPL3 != null && comPortOPL3.Tag.ContainsKey("ProxyY8950"))
                                                    {
                                                        if (adrs >= 0x20)
                                                            deferredWriteOPL3_P0(adrs, dt, dclk * 4);
                                                    }
                                                    if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950")
                                                        && !y8950_adpcmbit64k)
                                                    {
                                                        switch (adrs)
                                                        {
                                                            case 0x7:   //ctrl1
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x00, dt, dclk);
                                                                break;
                                                            case 0x8:   //ctrl2
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x01, 0xc0, dclk);
                                                                break;
                                                            case 0x9:   //start l
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x02, dt, dclk);
                                                                break;
                                                            case 0xa:   //start h
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x03, dt, dclk);
                                                                break;
                                                            case 0xb:   //stop l
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x04, dt, dclk);
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x0c, dt, dclk);
                                                                break;
                                                            case 0xc:   //stop h
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x05, dt, dclk);
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x0d, dt, dclk);
                                                                break;
                                                            case 0xd:   //prescale l
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x06, dt, dclk);
                                                                break;
                                                            case 0xe:   //prescale h
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x07, dt, dclk);
                                                                break;
                                                            case 0x10:   //delta h
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x09, dt, dclk);
                                                                break;
                                                            case 0x11:   //delta h
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x0a, dt, dclk);
                                                                break;
                                                            case 0x12:   //EG
                                                                deferredWriteOPNA_P1(comPortOPNA, 0x0b, dt, dclk);
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (comPortY8950 != null)
                                                {
                                                    deferredWriteY8950(adrs, dt, dclk);
                                                    comPortY8950.FlushDeferredWriteDataAndWait();
                                                    //HACK:
                                                    QueryPerformanceCounter(out before);
                                                    dbefore = before;
                                                }
                                            }
                                        }
                                        break;

                                    case 0x5D: //YMZ280B
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0x5E: //YMF262
                                        {
                                            uint dclk = vgmHead.cur.lngHzYMF262;
                                            if (command == 0x5A)
                                                dclk = vgmHead.cur.lngHzYM3812 * 4;
                                            else if (command == 0x5B)
                                                dclk = vgmHead.cur.lngHzYM3526 * 4;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPL3 != null)
                                                deferredWriteOPL3_P0(adrs, dt, dclk);
                                        }
                                        break;

                                    case 0x5F: //YMF262
                                        {
                                            uint dclk = vgmHead.cur.lngHzYMF262;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPL3 != null)
                                                deferredWriteOPL3_P1(adrs, dt, dclk);
                                        }
                                        break;


                                    case 0x61: //Wait N samples
                                        {
                                            ushort time = vgmReader.ReadUInt16();
                                            vgmWaitDelta += time;

                                            writeVgmLogFile(0x61);
                                            writeVgmLogFile(BitConverter.GetBytes(time));
                                        }
                                        break;

                                    case 0x62: //Wait 735 samples
                                        vgmWaitDelta += 735;
                                        writeVgmLogFile(0x62);
                                        break;

                                    case 0x63: //Wait 882 samples
                                        vgmWaitDelta += 882;
                                        writeVgmLogFile(0x63);
                                        break;

                                    case 0x66:
                                        //End of song
                                        writeVgmLogFile(true, 0x66);
                                        flushDeferredWriteData();
                                        if (!LoopByCount && !LoopByElapsed)
                                        {
                                            vgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                            looped = true;
                                            break;
                                        }
                                        else
                                        {
                                            if (vgmHead.cur.lngLoopOffset != 0 && vgmDataOffset < vgmHead.cur.lngLoopOffset)
                                                vgmReader.BaseStream?.Seek((vgmHead.cur.lngLoopOffset + 0x1c) - (vgmDataOffset), SeekOrigin.Begin);
                                            else
                                                vgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                            looped = true;
                                        }
                                        break;

                                    case 0x67: //Data Block
                                        {
                                            //0x66
                                            var data = readByte();
                                            //data type
                                            var dtype = readByte();
                                            //data size
                                            uint size = vgmReader.ReadUInt32();
                                            if (0 <= size && size <= Int32.MaxValue)
                                            {
                                                switch (dtype)
                                                {
                                                    case 0: //YM2612 PCM data for use with associated commands
                                                        {
                                                            dacDataOffset.Add(dacData.Count);
                                                            dacDataLength.Add((int)size);
                                                            if (size == 0)
                                                                dacData.AddRange(new byte[] { 0 });
                                                            else
                                                                dacData.AddRange(vgmReader.ReadBytes((int)size));
                                                        }
                                                        break;
                                                    case 1: //RF5C68  PCM data for use with associated commands
                                                    case 2: //RF5C164 PCM data for use with associated commands
                                                        {
                                                            uint saddr = 0;
                                                            List<byte> dd = new List<byte>();
                                                            dd.AddRange(vgmReader.ReadBytes((int)size));

                                                            if (comPortMCD != null)
                                                            {
                                                                byte prevbank = 0xff;
                                                                int percentage = 0;
                                                                int lastPercentage = -1;
                                                                for (var i = saddr; i < dd.Count; i++)
                                                                {
                                                                    if (i <= 8)
                                                                    {
                                                                        DeferredWriteMCDReg((int)(i & 0x1f), dd[(int)i]);
                                                                    }
                                                                    else if (i >= 0x1000)
                                                                    {
                                                                        byte bank = (byte)(0x00 | (((i - 0x1000) >> 12) & 0xf));
                                                                        if (bank != prevbank)
                                                                        {
                                                                            //Write mode and select mem bank
                                                                            DeferredWriteMCDReg(0x7, bank);
                                                                            prevbank = bank;
                                                                        }
                                                                        //Write PCM DATA
                                                                        DeferredWriteMCDMem((int)((i - 0x1000) & 0xfff), dd[(int)i]);

                                                                        percentage = (int)((100 * i) / dd.Count);
                                                                        if (percentage != lastPercentage)
                                                                        {
                                                                            lastPercentage = percentage;
                                                                            FormMain.TopForm.SetStatusText("RF5C164: Transferring PCM(" + percentage + "%)");
                                                                            switch (comPortMCD.SoundModuleType)
                                                                            {
                                                                                case VsifSoundModuleType.Genesis_FTDI:
                                                                                    comPortMCD?.FlushDeferredWriteDataAndWait();
                                                                                    break;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                switch (comPortMCD.SoundModuleType)
                                                                {
                                                                    case VsifSoundModuleType.Genesis_FTDI:
                                                                        comPortMCD?.FlushDeferredWriteDataAndWait();
                                                                        break;
                                                                }
                                                                DeferredWriteMCDReg(0x08, 0xFF); //HACK: koff all
                                                                FormMain.TopForm.SetStatusText("RF5C164: Transferred PCM.");
                                                            }
                                                        }
                                                        break;
                                                    case 4: //OKIM6258 ADPCM data for use with associated commands
                                                        {
                                                            dacDataOffset.Add(dacData.Count);
                                                            dacDataLength.Add((int)size);
                                                            if (size == 0)
                                                                dacData.AddRange(new byte[] { 0 });
                                                            else
                                                                dacData.AddRange(vgmReader.ReadBytes((int)size));
                                                            //StringBuilder sb = new StringBuilder();
                                                            //foreach (byte d in dacData)
                                                            //{
                                                            //    sb.AppendLine(d.ToString());
                                                            //}
                                                            //File.WriteAllBytes("d:\\aaa" + dacData.Count + ".bin", dacData.ToArray());
                                                            //Oki o = new Oki();
                                                            //List<byte> pcm = new List<byte>();
                                                            //for (int i = 0; i < dacData.Count; i++)
                                                            //{
                                                            //    pcm.Add((byte)(o.decode(dacData[i] >> 4) >> 8));
                                                            //    pcm.Add((byte)(o.decode(dacData[i] & 0xf) >> 8));
                                                            //}
                                                            //File.WriteAllBytes("d:\\aaa" + dacData.Count + ".pcm", pcm.ToArray());
                                                            //File.WriteAllText("d:\\aaa"+dacData.Count+".csv", sb.ToString());
                                                        }
                                                        break;
                                                    case 5: //HuC6280 PCM data for use with associated commands
                                                        {
                                                            dacDataOffset.Add(dacData.Count);
                                                            dacDataLength.Add((int)size);
                                                            if (size == 0)
                                                                dacData.AddRange(new byte[] { 0 });
                                                            else
                                                                dacData.AddRange(vgmReader.ReadBytes((int)size));
                                                        }
                                                        break;
                                                    case 7: //NES DPCM data for use with associated commands
                                                        {
                                                            dacDataOffset.Add(dacData.Count);
                                                            dacDataLength.Add((int)size);
                                                            if (size == 0)
                                                                dacData.AddRange(new byte[] { 0 });
                                                            else
                                                                dacData.AddRange(vgmReader.ReadBytes((int)size));
                                                        }
                                                        break;
                                                    case 0x80:  //SEGA PCM
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

#if DEBUG
                                                            /*
                                                            Console.WriteLine("SEGAPCM: (" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
                                                            */
#endif
                                                            var dat = vgmReader.ReadBytes((int)size);
                                                            segaPcm?.sega_pcm_write_rom(0, (int)romSize, (int)saddr, (int)size, dat);
                                                        }
                                                        break;
                                                    case 0x81:  //YM2608
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;
#if DEBUG
                                                            Console.WriteLine("YM2608: Transferring ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
#endif
                                                            //FormProgress.RunDialog("Updating ADPCM data",
                                                            //    new Action<FormProgress>((f) =>
                                                            {
                                                                if (size > 0)
                                                                    sendAdpcmDataYM2608(comPortOPNA, ym2608_adpcmbit8, vgmReader.ReadBytes((int)size), (int)saddr, null);
                                                            }
                                                            //));
#if DEBUG
                                                            Console.WriteLine("YM2608: Transferred ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
#endif
                                                            flushDeferredWriteData();
                                                        }
                                                        break;
                                                    case 0x82:  //YM2610 ADPCM A ROM data
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

                                                            if (0 <= size && size <= Int32.MaxValue)
                                                            {
                                                                byte[] romData = vgmReader.ReadBytes((int)size);
                                                                if (comPortOPNB != null)
                                                                {
                                                                    if (!looped)
                                                                        transferAdpcmDataForNeotron(size, saddr, romData, 1, "A");
                                                                }
                                                                else if (opnbPcm != null)
                                                                {
                                                                    for (uint i = 0; i < romData.Length; i++)
                                                                        opnbPcm.adpcm_a_engine.intf().ymfm_external_write(access_class.ACCESS_ADPCM_A, saddr + i, romData[i]);
                                                                }
#if DEBUG
                                                                Console.WriteLine("YM2610: Transferred ADPCM A(" +
                                                                    (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                    " (" + size.ToString("x") + ")");
#endif
                                                            }
                                                        }
                                                        break;
                                                    case 0x83:  //YM2610 DELTA-T ROM data
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

                                                            if (0 <= size && size <= Int32.MaxValue)
                                                            {
                                                                byte[] romData = vgmReader.ReadBytes((int)size);
                                                                if (comPortOPNB != null)
                                                                {
                                                                    if (!looped)
                                                                        transferAdpcmDataForNeotron(size, saddr, romData, 2, "B");
                                                                }
                                                                else if (opnbPcm != null)
                                                                {
                                                                    for (uint i = 0; i < romData.Length; i++)
                                                                        opnbPcm.adpcm_b_engine.intf().ymfm_external_write(access_class.ACCESS_ADPCM_B, saddr + i, romData[i]);
                                                                }
#if DEBUG
                                                                Console.WriteLine("YM2610: Transferred ADPCM B(" +
                                                                    (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                    " (" + size.ToString("x") + ")");
#endif
                                                            }
                                                        }
                                                        break;
                                                    case 0x88:  //YM8950
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

                                                            FormMain.TopForm.SetStatusText("Y8950: Transferring ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");

#if DEBUG
                                                            Console.WriteLine("Y8950: Transferring ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
#endif
                                                            //FormProgress.RunDialog("Updating ADPCM data",
                                                            //    new Action<FormProgress>((f) =>
                                                            {
                                                                if (size > 0)
                                                                {
                                                                    if (comPortY8950 != null)
                                                                        sendAdpcmDataY8950(vgmReader.ReadBytes((int)size), (int)saddr, null);
                                                                    else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8950"))
                                                                    {
                                                                        if (!y8950_adpcmbit64k)
                                                                            sendAdpcmDataYM2608(comPortOPNA, ym2608_adpcmbit8, vgmReader.ReadBytes((int)size), (int)saddr, null);
                                                                    }
                                                                }
                                                            }
                                                            //));

                                                            FormMain.TopForm.SetStatusText("Y8950: Transferred ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");

#if DEBUG
                                                            Console.WriteLine("Y8950: Transferred ADPCM(" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
#endif
                                                            flushDeferredWriteData();
                                                        }
                                                        break;
                                                    case 0x8b: //OKIM6295
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

                                                            var dat = vgmReader.ReadBytes((int)size);
                                                            okim6295?.okim6295_write_rom(0, (int)romSize, (int)saddr, (int)size, dat);
                                                        }
                                                        break;
                                                    case 0x8E:  //K053260 PCM
                                                        {
                                                            uint romSize = vgmReader.ReadUInt32();
                                                            uint saddr = vgmReader.ReadUInt32();
                                                            size -= 8;

#if DEBUG
                                                            /*
                                                            Console.WriteLine("K053260: (" +
                                                                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                                                                " (" + size.ToString("x") + ")");
                                                            */
#endif
                                                            var dat = vgmReader.ReadBytes((int)size);
                                                            k053260?.k053260_write_rom(0, (int)romSize, (int)saddr, (int)size, dat);

                                                            break;
                                                        }
                                                    case 0xc0:  //RF5C68
                                                    case 0xc1:  //RF5C164
                                                        {
                                                            uint saddr = vgmReader.ReadUInt16();
                                                            size -= 2;
                                                            List<byte> dd = new List<byte>();
                                                            dd.AddRange(vgmReader.ReadBytes((int)size));

                                                            if (comPortMCD != null)
                                                            {
                                                                byte prevbank = 0xff;
                                                                int percentage = 0;
                                                                int lastPercentage = -1;
                                                                for (var i = 0; i < dd.Count; i++)
                                                                {
                                                                    byte bank = (byte)(0x00 | (((saddr + i) >> 12) & 0xf));
                                                                    if (bank != prevbank)
                                                                    {
                                                                        //Write mode and select mem bank
                                                                        DeferredWriteMCDReg(0x7, bank);
                                                                        prevbank = bank;
                                                                    }
                                                                    //Write PCM DATA
                                                                    DeferredWriteMCDMem((int)((saddr + i) & 0xfff), dd[(int)i]);

                                                                    percentage = (int)((100 * i) / dd.Count);
                                                                    if (percentage != lastPercentage)
                                                                    {
                                                                        lastPercentage = percentage;
                                                                        FormMain.TopForm.SetStatusText("RF5C164: Transferring PCM(" + percentage + "%)");
                                                                        switch (comPortMCD.SoundModuleType)
                                                                        {
                                                                            case VsifSoundModuleType.Genesis_FTDI:
                                                                                comPortMCD?.FlushDeferredWriteDataAndWait();
                                                                                break;
                                                                        }
                                                                    }
                                                                    if (RequestedStat == SoundState.Stopped)
                                                                        break;
                                                                }
                                                                switch (comPortMCD.SoundModuleType)
                                                                {
                                                                    case VsifSoundModuleType.Genesis_FTDI:
                                                                        comPortMCD?.FlushDeferredWriteDataAndWait();
                                                                        break;
                                                                }
                                                                DeferredWriteMCDReg(0x08, 0xFF); //HACK: koff all
                                                                FormMain.TopForm.SetStatusText("RF5C164: Transferred PCM.");
                                                            }

                                                            break;
                                                        }
                                                    case 0xc2:
                                                        {
                                                            //NES DPCM
                                                            uint saddr = vgmReader.ReadUInt16();
                                                            size -= 2;
                                                            List<byte> dd = new List<byte>(new byte[(int)saddr - 0xc000]);
                                                            dd.AddRange(vgmReader.ReadBytes((int)size));
                                                            dpcmData = dd.ToArray();
                                                            break;
                                                        }
                                                    default:
                                                        vgmReader.ReadBytes((int)size);
                                                        break;
                                                }
                                            }
                                            //_vgmReader.BaseStream.Position += size;

                                            //HACK:
                                            QueryPerformanceCounter(out before);
                                            dbefore = before;
                                        }
                                        break;

                                    case 0x68: //PCM RAM Write
                                        {
                                            //compatibility command to
                                            readByte();
                                            //chip type
                                            int ct = readByte();
                                            //read offset in data block
                                            int rofset = readByte() | (readByte() << 8) | (readByte() << 16);
                                            //write offset in chip's ram (affected by chip's registers)
                                            int wofset = readByte() | (readByte() << 8) | (readByte() << 16);
                                            //size of data, in bytes
                                            int size = readByte() | (readByte() << 8) | (readByte() << 16);
                                            vgmReader.ReadBytes((int)size);
                                        }
                                        break;

                                    case int cmd when 0x70 <= cmd && cmd <= 0x7F:
                                        {
                                            writeVgmLogFile((byte)cmd);

                                            var time = (cmd & 15) + 1;
                                            vgmWaitDelta += time;
                                        }
                                        break;

                                    case int cmd when 0x80 <= cmd && cmd <= 0x8F:
                                        {
                                            var time = (command & 15);
                                            vgmWaitDelta += time;
                                            //_chip.WritePort0(0x2A, _DACData[_DACOffset]);
                                            if (dacData != null && dacOffset < dacData.Count)
                                            {
                                                byte dt = dacData[dacOffset];
                                                dt = (byte)Math.Round((double)dt * (double)PcmMixer.DacVolume / 100d);

                                                if (comPortOPN2 != null)
                                                {
                                                    DeferredWriteOPN2_DAC(comPortOPN2, dt);
                                                }
                                                else if (comPortTurboRProxy != null)
                                                {
                                                    DeferredWriteTurboR_DAC(comPortTurboRProxy, dt);
                                                }
                                                else if (comPortOPNA != null)
                                                {
                                                    DeferredWriteOPNA_PseudoDAC(comPortOPNA, (short)dt);
                                                }
                                            }
                                            dacOffset++;
                                        }
                                        break;

                                    case 0x90: //Setup Stream Control:
                                        {
                                            //stream id
                                            var sid = readByte();
                                            //chip type
                                            streamChipType = readByte();
                                            //port
                                            var port = readByte();
                                            //command
                                            var cmd = readByte();
                                            DacStream dacStream = null;
                                            switch (streamChipType)
                                            {
                                                case 2:   //YM2612
                                                    if (port == 0x00 && cmd == 0x2a)    //PCM ENABLE
                                                    {
                                                        if (comPortOPN2 != null)
                                                        {
                                                            deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);

                                                            dacStream = new DacStream(this, DacStream.DacProxyType.OPN2, comPortOPN2, null);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                        else if (comPortTurboRProxy != null && comPortOPNA == comPortTurboRProxy)
                                                        {
                                                            dacStream = new DacStream(this, DacStream.DacProxyType.TurboR, comPortTurboRProxy, null);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOPN2"))
                                                        {
                                                            dacStream = new DacStream(this, DacStream.DacProxyType.OPNA, comPortOPNA, null);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                    }
                                                    break;

                                                case 20:   //NES
                                                    if (port == 0x00 && cmd == 0x11)    //PCM ENABLE
                                                    {
                                                        dacStream = new DacStream(this, DacStream.DacProxyType.NES, comPortNES, null);
                                                        Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                        t.Priority = ThreadPriority.Highest;
                                                        t.Start();
                                                    }
                                                    break;

                                                case 23: //OKIM6258
                                                    if (port == 0x00 && cmd == 0x1)    //ADPCM ENABLE
                                                    {
                                                        if (comPortTurboRProxy != null && comPortTurboRProxy.Tag.ContainsKey("ProxyOKIM6258"))
                                                        {
                                                            okim6258 = new OKIM6258();
                                                            dacStream = new DacStream(this, DacStream.DacProxyType.TurboR, comPortTurboRProxy, okim6258);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                        else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOKIM6258"))
                                                        {
                                                            deferredWriteOPN2_P0(comPortOPN2, 0x2b, 0x80, 0);
                                                            okim6258 = new OKIM6258();
                                                            dacStream = new DacStream(this, DacStream.DacProxyType.OPN2, comPortTurboRProxy, okim6258);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                        else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOKIM6258"))
                                                        {
                                                            okim6258 = new OKIM6258();
                                                            dacStream = new DacStream(this, DacStream.DacProxyType.OPNA, comPortTurboRProxy, okim6258);
                                                            Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                            t.Priority = ThreadPriority.Highest;
                                                            t.Start();
                                                        }
                                                    }
                                                    break;
                                                case 27: //PCE
                                                    {
                                                        dacStream = new DacStream(this, DacStream.DacProxyType.PCE, comPortPCE, null, (byte)port, (byte)cmd);
                                                        Thread t = new Thread(new ThreadStart(dacStream.StreamSong));
                                                        t.Priority = ThreadPriority.Highest;
                                                        t.Start();
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                            if (!streamTable.ContainsKey(sid))
                                                streamTable.Add(sid, new StreamData(sid, dacStream));
                                        }
                                        break;

                                    case 0x91: //Set Stream Data:
                                        {
                                            //stream id
                                            var sid = readByte();
                                            //data bank
                                            var dbank = readByte();
                                            //step size
                                            var ssz = readByte();
                                            //step base
                                            var sbase = readByte();

                                            if (streamTable.ContainsKey(sid))
                                            {
                                                streamTable[sid].StreamDataBanks[dbank].StepSize = ssz;
                                                streamTable[sid].StreamDataBanks[dbank].StepBase = sbase;
                                            }
                                        }
                                        break;

                                    case 0x92: //Set Stream Frequency:
                                        {
                                            //stream id
                                            var sid = readByte();
                                            //sample rate
                                            uint sfreq = vgmReader.ReadUInt32();

                                            if (streamTable.ContainsKey(sid))
                                                streamTable[sid].Frequency = sfreq;
                                        }
                                        break;

                                    case 0x93: //Start Stream:
                                        {
                                            //stream id
                                            var sid = readByte();
                                            uint ofst = vgmReader.ReadUInt32();
                                            var lenMode = readByte();
                                            uint dataLen = vgmReader.ReadUInt32();
                                            if (lenMode == 1)
                                            {
                                                StreamParam param = new StreamParam();
                                                param.StreamID = sid;
                                                param.BlockID = 0;
                                                param.Offset = (int)ofst;
                                                param.Length = (int)dataLen;
                                                if ((lenMode & 0x80) != 0)
                                                    param.Mode |= StreamModes.Loop;
                                                else if ((lenMode & 0x10) != 0)
                                                    param.Mode |= StreamModes.Reverse;

                                                if (streamTable.ContainsKey(sid))
                                                    streamTable[sid].DacStream?.Play(param, streamTable[param.StreamID], streamChipType, dacData);
                                            }
                                        }
                                        break;

                                    case 0x94:  //Stop Stream
                                        {
                                            //stream id
                                            var sid = readByte();

                                            if (streamTable.ContainsKey(sid))
                                                streamTable[sid].DacStream?.Stop();
                                        }
                                        break;

                                    case 0x95: //Start Stream (fast call):
                                        {
                                            //stream id
                                            var sid = readByte();
                                            //block id
                                            var bid = readByte();
                                            //flags
                                            var flgs = readByte();

                                            StreamParam param = new StreamParam();
                                            param.StreamID = sid;
                                            param.BlockID = bid;
                                            param.Offset = dacDataOffset[bid];
                                            param.Length = dacDataLength[bid];
                                            if ((flgs & 0x01) != 0)
                                                param.Mode |= StreamModes.Loop;
                                            else if ((flgs & 0x10) != 0)
                                                param.Mode |= StreamModes.Reverse;

                                            if (streamTable.ContainsKey(sid))
                                                streamTable[sid].DacStream?.Play(param, streamTable[param.StreamID], streamChipType, dacData);
                                        }
                                        break;

                                    case 0xA0:  //Y8910
                                        {
                                            uint dclk = vgmHead.cur.lngHzAY8910;

                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            if (aa <= 0xd)
                                            {
                                                if (comPortY8910 != null)
                                                    deferredWriteY8910(aa, dd, dclk);
                                                else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyY8910"))
                                                    deferredWriteOPNA_P0(comPortOPNA, aa, dd, dclk * 4);
                                                else if (comPortOPN != null && comPortOPN.Tag.ContainsKey("ProxyY8910"))
                                                    deferredWriteOPN_P0(aa, dd, dclk * 2);
                                            }
                                            else if ((aa & 0x80) == 0x80)
                                            {
                                                aa &= 0x7f;
                                                if (comPortY8910 != null && aa < 0xd)
                                                    deferredWriteY8910(aa, dd, dclk, true);
                                            }
                                        }
                                        break;
                                    case 0xA5:  //2nd YM2203
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortOPNA != null)
                                            {
                                                uint dclk = vgmHead.cur.lngHzYM2203 * 2;

                                                if (adrs == 0x28)
                                                    dt |= 0b100;
                                                else if (adrs < 0x30)
                                                    break;

                                                if (adrs < 0x30)
                                                    deferredWriteOPNA_P0(comPortOPNA, adrs, dt, dclk);
                                                else
                                                    deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                            }
                                            else if (comPortOPN2 != null)
                                            {
                                                uint dclk = vgmHead.cur.lngHzYM2203 * 2;

                                                if (adrs == 0x28)
                                                    dt |= 0b100;
                                                else if (adrs < 0x30)
                                                    break;

                                                if (adrs < 0x30)
                                                    deferredWriteOPN2_P0(comPortOPN2, adrs, dt, dclk);
                                                else
                                                    deferredWriteOPN2_P1(comPortOPN2, adrs, dt, dclk);
                                            }
                                            break;
                                        }
                                    case int cmd when 0xA1 <= cmd && cmd <= 0xAF:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0xB0:  //RF5C68 MCD reg
                                    case 0xB1:  //RF5C164 MCD reg
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            uint dclk = vgmHead.cur.lngHzRF5C164;
                                            if (command == 0xB0)
                                                dclk = vgmHead.cur.lngHzRF5C68;

                                            if (comPortMCD != null && adrs <= 8)
                                            {
                                                int ch = comPortMCD.RegTable[7] << 8;

                                                switch (adrs)
                                                {
                                                    case 2:
                                                        //LO
                                                        comPortMCD.RegTable[adrs + ch] = dt;

                                                        if (!ConvertChipClock || (double)comPortMCD.ChipClockHz["RF5C164"] == (double)dclk)
                                                            goto default;
                                                        {
                                                            var hi = 0;
                                                            if (comPortMCD.RegTable.ContainsKey(ch + 3))
                                                                hi = comPortMCD.RegTable[ch + 3];
                                                            var ret = converRF5C164Frequency(hi, dt, comPortMCD.ChipClockHz["RF5C164"], dclk);
                                                            if (ret.noConverted)
                                                                goto default;
                                                            DeferredWriteMCDReg(0x2, (byte)ret.Lo);
                                                            DeferredWriteMCDReg(0x3, (byte)ret.Hi);
                                                        }
                                                        break;
                                                    case 3:
                                                        //HI
                                                        comPortMCD.RegTable[adrs + ch] = dt;

                                                        if (!ConvertChipClock || (double)comPortMCD.ChipClockHz["RF5C164"] == (double)dclk)
                                                            goto default;
                                                        {
                                                            var lo = 0;
                                                            if (comPortMCD.RegTable.ContainsKey(ch + 2))
                                                                lo = comPortMCD.RegTable[ch + 2];

                                                            var ret = converRF5C164Frequency(dt, lo, comPortMCD.ChipClockHz["RF5C164"], dclk);
                                                            if (ret.noConverted)
                                                                goto default;
                                                            DeferredWriteMCDReg(0x2, (byte)ret.Lo);
                                                            DeferredWriteMCDReg(0x3, (byte)ret.Hi);
                                                        }
                                                        break;
                                                    case 7:
                                                        if ((dt & 0x40) != 0)
                                                            comPortMCD.RegTable[7] = dt & 0x7;
                                                        else
                                                            comPortMCD.RegTable[0x100 + 7] = dt & 0xf;
                                                        DeferredWriteMCDReg(adrs, (byte)dt);
                                                        break;
                                                    default:
                                                        DeferredWriteMCDReg(adrs, (byte)dt);
                                                        break;
                                                }
                                            }
                                            break;
                                        }

                                    case 0xB2:  //PWM reg
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (comPortPWM != null)
                                            {
                                                DeferredWritePWMReg(adrs, (byte)dt);
                                            }
                                            break;
                                        }

                                    case int cmd when 0xB3 <= cmd && cmd <= 0xB3:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0xB4:  //NES APU
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if (0x20 <= adrs && adrs <= 0x3e)
                                            {
                                                adrs += 0x60;
                                            }
                                            else if (adrs == 0x3f)
                                            {
                                                adrs = 0x23;
                                            }
                                            if (comPortNES != null)
                                            {
                                                uint dclk = vgmHead.cur.lngHzNESAPU;
                                                DeferredWriteNES(adrs, dt, dclk);
                                            }
                                            break;
                                        }

                                    case 0xB5:
                                    case 0xB6:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0xB7:  //okim6258
                                        {
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            switch (aa)
                                            {
                                                case 0x00:
                                                    //okim6258_ctrl_w(ChipID, /*0x00, */Data);
                                                    break;
                                                case 0x01:
                                                    if (okim6258 != null)
                                                    {
                                                        if (!oki6285Adpcm2ndNibble)
                                                        {
                                                            var ddata = okim6258.decode(dd & 0xf);
                                                            ddata = (int)Math.Round((double)ddata * (double)PcmMixer.DacVolume / 100d);

                                                            if (comPortTurboRProxy != null && comPortTurboRProxy.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                                DeferredWriteTurboR_DAC(comPortTurboRProxy, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                                DeferredWriteOPN2_DAC(comPortOPN2, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8));
                                                                DeferredWriteOPNA_DAC(comPortOPNA, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            oki6285Adpcm2ndNibble = true;
                                                            vgmDataCurrentOffset--;
                                                        }
                                                        else
                                                        {
                                                            var ddata = okim6258.decode(dd >> 4);
                                                            //var ddata = decodeOpnaAdpcm(dd >> 4);
                                                            ddata = (int)Math.Round((double)ddata * (double)PcmMixer.DacVolume / 100d);

                                                            if (comPortTurboRProxy != null && comPortTurboRProxy.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                                DeferredWriteTurboR_DAC(comPortTurboRProxy, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            else if (comPortOPN2 != null && comPortOPN2.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                                DeferredWriteOPN2_DAC(comPortOPN2, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            else if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOKIM6258"))
                                                            {
                                                                byte bdata = (byte)((ddata >> 8));
                                                                DeferredWriteOPNA_DAC(comPortOPNA, bdata);
                                                                streamWaitDelta += 44.1 * 1000 / oki6258_sample_rate;
                                                            }
                                                            oki6285Adpcm2ndNibble = false;
                                                        }
                                                    }
                                                    break;
                                                case 0x02:
                                                    if (comPortOPNA != null && comPortOPNA.Tag.ContainsKey("ProxyOKIM6258"))
                                                    {
                                                        //LR
                                                        byte lr = (byte)((~dd << 6) & 0xc0);
                                                        deferredWriteOPNA_P1(comPortOPNA, 0x01, (byte)(lr | 0xc));
                                                    }
                                                    break;
                                                case 0x08:
                                                case 0x09:
                                                case 0x0A:
                                                    oki6258_clock_buffer[aa & 3] = (uint)dd;
                                                    break;
                                                case 0x0B:
                                                    oki6258_clock_buffer[aa & 3] = (uint)dd;
                                                    oki6258_master_clock = (oki6258_clock_buffer[0x00] << 0) |
                                                        (oki6258_clock_buffer[0x01] << 8) |
                                                        (oki6258_clock_buffer[0x02] << 16) |
                                                        (oki6258_clock_buffer[0x03] << 24);
                                                    oki6258_sample_rate = oki6258_master_clock / oki6258_divider;
                                                    break;
                                                case 0x0C:
                                                    uint[] dividers = new uint[] { 1024, 768, 512, 512 };
                                                    oki6258_divider = dividers[dd];
                                                    oki6258_sample_rate = oki6258_master_clock / oki6258_divider;
                                                    break;
                                            }
                                        }
                                        break;


                                    case 0xB8:  //okim6295
                                        {
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            okim6295?.okim6295_w(0, aa, (byte)dd);
                                        }
                                        break;

                                    case 0xB9:  //HuC6280
                                        {
                                            //uint dclk = vgmHead.lngHzHuC6280;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            DeferredWritePCE(adrs, (byte)dt);

                                            break;
                                        }

                                    case 0xBA:  //K053260
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            k053260?.k053260_w(0, adrs, (byte)dt);
                                        }
                                        break;

                                    case int cmd when 0xBB <= cmd && cmd <= 0xBC:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0xBD:  //SAA1099
                                        {
                                            uint dclk = vgmHead.cur.lngHzSAA1099;

                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;

                                            if ((adrs & 0x80) == 0)
                                                deferredWriteSAAReg(adrs, (byte)dt, dclk);

                                            break;
                                        }

                                    case int cmd when 0xBE <= cmd && cmd <= 0xBF:
                                        {
                                            var adrs = readByte();
                                            if (adrs < 0)
                                                break;
                                            var dt = readByte();
                                            if (dt < 0)
                                                break;
                                        }
                                        break;

                                    case 0xC0:  //SEGA PCM
                                        {
                                            var bb = readByte();
                                            if (bb < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            segaPcm?.sega_pcm_w(0, (aa << 8) | bb, (byte)dd);

                                            break;
                                        }

                                    case 0xC1:  //RF5C68 MCD Mem
                                    case 0xC2:  //RF5C164 MCD Mem
                                        {
                                            var bb = readByte();
                                            if (bb < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            if (comPortMCD != null)
                                            {
                                                DeferredWriteMCDMem((aa << 8) | bb, (byte)dd);

                                                var bank = 0;
                                                if (comPortMCD.RegTable.ContainsKey(0x100 + 7))
                                                    bank = (comPortMCD.RegTable[0x100 + 7]) << 12;
                                                FormMain.TopForm.SetStatusText("RF5C164: Transferring PCM Mem(0x" + (bank + (aa << 8) + bb).ToString("X") + ")");

                                                switch (comPortMCD.SoundModuleType)
                                                {
                                                    case VsifSoundModuleType.Genesis_FTDI:
                                                        //comPortMCD?.FlushDeferredWriteDataAndWait();
                                                        break;
                                                }
                                            }
                                            //HACK:
                                            QueryPerformanceCounter(out before);
                                            dbefore = before;
                                            break;
                                        }

                                    case int cmd when 0xC3 <= cmd && cmd <= 0xD1:
                                        {
                                            var pp = readByte();
                                            if (pp < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;
                                        }
                                        break;

                                    case 0xD2:  //SCC1
                                        {
                                            var pp = readByte();
                                            if (pp < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;

                                            deferredWriteSCC(pp, aa, dd);
                                        }
                                        break;

                                    case int cmd when 0xD3 <= cmd && cmd <= 0xD6:
                                        {
                                            var pp = readByte();
                                            if (pp < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;
                                        }
                                        break;

                                    case 0xE0: //Seek to offset in PCM databank
                                        uint offset = vgmReader.ReadUInt32();
                                        dacOffset = (int)offset;
                                        break;

                                    case int cmd when 0xE1 <= cmd && cmd <= 0xFF:
                                        {
                                            var mm = readByte();
                                            if (mm < 0)
                                                break;
                                            var ll = readByte();
                                            if (ll < 0)
                                                break;
                                            var aa = readByte();
                                            if (aa < 0)
                                                break;
                                            var dd = readByte();
                                            if (dd < 0)
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }

                                //if (_wait != 0)
                                //    _wait -= 1;

                            }
                            else
                            {
                                break;
                            }
                            if ((command == 0x66 || command == -1))
                            {
                                flushDeferredWriteData();
                                if ((!LoopByCount || (LoopByCount && LoopedCount >= 0 && CurrentLoopedCount >= LoopedCount))
                                    && !LoopByElapsed)
                                {
                                    break;
                                }
                                if (LoopedCount >= 0)
                                    CurrentLoopedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                        flushDeferredWriteData();
                        if ((!LoopByCount || (LoopByCount && LoopedCount >= 0 && CurrentLoopedCount >= LoopedCount))
                            && !LoopByElapsed)
                        {
                            break;
                        }
                        if (LoopedCount >= 0)
                            CurrentLoopedCount++;
                        vgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                        looped = true;
                    }

                    if (streamWaitDelta < vgmWaitDelta)
                    {
                        if (streamWaitDelta <= 0)
                        {
                            wait += vgmWaitDelta;
                            vgmWaitDelta = 0;
                        }
                        else
                        {
                            wait += streamWaitDelta;
                            vgmWaitDelta -= streamWaitDelta;
                            streamWaitDelta = 0;
                        }
                    }
                    else
                    {
                        if (vgmWaitDelta <= 0)
                        {
                            wait += streamWaitDelta;
                            streamWaitDelta = 0;
                        }
                        else
                        {
                            wait += vgmWaitDelta;
                            streamWaitDelta -= vgmWaitDelta;
                            vgmWaitDelta = 0;
                        }
                    }

                    if (wait <= 0)
                        continue;

                    //while (!IsDeferredDataFlushed()) ;

                    flushDeferredWriteData();

                    double pwait = wait / PlaybackSpeed;
                    //if (vgmHead.lngRate > 0)
                    //    pwait *= (double)vgmHead.lngRate / 60d;
                    double nextTime = dbefore + (pwait * ((double)freq / (double)(44.1 * 1000)));
                    QueryPerformanceCounter(out after);
                    if (after > nextTime)
                    {
                        NotifyProcessLoadOccurred();
                        switch (Program.Default.WaitAlg)
                        {
                            case 0: //Accurate
                                break;
                            case 1: //Wait
                                flushDeferredWriteDataAndWait();
                                QueryPerformanceCounter(out after);
                                nextTime = after;
                                break;
                        }
                    }
                    else
                    {
                        HighLoad = false;
                        while (after < nextTime)
                            QueryPerformanceCounter(out after);
                    }
                    wait = 0;
                    dbefore = nextTime;
                }
            }
            StopAllSounds(true);
            State = SoundState.Stopped;
            NotifyFinished();
        }

        private Dictionary<uint, bool>[] adpcmMemoryErased =
            new Dictionary<uint, bool>[]{
            new Dictionary<uint, bool>(), // ADPCM-A
            new Dictionary<uint, bool>() // ADPCM-B
        };

        /// <summary>
        /// Transfers ADPCM data to the YM2610 for a specific memory type.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="saddr"></param>
        /// <param name="romData"></param>
        /// <param name="memoryType"></param>
        /// <param name="memoryTypeName"></param>
        /// <returns></returns>
        private uint transferAdpcmDataForNeotron(uint size, uint saddr, byte[] romData, int memoryType, string memoryTypeName)
        {
            if (romData.Length == 0)
                return saddr;
            //return saddr;

            FormMain.TopForm.SetStatusText("YM2610: No ADPCM-" + memoryTypeName + " data to transfer.");
            FormMain.TopForm.SetStatusText("YM2610: Transferring ADPCM-" + memoryTypeName + "(" +
            (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
            " (" + size.ToString("x") + ")");

#if DEBUG
            Console.WriteLine("YM2610: Transferring ADPCM-" + memoryTypeName + "(" +
                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                " (" + size.ToString("x") + ")");
#endif
            uint chunkSize = 256;
            int lastPercentage = -1;
            for (uint oi = 0; oi < romData.Length; oi += chunkSize)
            {
                int percentage = (100 * (int)oi) / romData.Length;
                if (percentage != lastPercentage)
                {
                    FormMain.TopForm.SetStatusText("YM2610: Transferring ADPCM-" + memoryTypeName + "(" + percentage + "%)");
                    //fp.Percentage = percentage;
                    lastPercentage = percentage;
                }

                uint sz = Math.Min(chunkSize, (uint)romData.Length - oi);
                byte[] chunkData = new byte[chunkSize];
                Array.Copy(romData, oi, chunkData, 0, sz);

                // ここでchunkを使って処理を行う
                bool erase = false;
                if (!adpcmMemoryErased[memoryType - 1].ContainsKey((saddr >> 16)))
                {
                    adpcmMemoryErased[memoryType - 1][(saddr >> 16)] = true;
                    erase = true;
#if DEBUG
                    Console.WriteLine("YM2610: Erase ADPCM-" + memoryTypeName + "(" +
                        (saddr & 0xff0000).ToString("x") + " - " + (((saddr & 0xff0000) + 0xffff)).ToString("x") +
                        " (" + size.ToString("x") + ")");
#endif
                }
                comPortOPNB.DataWriter.RawWrite(new byte[] { 0x19, (byte)memoryType, (byte)(saddr >> 16), (byte)(saddr >> 8),
                    (byte)(erase ? 1 : 0)}, null);
                comPortOPNB.DataWriter.RawWrite(chunkData, null);
                saddr += chunkSize;

                //if(erase)
                //    comPortOPNB.DataWriter.WaitAck();
                if (RequestedStat == SoundState.Stopped)
                    break;
            }

            FormMain.TopForm.SetStatusText("YM2610: Transferred ADPCM-" + memoryTypeName + "(" +
                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                " (" + size.ToString("x") + ")");

#if DEBUG
            Console.WriteLine("YM2610: Transferred ADPCM-" + memoryTypeName + "(" +
                (saddr).ToString("x") + " - " + ((saddr + size - 1)).ToString("x") +
                " (" + size.ToString("x") + ")");
#endif
            return saddr;
        }

        private void deferredWriteY8950(int adrs, int dt, uint dclk)
        {
            comPortY8950.RegTable[adrs] = dt;

            switch (adrs)
            {
                case var adrs2 when 0xa0 <= adrs && adrs <= 0xa8:
                    if (!ConvertChipClock || (double)comPortY8950.ChipClockHz["Y8950"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOplFrequency(comPortY8950.RegTable[adrs + 0x10], dt, comPortY8950.ChipClockHz["Y8950"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteY8950(adrs + 0x10, ret.Hi);
                        deferredWriteY8950(adrs, dt);
                    }
                    break;
                case var adrs2 when 0xb0 <= adrs && adrs <= 0xb8:
                    if (!ConvertChipClock || (double)comPortY8950.ChipClockHz["Y8950"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOplFrequency(dt, comPortY8950.RegTable[adrs - 0x10], comPortY8950.ChipClockHz["Y8950"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteY8950(adrs, dt);
                        deferredWriteY8950(adrs - 0x10, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteY8950(adrs, dt);
                    break;
            }
        }

        private int deferredWriteOPM(int adrs, int dt, uint dclk)
        {
            comPortOPM.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0x28:
                case 0x29:
                case 0x2a:
                case 0x2b:
                case 0x2c:
                case 0x2d:
                case 0x2e:
                case 0x2f:
                    if (!ConvertChipClock || (double)comPortOPM.ChipClockHz["OPM"] == (double)dclk)
                        goto default;
                    {
                        //KC
                        var ret = convertOpmFrequency(comPortOPM.RegTable[adrs + 8], dt, comPortOPM.ChipClockHz["OPM"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPM(adrs + 8, ret.Hi);
                        deferredWriteOPM(adrs, dt);
                    }
                    break;
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    if (!ConvertChipClock || (double)comPortOPM.ChipClockHz["OPM"] == (double)dclk)
                        goto default;
                    {
                        //KF
                        var ret = convertOpmFrequency(dt, comPortOPM.RegTable[adrs - 8], comPortOPM.ChipClockHz["OPM"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPM(adrs, dt);
                        deferredWriteOPM(adrs - 8, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPM(adrs, dt);
                    break;
            }

            return dt;
        }

        private void deferredWriteOPLL(uint dclk, int adrs, int dt)
        {
            comPortOPLL.RegTable[adrs] = dt;

            switch (adrs)
            {
                case var adrs2 when 0x10 <= adrs && adrs <= 0x18:
                    if (!ConvertChipClock || (double)comPortOPLL.ChipClockHz["OPLL"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpllFrequency(comPortOPLL.RegTable[adrs + 0x10], dt, comPortOPLL.ChipClockHz["OPLL"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPLL(adrs + 0x10, ret.Hi);
                        deferredWriteOPLL(adrs, dt);
                    }
                    break;
                case var adrs2 when 0x20 <= adrs && adrs <= 0x28:
                    if (!ConvertChipClock || (double)comPortOPLL.ChipClockHz["OPLL"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpllFrequency(dt, comPortOPLL.RegTable[adrs - 0x10], comPortOPLL.ChipClockHz["OPLL"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPLL(adrs, dt);
                        deferredWriteOPLL(adrs - 0x10, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPLL(adrs, dt);
                    break;
            }
        }

        private void deferredWriteSCC(int pp, int aa, int dd)
        {
            if (comPortSCC != null)
            {
                switch (comPortSCC.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        SCCType type = (SCCType)comPortSCC.Tag["SCC.Type"];
                        var slot = (int)comPortSCC.Tag["SCC.Slot"];
                        if ((int)slot < 0)
                            //自動選択方式
                            comPortSCC.DeferredWriteData(3, (byte)type,
                                (byte)(-((int)slot + 1)), (int)Program.Default.BitBangWaitSCC);
                        else
                            //従来方式
                            comPortSCC.DeferredWriteData(3, (byte)(type + 4),
                                (byte)slot, (int)Program.Default.BitBangWaitSCC);

                        switch (type)
                        {
                            case SCCType.SCC1:
                                switch (pp)
                                {
                                    case 0: //Wav
                                        break;
                                    case 1: //Freq
                                        aa += 0xa0;
                                        break;
                                    case 2: //Vol
                                        aa += 0xaa;
                                        break;
                                    case 3: //Ena
                                        aa = 0xaf;
                                        break;
                                    case 5: //
                                        aa += 0xe0;
                                        break;
                                    default:
                                        break;
                                }

                                comPortSCC.DeferredWriteData(4, (byte)aa, (byte)dd, (int)Program.Default.BitBangWaitSCC);
                                break;
                            case SCCType.SCC1_Compat:
                            case SCCType.SCC:
                                switch (pp)
                                {
                                    case 0: //Wav
                                        break;
                                    case 1: //Freq
                                            //Console.WriteLine("Freq " + aa / 2 + "ch: " + dd);
                                        aa += 0x80;
                                        break;
                                    case 2: //Vol
                                            //Console.WriteLine("Vol  " + aa / 2 + "ch: " + dd);
                                        aa += 0x8a;
                                        break;
                                    case 3: //Ena
                                            //Console.WriteLine("En   " + Convert.ToString((int)dd, 2));
                                        aa = 0x8f;
                                        break;
                                    case 5: //
                                            //Console.WriteLine("Mode " + aa / 2 + "ch: " + dd);
                                        aa += 0xe0;
                                        break;
                                    default:
                                        break;
                                }

                                comPortSCC.DeferredWriteData(5, (byte)aa, (byte)dd, (int)Program.Default.BitBangWaitSCC);
                                break;
                        }
                        break;
                }
            }
        }

        public void DeferredWritePCE(int adrs, int dt)
        {
            switch (comPortPCE.SoundModuleType)
            {
                case VsifSoundModuleType.TurboEverDrive:
                    comPortPCE.DeferredWriteData(0, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitPCE);
                    break;
            }
        }

        public void deferredWriteSAAReg(int adrs, byte dt, uint dclk)
        {
            if (comPortSAA == null)
                return;

            comPortSAA.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0x8:
                case 0x9:
                case 0xa:
                case 0xb:
                case 0xc:
                case 0xd:
                    //Freq
                    if (!ConvertChipClock || (double)comPortSAA.ChipClockHz["SAA1099"] == (double)dclk)
                        goto default;
                    {
                        var hadr = 0x10 + ((adrs - 8) >> 1);
                        var hi = 0;
                        int oct = 0;
                        if (comPortSAA.RegTable.ContainsKey(hadr))
                        {
                            hi = comPortSAA.RegTable[hadr];
                            if ((adrs & 1) == 0)
                            {
                                oct = hi & 0x7;
                                hi = comPortSAA.RegTable[hadr + 0x100] & 0x70;
                            }
                            else
                            {
                                oct = (hi >> 4) & 0x7;
                                hi = comPortSAA.RegTable[hadr + 0x100] & 0x7;
                            }
                        }

                        var ret = converSAA1099Frequency(oct, dt, comPortSAA.ChipClockHz["SAA1099"], dclk);
                        if (ret.noConverted)
                            goto default;

                        //if (adrs == 0x8 && oct != 0)
                        //    Debug.WriteLine($"0L: {oct} {dt} -> {ret.Hi} {ret.Lo}");

                        if ((adrs & 1) == 0)
                            hi = hi | ret.Hi;
                        else
                            hi = hi | (ret.Hi << 4);

                        switch (comPortSAA.SoundModuleType)
                        {
                            case VsifSoundModuleType.MSX_FTDI:
                            case VsifSoundModuleType.TurboR_FTDI:
                            case VsifSoundModuleType.MSX_Pi:
                            case VsifSoundModuleType.MSX_PiTR:
                                comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)adrs, (int)Program.Default.BitBangWaitSAA);
                                comPortSAA.DeferredWriteData(0x1e, 0x4, (byte)ret.Lo, (int)Program.Default.BitBangWaitSAA);

                                comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)hadr, (int)Program.Default.BitBangWaitSAA);
                                comPortSAA.DeferredWriteData(0x1e, 0x4, (byte)hi, (int)Program.Default.BitBangWaitSAA);

                                comPortSAA.RegTable[adrs + 0x100] = (byte)ret.Lo;
                                comPortSAA.RegTable[hadr + 0x100] = (byte)hi;
                                break;
                        }
                        writeVgmLogFile(
                            0xbd, (byte)adrs, ret.Lo,
                            0xbd, (byte)hadr, (byte)hi);
                    }
                    break;
                case 0x10:
                case 0x11:
                case 0x12:
                    //Oct
                    if (!ConvertChipClock || (double)comPortSAA.ChipClockHz["SAA1099"] == (double)dclk)
                        goto default;
                    {
                        var ladr0 = 0x8 + ((adrs & 0xf) << 1);
                        var lo0 = 0;
                        if (comPortSAA.RegTable.ContainsKey(ladr0))
                            lo0 = comPortSAA.RegTable[ladr0];
                        var oct0 = dt & 0x7;
                        var ret0 = converSAA1099Frequency(oct0, lo0, comPortSAA.ChipClockHz["SAA1099"], dclk);
                        //if (adrs == 0x10)
                        //    Debug.WriteLine($"0H: {oct0} {lo0} -> {ret0.Hi} {ret0.Lo}");

                        var ladr1 = ladr0 + 1;
                        var lo1 = 0;
                        if (comPortSAA.RegTable.ContainsKey(ladr1))
                            lo1 = comPortSAA.RegTable[ladr1];
                        var oct1 = (dt >> 4) & 0x7;
                        var ret1 = converSAA1099Frequency(oct1, lo1, comPortSAA.ChipClockHz["SAA1099"], dclk);
                        //if (adrs == 0x10)
                        //    Debug.WriteLine($"1H: {oct1} {lo1} -> {ret1.Hi} {ret1.Lo}");

                        if (ret0.noConverted && ret1.noConverted)
                            goto default;

                        switch (comPortSAA.SoundModuleType)
                        {
                            case VsifSoundModuleType.MSX_FTDI:
                            case VsifSoundModuleType.TurboR_FTDI:
                            case VsifSoundModuleType.MSX_Pi:
                            case VsifSoundModuleType.MSX_PiTR:
                                comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)ladr0, (int)Program.Default.BitBangWaitSAA);
                                comPortSAA.DeferredWriteData(0x1e, 0x4, (byte)ret0.Lo, (int)Program.Default.BitBangWaitSAA);
                                Debug.WriteLine($"Data: \t{ladr0} {ret0.Lo}");

                                comPortSAA.RegTable[ladr0 + 0x100] = (byte)ret0.Lo;

                                comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)ladr1, (int)Program.Default.BitBangWaitSAA);
                                comPortSAA.DeferredWriteData(0x1e, 0x4, (byte)ret1.Lo, (int)Program.Default.BitBangWaitSAA);
                                Debug.WriteLine($"Data: \t{ladr1} {ret1.Lo}");

                                comPortSAA.RegTable[ladr1 + 0x100] = (byte)ret1.Lo;

                                comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)adrs, (int)Program.Default.BitBangWaitSAA);
                                comPortSAA.DeferredWriteData(0x1e, 0x4, (byte)((ret1.Hi << 4) | ret0.Hi), (int)Program.Default.BitBangWaitSAA);
                                Debug.WriteLine($"Data: \t{adrs} {((ret1.Hi << 4) | ret0.Hi)}");

                                comPortSAA.RegTable[adrs + 0x100] = (byte)((ret1.Hi << 4) | ret0.Hi);
                                break;
                        }

                        writeVgmLogFile(
                            0xbd, (byte)ladr0, (byte)ret0.Lo,
                            0xbd, (byte)ladr1, (byte)ret1.Lo,
                            0xbd, (byte)adrs, (byte)((ret1.Hi << 4) | ret0.Hi));

                    }
                    break;
                default:
                    switch (comPortSAA.SoundModuleType)
                    {
                        case VsifSoundModuleType.MSX_FTDI:
                        case VsifSoundModuleType.TurboR_FTDI:
                        case VsifSoundModuleType.MSX_Pi:
                        case VsifSoundModuleType.MSX_PiTR:
                            comPortSAA.DeferredWriteData(0x1e, 0x5, (byte)adrs, (int)Program.Default.BitBangWaitSAA);
                            comPortSAA.DeferredWriteData(0x1e, 0x4, dt, (int)Program.Default.BitBangWaitSAA);
                            break;
                    }

                    writeVgmLogFile(0xbd, (byte)adrs, dt);

                    break;
            }


        }

        public void DeferredWritePWMReg(int adrs, byte dt)
        {
            if (comPortPWM == null)
                return;

            switch (comPortPWM.SoundModuleType)
            {
                case VsifSoundModuleType.Genesis_FTDI:

                    sendPwmData((byte)adrs, dt, (int)Program.Default.BitBangWaitPWM, true);
                    break;
            }
        }

        private void sendPwmData(byte addressAndData, byte data, int f_ftdiClkWidth, bool wait)
        {
            comPortPWM.DeferredWriteData(
                new byte[] { 0, 0 },
                new byte[] { 6 << 2, 7 << 2 },
                new byte[] { (byte)addressAndData, (byte)data },
                f_ftdiClkWidth);
        }


        public void DeferredWriteMCDReg(int adrs, byte dt)
        {
            if (comPortMCD == null)
                return;
            if (0 <= adrs && adrs <= 8)
            {
                switch (comPortMCD.SoundModuleType)
                {
                    case VsifSoundModuleType.Genesis_FTDI:
                        adrs = ((adrs << 1) + 1);
                        sendMcdData((ushort)adrs, dt, (int)Program.Default.BitBangWaitMCD, true);
                        break;
                }
            }
        }

        public void DeferredWriteMCDMem(int adrs, byte dt)
        {
            if (comPortMCD == null)
                return;

            switch (comPortMCD.SoundModuleType)
            {
                case VsifSoundModuleType.Genesis_FTDI:

                    adrs = (ushort)((0x2000 + (adrs << 1)) + 1);
                    sendMcdData((ushort)adrs, dt, (int)Program.Default.BitBangWaitMCD, false);
                    break;
            }
        }

        private byte mcdClk = 0x80;

        private void sendMcdData(ushort address, byte data, int f_ftdiClkWidth, bool wait)
        {
            comPortMCD.DeferredWriteData(0, 10 << 2, data, f_ftdiClkWidth);
            comPortMCD.DeferredWriteData(0, 9 << 2, (byte)(address & 0xff), f_ftdiClkWidth);
            comPortMCD.DeferredWriteData(0, 8 << 2, (byte)(((address >> 8) & 0x3f) | mcdClk), f_ftdiClkWidth);

            mcdClk ^= 0x80;

            if (wait)
            {
                comPortMCD.DeferredWriteData(0xff, 0, 0, f_ftdiClkWidth);
                comPortMCD.DeferredWriteData(0xff, 0, 0, f_ftdiClkWidth);
                comPortMCD.DeferredWriteData(0xff, 0, 0, f_ftdiClkWidth);
            }
        }

        public void DeferredWriteNES(int adrs, int dt, uint dclk)
        {
            if (0x20 <= adrs && adrs <= 0x3e)
            {
                adrs += 0x60;
            }
            else if (0x3f == adrs)
            {
                adrs = 0x23;
            }
            comPortNES.RegTable[adrs] = dt;

            switch (adrs)
            {
                //LO
                case 2:
                case 6:
                case 0xA:
                    if (!ConvertChipClock || (double)comPortNES.ChipClockHz["NES"] == (double)dclk)
                        goto default;
                    {
                        var ret = convertNesFrequency(comPortNES.RegTable[adrs + 1] & 0x7, dt, comPortNES.ChipClockHz["NES"], dclk);
                        if (ret.noConverted)
                            goto default;
                        DeferredWriteNES(adrs, ret.Lo);
                        DeferredWriteNES(adrs + 1, (comPortNES.RegTable[adrs + 1] & 0xf8) | ret.Hi);
                    }
                    break;
                //HI
                case 3:
                case 7:
                case 0xB:
                    if (!ConvertChipClock || (double)comPortNES.ChipClockHz["NES"] == (double)dclk)
                        goto default;
                    {
                        var ret = convertNesFrequency(dt & 0x7, comPortNES.RegTable[adrs - 1], comPortNES.ChipClockHz["NES"], dclk);
                        if (ret.noConverted)
                            goto default;
                        DeferredWriteNES(adrs - 1, ret.Lo);
                        DeferredWriteNES(adrs, (dt & 0xf8) | ret.Hi);
                    }
                    break;
                case 0x10:
                    {
                        //int dpcmFreq = (int)comPortNES.ChipClockHz["NES"] / dpcmFreqTable[dt & 0xf];
                        //nesDpcm.SetSampleRate(dpcmFreq);
                        //nesDpcm.SetLoopEnable((dt & 0x40) == 0x40);
                    }
                    //goto default;
                    break;
                case 0x12:
                    //nesDpcm.SetLoopStart((int)dt << 6);
                    //goto default;
                    break;
                case 0x13:
                    //nesDpcm.SetLoopLength((int)dt << 4);
                    //goto default;
                    break;
                case 0x15:
                    if ((dt & 0x10) != 0)
                    {
                        if (!nesDpcm.IsPlaying())
                        {
                            int dpcmFreq = (int)comPortNES.ChipClockHz["NES"] / dpcmFreqTable[comPortNES.RegTable[0x10] & 0xf];

                            if (dpcmData != null)
                            {
                                nesDpcm.Play(dpcmFreq, dpcmData,
                                    (int)comPortNES.RegTable[0x12] << 6,
                                    (int)comPortNES.RegTable[0x13] << 4,
                                    (comPortNES.RegTable[0x10] & 0x40) == 0x40);
                            }
                        }
                        dt -= 0x10;
                    }
                    else
                    {
                        nesDpcm.Stop();
                    }
                    goto default;
                default:
                    DeferredWriteNES(adrs, dt);
                    break;
            }
        }

        private void deferredWriteY8910(int adrs, int dt, uint dclk, bool? secondChip = false)
        {
            bool sc = secondChip.Value;
            int adrsTable = adrs;
            if (sc)
                adrsTable += 0x100;
            if (adrs == 7)
                dt &= 0x3f;
            comPortY8910.RegTable[adrsTable] = dt;

            switch (adrs)
            {
                case 0:
                case 2:
                case 4:
                    if (!ConvertChipClock || (double)comPortY8910.ChipClockHz["Y8910"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910Frequency(comPortY8910.RegTable[adrsTable + 1], dt, comPortY8910.ChipClockHz["Y8910"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteY8910(adrs, dt, sc);
                        deferredWriteY8910(adrs + 1, ret.Hi, sc);
                    }
                    break;
                case 1:
                case 3:
                case 5:
                    if (!ConvertChipClock || (double)comPortY8910.ChipClockHz["Y8910"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910Frequency(dt, comPortY8910.RegTable[adrsTable - 1], comPortY8910.ChipClockHz["Y8910"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteY8910(adrs - 1, ret.Lo, sc);
                        deferredWriteY8910(adrs, dt, sc);
                    }
                    break;
                case 6:
                    if (!ConvertChipClock || (double)comPortY8910.ChipClockHz["Y8910"] == (double)dclk)
                        goto default;
                    {
                        var data = (int)Math.Round(dt * (dclk) / (double)comPortY8910.ChipClockHz["Y8910"]);
                        if (data > 32)
                            data = 32;
                        deferredWriteY8910(adrs, (byte)data, sc);
                    }
                    break;
                case 0xB:
                    if (!ConvertChipClock || (double)comPortY8910.ChipClockHz["Y8910"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910EnvFrequency(comPortY8910.RegTable[adrsTable + 1], dt, comPortY8910.ChipClockHz["Y8910"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteY8910(adrs, dt, sc);
                        deferredWriteY8910(adrs + 1, ret.Hi, sc);
                    }
                    break;
                case 0xC:
                    if (!ConvertChipClock || (double)comPortY8910.ChipClockHz["Y8910"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910EnvFrequency(dt, comPortY8910.RegTable[adrsTable - 1], comPortY8910.ChipClockHz["Y8910"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteY8910(adrs - 1, ret.Lo, sc);
                        deferredWriteY8910(adrs, dt, sc);
                    }
                    break;
                default:
                    deferredWriteY8910(adrs, dt, sc);
                    break;
            }
        }

        protected void deferredWriteY8910(int adrs, int dt, bool secondChip)
        {
            switch (comPortY8910.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    if (secondChip)
                    {
                        comPortY8910.DeferredWriteData(0x1e, (byte)0x10, (byte)adrs, (int)Program.Default.BitBangWaitAY8910);
                        comPortY8910.DeferredWriteData(0x1e, (byte)0x11, (byte)dt, (int)Program.Default.BitBangWaitAY8910);
                    }
                    else
                    {
                        comPortY8910.DeferredWriteData(0x17, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitAY8910);
                    }
                    break;
            }
        }

        private void deferredWriteDCSG(int data, uint dclk, bool? secondChip = false)
        {
            byte adrs = (byte)(data >> 4);
            bool sc = secondChip.Value;
            if ((data & 0x80) != 0)
                comPortDCSG.Tag["Last1stAddress." + sc] = adrs;
            else if (comPortDCSG.Tag.ContainsKey("Last1stAddress." + sc))
                adrs = (byte)((byte)comPortDCSG.Tag["Last1stAddress." + sc] & 0x7);

            if (comPortDCSG != null)
            {
                switch (adrs)
                {
                    case 0:
                    case 2:
                    case 4:
                        if (!ConvertChipClock || (double)comPortDCSG.ChipClockHz["DCSG"] == (double)dclk)
                            goto default;
                        {
                            comPortDCSG.RegTable[adrs] = data & 0x3f;
                            //HI
                            var ret = convertDcsgFrequency(data & 0x3f, comPortDCSG.RegTable[adrs + 0x8] & 0xf, comPortDCSG.ChipClockHz["DCSG"], dclk);
                            if (ret.noConverted)
                                goto default;
                            deferredWriteDCSG((0x80 | (adrs << 4)) | ret.Lo, sc);
                            deferredWriteDCSG(ret.Hi, sc);
                        }
                        break;
                    case 0x8:
                    case 0x8 + 2:
                    case 0x8 + 4:
                        if (!ConvertChipClock || (double)comPortDCSG.ChipClockHz["DCSG"] == (double)dclk)
                            goto default;
                        {
                            comPortDCSG.RegTable[adrs] = data & 0xf;
                            //LO
                            var ret = convertDcsgFrequency(comPortDCSG.RegTable[adrs - 0x8] & 0x3f, data & 0xf, comPortDCSG.ChipClockHz["DCSG"], dclk);
                            if (ret.noConverted)
                                goto default;
                            deferredWriteDCSG((adrs << 4) + ret.Lo, sc);
                            deferredWriteDCSG(ret.Hi, sc);
                        }
                        break;
                    default:
                        comPortDCSG.RegTable[adrs] = data;
                        deferredWriteDCSG(data, sc);
                        break;
                }
            }
        }

        protected void deferredWriteDCSG(int data, bool secondChip)
        {
            switch (comPortDCSG.SoundModuleType)
            {
                case VsifSoundModuleType.Genesis_FTDI:
                case VsifSoundModuleType.Genesis:
                case VsifSoundModuleType.Genesis_Low:
                    if (!secondChip)
                        comPortDCSG.DeferredWriteData(0, 5 << 2, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.SMS:
                    if (!secondChip)
                        comPortDCSG.DeferredWriteData(0, 0xFF, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.SMS_FTDI:
                    if (!secondChip)
                        comPortDCSG.DeferredWriteData(0, 0x00, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    if (!secondChip)
                        comPortDCSG.DeferredWriteData(0xF, 0, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    if (!secondChip)
                        comPortDCSG.DeferredWriteData(0x50, 0xFF, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    else
                        comPortDCSG.DeferredWriteData(0x30, 0xFF, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
            }
        }

        int lastDacValue;

        public void DeferredWriteNESDAC(int dt)
        {
            if (lastDacValue == dt)
                return;

            //DAC
            comPortNES.DeferredWriteData(1, (byte)0x11, (byte)dt, (int)Program.Default.BitBangWaitNES);

            lastDacValue = dt;
        }

        public void DeferredWriteNES(int adrs, int dt)
        {
            switch (comPortNES.SoundModuleType)
            {
                case VsifSoundModuleType.NES_FTDI_INDIRECT:
                    {
                        if (adrs == 0x11)
                        {
                            //DAC
                            comPortNES.DeferredWriteData(1, (byte)0x11, (byte)dt, (int)Program.Default.BitBangWaitNES);
                        }
                        else
                        {
                            switch (adrs)
                            {
                                case int cmd when 0x0 <= adrs && adrs <= 0x15:
                                    comPortNES.DeferredWriteData(0, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitNES);
                                    break;
                                case 36 + 1:
                                case 36 + 3:
                                    comPortNES.DeferredWriteData(0, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitNES);
                                    break;

                                    //case int cmd when 0x9000 <= adrs && adrs <= 0x9003:
                                    //    comPortNES.DeferredWriteData(0, (byte)(24 + (cmd & 0x03)), (byte)dt, (int)Program.Default.BitBangWaitNES);
                                    //    break;
                                    //case int cmd when 0xa000 <= adrs && adrs <= 0xa003:
                                    //    comPortNES.DeferredWriteData(0, (byte)(28 + (cmd & 0x03)), (byte)dt, (int)Program.Default.BitBangWaitNES);
                                    //    break;
                                    //case int cmd when 0xb000 <= adrs && adrs <= 0xb003:
                                    //    comPortNES.DeferredWriteData(0, (byte)(32 + (cmd & 0x03)), (byte)dt, (int)Program.Default.BitBangWaitNES);
                                    //    break;
                            }
                        }
                    }
                    break;
                case VsifSoundModuleType.NES_FTDI_DIRECT:
                    if (adrs == 0x11)
                    {
                        //DAC
                        comPortNES.DeferredWriteData(1, (byte)0x11, (byte)dt, (int)Program.Default.BitBangWaitNES);
                    }
                    else
                        comPortNES.DeferredWriteData(0, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitNES);
                    break;
            }
        }

        protected void deferredWriteOPM(int adrs, int dt)
        {
            switch (comPortOPM.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                    comPortOPM.DeferredWriteData(0xe, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPM);
                    break;
                case VsifSoundModuleType.TurboR_FTDI:
                    comPortOPM.DeferredWriteData(0x2e, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPM);
                    break;
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPM.DeferredWriteData(0x2e, (byte)adrs, (byte)dt, -2);
                    break;
                case VsifSoundModuleType.SpfmLight:
                case VsifSoundModuleType.Spfm:
                    comPortOPM.DeferredWriteData(0x10, (byte)adrs, (byte)dt, 0);
                    break;
                case VsifSoundModuleType.Gimic:
                    comPortOPM.WriteData(0x2, (byte)adrs, (byte)dt, 0);
                    break;
            }
        }

        protected void deferredWriteOPLL(int adrs, int dt)
        {
            switch (comPortOPLL.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                    {
                        int wait = (int)Program.Default.BitBangWaitOPLL;
                        if (comPortOPLL.SoundModuleType == VsifSoundModuleType.TurboR_FTDI)
                            wait *= 4;
                        var slot = (int)comPortOPLL.Tag["OPLL.Slot"];
                        if (slot == 1 || slot == 2)
                            comPortOPLL.DeferredWriteData(2, (byte)0, (byte)(slot - 1), wait);
                        if ((int)comPortOPLL.Tag["OPLL.Slot"] == 0)
                            comPortOPLL.DeferredWriteData(1, (byte)adrs, (byte)dt, wait);
                        else
                            comPortOPLL.DeferredWriteData(0xC, (byte)adrs, (byte)dt, wait);
                    }
                    break;
                case VsifSoundModuleType.SMS:
                    comPortOPLL.DeferredWriteData(0, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPLL);
                    break;
                case VsifSoundModuleType.SMS_FTDI:
                    comPortOPLL.DeferredWriteData(0, (byte)(adrs + 1), (byte)dt, (int)Program.Default.BitBangWaitOPLL);
                    break;
                case VsifSoundModuleType.MSX_Pi:
                    {
                        int wait = 0;

                        var slot = (int)comPortOPLL.Tag["OPLL.Slot"];
                        if (slot == 1 || slot == 2)
                            comPortOPLL.DeferredWriteData(2, (byte)0, (byte)(slot - 1), wait);
                        if ((int)comPortOPLL.Tag["OPLL.Slot"] == 0)
                            comPortOPLL.DeferredWriteData(1, (byte)adrs, (byte)dt, wait);
                        else
                            comPortOPLL.DeferredWriteData(0xC, (byte)adrs, (byte)dt, wait);
                    }
                    break;
                case VsifSoundModuleType.MSX_PiTR:
                    {
                        int wait = -2;

                        var slot = (int)comPortOPLL.Tag["OPLL.Slot"];
                        if (slot == 1 || slot == 2)
                            comPortOPLL.DeferredWriteData(0x22, (byte)0, (byte)(slot - 1), wait);
                        if ((int)comPortOPLL.Tag["OPLL.Slot"] == 0)
                            comPortOPLL.DeferredWriteData(0x21, (byte)adrs, (byte)dt, wait);
                        else
                            comPortOPLL.DeferredWriteData(0x2C, (byte)adrs, (byte)dt, wait);
                    }
                    break;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferData"></param>
        /// <param name="saddr"></param>
        /// <param name="fp"></param>
        private void sendAdpcmDataY8950(byte[] transferData, int saddr, FormProgress fp)
        {
            if (transferData.Length == 0)
                return;

            int slot = 0;
            if (comPortY8950 != null)
            {
                switch ((int)comPortY8950.Tag["Y8950.Slot"])
                {
                    case 0:
                        slot = 0;
                        break;
                    case 1:
                        slot = 9;
                        break;
                }
            }

            //http://ngs.no.coocan.jp/doc/wiki.cgi/datapack?page=4%2E5+Y8950%28MSX%2DAUDIO%29

            int wait = (int)Program.Default.BitBangWaitY8950;

            //$07レジスタリセット
            Y8950WriteData(comPortY8950, 0x07, 0, slot, 0, 0, (byte)0x01, false, wait);

            //各フラグをイネーブルにする。
            Y8950WriteData(comPortY8950, 0x04, 0, slot, 0, 0, 0x00, false, wait);
            //各フラグをリセット。
            Y8950WriteData(comPortY8950, 0x04, 0, slot, 0, 0, 0x80, false, wait);

            //メモリライトモードにする。
            Y8950WriteData(comPortY8950, 0x07, 0, slot, 0, 0, 0x60, false, wait);

            if (comPortY8950 != null)
            {
                //switch ((int)comPortY8950.Tag["Y8950.Slot"])
                //{
                //    case 0:
                //    case 2:
                //        //RAMタイプの指定。64Kbit
                //        YMF262WriteData(comPortY8950, 0x08, 0, slot, 0, 0, 0x02);

                //        if ((saddr & 0b1000) == 0b1000)
                //            saddr += 0b1000;
                //        if ((saddr & 0b1_0000_0000_0000) == 0b1_0000_0000_0000)
                //            saddr += 0b1_0000_0000_0000;

                //        //START
                //        YMF262WriteData(comPortY8950, 0x09, 0, slot, 0, 0, (byte)((saddr >> 5) & 0xff));
                //        YMF262WriteData(comPortY8950, 0x0a, 0, slot, 0, 0, (byte)((saddr >> (5 + 8)) & 0xff));
                //        //STOP
                //        YMF262WriteData(comPortY8950, 0x0b, 0, slot, 0, 0, (byte)(0xff - 0b1000));
                //        YMF262WriteData(comPortY8950, 0x0c, 0, slot, 0, 0, (byte)(0xff - 0b10000));
                //        break;
                //    case 1:
                //    case 3:
                //        //RAMタイプの指定。256Kbit
                //        YMF262WriteData(comPortY8950, 0x08, 0, slot, 0, 0, 0x00);
                //        //START
                //        YMF262WriteData(comPortY8950, 0x09, 0, slot, 0, 0, (byte)((saddr >> 5) & 0xff));
                //        YMF262WriteData(comPortY8950, 0x0a, 0, slot, 0, 0, (byte)((saddr >> (5 + 8)) & 0xff));
                //        //STOP
                //        YMF262WriteData(comPortY8950, 0x0b, 0, slot, 0, 0, (byte)0xff);
                //        YMF262WriteData(comPortY8950, 0x0c, 0, slot, 0, 0, (byte)0xff);
                //        break;
                //}
                int eaddr = saddr + transferData.Length - 1;
                saddr = saddr >> 2;
                eaddr = eaddr >> 2;

                if (y8950_adpcmbit64k)
                {
                    //メモリのタイプ指定。64Kbit
                    Y8950WriteData(comPortY8950, 0x08, 0, slot, 0, 0, 0x02, false, wait);

                    if ((saddr & 0b1000) == 0b1000)
                        saddr += 0b1000;
                    if ((saddr & 0b1_0000_0000_0000) == 0b1_0000_0000_0000)
                        saddr += 0b1_0000_0000_0000;
                    if ((eaddr & 0b1000) == 0b1000)
                        eaddr += 0b1000;
                    if ((eaddr & 0b1_0000_0000_0000) == 0b1_0000_0000_0000)
                        eaddr += 0b1_0000_0000_0000;

                    //START
                    Y8950WriteData(comPortY8950, 0x09, 0, slot, 0, 0, (byte)(saddr & 0xff), false, wait);
                    Y8950WriteData(comPortY8950, 0x0a, 0, slot, 0, 0, (byte)((saddr >> 8) & 0xff), false, wait);
                    //STOP
                    Y8950WriteData(comPortY8950, 0x0b, 0, slot, 0, 0, (byte)(eaddr & 0xff), false, wait);
                    Y8950WriteData(comPortY8950, 0x0c, 0, slot, 0, 0, (byte)((eaddr >> 8) & 0xff), false, wait);
                }
                else
                {
                    //メモリのタイプ指定。256Kbit
                    Y8950WriteData(comPortY8950, 0x08, 0, slot, 0, 0, 0x00, false, wait);
                    //START
                    Y8950WriteData(comPortY8950, 0x09, 0, slot, 0, 0, (byte)(saddr & 0xff), false, wait);
                    Y8950WriteData(comPortY8950, 0x0a, 0, slot, 0, 0, (byte)((saddr >> 8) & 0xff), false, wait);
                    //STOP
                    Y8950WriteData(comPortY8950, 0x0b, 0, slot, 0, 0, (byte)(eaddr & 0xff), false, wait);
                    Y8950WriteData(comPortY8950, 0x0c, 0, slot, 0, 0, (byte)((eaddr >> 8) & 0xff), false, wait);
                }
            }

            //Transfer
            int address = saddr;

            int len = transferData.Length;
            int index = 0;
            int percentage = 0;
            int lastPercentage = -1;
            for (int i = 0; i < transferData.Length; i++)
            {
                Y8950WriteData(comPortY8950, 0x0f, 0, slot, 0, 0, (byte)transferData[i], true, wait);

                //HACK: WAIT
                switch (comPortY8950?.SoundModuleType)
                {
                    case VsifSoundModuleType.Spfm:
                    case VsifSoundModuleType.SpfmLight:
                    case VsifSoundModuleType.Gimic:
                        comPortY8950?.FlushDeferredWriteDataAndWait();
                        break;
                }

                percentage = (100 * index) / len;
                if (percentage != lastPercentage)
                {
                    FormMain.TopForm.SetStatusText("Y8950: Transferring ADPCM(" + percentage + "%)");
                    //fp.Percentage = percentage;
                    comPortY8950?.FlushDeferredWriteDataAndWait();
                }
                lastPercentage = percentage;
                index++;

                address++;
                if (comPortY8950 != null)
                {
                    //switch ((int)comPortY8950.Tag["Y8950.Slot"])
                    //{
                    //    case 0:
                    //    case 2:
                    //        //RAMタイプの指定。64Kbit
                    //        if ((endAddress & 0b1000) == 0b1000)
                    //            endAddress += 0b1000;
                    //        if ((endAddress & 0b1_0000_0000_0000) == 0b1_0000_0000_0000)
                    //            endAddress += 0b1_0000_0000_0000;
                    //        break;
                    //    case 1:
                    //    case 3:
                    //        break;
                    //}
                    //if (y8950_adpcmbit64k)
                    //{
                    //    if ((endAddress & 0b1000) == 0b1000)
                    //        endAddress += 0b1000;
                    //    if ((endAddress & 0b1_0000_0000_0000) == 0b1_0000_0000_0000)
                    //        endAddress += 0b1_0000_0000_0000;
                    //}
                }
                if (RequestedStat == SoundState.Stopped)
                    break;
                else updateStatusForDataTransfer();
            }
            FormMain.TopForm.SetStatusText("Y8950: Transferred ADPCM");

            //○リセット
            Y8950WriteData(comPortY8950, 0x04, 0, slot, 0, 0, (byte)0x80, false, wait);
            //$07レジスタリセット
            Y8950WriteData(comPortY8950, 0x07, 0, slot, 0, 0, (byte)0x01, false, wait);
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteY8950(int adrs, int dt)
        {
            switch (comPortOPLL.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    {
                        int ytype = (int)comPortY8950.Tag["Y8950.Slot"];
                        switch (ytype)
                        {
                            case 0:
                                comPortY8950.DeferredWriteData(10, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitY8950);
                                break;
                            case 1:
                                comPortY8950.DeferredWriteData(11, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitY8950);
                                break;
                        }
                        break;
                    }
            }
        }

        private void deferredWriteOPL3_P0(int adrs, int dt, uint dclk)
        {
            comPortOPL3.RegTable[adrs] = dt;

            switch (adrs)
            {
                case var adrs2 when 0xa0 <= adrs && adrs <= 0xa8:
                    if (!ConvertChipClock || (double)comPortOPL3.ChipClockHz["OPL3"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOplFrequency(comPortOPL3.RegTable[adrs + 0x10], dt, comPortOPL3.ChipClockHz["OPL3"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPL3_P0(adrs + 0x10, ret.Hi);
                        deferredWriteOPL3_P0(adrs, dt);
                    }
                    break;
                case var adrs2 when 0xb0 <= adrs && adrs <= 0xb8:
                    if (!ConvertChipClock || (double)comPortOPL3.ChipClockHz["OPL3"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOplFrequency(dt, comPortOPL3.RegTable[adrs - 0x10], comPortOPL3.ChipClockHz["OPL3"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPL3_P0(adrs, dt);
                        deferredWriteOPL3_P0(adrs - 0x10, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPL3_P0(adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteOPL3_P0(int adrs, int dt)
        {
            switch (comPortOPL3.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    if (adrs != 0x10)   //ignore for Y8950 simulation
                        comPortOPL3.DeferredWriteData(10, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPL3);
                    break;
            }
        }

        private void deferredWriteOPL3_P1(int adrs, int dt, uint dclk)
        {
            comPortOPL3.RegTable[adrs + 0x100] = dt;

            switch (adrs)
            {
                case var adrs2 when 0xa0 <= adrs && adrs <= 0xa8:
                    if (!ConvertChipClock || (double)comPortOPL3.ChipClockHz["OPL3"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOplFrequency(comPortOPL3.RegTable[adrs + 0x10 + 0x100], dt, comPortOPL3.ChipClockHz["OPL3"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPL3_P1(adrs + 0x10, ret.Hi);
                        deferredWriteOPL3_P1(adrs, dt);
                    }
                    break;
                case var adrs2 when 0xb0 <= adrs && adrs <= 0xb8:
                    if (!ConvertChipClock || (double)comPortOPL3.ChipClockHz["OPL3"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOplFrequency(dt, comPortOPL3.RegTable[adrs - 0x10 + 0x100], comPortOPL3.ChipClockHz["OPL3"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPL3_P1(adrs, dt);
                        deferredWriteOPL3_P1(adrs - 0x10, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPL3_P1(adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteOPL3_P1(int adrs, int dt)
        {
            switch (comPortOPL3.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPL3.DeferredWriteData(11, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPL3);
                    break;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        /// <param name="dclk"></param>
        private void deferredWriteOPN_P0(int adrs, int dt, uint dclk)
        {
            if (adrs == 7)
                dt &= 0x3f;
            comPortOPN.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0:
                case 2:
                case 4:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910Frequency(comPortOPN.RegTable[adrs + 1], dt, comPortOPN.ChipClockHz["OPN_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPN_P0(adrs, dt);
                        deferredWriteOPN_P0(adrs + 1, ret.Hi);
                    }
                    break;
                case 1:
                case 3:
                case 5:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910Frequency(dt, comPortOPN.RegTable[adrs - 1], comPortOPN.ChipClockHz["OPN_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPN_P0(adrs - 1, ret.Lo);
                        deferredWriteOPN_P0(adrs, dt);
                    }
                    break;
                case 6:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN_SSG"] == (double)dclk)
                        goto default;
                    {
                        var data = (int)Math.Round(dt * (dclk) / (double)comPortOPN.ChipClockHz["OPN_SSG"]);
                        if (data > 32)
                            data = 32;
                        deferredWriteOPN_P0(adrs, (byte)data);
                    }
                    break;
                case 0xB:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910EnvFrequency(comPortOPN.RegTable[adrs + 1], dt, comPortOPN.ChipClockHz["OPN_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPN_P0(adrs, dt);
                        deferredWriteOPN_P0(adrs + 1, ret.Hi);
                    }
                    break;
                case 0xC:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910EnvFrequency(dt, comPortOPN.RegTable[adrs - 1], comPortOPN.ChipClockHz["OPN_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPN_P0(adrs - 1, ret.Lo);
                        deferredWriteOPN_P0(adrs, dt);
                    }
                    break;

                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPN.RegTable[adrs + 4], dt, comPortOPN.ChipClockHz["OPN"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPN_P0(adrs + 4, ret.Hi);
                        deferredWriteOPN_P0(adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPN.ChipClockHz["OPN"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPN.RegTable[adrs - 4], comPortOPN.ChipClockHz["OPN"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPN_P0(adrs, dt);
                        deferredWriteOPN_P0(adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPN_P0(adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteOPN_P0(int adrs, int dt)
        {
            switch (comPortOPN.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN.DeferredWriteData(0x12, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN);
                    break;
                case VsifSoundModuleType.PC88_FTDI:
                    if (0x10 <= adrs && adrs <= 0x1f)
                        comPortOPN.DeferredWriteData(0x02, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN);
                    else
                        comPortOPN.DeferredWriteData(0x00, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN);
                    break;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs2"></param>
        /// <param name="dt2"></param>
        private void writeY8950PcmAddressData(int adrs2, int dt2)
        {
            if (comPortY8950 != null)
            {
                switch (comPortOPLL.SoundModuleType)
                {
                    case VsifSoundModuleType.MSX_FTDI:
                    case VsifSoundModuleType.TurboR_FTDI:
                    case VsifSoundModuleType.MSX_Pi:
                    case VsifSoundModuleType.MSX_PiTR:
                        {
                            int ytype = (int)comPortY8950.Tag["Y8950.Slot"];
                            switch (ytype)
                            {
                                case 0:
                                    comPortY8950.DeferredWriteData(10, (byte)adrs2, (byte)dt2, (int)Program.Default.BitBangWaitY8950);
                                    break;
                                case 1:
                                    comPortY8950.DeferredWriteData(11, (byte)adrs2, (byte)dt2, (int)Program.Default.BitBangWaitY8950);
                                    break;
                            }
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void flushDeferredWriteData()
        {
            comPortDCSG?.FlushDeferredWriteData();
            comPortOPLL?.FlushDeferredWriteData();
            comPortOPN2?.FlushDeferredWriteData();
            comPortSCC?.FlushDeferredWriteData();
            comPortY8910?.FlushDeferredWriteData();
            comPortOPM?.FlushDeferredWriteData();
            comPortOPL3?.FlushDeferredWriteData();
            comPortOPNA?.FlushDeferredWriteData();
            comPortOPNB?.FlushDeferredWriteData();
            comPortY8950?.FlushDeferredWriteData();
            comPortOPN?.FlushDeferredWriteData();
            comPortNES?.FlushDeferredWriteData();
            comPortMCD?.FlushDeferredWriteData();
            comPortSAA?.FlushDeferredWriteData();
            comPortPCE?.FlushDeferredWriteData();
            comPortPWM?.FlushDeferredWriteData();
        }

        /// <summary>
        /// 
        /// </summary>
        private void flushDeferredWriteDataAndWait()
        {
            comPortDCSG?.FlushDeferredWriteDataAndWait();
            comPortOPLL?.FlushDeferredWriteDataAndWait();
            comPortOPN2?.FlushDeferredWriteDataAndWait();
            comPortSCC?.FlushDeferredWriteDataAndWait();
            comPortY8910?.FlushDeferredWriteDataAndWait();
            comPortOPM?.FlushDeferredWriteDataAndWait();
            comPortOPL3?.FlushDeferredWriteDataAndWait();
            comPortOPNA?.FlushDeferredWriteDataAndWait();
            comPortOPNB?.FlushDeferredWriteDataAndWait();
            comPortY8950?.FlushDeferredWriteDataAndWait();
            comPortOPN?.FlushDeferredWriteDataAndWait();
            comPortNES?.FlushDeferredWriteDataAndWait();
            comPortMCD?.FlushDeferredWriteDataAndWait();
            comPortSAA?.FlushDeferredWriteDataAndWait();
            comPortPCE?.FlushDeferredWriteDataAndWait();
            comPortPWM?.FlushDeferredWriteDataAndWait();
        }


        /// <summary>
        /// 
        /// </summary>
        private void abort()
        {
            comPortDCSG?.Abort();
            comPortOPLL?.Abort();
            comPortOPN2?.Abort();
            comPortSCC?.Abort();
            comPortY8910?.Abort();
            comPortOPM?.Abort();
            comPortOPL3?.Abort();
            comPortOPNA?.Abort();
            comPortOPNB?.Abort();
            comPortY8950?.Abort();
            comPortOPN?.Abort();
            comPortNES?.Abort();
            nesDpcm?.Stop();
            comPortMCD?.Abort();
            comPortSAA?.Abort();
            comPortPCE?.Abort();
            comPortPWM?.Abort();
        }

        private const int WAIT_TIMEOUT = 120 * 1000;


        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(SafeWaitHandle hTimer,
            [In] ref long pDueTime, int lPeriod,
            IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

        [DllImport("kernel32.dll")]
        internal static extern uint WaitForSingleObject(SafeWaitHandle hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemTimeAsFileTime(out long lpSystemTimeAsFileTime);

        private bool disposedValue;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)
                    Stop();
                }

                vgmReader?.Dispose();

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                comPortDCSG?.Dispose();
                comPortDCSG = null;
                comPortOPLL?.Dispose();
                comPortOPLL = null;
                comPortOPN2?.Dispose();
                comPortOPN2 = null;
                comPortSCC?.Dispose();
                comPortSCC = null;
                comPortY8910?.Dispose();
                comPortY8910 = null;
                comPortOPM?.Dispose();
                comPortOPM = null;
                comPortOPL3?.Dispose();
                comPortOPL3 = null;
                comPortOPNA?.Dispose();
                comPortOPNA = null;
                comPortOPNB?.Dispose();
                comPortOPNB = null;
                comPortY8950?.Dispose();
                comPortY8950 = null;
                comPortOPN?.Dispose();
                comPortOPN = null;
                segaPcm?.Dispose();
                segaPcm = null;
                k053260?.Dispose();
                k053260 = null;
                okim6295?.Dispose();
                okim6295 = null;
                opnbPcm?.Dispose();
                opnbPcm = null;
                foreach (var dt in streamTable.Values)
                    dt.DacStream?.Dispose();
                //dacStream?.Dispose();
                //dacStream = null;
                comPortNES?.Dispose();
                comPortNES = null;
                nesDpcm?.Dispose();
                nesDpcm = null;
                comPortMCD?.Dispose();
                comPortMCD = null;
                comPortSAA?.Dispose();
                comPortSAA = null;
                comPortPCE?.Dispose();
                comPortPCE = null;
                comPortPWM?.Dispose();
                comPortPWM = null;
#if DEBUG
                if (vgmLogFile != null)
                    writeVgmLogFile(true, 0x66);
                vgmLogFile?.Flush();
                vgmLogFile?.Dispose();
#endif
                // 大きなフィールドを null に設定します
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        // 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~VGMSong()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        uint oki6258_master_clock;

        uint oki6258_divider;

        uint oki6258_sample_rate;

        uint[] oki6258_clock_buffer = new uint[4];

        /// <summary>
        /// PCM値
        /// </summary>
        int pcmValueForEncode = 0;
        /// <summary>
        /// 予測変化量
        /// </summary>
        int predictValueForEncode = 127;
        /// <summary>
        /// 8bitデータの1つ目(上位4bit)かどうか
        /// </summary>
        bool firstDataForEncode = true;
        /// <summary>
        /// 実際のADPCM出力値
        /// </summary>
        byte outputValueForEncode = 0;

        private int? encodeAdpcm(int inputValue)
        {
            int? retValue = null;
            //https://www.piece-me.org/piece-lab/adpcm/adpcm2.html
            //変化量 <- PCM入力値 - PCM値
            int deltaValue = (inputValue * 256) - pcmValueForEncode;
            //ADPCM値
            int adpcmData = 0;
            //予測変化値に対する変化量の比率によってADPCM値を決定
            if (predictValueForEncode * 14 / 8 <= deltaValue)
                adpcmData = 7;
            else if (predictValueForEncode * 12 / 8 <= deltaValue && deltaValue < predictValueForEncode * 14 / 8)
                adpcmData = 6;
            else if (predictValueForEncode * 10 / 8 <= deltaValue && deltaValue < predictValueForEncode * 12 / 8)
                adpcmData = 5;
            else if (predictValueForEncode * 8 / 8 <= deltaValue && deltaValue < predictValueForEncode * 10 / 8)
                adpcmData = 4;
            else if (predictValueForEncode * 6 / 8 <= deltaValue && deltaValue < predictValueForEncode * 8 / 8)
                adpcmData = 3;
            else if (predictValueForEncode * 4 / 8 <= deltaValue && deltaValue < predictValueForEncode * 6 / 8)
                adpcmData = 2;
            else if (predictValueForEncode * 2 / 8 <= deltaValue && deltaValue < predictValueForEncode * 4 / 8)
                adpcmData = 1;
            else if (predictValueForEncode * 0 / 8 <= deltaValue && deltaValue < predictValueForEncode * 2 / 8)
                adpcmData = 0;
            else if (predictValueForEncode * -2 / 8 <= deltaValue && deltaValue < predictValueForEncode * 0 / 8)
                adpcmData = 8;
            else if (predictValueForEncode * -4 / 8 <= deltaValue && deltaValue < predictValueForEncode * -2 / 8)
                adpcmData = 9;
            else if (predictValueForEncode * -6 / 8 <= deltaValue && deltaValue < predictValueForEncode * -4 / 8)
                adpcmData = 10;
            else if (predictValueForEncode * -8 / 8 <= deltaValue && deltaValue < predictValueForEncode * -6 / 8)
                adpcmData = 11;
            else if (predictValueForEncode * -10 / 8 <= deltaValue && deltaValue < predictValueForEncode * -8 / 8)
                adpcmData = 12;
            else if (predictValueForEncode * -12 / 8 <= deltaValue && deltaValue < predictValueForEncode * -10 / 8)
                adpcmData = 13;
            else if (predictValueForEncode * -14 / 8 <= deltaValue && deltaValue < predictValueForEncode * -12 / 8)
                adpcmData = 14;
            else if (deltaValue < predictValueForEncode * -14 / 8)
                adpcmData = 15;

            //出力
            if (firstDataForEncode)
            {
                outputValueForEncode = (byte)(adpcmData << 4);
                firstDataForEncode = false;
            }
            else
            {
                outputValueForEncode |= (byte)adpcmData;
                //comPortOPNA.DeferredWriteData(0x13, (byte)0x8, outputValue, (int)Program.Default.BitBangWaitOPNA);
                retValue = outputValueForEncode;
                outputValueForEncode = 0;
                firstDataForEncode = true;
            }

            //変化率 = ADPCM値のbit2-0
            int factor = adpcmData & 0x7;
            //変化量 <- 予測変化量 x (変化率 x 2 + 1) / 8
            deltaValue = predictValueForEncode * (factor * 2 + 1) / 8;
            if ((adpcmData & 0x8) == 0)
            {
                //増加
                pcmValueForEncode = pcmValueForEncode + deltaValue;
            }
            else
            {
                //減少
                pcmValueForEncode = pcmValueForEncode - deltaValue;
            }
            //変化率によって次回の予測変化量を更新
            switch (factor)
            {
                case 0:
                    predictValueForEncode = predictValueForEncode * 57 / 64;
                    break;
                case 1:
                    predictValueForEncode = predictValueForEncode * 57 / 64;
                    break;
                case 2:
                    predictValueForEncode = predictValueForEncode * 57 / 64;
                    break;
                case 3:
                    predictValueForEncode = predictValueForEncode * 57 / 64;
                    break;
                case 4:
                    predictValueForEncode = predictValueForEncode * 77 / 64;
                    break;
                case 5:
                    predictValueForEncode = predictValueForEncode * 102 / 64;
                    break;
                case 6:
                    predictValueForEncode = predictValueForEncode * 128 / 64;
                    break;
                case 7:
                    predictValueForEncode = predictValueForEncode * 153 / 64;
                    break;
            }
            //
            if (predictValueForEncode < 127)
                predictValueForEncode = 127;
            else if (predictValueForEncode > 24576)
                predictValueForEncode = 24576;

            return retValue;
        }


        /// <summary>
        /// PCM値
        /// </summary>
        int pcmValueForDecode = 0;
        /// <summary>
        /// 予測変化量
        /// </summary>
        int predictValueForDecode = 127;

        private int decodeOpnaAdpcm(int inputValue)
        {
            int retValue = 0x0;

            //https://www.piece-me.org/piece-lab/adpcm/adpcm1.html
            //変化率 <- ADPCM値のbit2-0
            int deltaRatio = inputValue & 0x7;
            //変化量 <- 予測変化量*(変化率*2+1)/8
            int deltaValue = predictValueForDecode * ((deltaRatio * 2) + 1) / 8;
            if ((inputValue & 0x8) == 0)
            {
                //増加
                //PCM値 <- PCM値 + 変化量
                pcmValueForDecode = pcmValueForDecode + deltaValue;
            }
            else
            {
                //減少
                //PCM値 <- PCM値 - 変化量
                pcmValueForDecode = pcmValueForDecode - deltaValue;
            }
            pcmValueForDecode = pcmValueForDecode << 4;
            if (pcmValueForDecode > short.MaxValue)
                pcmValueForDecode = short.MaxValue;
            else if (pcmValueForDecode < short.MinValue)
                pcmValueForDecode = short.MinValue;

            retValue = pcmValueForDecode;

            //変化率によって、自戒の予測変化量を更新
            switch (deltaRatio)
            {
                case 0:
                    predictValueForDecode = predictValueForDecode * 57 / 64;
                    break;
                case 1:
                    predictValueForDecode = predictValueForDecode * 57 / 64;
                    break;
                case 2:
                    predictValueForDecode = predictValueForDecode * 57 / 64;
                    break;
                case 3:
                    predictValueForDecode = predictValueForDecode * 57 / 64;
                    break;
                case 4:
                    predictValueForDecode = predictValueForDecode * 77 / 64;
                    break;
                case 5:
                    predictValueForDecode = predictValueForDecode * 102 / 64;
                    break;
                case 6:
                    predictValueForDecode = predictValueForDecode * 128 / 64;
                    break;
                case 7:
                    predictValueForDecode = predictValueForDecode * 153 / 64;
                    break;
            }
            if (predictValueForDecode < 127)
            {
                predictValueForDecode = 127;
            }
            else if (predictValueForDecode > 24576)
            {
                predictValueForDecode = 24576;
            }

            return retValue;
        }


    }

    /// <summary>
    /// 
    /// </summary>
    public class StreamData
    {
        public int StreamID
        {
            get;
            private set;
        }

        public uint Frequency
        {
            get;
            set;
        }

        public DacStream DacStream
        {
            get;
            private set;
        }

        public StreamDataBank[] StreamDataBanks;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamID"></param>
        public StreamData(int streamID, DacStream dacStream)
        {
            StreamID = streamID;
            DacStream = dacStream;

            StreamDataBanks = new StreamDataBank[0x40];
            for (int i = 0; i < StreamDataBanks.Length; i++)
                StreamDataBanks[i] = new StreamDataBank();
        }
    }

    public class StreamDataBank
    {
        public int DataBankID
        {
            get;
            set;
        }

        public int StepSize
        {
            get;
            set;
        }

        public int StepBase
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public StreamDataBank()
        {
        }
    }

#pragma warning disable 0649, 0169

    /// <summary>
    /// 
    /// </summary>
    public class VGM_HEADER
    {
        public uint lngEOFOffset;
        public uint lngVersion;
        public uint lngHzDCSG;
        public uint lngHzYM2413;
        public uint lngGD3Offset;
        public uint lngTotalSamples;
        public uint lngLoopOffset;
        public uint lngLoopSamples;
        public uint lngRate;
        public ushort shtPSG_Feedback;
        public byte bytPSG_SRWidth;
        public byte bytPSG_Flags;
        public uint lngHzYM2612;
        public uint lngHzYM2151;
        public uint lngDataOffset;
        public uint lngHzSPCM;
        public uint lngSPCMIntf;
        public uint lngHzRF5C68;
        public uint lngHzYM2203;
        public uint lngHzYM2608;
        public uint lngHzYM2610;
        public uint lngHzYM3812;
        public uint lngHzYM3526;
        public uint lngHzY8950;
        public uint lngHzYMF262;
        public uint lngHzYMF278B;
        public uint lngHzYMF271;
        public uint lngHzYMZ280B;
        public uint lngHzRF5C164;
        public uint lngHzPWM;
        public uint lngHzAY8910;
        public byte bytAYType;
        public byte bytAYFlag;
        public byte bytAYFlagYM2203;
        public byte bytAYFlagYM2608;
        public byte bytVolumeModifier;
        public byte bytReserved2;
        public sbyte bytLoopBase;
        public byte bytLoopModifier;
        public uint lngHzGBDMG;
        public uint lngHzNESAPU;
        public uint lngHzMultiPCM;
        public uint lngHzUPD7759;
        public uint lngHzOKIM6258;
        public byte bytOKI6258Flags;
        public byte bytK054539Flags;
        public byte bytC140Type;
        public byte bytReservedFlags;
        public uint lngHzOKIM6295;
        public uint lngHzK051649;
        public uint lngHzK054539;
        public uint lngHzHuC6280;
        public uint lngHzC140;
        public uint lngHzK053260;
        public uint lngHzPokey;
        public uint lngHzQSound;
        public uint lngHzSCSP;
        public uint lngExtraOffset;
        public uint lngHzWSwan;
        public uint lngHzVSU;
        public uint lngHzSAA1099;
        public uint lngHzES5503;
        public uint lngHzES5506;
        public ushort shtESchns;
        public byte bytCD;
        public byte bytReservedFlags2;
        public uint lngHzX1_010;
        public uint lngHzC352;
        public uint lngHzGA20;
    }
#pragma warning restore 0649, 0169

}
