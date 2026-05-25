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
using zanac.MAmidiMEmo.Gimic;
using System.Xml.Linq;

//Sega Genesis VGM player. Player written and emulators ported by Landon Podbielski. 
namespace zanac.VGMPlayer
{
    public class XGMSong : SongBase
    {
        private VsifClient comPortDCSG;

        private VsifClient comPortOPN2;
        private VsifClient comPortOPNA;

        private VsifClient comPortTurboRProxy;

        private const uint FCC_VGM = 0x204D4758;    // 'XGM '

        private XGM_HEADER xgmMHead;

        private BinaryReader xgmReader;

        private byte[] dacData;

        private byte[] vgmData;

        private SampleData[] SampleDataTable = new SampleData[63];

        private class SampleData
        {
            public uint Address
            {
                get;
                private set;
            }

            public uint Size
            {
                get;
                private set;
            }

            private XGMSong xgmData;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="adress"></param>
            /// <param name="size"></param>
            public SampleData(XGMSong xgmData, uint adress, uint size)
            {
                Address = adress;
                Size = size;
                this.xgmData = xgmData;
            }

            private int[] index = new int[4];

            public void Restart(int ch)
            {
                index[ch] = 0;
            }

            public sbyte? GetDacData(int ch)
            {
                if (index[ch] >= Size)
                    return null;

                sbyte ret = (sbyte)xgmData.dacData[Address + index[ch]];
                index[ch]++;

                return ret;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        public XGMSong(string fileName) : base(fileName)
        {
            openVGMFile(fileName);
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
                comPortDCSG.ClearDeferredWriteData();

                switch (comPortDCSG?.SoundModuleType)
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
            if (comPortOPN2 != null)
            {
                if (comPortOPN2.SoundModuleType == VsifSoundModuleType.MSX_FTDI ||
                    comPortOPN2.SoundModuleType == VsifSoundModuleType.TurboR_FTDI ||
                    comPortOPN2.SoundModuleType == VsifSoundModuleType.MSX_Pi ||
                    comPortOPN2.SoundModuleType == VsifSoundModuleType.MSX_PiTR)
                {
                    // ALL KEY OFF
                    for (int i = 0; i < 3; i++)
                    {
                        deferredWriteOPN2_P0(0x28, i);
                        deferredWriteOPN2_P0(0x28, 0x4 | i);
                    }
                }

                if (volumeOff)
                {
                    //TL
                    for (int i = 0x40; i <= 0x8F; i++)
                        deferredWriteOPN2_P0(i, 0xFF);
                    for (int i = 0x40; i <= 0x8F; i++)
                        deferredWriteOPN2_P1(i, 0xFF);
                    //RR
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPN2_P0(i, 0xFF);
                    for (int i = 0x80; i <= 0x8F; i++)
                        deferredWriteOPN2_P1(i, 0xFF);
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
                        // ALL KEY OFF
                        for (int i = 0; i < 3; i++)
                        {
                            deferredWriteOPNA_P0(comPortOPNA, 0x28, i);
                            deferredWriteOPNA_P0(comPortOPNA, 0x28, 0x4 | i);
                        }
                        //RHYTHM
                        deferredWriteOPNA_P0(comPortOPNA, 0x10, 0x80);
                        //SSG
                        deferredWriteOPNA_P0(comPortOPNA, 0x07, 0xFF);
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
                    deferredWriteOPNA_P1(comPortOPNA, 0x00, 1);

                    EnablePseudoDacYM2608(comPortOPNA, false);
                }
            }

            pcmEngine.StopAll();

            flushDeferredWriteDataAndWait();
            Thread.Sleep(250);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteOPN2_P0(int adrs, int dt)
        {
            switch (comPortOPN2.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN2.DeferredWriteData(0x10, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                default:
                    //Genesis
                    comPortOPN2.DeferredWriteDataPrior(
                        new byte[] { 0, 0 },
                        new byte[] { 1 << 2, 2 << 2 },
                        new byte[] { (byte)adrs, (byte)dt },
                        (int)Program.Default.BitBangWaitOPN2);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        private void deferredWriteOPN2_P1(int adrs, int dt)
        {
            switch (comPortOPN2.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN2.DeferredWriteData(0x11, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                default: //Genesis
                    comPortOPN2.DeferredWriteDataPrior(
                        new byte[] { 0, 0 },
                        new byte[] { 3 << 2, 4 << 2 },
                        new byte[] { (byte)adrs, (byte)dt },
                        (int)Program.Default.BitBangWaitOPN2);
                    break;
            }
        }


        private void deferredWriteOPNA_P0(int adrs, int dt, uint dclk)
        {
            comPortOPNA.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPNA.RegTable[adrs + 4], dt, comPortOPNA.ChipClockHz["OPNA"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNA_P0(comPortOPNA, adrs + 4, ret.Hi);
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPNA.RegTable[adrs - 4], comPortOPNA.ChipClockHz["OPNA"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                        deferredWriteOPNA_P0(comPortOPNA, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                    break;
            }
        }

        private XGM_HEADER readXGMHeader(BinaryReader hFile)
        {
            XGM_HEADER curHead = new XGM_HEADER();
            FieldInfo[] fields = typeof(XGM_HEADER).GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(uint))
                {
                    uint val = hFile.ReadUInt32();
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(ushort))
                {
                    ushort val = hFile.ReadUInt16();
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(char))
                {
                    char val = hFile.ReadChar();
                    field.SetValue(curHead, val);
                }
                else if (field.FieldType == typeof(byte))
                {
                    byte val = hFile.ReadByte();
                    field.SetValue(curHead, val);
                }
            }

            return curHead;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool openVGMFile(string fileName)
        {
            //Read size
            using (FileStream vgmFile = File.Open(fileName, FileMode.Open))
            {
                var fileSize = (uint)vgmFile.Length;
                xgmReader = new BinaryReader(vgmFile);

                uint fccHeader;
                fccHeader = (uint)xgmReader.ReadUInt32();
                if (fccHeader != FCC_VGM)
                    return false;

                for (int i = 0; i < 63; i++)
                {
                    ushort adr = xgmReader.ReadUInt16();
                    ushort sz = xgmReader.ReadUInt16();
                    if (adr == 0xffff && sz == 0x1)
                        SampleDataTable[i] = new SampleData(this, 0, 0);
                    else
                        SampleDataTable[i] = new SampleData(this, (uint)(adr * 256), (uint)(sz * 256));
                }

                xgmMHead = readXGMHeader(xgmReader);

                //read dac
                dacData = xgmReader.ReadBytes(xgmMHead.shtSampleDataBlkSize * 256);

                //Music data bloc size.
                uint mlen = xgmReader.ReadUInt32();
                if (mlen < 0)
                    return false;

                //read vgm
                vgmData = xgmReader.ReadBytes((int)mlen);

                xgmReader = new BinaryReader(new MemoryStream(vgmData));

                if (Program.Default.OPN2_Enable)
                {
                    connectToOPN2();
                }
                else if (Program.Default.OPNA_Enable)
                {
                    if (connectToOPNA(7670453))
                    {
                        comPortOPNA.Tag["ProxyOPN2"] = true;
                        //Force OPN mode
                        deferredWriteOPNA_P0(comPortOPNA, 0x29, 0x80);
                        EnablePseudoDacYM2608(comPortOPNA, true);
                    }
                }
                if (Program.Default.DCSG_Enable)
                {
                    coonectToDCSG();
                }
                SongChipInformation += "OPN2@7.670453MHz ";
                SongChipInformation += "DCSG@3.579545MHz ";

                return true;
            }
        }

        private bool connectToOPNA(uint clock)
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
                                comPortOPNA.ChipClockHz["OPNA"] = 8 * 1000 * 1000;
                                UseChipInformation += "OPNA@8.000000MHz ";
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
                                    clock = GimicManager.SetClock(gimmic.OpnaIndex, clock);
                                    comPortOPNA.ChipClockHz["OPNA"] = clock;
                                    comPortOPNA.ChipClockHz["OPNA_SSG"] = clock;
                                    comPortOPNA.ChipClockHz["OPNA_org"] = clock;
                                    UseChipInformation += $"OPNA@{(double)clock / (double)1000000}MHz ";
                                }
                                else
                                {
                                    //HACK:
                                    if (gimmic.Opn3lIndex >= 0)
                                    {
                                        GimicManager.Reset(gimmic.Opn3lIndex);
                                        clock = GimicManager.SetClock(gimmic.Opn3lIndex, clock * 2);
                                        comPortOPNA.ChipClockHz["OPNA"] = clock;
                                        comPortOPNA.ChipClockHz["OPNA_SSG"] = clock;
                                        comPortOPNA.ChipClockHz["OPNA_org"] = clock;
                                        comPortOPNA.Tag["OPN3L"] = true;
                                        UseChipInformation += $"OPN3L@{(double)clock / (double)1000000}MHz ";
                                    }
                                    else
                                    {
                                        comPortOPNA?.Dispose();
                                        comPortOPNA = null;
                                    }
                                }
                            }
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
                                comPortOPNA.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPNA");
                            }
                            if (comPortTurboRProxy == null && comPortOPNA != null)
                            {
                                comPortTurboRProxy = comPortOPNA;
                                UseChipInformation += $"tR DAC ";
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
                                comPortOPNA.ChipClockHz["OPNA"] = 8 * 1000 * 1000;
                                UseChipInformation += "OPNA@8.000000MHz ";
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
                            }
                            if (comPortTurboRProxy == null && comPortOPNA != null)
                            {
                                comPortTurboRProxy = comPortOPNA;
                                UseChipInformation += $"tR DAC ";
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

        private bool connectToOPN2()
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
                                UseChipInformation += "OPN2@7.670453MHz ";
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
                                UseChipInformation += "OPN2@7.670453MHz ";
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
                                UseChipInformation += "OPN2@7.670453MHz ";
                                comPortOPN2.BitBangWait = typeof(Settings).GetProperty("BitBangWaitOPN2");
                            }
                            if (comPortTurboRProxy == null && comPortOPN2 != null)
                                comPortTurboRProxy = comPortOPN2;
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
                                UseChipInformation += "OPN2@7.670453MHz ";
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
                                UseChipInformation += "OPN2@7.670453MHz ";
                            }
                            if (comPortTurboRProxy == null && comPortOPN2 != null)
                                comPortTurboRProxy = comPortOPN2;
                        }
                        break;
                }
                if (comPortOPN2 != null)
                {
                    Accepted = true;

                    //LFO
                    deferredWriteOPN2_P0(0x22, 0x00);
                    //channel 3 mode
                    deferredWriteOPN2_P0(0x27, 0x00);
                    //DAC OFF
                    deferredWriteOPN2_P0(0x2B, 0x00);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPN2_P0(i, 0x00);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPN2_P0(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPN2_P0(i, 0x00);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPN2_P0(i, 0xC0);

                    for (int i = 0x30; i <= 0x3F; i++)
                        deferredWriteOPN2_P1(i, 0x00);
                    //for (int i = 0x40; i <= 0x4F; i++)
                    //    deferredWriteOPN2_P1(i, 0xff);
                    for (int i = 0x50; i <= 0xB3; i++)
                        deferredWriteOPN2_P1(i, 0x00);
                    for (int i = 0xB4; i <= 0xB6; i++)
                        deferredWriteOPN2_P1(i, 0xC0);

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
                                UseChipInformation += "DCSG@3.579545MHz ";
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

        private int readByte()
        {
            if (xgmReader.BaseStream == null)
                return -1;
            if (xgmReader.BaseStream.Position == xgmReader.BaseStream.Length)
                return -1;

            byte data = xgmReader.ReadByte();
            return data;
        }

        private int lastOPN2DacEn;

        protected override void StreamSong()
        {
            pcmEngine = new PcmEngine(this);
            pcmEngine.StartEngine();

            xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
            double wait = 0;
            {
                //bool firstKeyon = false;    //TODO: true
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
                        if (wait <= 0)
                        {

                            int command = readByte();
                            if (command != -1)
                            {
                                switch (command)
                                {
                                    case int cmd when 0x0 <= cmd && cmd <= 0x0F:
                                        {
                                            switch (xgmMHead.bytNTSC_PAL & 1)
                                            {
                                                case 0:
                                                    wait += 44100d / 60d;
                                                    break;
                                                case 1:
                                                    wait += 44100d / 50d;
                                                    break;
                                            }
                                            flushDeferredWriteData();
                                            break;
                                        }
                                    case int cmd when 0x10 <= cmd && cmd <= 0x1F:
                                        {
                                            uint dclk = 3579545;

                                            int size = cmd & 0xf;
                                            for (int i = 0; i <= size; i++)
                                            {
                                                var data = readByte();
                                                if (data < 0)
                                                    break;
                                                if (comPortDCSG != null)
                                                {
                                                    deferredWriteDCSG(data, dclk);
                                                }
                                            }
                                        }
                                        break;

                                    case int cmd when 0x20 <= cmd && cmd <= 0x2F: //YM2612 Write Port 0
                                        {
                                            uint dclk = 7670453;

                                            int size = cmd & 0xf;
                                            for (int i = 0; i <= size; i++)
                                            {
                                                var adrs = readByte();
                                                if (adrs < 0)
                                                    break;
                                                var dt = readByte();
                                                if (dt < 0)
                                                    break;

                                                //ignore test and unknown registers
                                                if (adrs < 0x30 && adrs != 0x22 && adrs != 0x27 && adrs != 0x28 && adrs != 0x2a && adrs != 0x2b)
                                                    break;
                                                if (adrs > 0xb6)
                                                    break;

                                                if (comPortOPN2 != null)
                                                {
                                                    switch (adrs)
                                                    {
                                                        case 0x2a:
                                                            //output DAC
                                                            DeferredWriteOPN2_DAC(comPortOPN2, dt);
                                                            break;
                                                        case 0x2b:
                                                            //Enable DAC
                                                            if (lastOPN2DacEn != (dt & 0x80))
                                                                deferredWriteOPN2_P0(adrs, dt, dclk);
                                                            lastOPN2DacEn = (dt & 0x80);
                                                            break;
                                                        default:
                                                            deferredWriteOPN2_P0(adrs, dt, dclk);
                                                            break;
                                                    }
                                                }
                                                else if (comPortOPNA != null)
                                                {
                                                    switch (adrs)
                                                    {
                                                        case 0x2a:
                                                            //output DAC
                                                            if (comPortTurboRProxy != null)
                                                            {
                                                                dt = (int)Math.Round((double)dt * (double)PcmMixer.DacVolume / 100d);
                                                                DeferredWriteTurboR_DAC(comPortTurboRProxy, dt);
                                                            }
                                                            else
                                                            {
                                                                dt = (int)Math.Round((double)dt * (double)PcmMixer.DacVolume / 100d);
                                                                DeferredWriteOPNA_PseudoDAC(comPortOPNA, dt);
                                                            }
                                                            break;
                                                        case 0x2b:
                                                            //Enable DAC
                                                            break;
                                                        default:
                                                            deferredWriteOPNA_P0(adrs, dt, dclk);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    case int cmd when 0x30 <= cmd && cmd <= 0x3F: //YM2612 Write Port 1
                                        {
                                            uint dclk = 7670453;

                                            int size = cmd & 0xf;
                                            for (int i = 0; i <= size; i++)
                                            {
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
                                                    deferredWriteOPN2_P1(adrs, dt, dclk);
                                                }
                                                else if (comPortOPNA != null)
                                                {
                                                    deferredWriteOPNA_P1(comPortOPNA, adrs, dt, dclk);
                                                    if (adrs == 0xb6)
                                                    {
                                                        deferredWriteOPNA_P1(comPortOPNA, 0x01, (byte)(dt & 0xC0));   //LR
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    case int cmd when 0x40 <= cmd && cmd <= 0x4F: //YM2612 Write Port 0
                                        {
                                            int size = cmd & 0xf;

                                            for (int i = 0; i <= size; i++)
                                            {
                                                var dt = readByte();
                                                if (dt < 0)
                                                    break;

                                                if (comPortOPN2 != null)
                                                {
                                                    deferredWriteOPN2_P0(0x28, dt);
                                                }
                                                else if (comPortOPNA != null)
                                                {
                                                    deferredWriteOPNA_P0(comPortOPNA, 0x28, dt);
                                                }
                                            }
                                        }
                                        break;

                                    case int cmd when 0x50 <= cmd && cmd <= 0x5F: //Play PCM
                                        {
                                            int ch = cmd & 0x3;

                                            var id = readByte();
                                            if (id < 0)
                                                break;

                                            if (id == 0)
                                                pcmEngine.Stop(ch);
                                            else
                                                pcmEngine.Play(ch, (uint)(id - 1));
                                        }
                                        break;
                                    case 0x7E:
                                        flushDeferredWriteData();
                                        int ofst1 = readByte();
                                        if (ofst1 < 0)
                                            xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                        int ofst2 = readByte();
                                        if (ofst2 < 0)
                                            xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                        int ofst3 = readByte();
                                        if (ofst3 < 0)
                                            xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);

                                        xgmReader.BaseStream?.Seek((int)(ofst1 | (ofst2 << 8) | (ofst3 << 16)), SeekOrigin.Begin);
                                        break;
                                    case 0x7F:
                                        flushDeferredWriteData();
                                        //End of song
                                        xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                                        break;
                                }
                            }
                            if ((command == 0x7E || command == 0x7F || command == -1))
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
                        xgmReader.BaseStream?.Seek(0, SeekOrigin.Begin);
                    }

                    if (wait <= 0)
                        continue;

                    flushDeferredWriteData();

                    double pwait = wait / PlaybackSpeed;
                    double nextTime = dbefore + (pwait * ((double)freq / (double)(44.1 * 1000)));
                    QueryPerformanceCounter(out after);
                    if (after > nextTime)
                    {
                        NotifyProcessLoadOccurred();
                        switch (Program.Default.WaitAlg)
                        {
                            case 0: //Accurate
                                break;
                            case 1: //Smooth
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

        private int deferredWriteOPN2_P1(int adrs, int dt, uint dclk)
        {
            comPortOPN2.RegTable[adrs + 0x100] = dt;

            switch (adrs)
            {
                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPN2.ChipClockHz["OPN2"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPN2.RegTable[adrs + 4 + 0x100], dt, comPortOPN2.ChipClockHz["OPN2"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPN2_P1(adrs + 4, ret.Hi);
                        deferredWriteOPN2_P1(adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPN2.ChipClockHz["OPN2"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPN2.RegTable[adrs - 4 + 0x100], comPortOPN2.ChipClockHz["OPN2"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPN2_P1(adrs, dt);
                        deferredWriteOPN2_P1(adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPN2_P1(adrs, dt);
                    break;
            }

            return dt;
        }

        private void deferredWriteOPN2_P0(int adrs, int dt, uint dclk)
        {
            comPortOPN2.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPN2.ChipClockHz["OPN2"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPN2.RegTable[adrs + 4], dt, comPortOPN2.ChipClockHz["OPN2"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPN2_P0(adrs + 4, ret.Hi);
                        deferredWriteOPN2_P0(adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPN2.ChipClockHz["OPN2"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPN2.RegTable[adrs - 4], comPortOPN2.ChipClockHz["OPN2"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPN2_P0(adrs, dt);
                        deferredWriteOPN2_P0(adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPN2_P0(adrs, dt);
                    break;
            }
            deferredWriteOPN2_P0(adrs, dt);
        }


        private void deferredWriteDCSG(int data, uint dclk)
        {
            byte adrs = (byte)(data >> 4);
            if ((data & 0x80) != 0)
                comPortDCSG.Tag["Last1stAddress"] = adrs;
            else if (comPortDCSG.Tag.ContainsKey("Last1stAddress"))
                adrs = (byte)((byte)comPortDCSG.Tag["Last1stAddress"] & 0x7);

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
                            var ret = convertDcsgFrequency(data & 0x3f, comPortDCSG.RegTable[adrs + 0x8], comPortDCSG.ChipClockHz["DCSG"], dclk);
                            if (ret.noConverted)
                                goto default;
                            deferredWriteDCSG((0x80 | (adrs << 4)) | ret.Lo);
                            deferredWriteDCSG(ret.Hi);
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
                            var ret = convertDcsgFrequency(comPortDCSG.RegTable[adrs - 0x8], data & 0xf, comPortDCSG.ChipClockHz["DCSG"], dclk);
                            if (ret.noConverted)
                                goto default;
                            deferredWriteDCSG((adrs << 4) + ret.Lo);
                            deferredWriteDCSG(ret.Hi);
                        }
                        break;
                    default:
                        comPortDCSG.RegTable[adrs] = data;
                        deferredWriteDCSG(data);
                        break;
                }
            }
        }

        protected void deferredWriteDCSG(int data)
        {
            switch (comPortDCSG.SoundModuleType)
            {
                case VsifSoundModuleType.Genesis_FTDI:
                case VsifSoundModuleType.Genesis:
                case VsifSoundModuleType.Genesis_Low:
                    comPortDCSG.DeferredWriteData(0, 5 << 2, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.SMS:
                    comPortDCSG.DeferredWriteData(0, 0xFF, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortDCSG.DeferredWriteData(0xF, 0, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortDCSG.DeferredWriteData(0x50, 0xFF, (byte)data, (int)Program.Default.BitBangWaitDCSG);
                    break;
            }
        }

        private void flushDeferredWriteData()
        {
            comPortOPN2?.FlushDeferredWriteData();
            comPortDCSG?.FlushDeferredWriteData();
            comPortOPNA?.FlushDeferredWriteData();
        }

        /// <summary>
        /// 
        /// </summary>
        private void flushDeferredWriteDataAndWait()
        {
            comPortDCSG?.FlushDeferredWriteDataAndWait();
            comPortOPN2?.FlushDeferredWriteDataAndWait();
            comPortOPNA?.FlushDeferredWriteDataAndWait();
        }

        /// <summary>
        /// 
        /// </summary>
        private void abort()
        {
            comPortDCSG?.Abort();
            comPortOPN2?.Abort();
            comPortOPNA?.Abort();
        }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)
                    Stop();
                }

                xgmReader?.Dispose();

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                comPortDCSG?.Dispose();
                comPortDCSG = null;
                comPortOPN2?.Dispose();
                comPortOPN2 = null;
                comPortOPNA?.Dispose();
                comPortOPNA = null;

                pcmEngine?.Dispose();
                pcmEngine = null;

                // 大きなフィールドを null に設定します
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        // 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~XGMSong()
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


        private PcmEngine pcmEngine;

        /// <summary>
        /// 
        /// </summary>
        private class PcmEngine : IDisposable
        {
            /// <summary>
            /// 
            /// </summary>
            public const int MAX_VOICE = 4;

            private object engineLockObject;

            private AutoResetEvent autoResetEvent;

            private bool stopEngineFlag;

            private bool disposedValue;

            private XGMSong xgmSong;

            private SampleData[] currentPlaySamples;

            /// <summary>
            /// 
            /// </summary>
            public PcmEngine(XGMSong xgmSong)
            {
                this.xgmSong = xgmSong;
                engineLockObject = new object();
                stopEngineFlag = true;
                autoResetEvent = new AutoResetEvent(false);
                currentPlaySamples = new SampleData[MAX_VOICE];
            }

            /// <summary>
            /// 
            /// </summary>
            public void StartEngine()
            {
                if (stopEngineFlag)
                {
                    stopEngineFlag = false;
                    Thread t = new Thread(processDac);
                    t.Priority = ThreadPriority.Highest;
                    t.Start();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public void StopEngine()
            {
                stopEngineFlag = true;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pcmData"></param>
            public void Play(int slot, uint id)
            {
                lock (engineLockObject)
                {
                    currentPlaySamples[slot] = xgmSong.SampleDataTable[id];
                    currentPlaySamples[slot].Restart(slot);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pcmData"></param>
            public void Stop(int slot)
            {
                lock (engineLockObject)
                {
                    currentPlaySamples[slot] = null;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pcmData"></param>
            public void StopAll()
            {
                lock (engineLockObject)
                {
                    for (int i = 0; i < currentPlaySamples.Length; i++)
                        currentPlaySamples[i] = null;
                }
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool QueryPerformanceFrequency(out long frequency);

            /// <summary>
            /// 
            /// </summary>
            private void processDac()
            {
                int overflowed = 0;
                uint sampleRate = 14000;

                long freq, before, after;
                double dbefore;
                QueryPerformanceFrequency(out freq);
                QueryPerformanceCounter(out before);
                dbefore = before;
                while (!stopEngineFlag)
                {
                    if (disposedValue)
                        break;

                    int dacData = 0;
                    bool playDac = false;
                    {
                        List<sbyte> data = new List<sbyte>();
                        lock (engineLockObject)
                        {
                            for (int i = 0; i < MAX_VOICE; i++)
                            {
                                var sd = currentPlaySamples[i];
                                if (sd == null)
                                    continue;

                                var d = sd.GetDacData(i);
                                if (d == null)
                                    continue;

                                data.Add(d.Value);
                                playDac = true;
                            }
                        }
                        dacData = (int)Math.Round(PcmMixer.Mix(data, PcmMixer.DacClipping));

                        if (playDac || overflowed != 0)
                        {
                            overflowed = 0;
                            if (dacData > sbyte.MaxValue)
                            {
                                dacData = sbyte.MaxValue;
                                xgmSong.NotifyDacClipOccurred();
                            }
                            else if (dacData < sbyte.MinValue)
                            {
                                dacData = sbyte.MinValue;
                                xgmSong.NotifyDacClipOccurred();
                            }
                            if (xgmSong.comPortOPN2 != null)
                            {
                                dacData += 0x80;
                                xgmSong.DeferredWriteOPN2_DAC(xgmSong.comPortOPN2, dacData);
                            }
                            else if (xgmSong.comPortOPNA != null)
                            {
                                dacData = (sbyte)Math.Round((double)dacData * (double)PcmMixer.DacVolume / 100d);
                                dacData += 0x80;
                                if (xgmSong.comPortTurboRProxy != null)
                                    xgmSong.DeferredWriteTurboR_DAC(xgmSong.comPortTurboRProxy, dacData);
                                else
                                    xgmSong.DeferredWriteOPNA_PseudoDAC(xgmSong.comPortOPNA, dacData);
                            }
                        }
                    }
                    QueryPerformanceCounter(out after);
                    double nextTime = dbefore + ((double)freq / (double)sampleRate);
                    while (after < nextTime)
                        QueryPerformanceCounter(out after);
                    dbefore = nextTime;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                        stopEngineFlag = true;

                        autoResetEvent?.Dispose();
                    }

                    // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                    // TODO: 大きなフィールドを null に設定します
                    disposedValue = true;
                }
            }

            // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
            // ~PcmEngine()
            // {
            //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

    }

#pragma warning disable 0649, 0169
    class XGM_HEADER
    {
        /// <summary>
        /// Sample data bloc size / 256, ex: $0010 means 256*16 = 4096 bytes
        /// </summary>
        public ushort shtSampleDataBlkSize;
        /// <summary>
        /// Version information (0x01 currently)    
        /// </summary>
        public byte bytVersion;
        /// <summary>
        ///  bit #0: NTSC / PAL information: 0=NTSC  1=PAL
        ///  This field is used to determine how interpret the frame wait command.
        ///  In NTSC mode a frame wait command is equivalent to 1/60 of second.
        ///  In PAL mode a frame wait command is equivalent to 1/50 of second.
        ///  bit #1: GD3 tags: 0=No 1=Yes
        ///  If present the tags are located right after music data (address = $108+SLEN+MLEN)
        ///  bit #2: Multi track file: 0=No 1=Yes
        ///  When we have a multi track file, next track data can be found at the end of current music data.
        ///  Next track data directly starts on 'Music data bloc size' field (MLEN) means samples are shared
        ///  between all tracks. Depending the presence of GD3 tags, the next music data are located at
        ///  ($108+SLEN+MLEN) or($108+SLEN+MLEN+ size of GD3 tags)
        /// </summary>
        public byte bytNTSC_PAL;
    }
#pragma warning restore 0649, 0169

}
