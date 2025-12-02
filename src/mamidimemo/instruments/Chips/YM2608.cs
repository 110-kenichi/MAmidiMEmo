// copyright-holders:K.Ito
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FM_SoundConvertor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;
using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Gui.FMEditor;
using zanac.MAmidiMEmo.Instruments.Envelopes;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.Scci;
using zanac.MAmidiMEmo.Gimic;
using zanac.MAmidiMEmo.VSIF;
using File = System.IO.File;
using Path = System.IO.Path;
using System.Diagnostics;
using static zanac.MAmidiMEmo.Instruments.Chips.RP2A03;
using System.Globalization;
using static zanac.MAmidiMEmo.Instruments.Chips.YM2612;
using System.Windows.Markup;
using zanac.MAmidiMEmo.Util;

//https://www.quarter-dev.info/archives/yamaha/YM2608_Applicatin_Manual.pdf

namespace zanac.MAmidiMEmo.Instruments.Chips
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    [InstLock]
    public class YM2608 : InstrumentBase
    {

        public override string Name => "YM2608";

        public override string Group => "FM";

        public override InstrumentType InstrumentType => InstrumentType.YM2608;

        [Browsable(false)]
        public override string ImageKey => "YM2608";

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected override string SoundInterfaceTagNamePrefix => "ym2608_";

        /// <summary>
        /// 
        /// </summary>
        [Category("MIDI")]
        [Description("MIDI Device ID")]
        [IgnoreDataMember]
        [JsonIgnore]
        public override uint DeviceID
        {
            get
            {
                return 23;
            }
        }

        private PortId portId = PortId.No1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set FTDI Port No for \"VSIF - MSX\"\r\n" +
            "See the manual about the VSIF.")]
        [DefaultValue(PortId.No1)]
        public PortId PortId
        {
            get
            {
                return portId;
            }
            set
            {
                if (portId != value)
                {
                    portId = value;
                    setSoundEngine(SoundEngine);
                }
            }
        }

        private object sndEnginePtrLock = new object();

        private IntPtr spfmPtr;

        private int gimicPtr = -1;

        private VsifClient vsifClient;

        private SoundEngineType f_SoundEngineType;

        private SoundEngineType f_CurrentSoundEngineType;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Select a sound engine type.\r\n" +
            "Supports Software and SPFM/VSIF/G.I.M.I.C .")]
        [DefaultValue(SoundEngineType.Software)]
        [TypeConverter(typeof(EnumConverterSoundEngineTypeSPFM))]
        public SoundEngineType SoundEngine
        {
            get
            {
                return f_SoundEngineType;
            }
            set
            {
                if (f_SoundEngineType != value)
                    setSoundEngine(value);
            }
        }

        private class EnumConverterSoundEngineTypeSPFM : EnumConverter<SoundEngineType>
        {
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var sc = new StandardValuesCollection(new SoundEngineType[] {
                    SoundEngineType.Software,
                    SoundEngineType.SPFM,
                    SoundEngineType.VSIF_MSX_FTDI,
                    SoundEngineType.VSIF_P6_FTDI,
                    SoundEngineType.GIMIC,
                    SoundEngineType.VSIF_PC88_FTDI,
                });

                return sc;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private void setSoundEngine(SoundEngineType value)
        {
            try
            {
                ignoreUpdatePcmData = true;
                AllSoundOff();
            }
            finally
            {
                ignoreUpdatePcmData = false;
            }

            lock (sndEnginePtrLock)
            {
                if (spfmPtr != IntPtr.Zero)
                {
                    ScciManager.ReleaseSoundChip(spfmPtr);
                    spfmPtr = IntPtr.Zero;
                }
                if (gimicPtr != -1)
                {
                    GimicManager.ReleaseModule(gimicPtr);
                    gimicPtr = -1;
                }
                if (vsifClient != null)
                {
                    vsifClient.Dispose();
                    vsifClient = null;
                }

                f_SoundEngineType = value;

                switch (f_SoundEngineType)
                {
                    case SoundEngineType.Software:
                        f_CurrentSoundEngineType = f_SoundEngineType;
                        SetDevicePassThru(false);
                        break;
                    case SoundEngineType.SPFM:
                        spfmPtr = ScciManager.TryGetSoundChip(SoundChipType.SC_TYPE_YM2608, (SC_CHIP_CLOCK)MasterClock);
                        if (spfmPtr != IntPtr.Zero)
                        {
                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        MasterClock = (int)MasterClockType.Default;
                        break;
                    case SoundEngineType.VSIF_MSX_FTDI:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 25)
                                    FtdiClkWidth = 25;
                            }
                            else
                            {
                                if (FtdiClkWidth < 32)
                                    FtdiClkWidth = 32;
                            }

                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        MasterClock = (int)MasterClockType.Default;
                        break;
                    case SoundEngineType.VSIF_P6_FTDI:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.P6_FTDI, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 25)
                                    FtdiClkWidth = 25;
                            }
                            else
                            {
                                if (FtdiClkWidth < 32)
                                    FtdiClkWidth = 32;
                            }

                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        MasterClock = (int)MasterClockType.Default;
                        break;
                    case SoundEngineType.VSIF_PC88_FTDI:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.PC88_FTDI, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 10)
                                    FtdiClkWidth = 10;
                            }
                            else
                            {
                                if (FtdiClkWidth < 20)
                                    FtdiClkWidth = 20;
                            }

                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        MasterClock = (int)MasterClockType.NEC;
                        break;
                    case SoundEngineType.GIMIC:
                        gimicPtr = GimicManager.GetModuleIndex(GimicManager.ChipType.CHIP_OPNA);
                        if (gimicPtr >= 0)
                        {
                            f_CurrentSoundEngineType = f_SoundEngineType;
                            f_MasterClock = GimicManager.SetClock(gimicPtr, f_MasterClock);
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            //HACK: OPN3L
                            gimicPtr = GimicManager.GetModuleIndex(GimicManager.ChipType.CHIP_OPN3L);
                            if (gimicPtr >= 0)
                            {
                                f_CurrentSoundEngineType = f_SoundEngineType;
                                f_MasterClock = GimicManager.SetClock(gimicPtr, f_MasterClock * 2);
                                SetDevicePassThru(true);
                            }
                            else
                            {
                                f_CurrentSoundEngineType = SoundEngineType.Software;
                                SetDevicePassThru(false);
                            }
                        }
                        MasterClock = (int)MasterClockType.Default;
                        break;
                    case SoundEngineType.VSIF_MSX_Pi:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi, PortId, false);
                        if (vsifClient != null)
                        {
                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        break;
                    case SoundEngineType.VSIF_MSX_PiTr:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR, PortId, false);
                        if (vsifClient != null)
                        {
                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        break;
                }
            }

            ClearWrittenDataCache();
            PrepareSound();
        }

        [Category("Chip(Dedicated)")]
        [Description("Current sound engine type.")]
        [DefaultValue(SoundEngineType.Software)]
        public SoundEngineType CurrentSoundEngine
        {
            get
            {
                return f_CurrentSoundEngineType;
            }
        }

        private BoardType f_PC88FMType = BoardType.Internal;

        [Category("Chip(Dedicated)")]
        [Description("Board type for PC-8801.")]
        [DefaultValue(BoardType.Internal)]
        public BoardType PC88BoardType
        {
            get
            {
                return f_PC88FMType;
            }
            set
            {
                if (f_PC88FMType != value)
                {
                    try
                    {
                        ignoreUpdatePcmData = true;
                        AllSoundOff();
                    }
                    finally
                    {
                        ignoreUpdatePcmData = false;
                    }

                    f_PC88FMType = value;

                    ClearWrittenDataCache();
                    PrepareSound();
                }
            }
        }

        private int f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_MSX_CLK_WIDTH;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [SlideParametersAttribute(1, 100)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("Set FTDI Clock Width[%].\r\n" +
            "MSX FT232R:25~\r\n" +
            "MSX FT232H:32~")]
        public int FtdiClkWidth
        {
            get
            {
                return f_ftdiClkWidth;
            }
            set
            {
                f_ftdiClkWidth = value;
            }
        }

        public bool ShouldSerializeFtdiClkWidth()
        {
            switch (f_SoundEngineType)
            {
                case SoundEngineType.VSIF_MSX_FTDI:
                case SoundEngineType.VSIF_P6_FTDI:
                    return f_ftdiClkWidth != VsifManager.FTDI_BAUDRATE_MSX_CLK_WIDTH;
            }
            return f_ftdiClkWidth != VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;
        }

        public void ResetFtdiClkWidth()
        {
            switch (f_SoundEngineType)
            {
                case SoundEngineType.VSIF_MSX_FTDI:
                case SoundEngineType.VSIF_P6_FTDI:
                    f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_MSX_CLK_WIDTH;
                    return;
            }
            f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;
        }

        /// <summary>
        /// 
        /// </summary>
        public enum MasterClockType : uint
        {
            Default = 8000000,
            NEC = 7987200,
        }

        private uint f_MasterClock;

        /// <summary>
        /// </summary>
        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set Master Clock of this chip")]
        [TypeConverter(typeof(EnumConverter<MasterClockType>))]
        public uint MasterClock
        {
            get
            {
                return f_MasterClock;
            }
            set
            {
                if (f_MasterClock != value)
                {
                    if (CurrentSoundEngine == SoundEngineType.GIMIC)
                        value = GimicManager.SetClock(gimicPtr, value);
                    f_MasterClock = value;
                    if (CurrentSoundEngine == SoundEngineType.SPFM)
                        setSoundEngine(f_SoundEngineType);
                    SetClock(UnitNumber, (uint)value);
                }
            }
        }


        public bool ShouldSerializeMasterClock()
        {
            return MasterClock != (uint)MasterClockType.Default;
        }

        public void ResetMasterClock()
        {
            MasterClock = (uint)MasterClockType.Default;
        }


        private bool pseudoDacMode;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("(Bonus feature)Pseudo DAC PCM mode for real chip. Exclusive to PCM-B.")]
        [DefaultValue(false)]
        public bool PseudoDacMode
        {
            get
            {
                lock (sndEnginePtrLock)
                {
                    if (CurrentSoundEngine == SoundEngineType.Software)
                        return false;
                }
                return pseudoDacMode;
            }
            set
            {
                if (pseudoDacMode != value)
                {
                    try
                    {
                        ignoreUpdatePcmData = true;
                        AllSoundOff();
                    }
                    finally
                    {
                        ignoreUpdatePcmData = false;
                    }

                    pseudoDacMode = value;

                    updatePcmData(null);
                }
            }
        }

        private bool f_EnableSmartDacClip;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Enabled Smart DAC Clip.")]
        [DefaultValue(false)]
        public bool EnableSmartDacClip
        {
            get
            {
                return f_EnableSmartDacClip;
            }
            set
            {
                if (f_EnableSmartDacClip != value)
                {
                    f_EnableSmartDacClip = value;
                }
            }
        }

        private byte f_EnvelopeFrequencyCoarse = 2;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("Chip(SSG)")]
        [Description("Set Envelope Coarse Frequency")]
        [SlideParametersAttribute(0, 255)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)2)]
        public byte EnvelopeFrequencyCoarse
        {
            get => f_EnvelopeFrequencyCoarse;
            set
            {
                if (f_EnvelopeFrequencyCoarse != value)
                {
                    f_EnvelopeFrequencyCoarse = value;
                    YM2608WriteData(UnitNumber, 0x0b, 0, 0, (byte)f_EnvelopeFrequencyCoarse);
                }
            }
        }

        private byte f_EnvelopeFrequencyFine;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("Chip(SSG)")]
        [Description("Set Envelope Fine Frequency")]
        [SlideParametersAttribute(0, 255)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)0)]
        public byte EnvelopeFrequencyFine
        {
            get => f_EnvelopeFrequencyFine;
            set
            {
                if (f_EnvelopeFrequencyFine != value)
                {
                    f_EnvelopeFrequencyFine = value;
                    YM2608WriteData(UnitNumber, 0x0c, 0, 0, (byte)f_EnvelopeFrequencyFine);
                }
            }
        }

        private byte f_EnvelopeType;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("Chip(SSG)")]
        [Description("Set Envelope Type (0-15)")]
        [SlideParametersAttribute(0, 15)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)0)]
        public byte EnvelopeType
        {
            get => f_EnvelopeType;
            set
            {
                byte v = (byte)(value & 15);
                if (f_EnvelopeType != v)
                {
                    f_EnvelopeType = v;
                    YM2608WriteData(UnitNumber, 0x0d, 0, 0, (byte)f_EnvelopeType);
                }
            }
        }

        private byte f_LFOEN;

        /// <summary>
        /// LFOEN (0-1)
        /// </summary>
        [DataMember]
        [Category("Chip(FM)")]
        [Description("LFO Enable (0:Off 1:Enable)")]
        [SlideParametersAttribute(0, 1)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)0)]
        public byte LFOEN
        {
            get
            {
                return f_LFOEN;
            }
            set
            {
                byte v = (byte)(value & 1);
                if (f_LFOEN != v)
                {
                    f_LFOEN = v;
                    YM2608WriteData(UnitNumber, 0x22, 0, 0, (byte)(LFOEN << 3 | LFRQ));
                }
            }
        }

        private byte f_LFRQ;

        /// <summary>
        /// LFRQ (0-7)
        /// </summary>
        [DataMember]
        [Category("Chip(FM)")]
        [Description("LFO Freq (0-7)\r\n" +
            "0:	3.82 Hz\r\n" +
            "1: 5.33 Hz\r\n" +
            "2: 5.77 Hz\r\n" +
            "3: 6.11 Hz\r\n" +
            "4: 6.60 Hz\r\n" +
            "5: 9.23 Hz\r\n" +
            "6: 46.11 Hz\r\n" +
            "7: 69.22 Hz\r\n")]
        [SlideParametersAttribute(0, 7)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)0)]
        public byte LFRQ
        {
            get
            {
                return f_LFRQ;
            }
            set
            {
                byte v = (byte)(value & 7);
                if (f_LFRQ != v)
                {
                    f_LFRQ = v;
                    YM2608WriteData(UnitNumber, 0x22, 0, 0, (byte)(LFOEN << 3 | LFRQ));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override TimbreBase[] BaseTimbres
        {
            get
            {
                return Timbres;
            }
            set
            {
                Timbres = (YM2608Timbre[])value;
            }
        }

        [DataMember]
        [Category(" Timbres")]
        [Description("Timbres")]
        [EditorAttribute(typeof(YM2608UITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public YM2608Timbre[] Timbres
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SerializeData", "SerializeDataSave", "SerializeDataLoad"></param>
        public override void RestoreFrom(string serializeData)
        {
            try
            {
                using (var obj = JsonConvert.DeserializeObject<YM2608>(serializeData))
                    this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
                YM2608SetCallbackB(UnitNumber, f_read_byte_b_callback);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;


                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_YM2608_write(uint unitNumber, uint address, byte data);


        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2608_write YM2608_write
        {
            get;
            set;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        internal override void DirectAccessToChip(uint address, uint data)
        {
            switch (address)
            {
                case 0x07:
                    data &= 0x3f;
                    break;
                case uint adr when 0x0e <= adr && adr <= 0x0f:
                    return;
                case 0x12:
                case 0x21:
                    return;
                case uint adr when 0x23 <= adr && adr <= 0x27:
                    return;
                case 0x29:
                    data &= 0x80;
                    break;
                case uint adr when 0x2a <= adr && adr <= 0x2f:
                    return;
            }

            /*
            uint port1 = 0;
            if (address >= 0x100)
                port1 = 2;
            */

            bool useCache = true;
            if (0x100 <= address && address <= 0x110)
                useCache = false;
            else if (0xa0 <= address && address < 0xb0)
                useCache = false;
            else if (0x1a0 <= address && address < 0x1b0)
                useCache = false;

            WriteData(address, data, useCache, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.SPFM:
                            ScciManager.SetRegister(spfmPtr, address, data, false);
                            break;
                        case SoundEngineType.VSIF_MSX_FTDI:
                        case SoundEngineType.VSIF_P6_FTDI:
                        case SoundEngineType.VSIF_MSX_Pi:
                            if (address < 0x100)
                                vsifClient.WriteData(0x10, (byte)address, (byte)data, f_ftdiClkWidth);
                            else
                            {
                                if (address == 0x10b)
                                    vsifClient.WriteData(0x13, (byte)0xb, (byte)data, f_ftdiClkWidth);
                                else
                                    vsifClient.WriteData(0x11, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                            }
                            break;
                        case SoundEngineType.VSIF_MSX_PiTr:
                            if (address < 0x100)
                                vsifClient.WriteData(0x30, (byte)address, (byte)data, -2);
                            else
                            {
                                if (address == 0x10b)
                                    vsifClient.WriteData(0x13, (byte)0xb, (byte)data, f_ftdiClkWidth);
                                else if (address <= 0x110)
                                    vsifClient.WriteData(0x31, (byte)(address & 0xff), (byte)data, -3);
                                else
                                    vsifClient.WriteData(0x31, (byte)(address & 0xff), (byte)data, -2);
                            }
                            break;
                        case SoundEngineType.VSIF_PC88_FTDI:
                            if (f_PC88FMType == BoardType.Internal)
                            {
                                if (address < 0x100)
                                {
                                    if (0x10 <= address && address <= 0x1f)
                                        vsifClient.WriteData(0x02, (byte)address, (byte)data, f_ftdiClkWidth);
                                    else
                                        vsifClient.WriteData(0x00, (byte)address, (byte)data, f_ftdiClkWidth);
                                }
                                else
                                {
                                    if (address == 0x108)
                                        vsifClient.WriteData(0x04, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                    else if (address == 0x10b)
                                        vsifClient.WriteData(0x03, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                    else
                                        vsifClient.WriteData(0x01, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                }
                            }
                            else
                            {
                                if (address < 0x100)
                                {
                                    if (0x10 <= address && address <= 0x1f)
                                        vsifClient.WriteData(0x0a, (byte)address, (byte)data, f_ftdiClkWidth);
                                    else
                                        vsifClient.WriteData(0x08, (byte)address, (byte)data, f_ftdiClkWidth);
                                }
                                else
                                {
                                    if (address == 0x108)
                                        vsifClient.WriteData(0x0c, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                    else if (address == 0x10b)
                                        vsifClient.WriteData(0x0b, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                    else
                                        vsifClient.WriteData(0x09, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                                }
                            }
                            break;
                        case SoundEngineType.GIMIC:
                            GimicManager.SetRegister2(gimicPtr, address, (byte)data);
                            break;
                    }
                }
                /* MEMO: レジスタ書き込みで ym2608_device::update_request(OPN->ST.device); が呼ばれクラッシュするかも
                DeferredWriteData(YM2608_write, UnitNumber, (uint)(port1 + 0), (byte)(address & 0xff));
                DeferredWriteData(YM2608_write, UnitNumber, (uint)(port1 + 1), (byte)data);
                //*/
            }));
        }

        private void YM2608WriteData(uint unitNumber, byte address, int op, int slot, byte data)
        {
            YM2608WriteData(unitNumber, address, op, slot, data, true);
        }
        /// <summary>
        /// 
        /// </summary>
        private void YM2608WriteData(uint unitNumber, byte address, int op, int slot, byte data, bool useCache)
        {
            YM2608WriteData(unitNumber, address, op, slot, data, useCache, false);
        }

        /// <summary>
        /// 
        /// </summary>
        private void YM2608WriteData(uint unitNumber, byte address, int op, int slot, byte data, bool useCache, bool internalOnly)
        {
            switch (op)
            {
                case 0:
                    op = 0;
                    break;
                case 1:
                    op = 2;
                    break;
                case 2:
                    op = 1;
                    break;
                case 3:
                    op = 3;
                    break;
            }

            uint reg = (uint)(slot / 3) << 8;
            uint adrs = (uint)(reg + address + (op * 4) + (slot % 3));

            WriteData(adrs, data, useCache, new Action(() =>
            {
                if (!internalOnly)
                {
                    lock (sndEnginePtrLock)
                    {
                        switch (CurrentSoundEngine)
                        {
                            case SoundEngineType.SPFM:
                                ScciManager.SetRegister(spfmPtr, adrs, data, false);
                                break;
                            case SoundEngineType.VSIF_MSX_FTDI:
                            case SoundEngineType.VSIF_P6_FTDI:
                            case SoundEngineType.VSIF_MSX_Pi:
                            case SoundEngineType.VSIF_MSX_PiTr:
                                if (adrs < 0x100)
                                    vsifClient.WriteData(0x10, (byte)adrs, (byte)data, f_ftdiClkWidth);
                                else
                                {
                                    if (adrs == 0x10b)
                                        vsifClient.WriteData(0x13, (byte)0xb, (byte)data, f_ftdiClkWidth);
                                    else
                                        vsifClient.WriteData(0x11, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                }
                                break;
                            case SoundEngineType.VSIF_PC88_FTDI:
                                if (f_PC88FMType == BoardType.Internal)
                                {
                                    if (adrs < 0x100)
                                    {
                                        if (0x10 <= adrs && adrs <= 0x1f)
                                            vsifClient.WriteData(0x02, (byte)adrs, (byte)data, f_ftdiClkWidth);
                                        else
                                            vsifClient.WriteData(0x00, (byte)adrs, (byte)data, f_ftdiClkWidth);
                                    }
                                    else
                                    {
                                        if (adrs == 0x108)
                                            vsifClient.WriteData(0x04, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                        else if (adrs == 0x10b)
                                            vsifClient.WriteData(0x03, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                        else
                                            vsifClient.WriteData(0x01, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                    }
                                }
                                else
                                {
                                    if (adrs < 0x100)
                                    {
                                        if (0x10 <= adrs && adrs <= 0x1f)
                                            vsifClient.WriteData(0x0a, (byte)adrs, (byte)data, f_ftdiClkWidth);
                                        else
                                            vsifClient.WriteData(0x08, (byte)adrs, (byte)data, f_ftdiClkWidth);
                                    }
                                    else
                                    {
                                        if (adrs == 0x108)
                                            vsifClient.WriteData(0x0c, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                        else if (adrs == 0x10b)
                                            vsifClient.WriteData(0x0b, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                        else
                                            vsifClient.WriteData(0x09, (byte)(adrs & 0xff), (byte)data, f_ftdiClkWidth);
                                    }
                                }
                                break;
                            case SoundEngineType.GIMIC:
                                GimicManager.SetRegister2(gimicPtr, adrs, data);
                                break;
                        }
                    }
                }

                uint yreg = (uint)(slot / 3) * 2;
                DeferredWriteData(YM2608_write, unitNumber, yreg + 0, (byte)(address + (op * 4) + (slot % 3)));
                DeferredWriteData(YM2608_write, unitNumber, yreg + 1, data);
                //FormMain.OutputDebugLog(this, "adr:" + (byte)(address + (op * 4) + (slot % 3)) + " dat:" + data);
                //try
                //{
                //    Program.SoundUpdating();
                //    YM2608_write(unitNumber, yreg + 0, (byte)(address + (op * 4) + (slot % 3)));
                //    YM2608_write(unitNumber, yreg + 1, data);
                //}
                //finally
                //{
                //    Program.SoundUpdated();
                //}
            }));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte delegate_YM2608_read(uint unitNumber, uint address);


        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2608_read YM2608_read
        {
            get;
            set;
        }


        /// <summary>
        /// 
        /// </summary>
        private byte YM2608ReadData(uint unitNumber, byte address, int op, int slot)
        {
            try
            {
                Program.SoundUpdating();
                FlushDeferredWriteData();

                uint reg = (uint)(slot / 3) << 9;
                uint adr = (uint)(reg + address + (op * 4) + (slot % 3));

                var wd = GetCachedWrittenData(adr);
                if (wd != null)
                    return (byte)wd.Value;

                if (CurrentSoundEngine == SoundEngineType.SPFM)
                {
                    lock (sndEnginePtrLock)
                        return (byte)ScciManager.GetWrittenRegisterData(spfmPtr, (uint)(reg + address + (op * 4) + (slot % 3)));
                }
                else if (CurrentSoundEngine == SoundEngineType.GIMIC)
                {
                    lock (sndEnginePtrLock)
                        return (byte)GimicManager.GetWrittenRegisterData(gimicPtr, (uint)(reg + address + (op * 4) + (slot % 3)));
                }
                else
                {
                    reg = (uint)(slot / 3) * 2;
                    YM2608_write(unitNumber, reg + 0, (byte)(address + (op * 4) + (slot % 3)));
                    return YM2608_read(unitNumber, reg + 1);
                }
            }
            finally
            {
                Program.SoundUpdated();
            }
        }


        private float f_GainLeft = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Gain Left ch. (0.0-*) of this Instrument")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0d, 10d, 0.1d)]
        public override float GainLeft
        {
            get
            {
                return f_GainLeft;
            }
            set
            {
                if (f_GainLeft != value)
                {
                    f_GainLeft = value;

                    SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 0, value);
                    SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 2, value);
                }
            }
        }

        private float f_GainRight = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Gain Right ch. (0.0-*) of this Instrument")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0d, 10d, 0.1d)]
        public override float GainRight
        {
            get
            {
                return f_GainRight;
            }
            set
            {
                if (f_GainRight != value)
                {
                    f_GainRight = value;

                    SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 1, value);
                    SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 3, value);
                }
            }
        }

        private const float DEFAULT_GAIN = 1.5f;

        public override bool ShouldSerializeGainLeft()
        {
            return GainLeft != DEFAULT_GAIN;
        }

        public override void ResetGainLeft()
        {
            GainLeft = DEFAULT_GAIN;
        }

        public override bool ShouldSerializeGainRight()
        {
            return GainRight != DEFAULT_GAIN;
        }

        public override void ResetGainRight()
        {
            GainRight = DEFAULT_GAIN;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte delg_adpcm_callback(byte pn, int pos);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_adpcm_callback(uint unitNumber, delg_adpcm_callback callback);

        /// <summary>
        /// 
        /// </summary>
        private static void YM2608SetCallbackA(uint unitNumber, delg_adpcm_callback callback)
        {
            try
            {
                Program.SoundUpdating();
                set_callbacka(unitNumber, callback);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void YM2608SetCallbackB(uint unitNumber, delg_adpcm_callback callback)
        {
            try
            {
                Program.SoundUpdating();
                set_callbackb(unitNumber, callback);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_set_adpcm_callback set_callbacka
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_set_adpcm_callback set_callbackb
        {
            get;
            set;
        }

        private Dictionary<int, byte[]> tmpPcmDataTable = new Dictionary<int, byte[]>();


        /// <summary>
        /// 
        /// </summary>
        static YM2608()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("ym2608_write");
            if (funcPtr != IntPtr.Zero)
            {
                YM2608_write = (delegate_YM2608_write)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2608_write));
            }
            funcPtr = MameIF.GetProcAddress("ym2608_read");
            if (funcPtr != IntPtr.Zero)
            {
                YM2608_read = (delegate_YM2608_read)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2608_read));
            }
            funcPtr = MameIF.GetProcAddress("ym2608_set_adpcma_callback");
            if (funcPtr != IntPtr.Zero)
                set_callbacka = (delegate_set_adpcm_callback)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_set_adpcm_callback));
            funcPtr = MameIF.GetProcAddress("ym2608_set_adpcmb_callback");
            if (funcPtr != IntPtr.Zero)
                set_callbackb = (delegate_set_adpcm_callback)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_set_adpcm_callback));
        }

        private delg_adpcm_callback f_read_byte_a_callback;

        private delg_adpcm_callback f_read_byte_b_callback;

        private YM2608SoundManager soundManager;

        private static byte[] ym2608_adpcm_rom;

        /// <summary>
        /// 
        /// </summary>
        public YM2608(uint unitNumber) : base(unitNumber)
        {
            SetDevicePassThru(false);

            MasterClock = (int)MasterClockType.Default;

            GainLeft = DEFAULT_GAIN;
            GainRight = DEFAULT_GAIN;

            Timbres = new YM2608Timbre[256];
            for (int i = 0; i < 256; i++)
                Timbres[i] = new YM2608Timbre();

            this.pcmEngine = new PcmEngine(this);
            this.pcmEngine.StartEngine();

            setPresetInstruments();

            this.soundManager = new YM2608SoundManager(this);

            if (ym2608_adpcm_rom == null)
            {
                try
                {
                    string adpcma = Path.Combine(Program.MAmiDir, "ym2608_adpcm_rom.bin");
                    if (File.Exists(adpcma))
                        ym2608_adpcm_rom = File.ReadAllBytes(adpcma);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(Exception))
                        throw;
                    else if (ex.GetType() == typeof(SystemException))
                        throw;
                }
            }

            f_read_byte_a_callback = new delg_adpcm_callback(read_byte_a_callback);
            YM2608SetCallbackA(UnitNumber, f_read_byte_a_callback);

            f_read_byte_b_callback = new delg_adpcm_callback(read_byte_b_callback);
            YM2608SetCallbackB(UnitNumber, f_read_byte_b_callback);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            soundManager?.Dispose();
            pcmEngine?.Dispose();

            base.Dispose();

            lock (sndEnginePtrLock)
            {
                if (spfmPtr != IntPtr.Zero)
                {
                    ScciManager.ReleaseSoundChip(spfmPtr);
                    spfmPtr = IntPtr.Zero;
                }
                if (gimicPtr >= 0)
                {
                    GimicManager.ReleaseModule(gimicPtr);
                    gimicPtr = -1;
                }
                if (vsifClient != null)
                {
                    vsifClient.Dispose();
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="pos"></param>
        private byte read_byte_a_callback(byte pn, int pos)
        {
            if (ym2608_adpcm_rom != null && 0 <= pos && pos < ym2608_adpcm_rom.Length)
                return ym2608_adpcm_rom[pos];
            return 0x80;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="pos"></param>
        private byte read_byte_b_callback(byte pn, int pos)
        {
            lock (tmpPcmDataTable)
            {
                if (tmpPcmDataTable.ContainsKey(pn))
                {
                    //HACK: Thread UNSAFE
                    byte[] pd = tmpPcmDataTable[pn];
                    if (pd != null && pd.Length != 0 && pos < pd.Length)
                        return pd[pos];
                }
            }
            return 0x80;
        }

        /// <summary>
        /// 
        /// </summary>
        internal override void PrepareSound()
        {
            base.PrepareSound();

            initGlobalRegisters();
        }

        private bool ignoreUpdatePcmData;

        private void initGlobalRegisters()
        {
            //SSG OFF
            YM2608WriteData(UnitNumber, 0x07, 0, 0, 0x3f);
            //RESET PRESCALER
            YM2608WriteData(UnitNumber, 0x2D, 0, 0, 0xFF, false);
            //6CH MODE
            YM2608WriteData(UnitNumber, 0x29, 0, 0, 0xFF, false);
            //ADPCM A TOTAL LEVEL MAX
            YM2608WriteData(UnitNumber, 0x11, 0, 0, 0x0);   //HACK: avoid GIMIC cache issue
            YM2608WriteData(UnitNumber, 0x11, 0, 0, 0x3f);
            //ADPCM B
            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x20, false);  //EXTMEM
            YM2608WriteData(UnitNumber, 0x01, 0, 3, 0xC2);  //LR, 8bit DRAM

            YM2608WriteData(UnitNumber, 0x0B, 0, 3, 0);   //HACK: avoid GIMIC cache issue
            YM2608WriteData(UnitNumber, 0x0B, 0, 3, 0xff); //ADPCM B TOTAL LEVEL MAX

            YM2608WriteData(UnitNumber, 0x0b, 0, 0, (byte)f_EnvelopeFrequencyCoarse);
            YM2608WriteData(UnitNumber, 0x0c, 0, 0, (byte)f_EnvelopeFrequencyFine);
            YM2608WriteData(UnitNumber, 0x0d, 0, 0, (byte)f_EnvelopeType);
            YM2608WriteData(UnitNumber, 0x22, 0, 0, (byte)(LFOEN << 3 | LFRQ));

            lock (sndEnginePtrLock)
                lastTransferPcmData = new byte[] { };

            if (!IsDisposing && !ignoreUpdatePcmData)
                updatePcmData(null);
        }

        /// <summary>
        /// 
        /// </summary>
        private void setPresetInstruments()
        {
            //Brass Section.dmp
            Timbres[0].FMS = 1;
            Timbres[0].AMS = 0;
            Timbres[0].FB = 7;
            Timbres[0].ALG = 3;

            Timbres[0].Ops[0].Enable = 1;
            Timbres[0].Ops[0].AR = 31;
            Timbres[0].Ops[0].D1R = 6;
            Timbres[0].Ops[0].SL = 15;
            Timbres[0].Ops[0].D2R = 0;
            Timbres[0].Ops[0].RR = 7;

            Timbres[0].Ops[0].MUL = 1;
            Timbres[0].Ops[0].RS = 0;
            Timbres[0].Ops[0].DT1 = 7;
            Timbres[0].Ops[0].AM = 0;
            Timbres[0].Ops[0].SSG_EG = 0;
            Timbres[0].Ops[0].TL = 20;

            Timbres[0].Ops[1].Enable = 1;
            Timbres[0].Ops[1].AR = 31;
            Timbres[0].Ops[1].D1R = 7;
            Timbres[0].Ops[1].SL = 4;
            Timbres[0].Ops[1].D2R = 0;
            Timbres[0].Ops[1].RR = 15;

            Timbres[0].Ops[1].MUL = 2;
            Timbres[0].Ops[1].RS = 0;
            Timbres[0].Ops[1].DT1 = 6;
            Timbres[0].Ops[1].AM = 0;
            Timbres[0].Ops[1].SSG_EG = 0;
            Timbres[0].Ops[1].TL = 21;

            Timbres[0].Ops[2].Enable = 1;
            Timbres[0].Ops[2].AR = 31;
            Timbres[0].Ops[2].D1R = 7;
            Timbres[0].Ops[2].SL = 4;
            Timbres[0].Ops[2].D2R = 0;
            Timbres[0].Ops[2].RR = 15;

            Timbres[0].Ops[2].MUL = 1;
            Timbres[0].Ops[2].RS = 0;
            Timbres[0].Ops[2].DT1 = 2;
            Timbres[0].Ops[2].AM = 0;
            Timbres[0].Ops[2].SSG_EG = 0;
            Timbres[0].Ops[2].TL = 12;

            Timbres[0].Ops[3].Enable = 1;
            Timbres[0].Ops[3].AR = 31;
            Timbres[0].Ops[3].D1R = 0;
            Timbres[0].Ops[3].SL = 0;
            Timbres[0].Ops[3].D2R = 0;
            Timbres[0].Ops[3].RR = 15;

            Timbres[0].Ops[3].MUL = 1;
            Timbres[0].Ops[3].RS = 0;
            Timbres[0].Ops[3].DT1 = 4;
            Timbres[0].Ops[3].AM = 0;
            Timbres[0].Ops[3].SSG_EG = 0;
            Timbres[0].Ops[3].TL = 12;

            //Additive Chimes A.dmp
            Timbres[2].FMS = 0;
            Timbres[2].AMS = 0;
            Timbres[2].FB = 0;
            Timbres[2].ALG = 7;

            Timbres[2].Ops[0].AR = 31;
            Timbres[2].Ops[0].D1R = 4;
            Timbres[2].Ops[0].SL = 15;
            Timbres[2].Ops[0].D2R = 0;
            Timbres[2].Ops[0].RR = 4;

            Timbres[2].Ops[0].MUL = 1;
            Timbres[2].Ops[0].RS = 0;
            Timbres[2].Ops[0].DT1 = 4;
            Timbres[2].Ops[0].AM = 0;
            Timbres[2].Ops[0].SSG_EG = 0;
            Timbres[2].Ops[0].TL = 20;

            Timbres[2].Ops[1].AR = 31;
            Timbres[2].Ops[1].D1R = 7;
            Timbres[2].Ops[1].SL = 15;
            Timbres[2].Ops[1].D2R = 0;
            Timbres[2].Ops[1].RR = 5;

            Timbres[2].Ops[1].MUL = 4;
            Timbres[2].Ops[1].RS = 0;
            Timbres[2].Ops[1].DT1 = 4;
            Timbres[2].Ops[1].AM = 0;
            Timbres[2].Ops[1].SSG_EG = 0;
            Timbres[2].Ops[1].TL = 20;

            Timbres[2].Ops[2].AR = 31;
            Timbres[2].Ops[2].D1R = 10;
            Timbres[2].Ops[2].SL = 15;
            Timbres[2].Ops[2].D2R = 0;
            Timbres[2].Ops[2].RR = 6;

            Timbres[2].Ops[2].MUL = 7;
            Timbres[2].Ops[2].RS = 0;
            Timbres[2].Ops[2].DT1 = 4;
            Timbres[2].Ops[2].AM = 0;
            Timbres[2].Ops[2].SSG_EG = 0;
            Timbres[2].Ops[2].TL = 20;

            Timbres[2].Ops[3].AR = 31;
            Timbres[2].Ops[3].D1R = 13;
            Timbres[2].Ops[3].SL = 15;
            Timbres[2].Ops[3].D2R = 0;
            Timbres[2].Ops[3].RR = 7;

            Timbres[2].Ops[3].MUL = 10;
            Timbres[2].Ops[3].RS = 0;
            Timbres[2].Ops[3].DT1 = 0;
            Timbres[2].Ops[3].AM = 0;
            Timbres[2].Ops[3].SSG_EG = 0;
            Timbres[2].Ops[3].TL = 20;

            //DX Piano1
            Timbres[1].FMS = 0;
            Timbres[1].AMS = 0;
            Timbres[1].FB = 0;
            Timbres[1].ALG = 1;

            Timbres[1].Ops[0].AR = 31;
            Timbres[1].Ops[0].D1R = 9;
            Timbres[1].Ops[0].SL = 15;
            Timbres[1].Ops[0].D2R = 0;
            Timbres[1].Ops[0].RR = 5;

            Timbres[1].Ops[0].MUL = 9;
            Timbres[1].Ops[0].RS = 2;
            Timbres[1].Ops[0].DT1 = 7;
            Timbres[1].Ops[0].AM = 0;
            Timbres[1].Ops[0].SSG_EG = 0;
            Timbres[1].Ops[0].TL = 60;

            Timbres[1].Ops[1].AR = 31;
            Timbres[1].Ops[1].D1R = 9;
            Timbres[1].Ops[1].SL = 15;
            Timbres[1].Ops[1].D2R = 0;
            Timbres[1].Ops[1].RR = 5;

            Timbres[1].Ops[1].MUL = 9;
            Timbres[1].Ops[1].RS = 2;
            Timbres[1].Ops[1].DT1 = 1;
            Timbres[1].Ops[1].AM = 0;
            Timbres[1].Ops[1].SSG_EG = 0;
            Timbres[1].Ops[1].TL = 60;

            Timbres[1].Ops[2].AR = 31;
            Timbres[1].Ops[2].D1R = 7;
            Timbres[1].Ops[2].SL = 15;
            Timbres[1].Ops[2].D2R = 0;
            Timbres[1].Ops[2].RR = 5;

            Timbres[1].Ops[2].MUL = 0;
            Timbres[1].Ops[2].RS = 2;
            Timbres[1].Ops[2].DT1 = 4;
            Timbres[1].Ops[2].AM = 0;
            Timbres[1].Ops[2].SSG_EG = 0;
            Timbres[1].Ops[2].TL = 28;

            Timbres[1].Ops[3].AR = 31;
            Timbres[1].Ops[3].D1R = 3;
            Timbres[1].Ops[3].SL = 15;
            Timbres[1].Ops[3].D2R = 0;
            Timbres[1].Ops[3].RR = 5;

            Timbres[1].Ops[3].MUL = 0;
            Timbres[1].Ops[3].RS = 2;
            Timbres[1].Ops[3].DT1 = 4;
            Timbres[1].Ops[3].AM = 0;
            Timbres[1].Ops[3].SSG_EG = 0;
            Timbres[1].Ops[3].TL = 10;
        }

        /// <summary>
        /// 
        /// </summary>
        private void updatePcmData(YM2608Timbre timbre)
        {
            lock (sndEnginePtrLock)
            {
                if (CurrentSoundEngine == SoundEngineType.Software)
                    return;
            }
            if (PseudoDacMode)
            {
                enablePseudoDacYM2608(true);
                return;
            }

            List<byte> pcmData = new List<byte>();
            uint nextStartAddress = 0;
            for (int i = 0; i < Timbres.Length; i++)
            {
                var tim = Timbres[i];

                tim.PcmAddressStart = 0;
                tim.PcmAddressEnd = 0;
                if (tim.PcmData.Length != 0)
                {
                    int tlen = tim.PcmData.Length;
                    int pad = (0x20 - (tlen & 0x1f)) & 0x1f;    //32 byte pad
                                                                //check bank
                    if (nextStartAddress >> 16 != (nextStartAddress + (uint)(tlen + pad - 1)) >> 16)
                    {
                        for (var j = nextStartAddress; j <= (nextStartAddress | 0xffff); j++)
                            pcmData.Add(0);
                        nextStartAddress |= 0xffff;
                        nextStartAddress += 1;
                    }
                    if (nextStartAddress + tlen + pad - 1 < 0x40000)   //MAX 256KB
                    {
                        tim.PcmAddressStart = nextStartAddress;
                        tim.PcmAddressEnd = (uint)(nextStartAddress + tlen + pad - 1);

                        //Write PCM data
                        pcmData.AddRange(tim.PcmData);
                        //Add 32 byte pad
                        for (int j = 0; j < pad; j++)
                            pcmData.Add(0x80);  //Adds silent data

                        nextStartAddress = Timbres[i].PcmAddressEnd + 1;
                    }
                    else
                    {
                        MessageBox.Show(Resources.AdpcmBufferExceeded, "Warning", MessageBoxButtons.OK);
                        break;
                    }
                }
            }
            if (pcmData.Count != 0 && CurrentSoundEngine != SoundEngineType.Software)
            {
                //transferPcmOnlyDiffData(pcmData.ToArray(), null);

                FormMain.OutputLog(this, Resources.UpdatingADPCM + " (" + timbre.DisplayName + ")");
                //if (Program.IsWriteLockHeld())
                //{
                try
                {
                    FormMain.AppliactionForm.Enabled = false;
                    using (FormProgress f = new FormProgress())
                    {
                        f.StartPosition = FormStartPosition.CenterScreen;
                        f.Message = Resources.UpdatingADPCM + " (" + timbre.DisplayName + ")";
                        f.Show();
                        transferPcmOnlyDiffData(pcmData.ToArray(), f);
                    }
                }
                finally
                {
                    FormMain.AppliactionForm.Enabled = true;
                }
                //}
                //else
                //{
                //    FormProgress.RunDialog(Resources.UpdatingADPCM,
                //            new Action<FormProgress>((f) =>
                //            {
                //                transferPcmOnlyDiffData(pcmData.ToArray(), f);
                //            }));
                //}
                FormMain.OutputLog(this, string.Format(Resources.AdpcmBufferUsed, pcmData.Count / 1024));
            }
        }

        private byte[] lastTransferPcmData;

        private void transferPcmOnlyDiffData(byte[] transferData, FormProgress fp)
        {
            for (int i = 0; i < transferData.Length; i++)
            {
                if (i >= lastTransferPcmData.Length || transferData[i] != lastTransferPcmData[i])
                {
                    sendPcmData(transferData, i, fp);
                    lastTransferPcmData = transferData;
                    break;
                }
            }
        }

        private void sendPcmData(byte[] transferData, int i, FormProgress fp)
        {
            //flag
            YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x13);   //CLEAR MASK
            YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x80);   //IRQ RESET
                                                             //Ctrl1
            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x01, false);   //RESET
            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x60, false);   //REC, EXTMEM
                                                                    //Ctrl2
            YM2608WriteData(UnitNumber, 0x01, 0, 3, 0x02);   //LR, 8bit DRAM

            //START
            YM2608WriteData(UnitNumber, 0x02, 0, 3, (byte)((i >> 5) & 0xff));
            YM2608WriteData(UnitNumber, 0x03, 0, 3, (byte)((i >> (5 + 8)) & 0xff));
            //STOP
            YM2608WriteData(UnitNumber, 0x04, 0, 3, 0xff);
            YM2608WriteData(UnitNumber, 0x05, 0, 3, 0xff);
            //LIMIT
            YM2608WriteData(UnitNumber, 0x0C, 0, 3, 0xff);
            YM2608WriteData(UnitNumber, 0x0D, 0, 3, 0xff);

            int endAddress = transferData.Length;
            if (endAddress > 256 * 1024)
                endAddress = 256 * 1024;

            //Transfer
            int startAddress = i & 0xffffe0;
            int len = endAddress - startAddress;
            int index = 0;
            int percentage = 0;
            int lastPercentage = 0;
            for (int adr = startAddress; adr < endAddress; adr++)
            {
                YM2608WriteData(UnitNumber, 0x08, 0, 3, transferData[adr], false);

                percentage = (100 * index) / len;
                if (percentage != lastPercentage)
                {
                    if (fp != null)
                    {
                        fp.Percentage = percentage;
                        Application.DoEvents();
                    }
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.SPFM:
                            while (!ScciManager.IsBufferEmpty(spfmPtr))
                                Thread.Sleep(10);
                            break;
                        case SoundEngineType.VSIF_MSX_FTDI:
                        case SoundEngineType.VSIF_P6_FTDI:
                        case SoundEngineType.VSIF_PC88_FTDI:
                            vsifClient.FlushDeferredWriteDataAndWait();
                            break;
                        case SoundEngineType.GIMIC:
                        case SoundEngineType.VSIF_MSX_Pi:
                        case SoundEngineType.VSIF_MSX_PiTr:
                            break;
                    }
                }
                lastPercentage = percentage;
                index++;
            }

            //Zero padding
            for (int j = endAddress; j < endAddress + ((0x20 - (endAddress & 0x1f)) & 0x1f); j++)
                YM2608WriteData(UnitNumber, 0x08, 0, 3, 0x80, false);   //Adds silent data

            // Finish
            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x01, false);  //RESET
            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x00, false);

            // Wait
            switch (CurrentSoundEngine)
            {
                case SoundEngineType.SPFM:
                    while (!ScciManager.IsBufferEmpty(spfmPtr))
                        Thread.Sleep(10);
                    break;
                case SoundEngineType.VSIF_MSX_FTDI:
                case SoundEngineType.VSIF_P6_FTDI:
                case SoundEngineType.VSIF_PC88_FTDI:
                    vsifClient.FlushDeferredWriteDataAndWait();
                    break;
                case SoundEngineType.GIMIC:
                case SoundEngineType.VSIF_MSX_Pi:
                case SoundEngineType.VSIF_MSX_PiTr:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected override void OnNoteOnEvent(TaggedNoteOnEvent midiEvent)
        {
            soundManager.ProcessKeyOn(midiEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected override void OnNoteOffEvent(NoteOffEvent midiEvent)
        {
            soundManager.ProcessKeyOff(midiEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected override void OnControlChangeEvent(ControlChangeEvent midiEvent)
        {
            base.OnControlChangeEvent(midiEvent);

            soundManager.ProcessControlChange(midiEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataMsb"></param>
        /// <param name="dataLsb"></param>
        protected override void OnNrpnDataEntered(ControlChangeEvent dataMsb, ControlChangeEvent dataLsb)
        {
            base.OnNrpnDataEntered(dataMsb, dataLsb);

            soundManager.ProcessNrpnData(dataMsb, dataLsb);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caft"></param>
        protected override void OnChannelAfterTouchEvent(ChannelAftertouchEvent caft)
        {
            base.OnChannelAfterTouchEvent(caft);

            soundManager.ProcessChannelAftertouch(caft);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected override void OnPitchBendEvent(PitchBendEvent midiEvent)
        {
            base.OnPitchBendEvent(midiEvent);

            soundManager.ProcessPitchBend(midiEvent);
        }

        internal override void AllSoundOff()
        {
            soundManager?.ProcessAllSoundOff();
            lock (sndEnginePtrLock)
            {
                //HACK:
                switch (f_SoundEngineType)
                {
                    case SoundEngineType.GIMIC:
                        if (CurrentSoundEngine == SoundEngineType.GIMIC)
                            GimicManager.Reset(gimicPtr);
                        break;
                }
            }
            ClearWrittenDataCache();
        }

        internal override void ResetAll()
        {
            ClearWrittenDataCache();
            PrepareSound();
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2608SoundManager : SoundManagerBase
        {
            private static SoundList<SoundBase> allSound = new SoundList<SoundBase>(-1);

            /// <summary>
            /// 
            /// </summary>
            protected override SoundList<SoundBase> AllSounds
            {
                get
                {
                    return allSound;
                }
            }

            private static SoundList<YM2608Sound> fmOnSounds = new SoundList<YM2608Sound>(6);

            private static SoundList<YM2608Sound> ssgOnSounds = new SoundList<YM2608Sound>(3);

            private static SoundList<YM2608Sound> drumBDOnSounds = new SoundList<YM2608Sound>(1);
            private static SoundList<YM2608Sound> drumSDOnSounds = new SoundList<YM2608Sound>(1);
            private static SoundList<YM2608Sound> drumHHOnSounds = new SoundList<YM2608Sound>(1);
            private static SoundList<YM2608Sound> drumTOMOnSounds = new SoundList<YM2608Sound>(1);
            private static SoundList<YM2608Sound> drumTOPOnSounds = new SoundList<YM2608Sound>(1);
            private static SoundList<YM2608Sound> drumRIMOnSounds = new SoundList<YM2608Sound>(1);


            private static SoundList<YM2608Sound> pcmbOnSounds = new SoundList<YM2608Sound>(1);

            private static SoundList<YM2608Sound> dacOnSounds = new SoundList<YM2608Sound>(MAX_DAC_VOICES);

            private YM2608 parentModule;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public YM2608SoundManager(YM2608 parent) : base(parent)
            {
                this.parentModule = parent;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="note"></param>
            public override SoundBase[] SoundOn(TaggedNoteOnEvent note)
            {
                List<SoundBase> rv = new List<SoundBase>();

                var bts = parentModule.GetBaseTimbres(note);
                var ids = parentModule.GetBaseTimbreIndexes(note);
                int tindex = 0;
                for (int i = 0; i < bts.Length; i++)
                {
                    YM2608Timbre timbre = (YM2608Timbre)bts[i];

                    tindex++;
                    var emptySlot = searchEmptySlot(note, timbre);
                    if (emptySlot.slot < 0)
                        continue;

                    YM2608Sound snd = new YM2608Sound(emptySlot.inst, this, timbre, tindex - 1, note, emptySlot.slot, ids[i]);
                    switch (timbre.ToneType)
                    {
                        case ToneType.FM:
                            fmOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn FM ch" + emptySlot + " " + note.ToString());
                            break;
                        case ToneType.RHYTHM:
                            switch (note.NoteNumber)
                            {
                                case 35:    //BD
                                case 36:    //BD

                                case 60:
                                case 61:
                                case 62:
                                case 63:
                                case 64:
                                case 65:
                                case 66:
                                case 72:
                                case 75:
                                case 76:
                                case 77:
                                    drumBDOnSounds.Add(snd);
                                    break;
                                case 37:    //STICK
                                    drumRIMOnSounds.Add(snd);
                                    break;
                                case 38:    //SD
                                case 39:    //CLAP
                                case 40:    //SD

                                case 67:
                                case 68:
                                case 69:
                                case 70:
                                    drumSDOnSounds.Add(snd);
                                    break;
                                case 41:    //TOM
                                case 43:    //TOM
                                case 45:    //TOM
                                case 47:    //TOM
                                case 48:    //TOM
                                case 50:    //TOM

                                case 71:
                                case 78:
                                    drumTOMOnSounds.Add(snd);
                                    break;
                                case 42:    //HH
                                case 44:    //HH
                                case 46:    //HH

                                case 54:    //BELL
                                case 56:    //BELL
                                case 58:    //BELL
                                case 80:    //BELL

                                case 73:
                                case 79:
                                    drumHHOnSounds.Add(snd);
                                    break;
                                case 49:    //Symbal
                                case 51:    //Symbal
                                case 52:    //Symbal
                                case 53:    //Symbal
                                case 55:    //Symbal
                                case 57:    //Symbal
                                case 59:    //Symbal

                                case 81:    //TRIANGLE
                                case 74:
                                    drumTOPOnSounds.Add(snd);
                                    break;
                            }
                            FormMain.OutputDebugLog(parentModule, "KeyOn PCM-A ch" + emptySlot + " " + note.ToString());

                            break;
                        case ToneType.ADPCM_B:
                            if (parentModule.PseudoDacMode)
                                break;

                            pcmbOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn PCM-B ch" + emptySlot + " " + note.ToString());

                            //HACK: store pcm data to local buffer to avoid "thread lock"
                            lock (parentModule.tmpPcmDataTable)
                                parentModule.tmpPcmDataTable[ids[i]] = timbre.PcmData;
                            break;

                        case ToneType.DAC:
                            if (!parentModule.PseudoDacMode)
                                break;
                            dacOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn DAC ch" + emptySlot + " " + note.ToString());
                            break;

                        case ToneType.SSG:
                            ssgOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn SSG ch" + emptySlot + " " + note.ToString());
                            break;
                    }
                    rv.Add(snd);
                }
                for (int i = 0; i < rv.Count; i++)
                {
                    var snd = rv[i];
                    if (!snd.IsDisposed)
                    {
                        ProcessKeyOn(snd);
                    }
                    else
                    {
                        rv.Remove(snd);
                        i--;
                    }
                }

                return rv.ToArray();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            private (YM2608 inst, int slot) searchEmptySlot(TaggedNoteOnEvent note, YM2608Timbre timbre)
            {
                var emptySlot = (parentModule, -1);

                switch (timbre.ToneType)
                {
                    case ToneType.FM:
                        {
                            if (parentModule.f_PC88FMType == BoardType.InternalOPN)
                                emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, 3);
                            else
                                emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, 6);
                            break;
                        }
                    case ToneType.RHYTHM:
                        {
                            switch (note.NoteNumber)
                            {
                                case 35:    //BD
                                case 36:    //BD

                                case 60:
                                case 61:
                                case 62:
                                case 63:
                                case 64:
                                case 65:
                                case 66:
                                case 72:
                                case 75:
                                case 76:
                                case 77:
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumBDOnSounds, note, 1);
                                    break;
                                case 37:    //STICK
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumRIMOnSounds, note, 1);
                                    break;
                                case 38:    //SD
                                case 39:    //CLAP
                                case 40:    //SD

                                case 67:
                                case 68:
                                case 69:
                                case 70:
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumSDOnSounds, note, 1);
                                    break;
                                case 41:    //TOM
                                case 43:    //TOM
                                case 45:    //TOM
                                case 47:    //TOM
                                case 48:    //TOM
                                case 50:    //TOM

                                case 71:
                                case 78:
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumTOMOnSounds, note, 1);
                                    break;
                                case 42:    //HH
                                case 44:    //HH
                                case 46:    //HH

                                case 54:    //BELL
                                case 56:    //BELL
                                case 58:    //BELL
                                case 80:    //BELL

                                case 73:
                                case 79:
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumHHOnSounds, note, 1);
                                    break;
                                case 49:    //Symbal
                                case 51:    //Symbal
                                case 52:    //Symbal
                                case 53:    //Symbal
                                case 55:    //Symbal
                                case 57:    //Symbal
                                case 59:    //Symbal

                                case 81:    //TRIANGLE
                                case 74:
                                    emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumTOPOnSounds, note, 1);
                                    break;
                            }
                            break;
                        }
                    case ToneType.ADPCM_B:
                        {
                            emptySlot = SearchEmptySlotAndOffForLeader(parentModule, pcmbOnSounds, note, 1);
                            break;
                        }
                    case ToneType.DAC:
                        {
                            return SearchEmptySlotAndOffForLeader(parentModule, dacOnSounds, note, MAX_DAC_VOICES);
                        }
                    case ToneType.SSG:
                        {
                            emptySlot = SearchEmptySlotAndOffForLeader(parentModule, ssgOnSounds, note, 3);
                            break;
                        }
                }

                return emptySlot;
            }


            internal override void ProcessAllSoundOff()
            {
                var me = new ControlChangeEvent((SevenBitNumber)120, (SevenBitNumber)0);
                ProcessControlChange(me);

                for (int i = 0; i < 6; i++)
                {
                    uint reg = (uint)(i / 3) * 2;
                    parentModule.YM2608WriteData(parentModule.UnitNumber, 0x28, 0, 0, (byte)(0x00 | (reg << 1) | (byte)(i % 3)));

                    for (int op = 0; op < 4; op++)
                        parentModule.YM2608WriteData(parentModule.UnitNumber, 0x40, op, i, 127);
                }
                //SSG
                parentModule.YM2608WriteData(parentModule.UnitNumber, 0x07, 0, 0, (byte)0x3f);
                //ADPCM
                if (parentModule.PseudoDacMode)
                {
                    for (int i = 0; i < MAX_DAC_VOICES; i++)
                        parentModule.pcmEngine.Stop(i);
                }
                else
                {
                    parentModule.YM2608WriteData(parentModule.UnitNumber, 0x00, 0, 3, (byte)0x01, false);   //RESET
                    parentModule.YM2608WriteData(parentModule.UnitNumber, 0x00, 0, 3, (byte)0x00, false);   //STOP
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private class YM2608Sound : SoundBase
        {

            private YM2608 parentModule;

            private uint unitNumber;

            private int timbreIndex;

            private YM2608Timbre timbre;

            private ToneType lastToneType;

            private SsgSoundType lastSoundType;

            private double baseFreq;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parentModule"></param>
            /// <param name="noteOnEvent"></param>
            /// <param name="programNumber"></param>
            /// <param name="slot"></param>
            public YM2608Sound(YM2608 parentModule, YM2608SoundManager manager, TimbreBase timbre, int tindex, TaggedNoteOnEvent noteOnEvent, int slot, int timbreIndex) : base(parentModule, manager, timbre, tindex, noteOnEvent, slot)
            {
                this.parentModule = parentModule;
                this.timbreIndex = timbreIndex;
                this.timbre = (YM2608Timbre)timbre;
                this.unitNumber = parentModule.UnitNumber;

                lastToneType = this.timbre.ToneType;
                lastSoundType = this.timbre.SsgSoundType;
                baseFreq = this.timbre.BaseFreqency;
            }

            /// <summary>
            /// 
            /// </summary>
            public override void KeyOn()
            {
                base.KeyOn();

                var gs = timbre.GlobalSettings;
                if (gs.Enable)
                {
                    if (gs.LFOEN.HasValue)
                        parentModule.LFOEN = gs.LFOEN.Value;
                    if (gs.LFRQ.HasValue)
                        parentModule.LFRQ = gs.LFRQ.Value;

                    if (gs.EnvelopeType.HasValue)
                        parentModule.EnvelopeType = gs.EnvelopeType.Value;
                    if (gs.EnvelopeFrequencyFine.HasValue)
                        parentModule.EnvelopeFrequencyFine = gs.EnvelopeFrequencyFine.Value;
                    if (gs.EnvelopeFrequencyCoarse.HasValue)
                        parentModule.EnvelopeFrequencyCoarse = gs.EnvelopeFrequencyCoarse.Value;
                }
                switch (lastToneType)
                {
                    case ToneType.FM:
                        {
                            //
                            setFmTimbre();

                            //Freq
                            OnPitchUpdated();

                            //Volume
                            OnVolumeUpdated();

                            //On
                            uint reg = (uint)(Slot / 3) * 2;
                            byte op = (byte)(timbre.Ops[0].Enable << 4 | timbre.Ops[1].Enable << 5 | timbre.Ops[2].Enable << 6 | timbre.Ops[3].Enable << 7);
                            parentModule.YM2608WriteData(unitNumber, 0x28, 0, 0, (byte)(op | (reg << 1) | (byte)(Slot % 3)));
                        }
                        break;
                    case ToneType.SSG:
                        {
                            OnPitchUpdated();
                            OnVolumeUpdated();

                            if (((int)lastSoundType & 4) != 0)
                                parentModule.YM2608WriteData(unitNumber, (byte)(13), 0, 0, parentModule.EnvelopeType, false);
                        }
                        break;
                    case ToneType.RHYTHM:
                        {
                            //KeyOn
                            byte kon, ofst;
                            getDrumKeyAndOffset(out kon, out ofst);
                            if (kon != 0)
                            {
                                int pan = CalcCurrentPanpot();
                                if (pan < 32)
                                    pan = 0x2;
                                else if (pan > 96)
                                    pan = 0x1;
                                else
                                    pan = 0x3;

                                OnVolumeUpdated();

                                parentModule.YM2608WriteData(unitNumber, (byte)(0x18 + ofst), 0, 0, (byte)((pan << 6) | (NoteOnEvent.Velocity >> 2)));
                                parentModule.YM2608WriteData(unitNumber, (byte)(0x10), 0, 0, kon, false);
                            }
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            if (parentModule.PseudoDacMode)
                                break;

                            parentModule.YM2608WriteData(unitNumber, 0x10, 0, 3, 0x1B, false); //CLEAR FLAGS
                            parentModule.YM2608WriteData(unitNumber, 0x10, 0, 3, 0x80, false); //IRQ RESET

                            parentModule.YM2608WriteData(unitNumber, 0x00, 0, 3, 0x20); //ACCESS TO MEM

                            OnPitchUpdated();
                            OnVolumeUpdated();
                            OnPanpotUpdated();

                            if (parentModule.CurrentSoundEngine != SoundEngineType.Software)
                            {
                                //pcm start
                                parentModule.YM2608WriteData(unitNumber, 0x02, 0, 3, (byte)((timbre.PcmAddressStart >> 5) & 0xff));
                                parentModule.YM2608WriteData(unitNumber, 0x03, 0, 3, (byte)((timbre.PcmAddressStart >> (5 + 8) & 0xff)));
                                //pcm end
                                parentModule.YM2608WriteData(unitNumber, 0x04, 0, 3, (byte)((timbre.PcmAddressEnd >> 5) & 0xff));
                                parentModule.YM2608WriteData(unitNumber, 0x05, 0, 3, (byte)((timbre.PcmAddressEnd >> (5 + 8)) & 0xff));
                                //limit
                                parentModule.YM2608WriteData(unitNumber, 0x0C, 0, 3, (byte)(0xff));
                                parentModule.YM2608WriteData(unitNumber, 0x0D, 0, 3, (byte)(0xff));
                            }
                            else
                            {
                                //HACK: mamidimemo
                                //prognum
                                parentModule.YM2608WriteData(unitNumber, 0x06, 0, 3, (byte)(timbreIndex), false, true);
                                //pcm start
                                parentModule.YM2608WriteData(unitNumber, 0x02, 0, 3, (byte)(0), false, true);
                                parentModule.YM2608WriteData(unitNumber, 0x03, 0, 3, (byte)(0), false, true);
                                //pcm end
                                ushort len = 0;
                                if (timbre.PcmData.Length > 0)
                                    len = (ushort)(((timbre.PcmData.Length - 1) & 0xffffff) >> 5);
                                parentModule.YM2608WriteData(unitNumber, 0x04, 0, 3, (byte)(len & 0xff), false, true);
                                parentModule.YM2608WriteData(unitNumber, 0x05, 0, 3, (byte)(len >> 8), false, true);
                            }
                            //KeyOn
                            byte loop = timbre.LoopEnable ? (byte)0x10 : (byte)0x00;
                            parentModule.YM2608WriteData(unitNumber, 0x00, 0, 3, (byte)(0x80 | 0x20 | loop), false);   //PLAY, ACCESS TO MEM, LOOP
                        }
                        break;
                    case ToneType.DAC:
                        {
                            if (!parentModule.PseudoDacMode)
                                break;
                            lock (parentModule.sndEnginePtrLock)
                                if (parentModule.CurrentSoundEngine == SoundEngineType.Software)
                                    break;
                            
                            parentModule.pcmEngine.Play(this, NoteOnEvent, Slot, timbre, CalcCurrentFrequency(), CalcCurrentVolume());
                            break;
                        }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="kon"></param>
            /// <param name="ofst"></param>
            private void getDrumKeyAndOffset(out byte kon, out byte ofst)
            {
                kon = 0;
                ofst = 0;
                switch (NoteOnEvent.NoteNumber)
                {
                    case 35:    //BD
                    case 36:    //BD

                    case 60:
                    case 61:
                    case 62:
                    case 63:
                    case 64:
                    case 65:
                    case 66:
                    case 72:
                    case 75:
                    case 76:
                    case 77:
                        kon = 0x01;
                        ofst = 0;
                        break;
                    case 37:    //STICK
                        kon = 0x20;
                        ofst = 5;
                        break;
                    case 38:    //SD
                    case 39:    //CLAP
                    case 40:    //SD

                    case 67:
                    case 68:
                    case 69:
                    case 70:
                        kon = 0x02;
                        ofst = 1;
                        break;
                    case 41:    //TOM
                    case 43:    //TOM
                    case 45:    //TOM
                    case 47:    //TOM
                    case 48:    //TOM
                    case 50:    //TOM

                    case 71:
                    case 78:
                        kon = 0x10;
                        ofst = 4;
                        break;
                    case 42:    //HH
                    case 44:    //HH
                    case 46:    //HH

                    case 54:    //BELL
                    case 56:    //BELL
                    case 58:    //BELL
                    case 80:    //BELL

                    case 73:
                    case 79:
                        kon = 0x08;
                        ofst = 3;
                        break;
                    case 49:    //Symbal
                    case 51:    //Symbal
                    case 52:    //Symbal
                    case 53:    //Symbal
                    case 55:    //Symbal
                    case 57:    //Symbal
                    case 59:    //Symbal

                    case 81:    //TRIANGLE
                    case 74:
                        kon = 0x04;
                        ofst = 2;
                        break;
                }
            }

            public override void OnSoundParamsUpdated()
            {
                var gs = timbre.GlobalSettings;
                if (gs.Enable)
                {
                    if (gs.LFOEN.HasValue)
                        parentModule.LFOEN = gs.LFOEN.Value;
                    if (gs.LFRQ.HasValue)
                        parentModule.LFRQ = gs.LFRQ.Value;

                    if (gs.EnvelopeType.HasValue)
                        parentModule.EnvelopeType = gs.EnvelopeType.Value;
                    if (gs.EnvelopeFrequencyFine.HasValue)
                        parentModule.EnvelopeFrequencyFine = gs.EnvelopeFrequencyFine.Value;
                    if (gs.EnvelopeFrequencyCoarse.HasValue)
                        parentModule.EnvelopeFrequencyCoarse = gs.EnvelopeFrequencyCoarse.Value;
                }

                switch (lastToneType)
                {
                    case ToneType.FM:
                        for (int op = 0; op < 4; op++)
                        {
                            //$30+: multiply and detune
                            parentModule.YM2608WriteData(unitNumber, 0x30, op, Slot, (byte)((timbre.Ops[op].DT1 << 4 | timbre.Ops[op].MUL)));
                            //$40+: total level
                            switch (timbre.ALG)
                            {
                                case 0:
                                    if (op != 3)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 1:
                                    if (op != 3)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 2:
                                    if (op != 3)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 3:
                                    if (op != 3)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 4:
                                    if (op != 1 && op != 3)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 5:
                                    if (op == 4)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 6:
                                    if (op == 4)
                                        parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                                    break;
                                case 7:
                                    break;
                            }
                            //$50+: attack rate and rate scaling
                            parentModule.YM2608WriteData(unitNumber, 0x50, op, Slot, (byte)((timbre.Ops[op].RS << 6 | timbre.Ops[op].AR)));
                            //$60+: 1st decay rate and AM enable
                            parentModule.YM2608WriteData(unitNumber, 0x60, op, Slot, (byte)((timbre.Ops[op].AM << 7 | timbre.Ops[op].D1R)));
                            //$70+: 2nd decay rate
                            parentModule.YM2608WriteData(unitNumber, 0x70, op, Slot, (byte)timbre.Ops[op].D2R);
                            //$80+: release rate and sustain level
                            parentModule.YM2608WriteData(unitNumber, 0x80, op, Slot, (byte)((timbre.Ops[op].SL << 4 | timbre.Ops[op].RR)));
                            //$90+: SSG-EG
                            parentModule.YM2608WriteData(unitNumber, 0x90, op, Slot, (byte)timbre.Ops[op].SSG_EG);
                        }

                        //$B0+: algorithm and feedback
                        parentModule.YM2608WriteData(unitNumber, 0xB0, 0, Slot, (byte)(timbre.FB << 3 | timbre.ALG));

                        if (!IsKeyOff)
                        {
                            //On
                            uint reg = (uint)(Slot / 3) * 2;
                            byte open = (byte)(timbre.Ops[0].Enable << 4 | timbre.Ops[1].Enable << 5 | timbre.Ops[2].Enable << 6 | timbre.Ops[3].Enable << 7);
                            parentModule.YM2608WriteData(unitNumber, 0x28, 0, 0, (byte)(open | (reg << 1) | (byte)(Slot % 3)));
                        }
                        break;
                    case ToneType.SSG:
                        break;
                    case ToneType.RHYTHM:
                        break;
                    case ToneType.ADPCM_B:
                        break;
                    case ToneType.DAC:
                        break;
                }

                base.OnSoundParamsUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnVolumeUpdated()
            {
                switch (lastToneType)
                {
                    case ToneType.FM:
                        List<int> ops = new List<int>();
                        List<int> exops = new List<int>();
                        switch (timbre.ALG)
                        {
                            case 0:
                                ops.Add(3);
                                exops.Add(0);
                                exops.Add(1);
                                exops.Add(2);
                                break;
                            case 1:
                                ops.Add(3);
                                exops.Add(0);
                                exops.Add(1);
                                exops.Add(2);
                                break;
                            case 2:
                                ops.Add(3);
                                exops.Add(0);
                                exops.Add(1);
                                exops.Add(2);
                                break;
                            case 3:
                                ops.Add(3);
                                exops.Add(0);
                                exops.Add(1);
                                exops.Add(2);
                                break;
                            case 4:
                                ops.Add(1);
                                ops.Add(3);
                                exops.Add(0);
                                exops.Add(2);
                                break;
                            case 5:
                                ops.Add(1);
                                ops.Add(2);
                                ops.Add(3);
                                exops.Add(0);
                                break;
                            case 6:
                                ops.Add(1);
                                ops.Add(2);
                                ops.Add(3);
                                exops.Add(0);
                                break;
                            case 7:
                                ops.Add(0);
                                ops.Add(1);
                                ops.Add(2);
                                ops.Add(3);
                                break;
                        }
                        var v = CalcCurrentVolume();
                        int velo = 1 + timbre.MDS.VelocitySensitivity;
                        foreach (int op in ops)
                        {
                            //$40+: total level
                            parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)((127 / velo) - Math.Round(((127 / velo) - (timbre.Ops[op].TL / velo)) * v)));
                        }
                        if (timbre.UseExprForModulator)
                        {
                            //$40+: total level
                            var mul = CalcModulatorMultiply();
                            foreach (int op in exops)
                            {
                                if (((1 << op) & (int)timbre.ExprTargetModulators) == 0)
                                    continue;

                                double vol = timbre.Ops[op].TL;
                                if (mul > 0)
                                    vol = vol + ((127 - vol) * mul);
                                else if (mul < 0)
                                    vol = vol + ((vol) * mul);
                                vol = Math.Round(vol);
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)vol);
                            }
                        }
                        break;
                    case ToneType.SSG:
                        updatePsgVolume();
                        break;
                    case ToneType.RHYTHM:
                        byte fv = (byte)(((byte)Math.Round(63 * CalcCurrentVolume(true)) & 0x3f));
                        parentModule.YM2608WriteData(unitNumber, 0x11, 0, 0, (byte)fv);
                        break;
                    case ToneType.ADPCM_B:
                        if (parentModule.PseudoDacMode)
                            break;
                        parentModule.YM2608WriteData(unitNumber, 0x0B, 0, 3, (byte)(Math.Round(127 * CalcCurrentVolume())));
                        break;
                    case ToneType.DAC:
                        if (!parentModule.PseudoDacMode)
                            break;
                        lock (parentModule.sndEnginePtrLock)
                            if (parentModule.CurrentSoundEngine == SoundEngineType.Software)
                                break;

                        parentModule.pcmEngine.Volume(Slot, CalcCurrentVolume());
                        break;
                }
            }


            /// <summary>
            /// 
            /// </summary>
            private void updatePsgVolume()
            {
                if (IsSoundOff)
                    return;

                byte fv = (byte)(((byte)Math.Round(15 * CalcCurrentVolume()) & 0xf));

                var st = lastSoundType;

                if (((int)st & 4) == 0)
                    // PSG/Noise
                    parentModule.YM2608WriteData(unitNumber, (byte)(0x08 + Slot), 0, 0, (byte)(fv & 0xf));
                else
                    //Envelope
                    parentModule.YM2608WriteData(unitNumber, (byte)(0x08 + Slot), 0, 0, (byte)(0x10 | fv));

                //key on
                byte data = parentModule.YM2608ReadData(unitNumber, (byte)(7), 0, 0);
                data |= (byte)((1 | 8) << Slot);
                switch ((int)st & 3)
                {
                    case 1:
                        data &= (byte)(~(1 << Slot));
                        break;
                    case 2:
                        data &= (byte)(~(8 << Slot));
                        break;
                    case 3:
                        data &= (byte)(~((1 | 8) << Slot));
                        break;
                }
                parentModule.YM2608WriteData(unitNumber, (byte)(7), 0, 0, data);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            public override void OnPitchUpdated()
            {
                double d = CalcCurrentPitchDeltaNoteNumber();

                switch (lastToneType)
                {
                    case ToneType.FM:
                        {
                            int nn = NoteOnEvent.NoteNumber;
                            if (ParentModule.ChannelTypes[NoteOnEvent.Channel] == ChannelType.Drum ||
                                ParentModule.ChannelTypes[NoteOnEvent.Channel] == ChannelType.DrumGt)
                                nn = (int)ParentModule.DrumTimbres[NoteOnEvent.NoteNumber].BaseNote;
                            int noteNum = nn + (int)d;
                            if (noteNum > 127)
                                noteNum = 127;
                            else if (noteNum < 0)
                                noteNum = 0;
                            var nnOn = new TaggedNoteOnEvent((SevenBitNumber)noteNum, (SevenBitNumber)127);
                            var freq = convertFmFrequency(nnOn, 0);
                            var octave = nnOn.GetNoteOctave();

                            if (d != 0)
                                freq += (convertFmFrequency(nnOn, (d < 0) ? -1 : +1) - freq) * Math.Abs(d - Math.Truncate(d));

                            if (octave < 0)
                            {
                                freq /= 2 * -octave;
                                octave = 0;
                            }
                            else if (octave > 7)
                            {
                                freq *= 2 * (octave - 7);
                                if (freq > 0x7ff)
                                    freq = 0x7ff;
                                octave = 7;
                            }
                            ushort dfreq = (ushort)Math.Round(freq);
                            octave = octave << 3;

                            parentModule.YM2608WriteData(unitNumber, 0xa4, 0, Slot, (byte)(octave | ((dfreq >> 8) & 7)), false);
                            parentModule.YM2608WriteData(unitNumber, 0xa0, 0, Slot, (byte)(0xff & dfreq), false);
                        }
                        break;
                    case ToneType.SSG:
                        {
                            var st = lastSoundType;

                            //key on
                            byte data = parentModule.YM2608ReadData(unitNumber, (byte)(7), 0, 0);
                            data |= (byte)((1 | 8) << Slot);
                            switch ((int)st & 3)
                            {
                                case 1:
                                    data &= (byte)(~(1 << Slot));
                                    break;
                                case 2:
                                    data &= (byte)(~(8 << Slot));
                                    break;
                                case 3:
                                    data &= (byte)(~((1 | 8) << Slot));
                                    break;
                            }
                            parentModule.YM2608WriteData(unitNumber, (byte)(7), 0, 0, data);

                            if (((int)st & 1) == 1)
                                updatePsgPitch();

                            if (((int)st & 2) == 2)
                                updateNoisePitch();

                            if (((int)st & 4) != 0)
                            {
                                var gs = timbre.GlobalSettings;
                                if (gs.Enable)
                                {
                                    double freq = CalcCurrentFrequency();

                                    // fE = fCLOCK / (256*EP)
                                    // EP = CT+FT

                                    // 256*EP * fE = fCLOCK
                                    // EP = fCLOCK/(256 * fE)
                                    // EP = 1.7897725 * 1000 * 1000 / (
                                    if (gs.SyncWithNoteFrequency.HasValue)
                                    {
                                        var fm = freq;
                                        if (gs.SyncWithNoteFrequencyDivider.HasValue)
                                            fm /= gs.SyncWithNoteFrequencyDivider.Value;

                                        var EP = (int)Math.Round((double)parentModule.MasterClock / (256d * fm));

                                        parentModule.f_EnvelopeFrequencyCoarse = (byte)(EP >> 8);
                                        parentModule.f_EnvelopeFrequencyFine = (byte)(EP & 0xff);
                                    }
                                }

                                parentModule.YM2608WriteData(unitNumber, (byte)(12), 0, 0, parentModule.EnvelopeFrequencyCoarse);
                                parentModule.YM2608WriteData(unitNumber, (byte)(11), 0, 0, parentModule.EnvelopeFrequencyFine);
                                parentModule.YM2608WriteData(unitNumber, (byte)(13), 0, 0, parentModule.EnvelopeType, false);
                            }
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            if (parentModule.PseudoDacMode)
                                break;

                            uint freq = (uint)Math.Round((CalcCurrentFrequency() / baseFreq) * ((double)parentModule.MasterClock / 8000000d) * 65536d);
                            if (freq > 0xffff)
                                freq = 0xffff;

                            parentModule.YM2608WriteData(unitNumber, (byte)(0x0A), 0, 3, (byte)(freq >> 8));
                            parentModule.YM2608WriteData(unitNumber, (byte)(0x09), 0, 3, (byte)(freq & 0xff));
                        }
                        break;
                    case ToneType.DAC:
                        {
                            if (!parentModule.PseudoDacMode)
                                break;
                            lock (parentModule.sndEnginePtrLock)
                                if (parentModule.CurrentSoundEngine == SoundEngineType.Software)
                                    break;

                            parentModule.pcmEngine.Pitch(Slot, CalcCurrentFrequency());
                            break;
                        }
                }
                base.OnPitchUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            private void updatePsgPitch()
            {
                double freq = CalcCurrentFrequency();

                if (parentModule.CurrentSoundEngine != SoundEngineType.Software)
                {
                    freq = Math.Round(parentModule.MasterClock / 64 / freq);
                    if (freq > 0xfff)
                        freq = 0xfff;
                    ushort tp = (ushort)freq;

                    parentModule.YM2608WriteData(unitNumber, (byte)(0 + (Slot * 2)), 0, 0, (byte)(tp & 0xff));
                    parentModule.YM2608WriteData(unitNumber, (byte)(1 + (Slot * 2)), 0, 0, (byte)((tp >> 8) & 0xf));
                }
                else
                {
                    //HACK: Sync with FM sample rate because SSG stream is mixed with OPNA stream directly by MAmidiMEmo design.
                    freq = Math.Round(parentModule.MasterClock / 72 / 2 / freq);
                    if (freq > 0xfff)
                        freq = 0xfff;
                    ushort tp = (ushort)freq;

                    parentModule.YM2608WriteData(unitNumber, (byte)(0 + (Slot * 2)), 0, 0, (byte)(tp & 0xff), true, true);
                    parentModule.YM2608WriteData(unitNumber, (byte)(1 + (Slot * 2)), 0, 0, (byte)((tp >> 8) & 0xf), true, true);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            private void updateNoisePitch()
            {
                int nn = NoteOnEvent.NoteNumber;
                if (ParentModule.ChannelTypes[NoteOnEvent.Channel] == ChannelType.Drum ||
                    ParentModule.ChannelTypes[NoteOnEvent.Channel] == ChannelType.DrumGt)
                    nn = (int)ParentModule.DrumTimbres[NoteOnEvent.NoteNumber].BaseNote;

                int v = nn % 15;

                parentModule.YM2608WriteData(unitNumber, (byte)(6), 0, 0, (byte)v);
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnPanpotUpdated()
            {
                int pan = CalcCurrentPanpot();
                if (pan < 32)
                    pan = 0x2;
                else if (pan > 96)
                    pan = 0x1;
                else
                    pan = 0x3;
                switch (lastToneType)
                {
                    case ToneType.FM:
                        //$B4+: panning, FMS, AMS
                        parentModule.YM2608WriteData(unitNumber, 0xB4, 0, Slot, (byte)(pan << 6 | (timbre.AMS << 4) | timbre.FMS));
                        break;
                    case ToneType.RHYTHM:
                        byte kon, ofst;
                        getDrumKeyAndOffset(out kon, out ofst);
                        if (kon != 0)
                        {
                            byte fv = (byte)(((byte)Math.Round(31 * CalcCurrentVolume()) & 0x1f));
                            parentModule.YM2608WriteData(unitNumber, (byte)(0x18 + ofst), 0, 0, (byte)((pan << 6) | (NoteOnEvent.Velocity >> 2)));
                        }
                        break;
                    case ToneType.ADPCM_B:
                    case ToneType.DAC:
                        parentModule.YM2608WriteData(unitNumber, 0x01, 0, 3, (byte)(pan << 6 | 0x02)); //LR, 8bit RAM
                        break;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            private void setFmTimbre()
            {
                for (int op = 0; op < 4; op++)
                {
                    //$30+: multiply and detune
                    parentModule.YM2608WriteData(unitNumber, 0x30, op, Slot, (byte)((timbre.Ops[op].DT1 << 4 | timbre.Ops[op].MUL)));
                    //$40+: total level
                    switch (timbre.ALG)
                    {
                        case 0:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 1:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 2:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 3:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 4:
                            if (op != 1 && op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 5:
                            if (op == 0 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 6:
                            if (op == 0 && !timbre.UseExprForModulator)
                                parentModule.YM2608WriteData(unitNumber, 0x40, op, Slot, (byte)timbre.Ops[op].TL);
                            break;
                        case 7:
                            break;
                    }
                    //$50+: attack rate and rate scaling
                    parentModule.YM2608WriteData(unitNumber, 0x50, op, Slot, (byte)((timbre.Ops[op].RS << 6 | timbre.Ops[op].AR)));
                    //$60+: 1st decay rate and AM enable
                    parentModule.YM2608WriteData(unitNumber, 0x60, op, Slot, (byte)((timbre.Ops[op].AM << 7 | timbre.Ops[op].D1R)));
                    //$70+: 2nd decay rate
                    parentModule.YM2608WriteData(unitNumber, 0x70, op, Slot, (byte)timbre.Ops[op].D2R);
                    //$80+: release rate and sustain level
                    parentModule.YM2608WriteData(unitNumber, 0x80, op, Slot, (byte)((timbre.Ops[op].SL << 4 | timbre.Ops[op].RR)));
                    //$90+: SSG-EG
                    parentModule.YM2608WriteData(unitNumber, 0x90, op, Slot, (byte)timbre.Ops[op].SSG_EG);
                }

                //$B0+: algorithm and feedback
                parentModule.YM2608WriteData(unitNumber, 0xB0, 0, Slot, (byte)(timbre.FB << 3 | timbre.ALG));

                OnPanpotUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void SoundOff()
            {
                base.SoundOff();

                switch (lastToneType)
                {
                    case ToneType.FM:
                        uint reg = (uint)(Slot / 3) * 2;
                        parentModule.YM2608WriteData(unitNumber, 0x28, 0, 0, (byte)(0x00 | (reg << 1) | (byte)(Slot % 3)));
                        break;
                    case ToneType.SSG:
                        byte data = parentModule.YM2608ReadData(unitNumber, 7, 0, 0);
                        data |= (byte)((1 | 8) << Slot);
                        parentModule.YM2608WriteData(unitNumber, 7, 0, 0, (byte)data);

                        parentModule.YM2608WriteData(unitNumber, (byte)(8 + Slot), 0, 0, (byte)0);
                        break;
                    case ToneType.RHYTHM:
                        {
                            //KeyOn
                            byte kon, ofst;
                            getDrumKeyAndOffset(out kon, out ofst);
                            if (kon != 0)
                            {
                                parentModule.YM2608WriteData(unitNumber, (byte)(0x18 + ofst), 0, 0, 0);
                                parentModule.YM2608WriteData(unitNumber, 0x10, 0, 0, (byte)(0x80 | kon), false);
                            }
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            if (parentModule.PseudoDacMode)
                                break;

                            parentModule.YM2608WriteData(unitNumber, 0x0B, 0, 3, 0);    //VOLUME 0
                            parentModule.YM2608WriteData(unitNumber, 0x00, 0, 3, 0x00, false);  //STOP
                            parentModule.YM2608WriteData(unitNumber, 0x00, 0, 3, 0x01, false);  //RESET
                        }
                        break;
                    case ToneType.DAC:
                        {
                            if (!parentModule.PseudoDacMode)
                                break;
                            lock (parentModule.sndEnginePtrLock)
                                if (parentModule.CurrentSoundEngine == SoundEngineType.Software)
                                    break;

                            parentModule.pcmEngine.Stop(Slot);
                            break;
                        }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="note"></param>
            /// <param name="freq"></param>
            /// <returns></returns>
            private double convertFmFrequency(TaggedNoteOnEvent note, int deltaNoteNum)
            {
                int nn = note.NoteNumber + deltaNoteNum;
                int oct = note.GetNoteOctave();

                var freq = MidiManager.CalcCurrentFrequency(nn);

                //https://github.com/jotego/jt12/blob/master/doc/YM2608J.PDF
                var rv = (144 * freq * Math.Pow(2, 20) / parentModule.MasterClock) / Math.Pow(2, oct - 1);

                return rv;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2608Timbre>))]
        [DataContract]
        [InstLock]
        public class YM2608Timbre : TimbreBase, IFmTimbre
        {

            [DataMember]
            [Category("Sound")]
            [Description("Sound Type")]
            [DefaultValue(ToneType.FM)]
            public ToneType ToneType
            {
                get;
                set;
            }

            #region SSG

            [DataMember]
            [Category("Sound(SSG)")]
            [Description("SSG Sound Type")]
            [DefaultValue(SsgSoundType.PSG)]
            [TypeConverter(typeof(FlagsEnumConverter))]
            public SsgSoundType SsgSoundType
            {
                get;
                set;
            }

            #endregion

            #region FM Synth


            /// <summary>
            /// 
            /// </summary>
            /// <param name="inst"></param>
            /// <param name="timbre"></param>
            public override bool CanOpenTimbreEditor(InstrumentBase inst)
            {
                return true;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="inst"></param>
            /// <param name="timbre"></param>
            public override void OpenTimbreEditor(InstrumentBase inst)
            {
                PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(this);
                PropertyDescriptor pd = pdc["Detailed"];
                UITypeEditor ed = (UITypeEditor)pd.GetEditor(typeof(UITypeEditor));
                RuntimeServiceProvider serviceProvider = new RuntimeServiceProvider(null, this, pd);
                ed.EditValue(serviceProvider, serviceProvider, Detailed);

                //using (var f = new FormYM2608Editor((YM2608)inst, this, true))
                //{
                //    var sd = SerializeData;
                //    var r = f.ShowDialog();
                //    if (r != System.Windows.Forms.DialogResult.OK)
                //        SerializeData = sd;
                //}
            }

            public virtual bool ShouldSerializeDetailed()
            {
                return false;
            }

            [Category("Sound")]
            [Editor(typeof(YM2608UITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [IgnoreDataMember]
            [JsonIgnore]
            [DisplayName("(FM resisters)")]
            [Description("Open FM register editor.")]
            [TypeConverter(typeof(OpenEditorTypeConverter))]
            public string Detailed
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        nameof(ALG),
                        nameof(FB),
                        nameof(AMS),
                        nameof(FMS),

                        "GlobalSettings.EN",
                        "GlobalSettings.LFOEN",
                        "GlobalSettings.LFRQ",

                        "Ops[0].EN",
                        "Ops[0].AR",
                        "Ops[0].D1R",
                        "Ops[0].D2R",
                        "Ops[0].RR",
                        "Ops[0].SL",
                        "Ops[0].TL",
                        "Ops[0].RS",
                        "Ops[0].MUL",
                        "Ops[0].DT1",
                        "Ops[0].AM",
                        "Ops[0].SSG_EG",

                        "Ops[1].EN",
                        "Ops[1].AR",
                        "Ops[1].D1R",
                        "Ops[1].D2R",
                        "Ops[1].RR",
                        "Ops[1].SL",
                        "Ops[1].TL",
                        "Ops[1].RS",
                        "Ops[1].MUL",
                        "Ops[1].DT1",
                        "Ops[1].AM",
                        "Ops[1].SSG_EG",

                        "Ops[2].EN",
                        "Ops[2].AR",
                        "Ops[2].D1R",
                        "Ops[2].D2R",
                        "Ops[2].RR",
                        "Ops[2].SL",
                        "Ops[2].TL",
                        "Ops[2].RS",
                        "Ops[2].MUL",
                        "Ops[2].DT1",
                        "Ops[2].AM",
                        "Ops[2].SSG_EG",

                        "Ops[3].EN",
                        "Ops[3].AR",
                        "Ops[3].D1R",
                        "Ops[3].D2R",
                        "Ops[3].RR",
                        "Ops[3].SL",
                        "Ops[3].TL",
                        "Ops[3].RS",
                        "Ops[3].MUL",
                        "Ops[3].DT1",
                        "Ops[3].AM",
                        "Ops[3].SSG_EG");
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        nameof(ALG),
                        nameof(FB),
                        nameof(AMS),
                        nameof(FMS),

                        "GlobalSettings.EN",
                        "GlobalSettings.LFOEN",
                        "GlobalSettings.LFRQ",

                        "Ops[0].EN",
                        "Ops[0].AR",
                        "Ops[0].D1R",
                        "Ops[0].D2R",
                        "Ops[0].RR",
                        "Ops[0].SL",
                        "Ops[0].TL",
                        "Ops[0].RS",
                        "Ops[0].MUL",
                        "Ops[0].DT1",
                        "Ops[0].AM",
                        "Ops[0].SSG_EG",

                        "Ops[1].EN",
                        "Ops[1].AR",
                        "Ops[1].D1R",
                        "Ops[1].D2R",
                        "Ops[1].RR",
                        "Ops[1].SL",
                        "Ops[1].TL",
                        "Ops[1].RS",
                        "Ops[1].MUL",
                        "Ops[1].DT1",
                        "Ops[1].AM",
                        "Ops[1].SSG_EG",

                        "Ops[2].EN",
                        "Ops[2].AR",
                        "Ops[2].D1R",
                        "Ops[2].D2R",
                        "Ops[2].RR",
                        "Ops[2].SL",
                        "Ops[2].TL",
                        "Ops[2].RS",
                        "Ops[2].MUL",
                        "Ops[2].DT1",
                        "Ops[2].AM",
                        "Ops[2].SSG_EG",

                        "Ops[3].EN",
                        "Ops[3].AR",
                        "Ops[3].D1R",
                        "Ops[3].D2R",
                        "Ops[3].RR",
                        "Ops[3].SL",
                        "Ops[3].TL",
                        "Ops[3].RS",
                        "Ops[3].MUL",
                        "Ops[3].DT1",
                        "Ops[3].AM",
                        "Ops[3].SSG_EG");
                }
            }

            private String patchFile;

            [DataMember]
            [Category("Sound")]
            [Description("FM Patch file info.\r\n*Warning* May contain privacy information. Check the options dialog.")]
            [ReadOnly(true)]
            public String PatchInfo
            {
                get
                {
                    if (Settings.Default.DoNotUsePrivacySettings)
                        return null;

                    return patchFile;
                }
                set
                {
                    patchFile = value;
                }
            }

            private byte f_ALG;

            [DataMember]
            [Category("Sound(FM)")]
            [Description("Algorithm (0-7)\r\n" +
                "0: 1->2->3->4 (for Distortion guitar sound)\r\n" +
                "1: (1+2)->3->4 (for Harp, PSG sound)\r\n" +
                "2: (1+(2->3))->4 (for Bass, electric guitar, brass, piano, woods sound)\r\n" +
                "3: ((1->2)+3)->4 (for Strings, folk guitar, chimes sound)\r\n" +
                "4: (1->2)+(3->4) (for Flute, bells, chorus, bass drum, snare drum, tom-tom sound)\r\n" +
                "5: (1->2)+(1->3)+(1->4) (for Brass, organ sound)\r\n" +
                "6: (1->2)+3+4 (for Xylophone, tom-tom, organ, vibraphone, snare drum, base drum sound)\r\n" +
                "7: 1+2+3+4 (for Pipe organ sound)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 7)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte ALG
            {
                get
                {
                    return f_ALG;
                }
                set
                {
                    f_ALG = (byte)(value & 7);
                }
            }

            private byte f_FB;

            [DataMember]
            [Category("Sound(FM)")]
            [Description("Feedback (0-7)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 7)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte FB
            {
                get
                {
                    return f_FB;
                }
                set
                {
                    f_FB = (byte)(value & 7);
                }
            }

            private byte f_AMS;

            [DataMember]
            [Category("Sound(FM)")]
            [Description("Amplitude Modulation Sensitivity (0-3)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 3)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte AMS
            {
                get
                {
                    return f_AMS;
                }
                set
                {
                    f_AMS = (byte)(value & 3);
                }
            }

            private byte f_FMS;

            [DataMember]
            [Category("Sound(FM)")]
            [Description("Frequency Modulation Sensitivity (0-7)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 7)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte FMS
            {
                get
                {
                    return f_FMS;
                }
                set
                {
                    f_FMS = (byte)(value & 7);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Operators")]
            [TypeConverter(typeof(ExpandableCollectionConverter))]
            [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
            [DisplayName("Operators[Ops]")]
            public YM2608Operator[] Ops
            {
                get;
                set;
            }

            public virtual bool ShouldSerializeOps()
            {
                bool ret = false;
                Parallel.For(0, Ops.Length, (i, loopState) =>
                {
                    if (loopState.IsStopped)
                        return;
                    if (!string.Equals(JsonConvert.SerializeObject(Ops[i], Formatting.Indented), "{}"))
                    {
                        loopState.Stop();
                        ret = true;
                        return;
                    }
                });
                return ret;
            }

            public virtual void ResetOps()
            {
                var ops = new YM2608Operator[] {
                    new YM2608Operator(),
                    new YM2608Operator(),
                    new YM2608Operator(),
                    new YM2608Operator() };
                for (int i = 0; i < Ops.Length; i++)
                    Ops[i].InjectFrom(new LoopInjection(), ops[i]);
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        typeof(UITypeEditor)), Localizable(false)]
            [IgnoreDataMember]
            [JsonIgnore]
            [Category("Sound(FM)")]
            [Description("You can copy and paste this text data to other same type timber.\r\n" +
        "ALG, FB, AR, D1R(DR), D2R(SR), RR, SL, TL, RS(KS), MUL, DT1, AM(AMS), SSG_EG, ...\r\n" +
                "You can use comma or space chars as delimiter.")]
            public string MmlSerializeData
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        nameof(ALG),
                        nameof(FB),
                        "Ops[0].AR",
                        "Ops[0].D1R",
                        "Ops[0].D2R",
                        "Ops[0].RR",
                        "Ops[0].SL",
                        "Ops[0].TL",
                        "Ops[0].RS",
                        "Ops[0].MUL",
                        "Ops[0].DT1",
                        "Ops[0].AM",
                        "Ops[0].SSG_EG",

                        "Ops[1].AR",
                        "Ops[1].D1R",
                        "Ops[1].D2R",
                        "Ops[1].RR",
                        "Ops[1].SL",
                        "Ops[1].TL",
                        "Ops[1].RS",
                        "Ops[1].MUL",
                        "Ops[1].DT1",
                        "Ops[1].AM",
                        "Ops[1].SSG_EG",

                        "Ops[2].AR",
                        "Ops[2].D1R",
                        "Ops[2].D2R",
                        "Ops[2].RR",
                        "Ops[2].SL",
                        "Ops[2].TL",
                        "Ops[2].RS",
                        "Ops[2].MUL",
                        "Ops[2].DT1",
                        "Ops[2].AM",
                        "Ops[2].SSG_EG",

                        "Ops[3].AR",
                        "Ops[3].D1R",
                        "Ops[3].D2R",
                        "Ops[3].RR",
                        "Ops[3].SL",
                        "Ops[3].TL",
                        "Ops[3].RS",
                        "Ops[3].MUL",
                        "Ops[3].DT1",
                        "Ops[3].AM",
                        "Ops[3].SSG_EG",

                        nameof(AMS),
                        nameof(FMS));
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        nameof(ALG),
                        nameof(FB),
                        "Ops[0].AR",
                        "Ops[0].D1R",
                        "Ops[0].D2R",
                        "Ops[0].RR",
                        "Ops[0].SL",
                        "Ops[0].TL",
                        "Ops[0].RS",
                        "Ops[0].MUL",
                        "Ops[0].DT1",
                        "Ops[0].AM",
                        "Ops[0].SSG_EG",

                        "Ops[1].AR",
                        "Ops[1].D1R",
                        "Ops[1].D2R",
                        "Ops[1].RR",
                        "Ops[1].SL",
                        "Ops[1].TL",
                        "Ops[1].RS",
                        "Ops[1].MUL",
                        "Ops[1].DT1",
                        "Ops[1].AM",
                        "Ops[1].SSG_EG",

                        "Ops[2].AR",
                        "Ops[2].D1R",
                        "Ops[2].D2R",
                        "Ops[2].RR",
                        "Ops[2].SL",
                        "Ops[2].TL",
                        "Ops[2].RS",
                        "Ops[2].MUL",
                        "Ops[2].DT1",
                        "Ops[2].AM",
                        "Ops[2].SSG_EG",

                        "Ops[3].AR",
                        "Ops[3].D1R",
                        "Ops[3].D2R",
                        "Ops[3].RR",
                        "Ops[3].SL",
                        "Ops[3].TL",
                        "Ops[3].RS",
                        "Ops[3].MUL",
                        "Ops[3].DT1",
                        "Ops[3].AM",
                        "Ops[3].SSG_EG",

                        nameof(AMS),
                        nameof(FMS));
                }
            }

            #endregion

            [DataMember]
            [Category("Sound(ADPCM-B)")]
            [Description("Set ADPCM-B base frequency [Hz]")]
            [DefaultValue(typeof(double), "440")]
            [DoubleSlideParametersAttribute(100, 2000, 1)]
            [EditorAttribute(typeof(DoubleSlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public double BaseFreqency
            {
                get;
                set;
            } = 440;

            private bool f_LoopEnable;

            [DataMember]
            [Category("Sound(ADPCM-B)")]
            [Description("ADPCM-B Loop enable")]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DefaultValue(false)]
            public bool LoopEnable
            {
                get
                {
                    return f_LoopEnable;
                }
                set
                {
                    f_LoopEnable = value;
                }
            }

            private byte[] f_PcmData = new byte[0];

            [TypeConverter(typeof(LoadDataTypeConverter))]
            [Editor(typeof(OpnAdpcmFileLoaderUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DataMember]
            [Category("Sound(ADPCM-B)")]
            [Description("YM2608 ADPCM-B DATA. 55.5 kHz sampling rate at 12-bit from 4-bit data.\r\n" +
                "Or, wave file 16bit mono. MAX 64KB.")]
            [PcmFileLoaderEditor("Audio File(*.pcmb, *.wav)|*.pcmb;*.wav", 0, 16, 1, 65535)]
            public byte[] PcmData
            {
                get
                {
                    return f_PcmData;
                }
                set
                {
                    if (!Util.Utility.IsEquals(f_PcmData, value))
                    {
                        f_PcmData = value;

                        var inst = (YM2608)this.Instrument;
                        if (inst != null)
                            inst.updatePcmData(this);
                    }
                }
            }

            public bool ShouldSerializePcmData()
            {
                return f_PcmData.Length != 0;
            }

            public void ResetPcmData()
            {
                PcmData = new byte[0];
            }

            private String pcmDataInfo;

            [DataMember]
            [Category("Sound(ADPCM-B)")]
            [Description("PcmData information.\r\n*Warning* May contain privacy information. Check the options dialog.")]
            [ReadOnly(true)]
            public String PcmDataInfo
            {
                get
                {
                    if (Settings.Default.DoNotUsePrivacySettings)
                        return null;
                    return pcmDataInfo;
                }
                set
                {
                    pcmDataInfo = value;
                }
            }

            [DataMember]
            [Browsable(false)]
            public uint PcmAddressStart
            {
                get;
                set;
            }

            [DataMember]
            [Browsable(false)]
            public uint PcmAddressEnd
            {
                get;
                set;
            }

            [DataMember]
            [Category("Sound(DAC)")]
            [Description("DAC Settings")]
            public YM2608DacSettings DAC
            {
                get;
                set;
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [DefaultValue(false)]
            [Description("Use MIDI Expresion for Modulator Total Level.")]
            [Browsable(true)]
            public override bool UseExprForModulator
            {
                get;
                set;
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Traget modulator for UseExprForModulator.")]
            [Browsable(true)]
            [Editor(typeof(FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public ExprModulators ExprTargetModulators
            {
                get;
                set;
            } = ExprModulators.All;

            public virtual bool ShouldSerializeExprTargetModulators()
            {
                return ExprTargetModulators != ExprModulators.All;
            }

            public virtual void ResetExprTargetModulators()
            {
                ExprTargetModulators = ExprModulators.All;
            }

            [FlagsAttribute]
            public enum ExprModulators
            {
                [Description("Operator 1[Ops[0]")]
                Ops0 = 1,
                [Description("Operator 2[Ops[1]")]
                Ops1 = 2,
                [Description("Operator 3[Ops[2]")]
                Ops2 = 4,
                [Description("All")]
                All = 7,
            }

            [DataMember]
            [Category("Chip(Global)")]
            [Description("Global Settings")]
            public YM2608GlobalSettings GlobalSettings
            {
                get;
                set;
            }

            /// <summary>
            /// 
            /// </summary>
            public YM2608Timbre()
            {
                Ops = new YM2608Operator[] {
                    new YM2608Operator(),
                    new YM2608Operator(),
                    new YM2608Operator(),
                    new YM2608Operator() };
                GlobalSettings = new YM2608GlobalSettings();
                SsgSoundType = SsgSoundType.PSG;
                DAC = new YM2608DacSettings();
            }

            protected override void InitializeFxS()
            {
                this.SDS.FxS = new YM2608FxSettings();
            }

            public override void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<YM2608Timbre>(serializeData);
                    this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(Exception))
                        throw;
                    else if (ex.GetType() == typeof(SystemException))
                        throw;


                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
        }


        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2608FxSettings>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [DataContract]
        [InstLock]
        public class YM2608FxSettings : BasicFxSettings
        {

            private string f_SoundTypeEnvelopes;

            [DataMember]
            [Description("Set dutysound type envelop by text. Input sound type value and split it with space like the FamiTracker.\r\n" +
                       "1:PSG 2:NOISE 4:ENVELOPE \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 7)]
            public string SoundTypeEnvelopes
            {
                get
                {
                    return f_SoundTypeEnvelopes;
                }
                set
                {
                    if (f_SoundTypeEnvelopes != value)
                    {
                        SoundTypeEnvelopesRepeatPoint = -1;
                        SoundTypeEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            SoundTypeEnvelopesNums = new int[] { };
                            f_SoundTypeEnvelopes = string.Empty;
                            return;
                        }
                        f_SoundTypeEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                SoundTypeEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                SoundTypeEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    else if (v > 7)
                                        v = 7;
                                    vs.Add(v);
                                }
                            }
                        }
                        SoundTypeEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < SoundTypeEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (SoundTypeEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (SoundTypeEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < SoundTypeEnvelopesNums.Length)
                                sb.Append(SoundTypeEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_SoundTypeEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeSoundTypeEnvelopes()
            {
                return !string.IsNullOrEmpty(SoundTypeEnvelopes);
            }

            public void ResetSoundTypeEnvelopes()
            {
                SoundTypeEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] SoundTypeEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int SoundTypeEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int SoundTypeEnvelopesReleasePoint { get; set; } = -1;

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override AbstractFxEngine CreateEngine()
            {
                return new YM2608FxEngine(this);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public class YM2608FxEngine : BasicFxEngine
        {
            private YM2608FxSettings settings;

            /// <summary>
            /// 
            /// </summary>
            public YM2608FxEngine(YM2608FxSettings settings) : base(settings)
            {
                this.settings = settings;
            }

            private uint f_SoundType;

            public byte? SoundType
            {
                get;
                private set;
            }

            protected override bool ProcessCore(SoundBase sound, bool isKeyOff, bool isSoundOff)
            {
                bool process = base.ProcessCore(sound, isKeyOff, isSoundOff);

                SoundType = null;
                if (settings.SoundTypeEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.SoundTypeEnvelopesNums.Length;
                        if (settings.SoundTypeEnvelopesReleasePoint >= 0)
                            vm = settings.SoundTypeEnvelopesReleasePoint;
                        if (f_SoundType >= vm)
                        {
                            if (settings.SoundTypeEnvelopesRepeatPoint >= 0)
                                f_SoundType = (uint)settings.SoundTypeEnvelopesRepeatPoint;
                            else
                                f_SoundType = (uint)vm - 1;
                        }
                    }
                    else
                    {
                        if (f_SoundType < settings.SoundTypeEnvelopesNums.Length)
                        {
                            if (settings.SoundTypeEnvelopesReleasePoint >= 0 && f_SoundType <= (uint)settings.SoundTypeEnvelopesReleasePoint)
                                f_SoundType = (uint)settings.SoundTypeEnvelopesReleasePoint;
                            else if (settings.SoundTypeEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_SoundType = (uint)settings.SoundTypeEnvelopesNums.Length;
                        }
                    }
                    if (f_SoundType < settings.SoundTypeEnvelopesNums.Length)
                    {
                        int vol = settings.SoundTypeEnvelopesNums[f_SoundType++];

                        SoundType = (byte)vol;
                        process = true;
                    }
                }

                return process;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2608Operator>))]
        [DataContract]
        [InstLock]
        public class YM2608Operator : ContextBoundObject, ISerializeDataSaveLoad
        {

            private byte f_Enable = 1;

            /// <summary>
            /// Enable(0-1)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Whether this operator enable or not")]
            [DefaultValue((byte)1)]
            public byte Enable
            {
                get
                {
                    return f_Enable;
                }
                set
                {
                    f_Enable = (byte)(value & 1);
                }
            }

            [IgnoreDataMember]
            [JsonIgnore]
            [Browsable(false)]
            public byte EN
            {
                get
                {
                    return Enable;
                }
                set
                {
                    Enable = value;
                }
            }

            private byte f_AR;

            /// <summary>
            /// Attack Rate(0-31)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Attack Rate (0-31)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 31)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte AR
            {
                get
                {
                    return f_AR;
                }
                set
                {
                    f_AR = (byte)(value & 31);
                }
            }

            private byte f_D1R;

            /// <summary>
            /// Decay rate(0-31)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Decay Rate (0-31)")]
            [DisplayName("D1R(DR)[D1R]")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 31)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte D1R
            {
                get
                {
                    return f_D1R;
                }
                set
                {
                    f_D1R = (byte)(value & 31);
                }
            }

            private byte f_D2R;

            /// <summary>
            /// Sustain Rate(2nd Decay Rate)(0-31)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Sustain Rate(2nd Decay Rate) (0-31)")]
            [DisplayName("D2R(SR)[D2R]")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 31)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte D2R
            {
                get
                {
                    return f_D2R;
                }
                set
                {
                    f_D2R = (byte)(value & 31);
                }
            }

            private byte f_RR;

            /// <summary>
            /// release rate(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Release Rate (0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte RR
            {
                get
                {
                    return f_RR;
                }
                set
                {
                    f_RR = (byte)(value & 15);
                }
            }

            private byte f_SL;

            /// <summary>
            /// sustain level(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Sustain Level(0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte SL
            {
                get
                {
                    return f_SL;
                }
                set
                {
                    f_SL = (byte)(value & 15);
                }
            }

            private byte f_TL;

            /// <summary>
            /// Total Level(0-127)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Total Level (0-127)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 127)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte TL
            {
                get
                {
                    return f_TL;
                }
                set
                {
                    f_TL = (byte)(value & 127);
                }
            }

            private byte f_RS;

            /// <summary>
            /// Rate Scaling(0-3)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Rate Scaling (0-3)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 3)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte RS
            {
                get
                {
                    return f_RS;
                }
                set
                {
                    f_RS = (byte)(value & 3);
                }
            }

            private byte f_MUL;

            /// <summary>
            /// Multiply(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Multiply (0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte MUL
            {
                get
                {
                    return f_MUL;
                }
                set
                {
                    f_MUL = (byte)(value & 15);
                }
            }

            private byte f_DT1 = 4;

            /// <summary>
            /// Detune1(0-7)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("DeTune 1 (7(-3),6(-2),5(-1),4(+0),0(+0),1(+1),2(+2),3(+3))")]
            [DefaultValue((byte)4)]
            [SlideParametersAttribute(1, 7)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte DT1
            {
                get
                {
                    return f_DT1;
                }
                set
                {
                    f_DT1 = (byte)(value & 7);
                }
            }


            private byte f_AM;

            /// <summary>
            /// amplitude modulation sensivity(0-1)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("Amplitude Modulation Sensivity (0-1)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte AM
            {
                get
                {
                    return f_AM;
                }
                set
                {
                    f_AM = (byte)(value & 1);
                }
            }

            private byte f_SSG_EG;

            /// <summary>
            /// SSG-EG(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound(FM)")]
            [Description("SSG-EG (0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte SSG_EG
            {
                get
                {
                    return f_SSG_EG;
                }
                set
                {
                    f_SSG_EG = (byte)(value & 15);
                }
            }

            [IgnoreDataMember]
            [JsonIgnore]
            [Browsable(false)]
            public byte SSG
            {
                get
                {
                    return SSG_EG;
                }
                set
                {
                    SSG_EG = value;
                }
            }

            #region Etc

            [DataMember]
            [Description("Memo")]
            [DefaultValue(null)]
            public string Memo
            {
                get;
                set;
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                typeof(UITypeEditor)), Localizable(false)]
            [IgnoreDataMember]
            [JsonIgnore]
            [Description("You can copy and paste this text data to other same type timber.\r\n" +
                "AR, D1R(DR), D2R(SR), RR, SL, TL, RS(KS), MUL, DT1, AM(AMS), SSG_EG\r\n" +
                "You can use comma or space chars as delimiter.")]
            public string MmlSerializeData
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        nameof(AR),
                        nameof(D1R),
                        nameof(D2R),
                        nameof(RR),
                        nameof(SL),
                        nameof(TL),
                        nameof(RS),
                        nameof(MUL),
                        nameof(DT1),
                        nameof(AM),
                        nameof(SSG_EG));
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        nameof(AR),
                        nameof(D1R),
                        nameof(D2R),
                        nameof(RR),
                        nameof(SL),
                        nameof(TL),
                        nameof(RS),
                        nameof(MUL),
                        nameof(DT1),
                        nameof(AM),
                        nameof(SSG_EG));
                }
            }

            [Editor(typeof(SerializeSaveUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [IgnoreDataMember]
            [JsonIgnore]
            [DisplayName("(Save settings)")]
            [Description("Save all parameters as serialize data to the file.")]
            [TypeConverter(typeof(OpenFileBrowserTypeConverter))]
            public string SerializeDataSave
            {
                get
                {
                    return SerializeData;
                }
                set
                {
                    SerializeData = value;
                }
            }


            [Editor(typeof(SerializeLoadUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [IgnoreDataMember]
            [JsonIgnore]
            [DisplayName("(Load settings)")]
            [Description("Load all parameters as serialize data from the file.")]
            [TypeConverter(typeof(OpenFileBrowserTypeConverter))]
            public string SerializeDataLoad
            {
                get
                {
                    return SerializeData;
                }
                set
                {
                    SerializeData = value;
                }
            }

            public virtual bool ShouldSerializeSerializeDataSave()
            {
                return false;
            }

            public virtual bool ShouldSerializeSerializeDataLoad()
            {
                return false;
            }

            [Browsable(false)]
            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                typeof(UITypeEditor)), Localizable(false)]
            [IgnoreDataMember]
            [JsonIgnore]
            [Description("You can copy and paste this text data to other same type timber.\r\nNote: Open dropdown editor then copy all text and paste to dropdown editor. Do not copy and paste one liner text.")]
            public string SerializeData
            {
                get
                {
                    return JsonConvert.SerializeObject(this, Formatting.Indented);
                }
                set
                {
                    RestoreFrom(value);
                }
            }

            public void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<YM2608Operator>(serializeData);
                    this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(Exception))
                        throw;
                    else if (ex.GetType() == typeof(SystemException))
                        throw;


                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }

            #endregion

        }

        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2608GlobalSettings>))]
        [DataContract]
        [InstLock]
        public class YM2608GlobalSettings : ContextBoundObject
        {
            [DataMember]
            [Category("Chip(Global)")]
            [Description("Override global settings")]
            [DefaultValue(false)]
            public bool Enable
            {
                get;
                set;
            }

            [IgnoreDataMember]
            [JsonIgnore]
            [Browsable(false)]
            public byte EN
            {
                get
                {
                    return Enable ? (byte)1 : (byte)0;
                }
                set
                {
                    Enable = value == 0 ? false : true;
                }
            }


            private bool? f_SyncWithNoteFrequency;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Chip(Global)")]
            [Description("Set Env frequency to Note on (nearly) frequency.")]
            [DefaultValue(null)]
            public bool? SyncWithNoteFrequency
            {
                get => f_SyncWithNoteFrequency;
                set
                {
                    f_SyncWithNoteFrequency = value;
                }
            }

            private double? f_SyncWithNoteFrequencyMultiple;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Chip(Global)")]
            [Description("Set Env frequency divider for SyncWithNoteFrequency prop.")]
            [DoubleSlideParametersAttribute(0d, 5d, 1d)]
            [EditorAttribute(typeof(DoubleSlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DefaultValue(null)]
            public double? SyncWithNoteFrequencyDivider
            {
                get => f_SyncWithNoteFrequencyMultiple;
                set
                {
                    f_SyncWithNoteFrequencyMultiple = value;
                }
            }

            private byte? f_EnvelopeFrequencyCoarse;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Chip(SSG)")]
            [DefaultValue(null)]
            [Description("Set Envelope Coarse Frequency")]
            [SlideParametersAttribute(0, 255)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte? EnvelopeFrequencyCoarse
            {
                get => f_EnvelopeFrequencyCoarse;
                set
                {
                    if (value.HasValue)
                    {
                        if (f_EnvelopeFrequencyCoarse != value)
                            f_EnvelopeFrequencyCoarse = value;
                    }
                    else
                        f_EnvelopeFrequencyCoarse = value;
                }
            }

            private byte? f_EnvelopeFrequencyFine;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Chip(SSG)")]
            [Description("Set Envelope Fine Frequency")]
            [SlideParametersAttribute(0, 255)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DefaultValue(null)]
            public byte? EnvelopeFrequencyFine
            {
                get => f_EnvelopeFrequencyFine;
                set
                {
                    if (value.HasValue)
                    {
                        if (f_EnvelopeFrequencyFine != value)
                            f_EnvelopeFrequencyFine = value;
                    }
                    else
                        f_EnvelopeFrequencyFine = value;
                }
            }

            private byte? f_EnvelopeType;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Chip(SSG)")]
            [Description("Set Envelope Type")]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DefaultValue(null)]
            public byte? EnvelopeType
            {
                get => f_EnvelopeType;
                set
                {
                    if (value.HasValue)
                    {
                        byte v = (byte)(value & 15);
                        if (f_EnvelopeType != v)
                            f_EnvelopeType = v;
                    }
                    else
                        f_EnvelopeType = value;
                }
            }

            private byte? f_LFOEN;

            /// <summary>
            /// LFOEN (0-1)
            /// </summary>
            [DataMember]
            [Category("Chip(FM)")]
            [Description("LFO Enable (0:Off 1:Enable)")]
            [DefaultValue(null)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte? LFOEN
            {
                get
                {
                    return f_LFOEN;
                }
                set
                {
                    byte? v = value;
                    if (value.HasValue)
                        v = (byte)(value & 1);
                    f_LFOEN = v;
                }
            }

            private byte? f_LFRQ;

            /// <summary>
            /// LFRQ (0-7)
            /// </summary>
            [DataMember]
            [Category("Chip(FM)")]
            [Description("LFO Freq (0-7)\r\n" +
                "0:	3.82 Hz\r\n" +
                "1: 5.33 Hz\r\n" +
                "2: 5.77 Hz\r\n" +
                "3: 6.11 Hz\r\n" +
                "4: 6.60 Hz\r\n" +
                "5: 9.23 Hz\r\n" +
                "6: 46.11 Hz\r\n" +
                "7: 69.22 Hz\r\n")]
            [DefaultValue(null)]
            [SlideParametersAttribute(0, 7)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte? LFRQ
            {
                get
                {
                    return f_LFRQ;
                }
                set
                {
                    byte? v = value;
                    if (value.HasValue)
                        v = (byte)(value & 7);
                    f_LFRQ = v;
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum SsgSoundType
        {
            NONE = 0,
            PSG = 1,
            NOISE = 2,
            ENVELOPE = 4,
        }

        /// <summary>
        /// 
        /// </summary>
        public enum ToneType
        {
            FM,
            SSG,
            RHYTHM,
            ADPCM_B,
            DAC,
        }


        /// <summary>
        /// 
        /// </summary>
        public enum BoardType
        {
            Internal,
            SoundBoard2,
            InternalOPN,
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        [JsonIgnore]
        public override bool CanImportToneFile
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timbre"></param>
        /// <param name="tone"></param>
        public override void ImportToneFile(TimbreBase timbre, Tone tone)
        {
            YM2608Timbre tim = (YM2608Timbre)timbre;

            tim.ToneType = ToneType.FM;

            tim.ALG = (byte)tone.AL;
            tim.FB = (byte)tone.FB;
            tim.AMS = (byte)tone.AMS;
            tim.FMS = (byte)tone.PMS;
            tim.GlobalSettings.Enable = false;
            tim.GlobalSettings.LFRQ = null;
            tim.GlobalSettings.LFOEN = null;

            for (int i = 0; i < 4; i++)
            {
                tim.Ops[i].Enable = 1;
                tim.Ops[i].AR = (byte)tone.aOp[i].AR;
                tim.Ops[i].D1R = (byte)tone.aOp[i].DR;
                tim.Ops[i].D2R = tone.aOp[i].SR < 0 ? (byte)0 : (byte)tone.aOp[i].SR;
                tim.Ops[i].RR = (byte)tone.aOp[i].RR;
                tim.Ops[i].SL = (byte)tone.aOp[i].SL;
                tim.Ops[i].TL = (byte)tone.aOp[i].TL;
                tim.Ops[i].RS = (byte)tone.aOp[i].KS;
                tim.Ops[i].MUL = (byte)tone.aOp[i].ML;
                tim.Ops[i].DT1 = (byte)tone.aOp[i].DT;
                tim.Ops[i].AM = (byte)tone.aOp[i].AM;
                tim.Ops[i].SSG = (byte)tone.aOp[i].SSG;
            }
            timbre.TimbreName = tone.Name;
        }


        private YM2608CustomToneImporter importer;

        /// <summary>
        /// 
        /// </summary>
        public override CustomToneImporter CustomToneImporter
        {
            get
            {
                if (importer == null)
                {
                    importer = new YM2608CustomToneImporter();
                }
                return importer;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2608CustomToneImporter : FmToneImporter
        {
            /// <summary>
            /// 
            /// </summary>
            public override string ExtensionsFilterExt
            {
                get => "*.mopn;*.mopm";
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="file"></param>
            /// <returns></returns>
            public override IEnumerable<Tone> ImportToneFile(string file)
            {
                IEnumerable<Tone> tones = base.ImportToneFile(file);
                if (tones != null)
                    return tones;

                string ext = System.IO.Path.GetExtension(file);

                if (ext.ToUpper(CultureInfo.InvariantCulture).Equals(".MOPM"))
                {
                    try
                    {
                        string txt = System.IO.File.ReadAllText(file);
                        StringReader rs = new StringReader(txt);

                        string ftname = rs.ReadLine();
                        if ("*.mopm" == ftname)
                        {
                            string ver = rs.ReadLine();
                            if (ver != "1.0")
                                throw new InvalidDataException();
                            int num = int.Parse(rs.ReadLine());
                            List<string> lines = new List<string>();
                            List<Tone> ts = new List<Tone>();
                            int progNo = 0;
                            while (true)
                            {
                                string line = rs.ReadLine();
                                if (line == null || line == "-")
                                {
                                    if (lines.Count == 0)
                                        break;
                                    Tone t = new Tone();
                                    var mml = lines.ToArray();

                                    var general = mml[1].Split(',');
                                    mml[1] = String.Format("{0},{1},{2},0,0,,", general[0], general[1], general[2]);
                                    for (int i = 2; i < mml.Length; i++)
                                    {
                                        var op = mml[i].Split(',');
                                        mml[i] = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},0",
                                            op[0], op[1], op[2], op[3], op[4], op[5], op[6], op[7], op[8], op[9], op[10]);
                                    }
                                    t.MML = mml;

                                    t.Name = t.MML[0];
                                    t.Number = progNo++;
                                    ts.Add(t);
                                    lines.Clear();
                                    if (line == null)
                                        break;
                                    continue;
                                }
                                lines.Add(line);
                            }
                            tones = ts;
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
                }
                return tones;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="tones"></param>
            /// <returns></returns>
            public override IEnumerable<TimbreBase> ImportToneFileAsTimbre(string file)
            {
                IEnumerable<Tone> tones = ImportToneFile(file);
                if (tones != null)
                {
                    List<TimbreBase> rv = new List<TimbreBase>();
                    foreach (var t in tones)
                    {
                        YM2608Timbre tim = new YM2608Timbre();
                        tim.TimbreName = t.MML[0];
                        tim.Detailed = t.MML[1] + "," + t.MML[2] + "," + t.MML[3] + "," + t.MML[4] + "," + t.MML[5];
                        rv.Add(tim);
                    }
                    return rv;
                }
                return null;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferData"></param>
        /// <param name="saddr"></param>
        /// <param name="fp"></param>
        private void enablePseudoDacYM2608(bool enable)
        {
            lock (sndEnginePtrLock)
                lastTransferPcmData = new byte[] { };

            YM2608WriteData(UnitNumber, 0x00, 0, 3, 3, false);  //RESET
            if (enable)
            {
                //https://www.piece-me.org/piece-lab/adpcm/adpcm2.html
                //MAX Attenuation
                byte[] adpcmdata = new byte[32];
                for (int i = 0; i < adpcmdata.Length; i++)
                    adpcmdata[i] = 0x80;
                sendPcmData(adpcmdata, 0, null);

                //*
                //ADPCM mode
                YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x17, false);        //ENA FLAG BRDY
                YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x80, false);   //RESET FLAGS
                YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x80, false);   //CPU->OPNA
                YM2608WriteData(UnitNumber, 0x01, 0, 3, 0xC0, false);   //LR

                // (f / 55.5) * 65536
                // 8KHz = 9447
                //int f = (int)Math.Round((44.1 / 55.5) * 65536);
                //YM2608WriteData(comPort, 0x09, 0, 3, (byte)(f & 0xff), false);   //14KHz
                //YM2608WriteData(comPort, 0x0A, 0, 3, (byte)((f >> 8) & 0xff), false);   //14KHz

                YM2608WriteData(UnitNumber, 0x09, 0, 3, 0xff, false);   //55.5KHz
                YM2608WriteData(UnitNumber, 0x0A, 0, 3, 0xff, false);   //55.5KHz

                YM2608WriteData(UnitNumber, 0x0B, 0, 3, 0x00, false);   // Volume 0

                //playAdpcmDataYM2608(0, 0, false);
                //Thread.Sleep(1);
                playAdpcmDataYM2608(0, 0, false);

                /*
                deferredWriteOPNA_P1(comPort, 0x08, 0xff);  //255
                deferredWriteOPNA_P1(comPort, 0x08, 0x77);  //119
                deferredWriteOPNA_P1(comPort, 0x08, 0x77);  //119
                deferredWriteOPNA_P1(comPort, 0x08, 0x77);  //119
                deferredWriteOPNA_P1(comPort, 0x08, 0xff);  //255
                deferredWriteOPNA_P1(comPort, 0x08, 0x70);  //112
                deferredWriteOPNA_P1(comPort, 0x08, 0x80);  //128
                //*/

                /*/
                //* DAC mode
                //flag
                deferredWriteOPNA_P1(comPort, 0x10, 0x1B);   //ENA FLAG EOS
                deferredWriteOPNA_P1(comPort, 0x10, 0x80);   //RESET FLAGS
                deferredWriteOPNA_P1(comPort, 0x06, 0xF4);   //16KHz
                deferredWriteOPNA_P1(comPort, 0x07, 0x01);   //16KHz
                deferredWriteOPNA_P1(comPort, 0x01, 0xCC);   //Sart
                deferredWriteOPNA_P1(comPort, 0x0B, 0xff);   // Volume 

                //*/
            }
        }

        private void playAdpcmDataYM2608(uint saddr, uint eaddr, bool loop)
        {
            YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x1B, false); //CLEAR FLAGS
            YM2608WriteData(UnitNumber, 0x10, 0, 3, 0x80, false); //IRQ RESET

            YM2608WriteData(UnitNumber, 0x00, 0, 3, 0x20, false); //ACCESS TO MEM

            //pcm start
            YM2608WriteData(UnitNumber, 0x02, 0, 3, (byte)((saddr >> 5) & 0xff), false);
            YM2608WriteData(UnitNumber, 0x03, 0, 3, (byte)((saddr >> (5 + 8) & 0xff)), false);
            //pcm end
            YM2608WriteData(UnitNumber, 0x04, 0, 3, (byte)((eaddr >> 5) & 0xff), false);
            YM2608WriteData(UnitNumber, 0x05, 0, 3, (byte)((eaddr >> (5 + 8)) & 0xff), false);
            //limit
            YM2608WriteData(UnitNumber, 0x0C, 0, 3, (byte)(0xff), false);
            YM2608WriteData(UnitNumber, 0x0D, 0, 3, (byte)(0xff), false);

            //KeyOn
            YM2608WriteData(UnitNumber, 0x00, 0, 3, (byte)(0x80 | 0x20 | (loop ? (byte)0x10 : (byte)0x00)), false);   //PLAY, ACCESS TO MEM, LOOP
        }

        private PcmEngine pcmEngine;

        private const int MAX_DAC_VOICES = 8;

        /// <summary>
        /// 
        /// </summary>
        private class PcmEngine : IDisposable
        {
            private YM2608Sound sound;

            private object engineLockObject;

            private bool stopEngineFlag;

            private bool disposedValue;


            private YM2608 parentModule;

            private uint unitNumber;

            private SampleData[] currentSampleData;

            /// <summary>
            /// 
            /// </summary>
            public PcmEngine(YM2608 parentModule)
            {
                this.parentModule = parentModule;
                unitNumber = parentModule.UnitNumber;
                engineLockObject = new object();
                stopEngineFlag = true;
                currentSampleData = new SampleData[YM2608.MAX_DAC_VOICES];
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
                    t.Priority = ThreadPriority.AboveNormal;
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
            public void Play(YM2608Sound sound, TaggedNoteOnEvent note, int slot, YM2608Timbre pcmTimbre, double freq, double volume)
            {
                this.sound = sound;
                lock (engineLockObject)
                {
                    /*
                    int nn = (int)NoteNames.C3;
                    double noteNum = Math.Pow(2.0, ((double)nn - 69.0) / 12.0);
                    double basefreq = 440.0 * noteNum;
                    */
                    double basefreq = ((YM2608Timbre)sound.Timbre).DAC.BaseFreqency;

                    var sd = new SampleData(sound, note, pcmTimbre.DAC.PcmData, pcmTimbre.DAC.SampleRate, false, basefreq);
                    sd.Gain = pcmTimbre.DAC.PcmGain;
                    sd.Pitch = freq / basefreq;
                    sd.Volume = volume;
                    sd.LoopEnabled = pcmTimbre.DAC.LoopEnabled;
                    sd.LoopPoint = pcmTimbre.DAC.LoopPoint;
                    currentSampleData[slot] = sd;

                    //var data = new PortWriteData() { Type = (byte)6, Address = (byte)slot, Data = 1, Tag = new Dictionary<string, object>() };
                    //data.Tag["PcmData"] = pcmTimbre.DAC.PcmData;
                    //data.Tag["PcmGain"] = pcmTimbre.DAC.PcmGain;
                    //parentModule.XgmWriter?.RecordData(data);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pcmData"></param>
            public void Pitch(int slot, double freq)
            {
                lock (engineLockObject)
                {
                    if (currentSampleData[slot] != null)
                        currentSampleData[slot].Pitch = freq / currentSampleData[slot].BaseFreq;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="pcmData"></param>
            public void Volume(int slot, double volume)
            {
                lock (engineLockObject)
                {
                    if (currentSampleData[slot] != null)
                        currentSampleData[slot].Volume = volume;
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
                    currentSampleData[slot] = null;

                    //parentModule.XgmWriter?.RecordData(new PortWriteData()
                    //{ Type = (byte)6, Address = (byte)slot, Data = 0 });
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
                    for (int i = 0; i < currentSampleData.Length; i++)
                        currentSampleData[i] = null;
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
                    uint sampleRate = 15600;

                    {
                        lock (engineLockObject)
                        {
                            foreach (var sd in currentSampleData)
                            {
                                if (sd == null)
                                    continue;

                                var d = sd.PeekDacData();
                                if (d == null)
                                    continue;

                                sampleRate = Math.Max(sampleRate, sd.SampleRate);
                            }
                            List<sbyte> data = new List<sbyte>();
                            foreach (var sd in currentSampleData)
                            {
                                if (sd == null)
                                    continue;

                                var d = sd.GetDacData(sampleRate);
                                if (d == null)
                                    continue;

                                int val = ((int)d.Value - 0x80);
                                if (sd.Gain != 1.0f)
                                    val = (int)Math.Round(val * sd.Gain);
                                if (!sd.DisableVelocity)
                                    val = (int)Math.Round(((float)val * (float)sd.Note.Velocity) / 127f);
                                if (sd.Volume != 1.0f)
                                    val = (int)Math.Round(val * sd.Volume);
                                if (val > sbyte.MaxValue)
                                    val = sbyte.MaxValue;
                                else if (val < sbyte.MinValue)
                                    val = sbyte.MinValue;
                                data.Add((sbyte)val);
                                playDac = true;
                            }
                            dacData = (int)Math.Round(PcmMixer.Mix(data, parentModule.EnableSmartDacClip));
                        }

                        int lastDacData = 0;
                        if (playDac || lastDacData != 0)
                        {
                            if (!playDac)
                                dacData = lastDacData;
                            if (dacData > sbyte.MaxValue)
                            {
                                dacData = sbyte.MaxValue;
                            }
                            else if (dacData < sbyte.MinValue)
                            {
                                dacData = sbyte.MinValue;
                            }
                            if (dacData != lastDacData)
                                parentModule.YM2608WriteData(unitNumber, 0x0b, 0, 3, (byte)(dacData + 0x80), false, false);
                            lastDacData = dacData;
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


        /// <summary>
        /// 
        /// </summary>
        private class SampleData
        {
            private YM2608Sound sound;

            public byte[] PcmData
            {
                get;
                private set;
            }

            public uint SampleRate
            {
                get;
                private set;
            }

            public TaggedNoteOnEvent Note
            {
                get;
                private set;
            }

            public bool DisableVelocity
            {
                get;
                private set;
            }

            public float Gain
            {
                get;
                set;
            }

            public double Volume
            {
                get;
                set;
            }

            public double BaseFreq
            {
                get;
                set;
            }

            public double Pitch
            {
                get;
                set;
            }

            public bool LoopEnabled
            {
                get;
                set;
            }

            public uint LoopPoint
            {
                get;
                set;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="adress"></param>
            /// <param name="size"></param>
            public SampleData(YM2608Sound sound, TaggedNoteOnEvent note, byte[] pcmData, uint sampleRate, bool disableVelocity, double baseFreq)
            {
                this.sound = sound;
                Note = note;
                PcmData = (byte[])pcmData.Clone();
                SampleRate = sampleRate;
                DisableVelocity = disableVelocity;
                BaseFreq = baseFreq;
                Gain = 1;
                Volume = 1;
                Pitch = 1;
                LoopEnabled = false;
                LoopPoint = 0;
            }

            private double index;

            public void Restart()
            {
                index = 0;
            }

            public byte? PeekDacData()
            {
                uint idx = (uint)Math.Round(index);
                if (idx >= PcmData.Length)
                {
                    if (LoopEnabled && LoopPoint < PcmData.Length)
                    {
                        idx = LoopPoint;
                    }
                    else
                    {
                        return null;
                    }
                }
                return PcmData[idx];
            }

            public byte? GetDacData(uint sampleRate)
            {
                uint idx = (uint)Math.Round(index);
                if (idx >= PcmData.Length)
                {
                    if (LoopEnabled && LoopPoint < PcmData.Length)
                    {
                        index = LoopPoint;
                        idx = LoopPoint;
                    }
                    else
                    {
                        sound.TrySoundOff();
                        return null;
                    }
                }

                if (idx >= PcmData.Length - 1)
                {
                    index += (Pitch * (double)sampleRate) / (double)SampleRate;
                    return PcmData[idx];
                }

                var ret = (lerp(PcmData[idx], PcmData[idx + 1], index - idx)) + 128;
                index += (Pitch * (double)sampleRate) / (double)SampleRate;
                return (byte)Math.Round(ret);
            }

            static double lerp(double y0, double y1, double x)
            {
                y0 -= 128;
                y1 -= 128;
                return y0 + (y1 - y0) * x;
            }
        }

        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2608DacSettings>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [DataContract]
        [InstLock]
        public class YM2608DacSettings : ContextBoundObject
        {

            private byte[] f_PcmData = new byte[0];

            [TypeConverter(typeof(LoadDataTypeConverter))]
            [Editor(typeof(PcmFileLoaderUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Set DAC PCM data. Unigned 8bit PCM Raw Data or WAV Data. (1ch)")]
            [PcmFileLoaderEditor("Audio File(*.raw, *.wav)|*.raw;*.wav", 0, 8, 1, 0)]
            public byte[] PcmData
            {
                get
                {
                    return f_PcmData;
                }
                set
                {
                    f_PcmData = value;
                }
            }

            public bool ShouldSerializePcmData()
            {
                return PcmData.Length != 0;
            }

            public void ResetPcmData()
            {
                PcmData = new byte[0];
            }

            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Set PCM base frequency [Hz]")]
            [DefaultValue(typeof(double), "440")]
            [DoubleSlideParametersAttribute(100, 2000, 1)]
            [EditorAttribute(typeof(DoubleSlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public double BaseFreqency
            {
                get;
                set;
            } = 440;


            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Set DAC PCM sample rate [Hz].")]
            [DefaultValue(typeof(uint), "15600")]
            [SlideParametersAttribute(3900, 15600)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public uint SampleRate
            {
                get;
                set;
            } = 15600;

            private float f_PcmGain = 1.0f;

            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Set DAC PCM gain(0.0-*).")]
            [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
            [DoubleSlideParameters(0d, 10d, 0.1d)]
            [DefaultValue(typeof(float), "1.0")]
            public float PcmGain
            {
                get
                {
                    return f_PcmGain;
                }
                set
                {
                    if (f_PcmGain != value)
                    {
                        f_PcmGain = value;
                    }
                }
            }

            public virtual bool ShouldSerializePcmGain()
            {
                return f_PcmGain != 1.0f;
            }

            public virtual void ResetGainPcmGain()
            {
                f_PcmGain = 1.0f;
            }

            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Set loop point")]
            [DefaultValue(typeof(uint), "0")]
            [SlideParametersAttribute(0, 65535)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public uint LoopPoint
            {
                get;
                set;
            }

            private bool f_LoopEnabled;

            [DataMember]
            [Category("Sound(PCM)")]
            [Description("Loop point enable")]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DefaultValue(false)]
            public bool LoopEnabled
            {
                get
                {
                    return f_LoopEnabled;
                }
                set
                {
                    f_LoopEnabled = value;
                }
            }

        }
    }
}