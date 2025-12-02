// copyright-holders:K.Ito
using FM_SoundConvertor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;
using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gimic;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Gui.FMEditor;
using zanac.MAmidiMEmo.Instruments.Envelopes;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.Scci;
using zanac.MAmidiMEmo.VSIF;
using static zanac.MAmidiMEmo.Instruments.Chips.SCC1;
using static zanac.MAmidiMEmo.Instruments.Chips.SPC700;
using static zanac.MAmidiMEmo.Instruments.Chips.YM2151;
using static zanac.MAmidiMEmo.Instruments.Chips.YM2612;

//http://d4.princess.ne.jp/msx/datas/OPLL/YM2413AP.html#31
//http://www.smspower.org/maxim/Documents/YM2413ApplicationManual
//http://hp.vector.co.jp/authors/VA054130/yamaha_curse.html

namespace zanac.MAmidiMEmo.Instruments.Chips
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    [InstLock]
    public class YM2413 : InstrumentBase
    {

        public override string Name => "YM2413";

        public override string Group => "FM";

        public override InstrumentType InstrumentType => InstrumentType.YM2413;

        [Browsable(false)]
        public override string ImageKey => "YM2413";

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected override string SoundInterfaceTagNamePrefix => "ym2413_";

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
                return 9;
            }
        }

        private PortId portId = PortId.No1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set FTDI or COM Port No for \"VSIF - SMS/MSX\"\r\n" +
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

        private VsifClient vsifClient;

        private IntPtr spfmPtr;

        private int gimicPtr = -1;

        private SoundEngineType f_SoundEngineType;

        private SoundEngineType f_CurrentSoundEngineType;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Select a sound engine type.")]
        [DefaultValue(SoundEngineType.Software)]
        [TypeConverter(typeof(EnumConverterSoundEngineTypeYM2413))]
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

        [Category("Chip(Dedicated)")]
        [Description("Current sound engine type.")]
        [DefaultValue(SoundEngineType.Software)]
        [RefreshProperties(RefreshProperties.All)]
        public SoundEngineType CurrentSoundEngine
        {
            get
            {
                return f_CurrentSoundEngineType;
            }
        }

        private void setSoundEngine(SoundEngineType value)
        {
            AllSoundOff();

            lock (sndEnginePtrLock)
            {
                if (gimicPtr != -1)
                {
                    GimicManager.ReleaseModule(gimicPtr);
                    gimicPtr = -1;
                }
                if (spfmPtr != IntPtr.Zero)
                {
                    ScciManager.ReleaseSoundChip(spfmPtr);
                    spfmPtr = IntPtr.Zero;
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
                    case SoundEngineType.VSIF_SMS:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS, PortId, false);
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
                    case SoundEngineType.VSIF_SMS_FTDI:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS_FTDI, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 20)
                                    FtdiClkWidth = 20;
                            }
                            else
                            {
                                if (FtdiClkWidth < 25)
                                    FtdiClkWidth = 25;
                            }

                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
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
                            enableOpll(ExtOPLLSlot, 2, true, f_ftdiClkWidth);
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        break;
                    case SoundEngineType.VSIF_NES_FTDI_VRC6:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NES_FTDI_INDIRECT, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 11)
                                    FtdiClkWidth = 11;
                            }
                            else
                            {
                                if (FtdiClkWidth < 27)
                                    FtdiClkWidth = 27;
                            }

                            f_CurrentSoundEngineType = f_SoundEngineType;
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
                        break;
                    case SoundEngineType.SPFM:
                        spfmPtr = ScciManager.TryGetSoundChip(SoundChipType.SC_TYPE_YM2413, SC_CHIP_CLOCK.SC_CLOCK_3579545);
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
                        break;
                    case SoundEngineType.GIMIC:
                        gimicPtr = GimicManager.GetModuleIndex(
                            new String[]{
                            GimicManager.ChipName[(int)GimicManager.ChipType.CHIP_STIC_OPLL]
                            });
                        if (gimicPtr >= 0)
                        {
                            f_CurrentSoundEngineType = f_SoundEngineType;
                            GimicManager.SetClock(gimicPtr, 3579545);
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            SetDevicePassThru(false);
                        }
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

        private int f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_MSX_CLK_WIDTH;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [SlideParametersAttribute(1, 100)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("Set FTDI Clock Width[%].\r\n" +
            "MSX FT232R:25~\r\n" +
            "MSX FT232H:32~\r\n" +
            "SMS FT232R:20~\r\n" +
            "SMS FT232H:25~\r\n" +
            "NES FT232R:11~\r\n" +
            "NES FT232H:27~")]
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
                case SoundEngineType.VSIF_SMS_FTDI:
                    return f_ftdiClkWidth != 20;
                case SoundEngineType.VSIF_NES_FTDI_VRC6:
                    return f_ftdiClkWidth != 11;
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
                case SoundEngineType.VSIF_SMS_FTDI:
                    f_ftdiClkWidth = 20;
                    return;
                case SoundEngineType.VSIF_NES_FTDI_VRC6:
                    f_ftdiClkWidth = 11;
                    return;
            }
            f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;
        }

        private bool f_UseAltVRC7Cart;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [SlideParametersAttribute(0, 1)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue(false)]
        [Description("Use alternative VRC7 Cart.")]
        public bool UseAltVRC7Cart
        {
            get
            {
                return f_UseAltVRC7Cart;
            }
            set
            {
                f_UseAltVRC7Cart = value;
            }
        }

        private OPLLSlotNo f_extOPLLSlot = OPLLSlotNo.IO;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [DefaultValue(OPLLSlotNo.IO)]
        [Description("Specify the OPLL ID number for VSIF(MSX).")]
        public OPLLSlotNo ExtOPLLSlot
        {
            get
            {
                return f_extOPLLSlot;
            }
            set
            {
                if (f_extOPLLSlot != value)
                {
                    switch (value)
                    {
                        case OPLLSlotNo.IO:
                        case OPLLSlotNo.MMIO_1:
                        case OPLLSlotNo.MMIO_2:
                            f_extOPLLSlot = value;
                            break;
                    }
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.VSIF_MSX_FTDI:
                            enableOpll(value, 2, true, f_ftdiClkWidth);
                            break;
                        case SoundEngineType.VSIF_MSX_Pi:
                            enableOpll(value, 2, true, 0);
                            break;
                        case SoundEngineType.VSIF_MSX_PiTr:
                            enableOpll(value, 0x22, true, -2);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slot"></param>
        private void enableOpll(OPLLSlotNo slot, byte type, bool clearCache, int wait)
        {
            if (slot == OPLLSlotNo.MMIO_1 || slot == OPLLSlotNo.MMIO_2)
            {
                lock (sndEnginePtrLock)
                {
                    vsifClient?.WriteData(type, (byte)0, (byte)(slot - 1), wait);
                }
            }
            if (clearCache)
                ClearWrittenDataCache();
        }

        /// <summary>
        /// 
        /// </summary>
        public enum OpllType
        {
            YM2413 = 0x0,
            YM2423 = 0x1,
            YMF281 = 0x2,
            DS1001 = 0x3,
        }

        private OpllType f_OpllVariation;

        /// <summary>
        /// Rhythm mode
        /// </summary>
        [DataMember]
        [Category("Chip(Global)")]
        [DefaultValue(OpllType.YM2413)]
        [Description("Specify a variation of OPLL for the Software sound engine.")]
        public OpllType Variation
        {
            get
            {
                return f_OpllVariation;
            }
            set
            {
                if (f_OpllVariation != value)
                {
                    f_OpllVariation = value;

                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.Software:
                            YM2413_write(UnitNumber, 0x2, (byte)f_OpllVariation);
                            break;
                    }
                }
            }
        }

        private byte f_RHY;

        /// <summary>
        /// Rhythm mode
        /// </summary>
        [DataMember]
        [Category("Chip(Global)")]
        [Description("Rhythm mode (0:Off(9ch) 1:On(6ch))\r\n" +
            "Set DrumSet to ToneType in Timbre to output drum sound")]
        [SlideParametersAttribute(0, 1)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [DefaultValue((byte)0)]
        public byte RHY
        {
            get
            {
                return f_RHY;
            }
            set
            {
                var v = (byte)(value & 1);
                if (f_RHY != v)
                {
                    f_RHY = v;

                    initGlobalRegisters();
                }
            }
        }

        private void initGlobalRegisters()
        {
            OnControlChangeEvent(new ControlChangeEvent((SevenBitNumber)120, (SevenBitNumber)0));

            YM2413WriteData(UnitNumber, 0x0e, 0, (byte)(RHY << 5));
            if (RHY == 1)
            {
                YM2413WriteData(UnitNumber, (byte)(0x16), 0, 0x20);
                YM2413WriteData(UnitNumber, (byte)(0x17), 0, 0x50);
                YM2413WriteData(UnitNumber, (byte)(0x18), 0, 0xc0);
                YM2413WriteData(UnitNumber, (byte)(0x26), 0, 0x05);
                YM2413WriteData(UnitNumber, (byte)(0x27), 0, 0x05);
                YM2413WriteData(UnitNumber, (byte)(0x28), 0, 0x01);
            }
            soundManager?.ProcessAllSoundOff();
        }

        /// <summary>
        /// FrequencyCalculationMode
        /// </summary>
        [DataMember]
        [Category("Chip(Global)")]
        [Description("Select Frequency Accuracy Mode (False:3.6MHz mode(Not accurate) True:3.579545MHz mode(Accurate)")]
        [DefaultValue(false)]
        public bool FrequencyAccuracyMode
        {
            get;
            set;
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
                Timbres = (YM2413Timbre[])value;
            }
        }

        [DataMember]
        [Category(" Timbres")]
        [Description("Timbres")]
        [EditorAttribute(typeof(YM2413UITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public YM2413Timbre[] Timbres
        {
            get;
            set;
        }

        private const float DEFAULT_GAIN = 4.0f;

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

        private byte lastDrumKeyOn;
        private byte lastDrumVolume37;
        private byte lastDrumVolume38;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializeData"></param>
        public override void RestoreFrom(string serializeData)
        {
            try
            {
                using (var obj = JsonConvert.DeserializeObject<YM2413>(serializeData))
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_YM2413_write(uint unitNumber, uint address, byte data);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte delegate_YM2413_read(uint unitNumber, uint address);

        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2413_write YM2413_write
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2413_read YM2413_read
        {
            get;
            set;
        }

        private static byte[] addressTable = new byte[] { 0x00, 0x01, 0x02, 0x08, 0x09, 0x0a, 0x10, 0x11, 0x12 };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        internal override void DirectAccessToChip(uint address, uint data)
        {
            YM2413WriteData(UnitNumber, (byte)address, 0, (byte)data, !(0x20 <= address && address <= 0x28));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitNumber"></param>
        /// <param name="address"></param>
        /// <param name="slot"></param>
        /// <param name="data"></param>
        private void YM2413WriteData(uint unitNumber, byte address, int slot, byte data)
        {
            YM2413WriteData(unitNumber, address, slot, data, true);
        }

        /// <summary>
        /// 
        /// </summary>
        private void YM2413WriteData(uint unitNumber, byte address, int slot, byte data, bool useCache)
        {
            address = (byte)(address + slot);
            WriteData(address, data, useCache, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.VSIF_SMS:
                            vsifClient.WriteData(0, address, data, f_ftdiClkWidth);
                            break;
                        case SoundEngineType.VSIF_SMS_FTDI:
                            vsifClient.WriteData(0, (byte)(address + 1), data, f_ftdiClkWidth);
                            break;
                        case SoundEngineType.VSIF_MSX_FTDI:
                        case SoundEngineType.VSIF_MSX_Pi:
                            {
                                int wait = f_ftdiClkWidth;
                                if (CurrentSoundEngine == SoundEngineType.VSIF_MSX_Pi)
                                    wait = 0;
                                enableOpll(f_extOPLLSlot, 2, false, wait);
                                if (f_extOPLLSlot == OPLLSlotNo.IO)
                                    vsifClient.WriteData(1, address, data, wait);
                                else
                                    vsifClient.WriteData(0xC, address, data, wait);
                            }
                            break;
                        case SoundEngineType.VSIF_MSX_PiTr:
                            {
                                int wait = -2;
                                enableOpll(f_extOPLLSlot, 0x22, false, wait);
                                if (f_extOPLLSlot == OPLLSlotNo.IO)
                                    vsifClient.WriteData(0x21, address, data, wait);
                                else
                                    vsifClient.WriteData(0x2C, address, data, wait);
                            }
                            break;
                        case SoundEngineType.VSIF_NES_FTDI_VRC6:
                            if (!UseAltVRC7Cart)
                            {
                                vsifClient.WriteData(0, (byte)(36 + 1), address, f_ftdiClkWidth);
                                vsifClient.WriteData(0, (byte)(36 + 3), data, f_ftdiClkWidth);
                            }
                            else
                            {
                                vsifClient.WriteData(0, (byte)(36 + 0), address, f_ftdiClkWidth);
                                vsifClient.WriteData(0, (byte)(36 + 3), data, f_ftdiClkWidth);
                            }
                            break;
                        case SoundEngineType.SPFM:
                            ScciManager.SetRegister(spfmPtr, address, data, false);
                            break;
                        case SoundEngineType.GIMIC:
                            GimicManager.SetRegister2(gimicPtr, address, data);
                            break;
                    }
                }
                DeferredWriteData(YM2413_write, unitNumber, (uint)0, address);
                DeferredWriteData(YM2413_write, unitNumber, (uint)1, data);
            }));
            /*
            try
            {
                Program.SoundUpdating();
                YM2413_write(unitNumber, 0, (byte)(address + slot));
                YM2413_write(unitNumber, 1, data);
            }
            finally
            {
                Program.SoundUpdated();
            }*/
        }


        /// 
        /// </summary>
        static YM2413()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("ym2413_write");
            if (funcPtr != IntPtr.Zero)
                YM2413_write = (delegate_YM2413_write)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2413_write));
            /*
            funcPtr = MameIF.GetProcAddress("ym2413_read");
            if (funcPtr != IntPtr.Zero)
                YM2413_read = (delegate_YM2413_read)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2413_read));*/
        }

        private YM2413SoundManager soundManager;

        /// <summary>
        /// 
        /// </summary>
        public YM2413(uint unitNumber) : base(unitNumber)
        {
            GainLeft = DEFAULT_GAIN;
            GainRight = DEFAULT_GAIN;

            Timbres = new YM2413Timbre[InstrumentBase.DEFAULT_MAX_TIMBRES];
            for (int i = 0; i < InstrumentBase.DEFAULT_MAX_TIMBRES; i++)
                Timbres[i] = new YM2413Timbre();
            setPresetInstruments();

            this.soundManager = new YM2413SoundManager(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            soundManager?.Dispose();

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
                    vsifClient.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vgmPath"></param>
        public override void StartVgmRecordingTo(string vgmPath)
        {
            base.StartVgmRecordingTo(vgmPath);

            ClearWrittenDataCache();
            initGlobalRegisters();
        }

        /// <summary>
        /// 
        /// </summary>
        private void setPresetInstruments()
        {
            Timbres[0].Career.AR = 15;
            Timbres[0].Career.DIST = 1;
            Timbres[0].Modulator.AR = 14;
            Timbres[0].Modulator.VIB = 1;
        }

        internal override void PrepareSound()
        {
            base.PrepareSound();

            initGlobalRegisters();
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
        }

        internal override void ResetAll()
        {
            ClearWrittenDataCache();
            PrepareSound();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void ClearWrittenDataCache()
        {
            base.ClearWrittenDataCache();

            switch (CurrentSoundEngine)
            {
                case SoundEngineType.VSIF_MSX_FTDI:
                    enableOpll(f_extOPLLSlot, 2, false, f_ftdiClkWidth);
                    break;
                case SoundEngineType.VSIF_MSX_Pi:
                    enableOpll(f_extOPLLSlot, 2, false, 0);
                    break;
                case SoundEngineType.VSIF_MSX_PiTr:
                    enableOpll(f_extOPLLSlot, 0x22, false, -2);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2413SoundManager : SoundManagerBase
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

            private static SoundList<YM2413Sound> fmOnSounds = new SoundList<YM2413Sound>(9);

            private static SoundList<YM2413Sound> drumBDOnSounds = new SoundList<YM2413Sound>(1);
            private static SoundList<YM2413Sound> drumSDOnSounds = new SoundList<YM2413Sound>(1);
            private static SoundList<YM2413Sound> drumHHOnSounds = new SoundList<YM2413Sound>(1);
            private static SoundList<YM2413Sound> drumTOMOnSounds = new SoundList<YM2413Sound>(1);
            private static SoundList<YM2413Sound> drumSYMOnSounds = new SoundList<YM2413Sound>(1);

            private YM2413 parentModule;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public YM2413SoundManager(YM2413 parent) : base(parent)
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

                int tindex = 0;
                foreach (YM2413Timbre timbre in parentModule.GetBaseTimbres(note))
                {
                    tindex++;
                    var emptySlot = searchEmptySlot(note, timbre);
                    if (emptySlot.slot < 0)
                        continue;

                    YM2413Sound snd = new YM2413Sound(emptySlot.inst, this, timbre, tindex - 1, note, emptySlot.slot);
                    if (parentModule.RHY == 0)
                    {
                        fmOnSounds.Add(snd);
                    }
                    else
                    {
                        if (timbre.ToneType != ToneType.DrumSet &&
                            timbre.ToneType != ToneType.DrumSetEnhanced)
                        {
                            fmOnSounds.Add(snd);
                        }
                        else
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
                                    drumBDOnSounds.Add(snd);
                                    break;
                                case 37:    //STICK
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

                                case 54:    //BELL
                                case 56:    //BELL
                                case 58:    //BELL
                                case 80:    //BELL

                                case 73:
                                case 79:
                                    drumHHOnSounds.Add(snd);
                                    break;
                                case 46:    //HH

                                case 49:    //Cymbal
                                case 51:    //Cymbal
                                case 52:    //Cymbal
                                case 53:    //Cymbal
                                case 55:    //Cymbal
                                case 57:    //Cymbal
                                case 59:    //Cymbal

                                case 81:    //TRIANGLE
                                case 74:
                                    drumSYMOnSounds.Add(snd);
                                    break;
                            }
                        }
                    }
                    FormMain.OutputDebugLog(parentModule, "KeyOn FM ch" + emptySlot + " " + note.ToString());
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
            private (YM2413 inst, int slot) searchEmptySlot(TaggedNoteOnEvent note, YM2413Timbre timbre)
            {
                var emptySlot = (parentModule, -1);

                byte RHY = parentModule.RHY;
                var gs = timbre.GlobalSettings;
                if (gs.Enable)
                {
                    if (gs.RHY.HasValue)
                        RHY = gs.RHY.Value;
                }

                if (RHY == 0)
                {
                    if (parentModule.Variation != OpllType.DS1001)
                        emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, 9);
                    else
                        emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, 6);

                    //sound off diffrent custom sound
                    {
                        if (timbre.ToneType == ToneType.Custom)
                        {
                            var psm = emptySlot.parentModule.soundManager;
                            //int nidx = InstrumentManager.FindInstrumentIndex(emptySlot.parentModule, timbre);
                            for (int i = 0; i < psm.AllSounds.Count; i++)
                            {
                                var snd = psm.AllSounds[i];
                                YM2413Timbre tim = (YM2413Timbre)snd.Timbre;
                                if (tim.ToneType == ToneType.Custom && tim != timbre)
                                {
                                    //int idx = InstrumentManager.FindInstrumentIndex(emptySlot.parentModule, tim);
                                    //if (idx != nidx)
                                    snd.SoundOff();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (timbre.ToneType != ToneType.DrumSet &&
                        timbre.ToneType != ToneType.DrumSetEnhanced)
                    {
                        emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, 6);

                        //sound off diffrent custom sound
                        {
                            if (timbre.ToneType == ToneType.Custom)
                            {
                                var psm = emptySlot.parentModule.soundManager;
                                //int nidx = InstrumentManager.FindInstrumentIndex(emptySlot.parentModule, timbre);
                                for (int i = 0; i < psm.AllSounds.Count; i++)
                                {
                                    var snd = psm.AllSounds[i];
                                    YM2413Timbre tim = (YM2413Timbre)snd.Timbre;
                                    if (tim.ToneType == ToneType.Custom && tim != timbre)
                                    {
                                        //int idx = InstrumentManager.FindInstrumentIndex(emptySlot.parentModule, tim);
                                        //if (idx != nidx)
                                        snd.SoundOff();
                                    }
                                }
                            }
                        }
                    }
                    else
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

                            case 54:    //BELL
                            case 56:    //BELL
                            case 58:    //BELL
                            case 80:    //BELL

                            case 73:
                            case 79:
                                emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumHHOnSounds, note, 1);
                                break;
                            case 46:    //HH

                            case 49:    //Cymbal
                            case 51:    //Cymbal
                            case 52:    //Cymbal
                            case 53:    //Cymbal
                            case 55:    //Cymbal
                            case 57:    //Cymbal
                            case 59:    //Cymbal

                            case 81:    //TRIANGLE
                            case 74:
                                emptySlot = SearchEmptySlotAndOffForLeader(parentModule, drumSYMOnSounds, note, 1);
                                break;
                        }
                    }
                }

                return emptySlot;
            }

            internal override void ProcessAllSoundOff()
            {
                var me = new ControlChangeEvent((SevenBitNumber)120, (SevenBitNumber)0);
                ProcessControlChange(me);

                lock (parentModule.sndEnginePtrLock)
                {
                    if (parentModule.RHY == 0)
                    {
                        if (parentModule.Variation != OpllType.DS1001)
                        {
                            for (int i = 0; i < 9; i++)
                            {
                                parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x20 + i), 0, (byte)(0));
                                parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                            }
                            for (int i = 0; i < 9; i++)
                            {
                                parentModule.YM2413WriteData(parentModule.UnitNumber, 0x30, i, 0xf);
                                parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x20 + i), 0, (byte)(0));
                                parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                            }
                            for (int i = 0; i < 6; i++)
                            {
                                parentModule.YM2413WriteData(parentModule.UnitNumber, 0x30, i, 0xf);
                                parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x20 + i), 0, (byte)(0));
                            parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x30, i, 0xf);
                            parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                        }
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x36, 0, 0xf);
                        parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x37, 0, 0xff);
                        parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x38, 0, 0xff);
                        parentModule.vsifClient?.FlushDeferredWriteDataAndWait();

                        parentModule.lastDrumKeyOn = 0;
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0xe, 0, (byte)(0x20));
                        parentModule.vsifClient?.FlushDeferredWriteDataAndWait();
                    }
                }
            }

        }


        /// <summary>
        /// 
        /// </summary>
        private class YM2413Sound : SoundBase
        {
            private YM2413 parentModule;

            private YM2413Timbre timbre;

            private byte lastFreqData;

            private ToneType lastToneType;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parentModule"></param>
            /// <param name="noteOnEvent"></param>
            /// <param name="programNumber"></param>
            /// <param name="slot"></param>
            public YM2413Sound(YM2413 parentModule, YM2413SoundManager manager, TimbreBase timbre, int tindex, TaggedNoteOnEvent noteOnEvent, int slot) : base(parentModule, manager, timbre, tindex, noteOnEvent, slot)
            {
                this.parentModule = parentModule;
                this.timbre = (YM2413Timbre)timbre;
                lastToneType = this.timbre.ToneType;
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
                    if (gs.RHY.HasValue)
                        parentModule.RHY = gs.RHY.Value;
                }

                SetTimbre();
                //Volume
                OnVolumeUpdated();
                //Freq & kon
                OnPitchUpdated();

                if ((lastToneType == ToneType.DrumSet ||
                    lastToneType == ToneType.DrumSetEnhanced) && parentModule.RHY == 1)
                {
                    byte kon = 0;

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
                            kon = 0x10;
                            break;
                        case 37:    //STICK
                        case 38:    //SD
                        case 39:    //CLAP
                        case 40:    //SD

                        case 67:
                        case 68:
                        case 69:
                        case 70:
                            kon = 0x08;
                            break;
                        case 41:    //TOM
                        case 43:    //TOM
                        case 45:    //TOM
                        case 47:    //TOM
                        case 48:    //TOM
                        case 50:    //TOM

                        case 71:
                        case 78:
                            kon = 0x04;
                            break;
                        case 42:    //HH
                        case 44:    //HH

                        case 54:    //BELL
                        case 56:    //BELL
                        case 58:    //BELL
                        case 80:    //BELL

                        case 73:
                        case 79:
                            kon = 0x01;
                            break;
                        case 46:    //HH

                        case 49:    //Cymbal
                        case 51:    //Cymbal
                        case 52:    //Cymbal
                        case 53:    //Cymbal
                        case 55:    //Cymbal
                        case 57:    //Cymbal
                        case 59:    //Cymbal

                        case 81:    //TRIANGLE
                        case 74:
                            kon = 0x02;
                            break;
                    }
                    if (kon != 0)
                    {
                        parentModule.lastDrumKeyOn = (byte)(parentModule.lastDrumKeyOn & ~kon);
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0xe, 0, (byte)(0x20 | parentModule.lastDrumKeyOn));  //off

                        parentModule.lastDrumKeyOn |= (byte)kon;
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0xe, 0, (byte)(0x20 | parentModule.lastDrumKeyOn));  //on
                    }
                }
            }

            public override void OnSoundParamsUpdated()
            {
                var gs = timbre.GlobalSettings;
                if (gs.Enable)
                {
                    if (gs.RHY.HasValue)
                        parentModule.RHY = gs.RHY.Value;
                }

                SetTimbre();

                base.OnSoundParamsUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnVolumeUpdated()
            {
                byte tl = (byte)(15 - (byte)Math.Round(15 * CalcCurrentVolume()));
                if (lastToneType != ToneType.DrumSet &&
                    lastToneType != ToneType.DrumSetEnhanced)
                {
                    var tt = timbre.ToneType;
                    if (FxEngine != null && FxEngine.Active)
                    {
                        var eng = FxEngine as YM2413FxEngine;
                        if (eng?.ToneValue != null)
                            tt = (ToneType)(eng.ToneValue.Value & 15);
                    }
                    parentModule.YM2413WriteData(parentModule.UnitNumber, 0x30, Slot, (byte)((int)tt << 4 | tl));

                    if (timbre.UseExprForModulator && lastToneType == ToneType.Custom)
                    {
                        var mul = CalcModulatorMultiply();
                        double vol = timbre.Modulator.TL;
                        if (mul > 0)
                            vol = vol + ((63 - vol) * mul);
                        else if (mul < 0)
                            vol = vol + ((vol) * mul);
                        vol = Math.Round(vol);
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x02, 0, (byte)((timbre.Modulator.KSL << 6 | (byte)vol)));
                    }
                }
                else if (parentModule.RHY == 1)
                {
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
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x36, 0, tl);
                            break;
                        case 37:    //STICK
                        case 38:    //SD
                        case 39:    //CLAP
                        case 40:    //SD

                        case 67:
                        case 68:
                        case 69:
                        case 70:
                            parentModule.lastDrumVolume37 = (byte)(tl | (parentModule.lastDrumVolume37 & 0xf0));
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x37, 0, parentModule.lastDrumVolume37);
                            break;
                        case 41:    //TOM
                        case 43:    //TOM
                        case 45:    //TOM
                        case 47:    //TOM
                        case 48:    //TOM
                        case 50:    //TOM

                        case 71:
                        case 78:
                            parentModule.lastDrumVolume38 = (byte)(tl << 4 | (parentModule.lastDrumVolume38 & 0x0f));
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x38, 0, parentModule.lastDrumVolume38);
                            break;
                        case 42:    //HH
                        case 44:    //HH

                        case 54:    //BELL
                        case 56:    //BELL
                        case 58:    //BELL
                        case 80:    //BELL

                        case 73:
                        case 79:
                            parentModule.lastDrumVolume37 = (byte)(tl << 4 | (parentModule.lastDrumVolume37 & 0x0f));
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x37, 0, parentModule.lastDrumVolume37);
                            break;
                        case 46:    //HH

                        case 49:    //Cymbal
                        case 51:    //Cymbal
                        case 52:    //Cymbal
                        case 53:    //Cymbal
                        case 55:    //Cymbal
                        case 57:    //Cymbal
                        case 59:    //Cymbal

                        case 81:    //TRIANGLE
                        case 74:
                            parentModule.lastDrumVolume38 = (byte)(tl | (parentModule.lastDrumVolume38 & 0xf0));
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x38, 0, parentModule.lastDrumVolume38);
                            break;
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            public override void OnPitchUpdated()
            {
                switch (lastToneType)
                {
                    case ToneType.DrumSet:
                    case ToneType.DrumSetEnhanced:
                        if (parentModule.RHY == 1)
                        {
                            int ch7Ofst = 0;
                            int ch8Ofst = 0;
                            int ch9Ofst = 0;

                            bool ch7 = false;
                            bool ch8 = false;
                            bool ch9 = false;
                            switch (NoteOnEvent.NoteNumber)
                            {
                                case 35:    //BD
                                    ch7Ofst = -100;
                                    ch7 = true;
                                    break;
                                case 36:    //BD
                                    ch7 = true;
                                    break;

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
                                    ch7 = true;
                                    break;

                                case 38:    //SD
                                    ch8Ofst = -1000;
                                    ch8 = true;
                                    break;

                                case 40:    //SD
                                    ch8 = true;
                                    break;

                                case 37:    //STICK
                                case 39:    //CLAP
                                case 67:
                                case 68:
                                case 69:
                                case 70:
                                    ch8 = true;
                                    break;

                                case 41:    //TOM
                                    ch9Ofst = -250;
                                    ch9 = true;
                                    break;
                                case 43:    //TOM
                                    ch9Ofst = -200;
                                    ch9 = true;
                                    break;
                                case 45:    //TOM
                                    ch9Ofst = -150;
                                    ch9 = true;
                                    break;
                                case 47:    //TOM
                                    ch9Ofst = -100;
                                    ch9 = true;
                                    break;
                                case 48:    //TOM
                                    ch9Ofst = -50;
                                    ch9 = true;
                                    break;

                                case 50:    //TOM

                                case 71:
                                case 78:
                                    ch9 = true;
                                    break;
                                case 42:    //HH
                                case 44:    //HH

                                case 54:    //BELL
                                case 56:    //BELL
                                case 58:    //BELL
                                case 80:    //BELL

                                case 73:
                                case 79:
                                    ch8 = true;
                                    ch9 = true;
                                    break;
                                case 49:    //Cymbal
                                    ch8Ofst = -200;
                                    ch8 = true;
                                    ch9 = true;
                                    break;

                                case 51:    //Cymbal
                                    ch8Ofst = 100;
                                    ch8 = true;
                                    ch9 = true;
                                    break;

                                case 46:    //HH

                                case 52:    //Cymbal
                                case 53:    //Cymbal
                                case 55:    //Cymbal
                                case 57:    //Cymbal
                                case 59:    //Cymbal

                                case 81:    //TRIANGLE
                                case 74:
                                    ch8 = true;
                                    ch9 = true;
                                    break;
                            }
                            int val7 = timbre.DrumFNum.Ch7;
                            int val8 = timbre.DrumFNum.Ch8;
                            int val9 = timbre.DrumFNum.Ch9;
                            if (FxEngine != null && FxEngine.Active)
                            {
                                var eng = FxEngine as YM2413FxEngine;
                                if (eng != null)
                                {
                                    if (eng.DrumCh7FNumValue != null)
                                        val7 = eng.DrumCh7FNumValue.Value;
                                    if (eng.DrumCh8FNumValue != null)
                                        val8 = eng.DrumCh8FNumValue.Value;
                                    if (eng.DrumCh9FNumValue != null)
                                        val9 = eng.DrumCh9FNumValue.Value;
                                }
                            }

                            var pitch = (int)ParentModule.Pitchs[NoteOnEvent.Channel] - 8192;

                            if (ch7)
                            {
                                byte reg7 = 0x16;
                                val7 += val7 * pitch / 4096;
                                if (lastToneType == ToneType.DrumSetEnhanced)
                                    val7 += ch7Ofst;
                                if (val7 > 4095)
                                    val7 = 4095;
                                else if (val7 < 0)
                                    val7 = 0;
                                parentModule.YM2413WriteData(parentModule.UnitNumber, reg7, 0, (byte)(val7 & 0xff));
                                parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(reg7 + 0x10), 0, (byte)((val7 >> 8) & 0xf));
                            }
                            if (ch8)
                            {
                                byte reg8 = 0x17;
                                val8 += val8 * pitch / 4096;
                                if (lastToneType == ToneType.DrumSetEnhanced)
                                    val8 += ch8Ofst;
                                if (val8 > 4095)
                                    val8 = 4095;
                                else if (val8 < 0)
                                    val8 = 0;
                                parentModule.YM2413WriteData(parentModule.UnitNumber, reg8, 0, (byte)(val8 & 0xff));
                                parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(reg8 + 0x10), 0, (byte)((val8 >> 8) & 0xf));
                            }
                            if (ch9)
                            {
                                byte reg9 = 0x18;
                                val9 += val9 * pitch / 4096;
                                if (lastToneType == ToneType.DrumSetEnhanced)
                                    val9 += ch9Ofst;
                                if (val9 > 4095)
                                    val9 = 4095;
                                else if (val9 < 0)
                                    val9 = 0;
                                parentModule.YM2413WriteData(parentModule.UnitNumber, reg9, 0, (byte)(val9 & 0xff));
                                parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(reg9 + 0x10), 0, (byte)((val9 >> 8) & 0xf));
                            }
                        }
                        break;
                    default:
                        {
                            double d = CalcCurrentPitchDeltaNoteNumber();

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
                            int freq = convertFmFrequency(nnOn);
                            int oct = nnOn.GetNoteOctave();

                            if (d != 0)
                                freq += (int)(((double)(convertFmFrequency(nnOn, (d < 0) ? false : true) - freq)) * Math.Abs(d - Math.Truncate(d)));

                            if (oct < 0)
                            {
                                freq /= 2 * -oct;
                                oct = 0;
                            }
                            else if (oct > 7)
                            {
                                freq *= 2 * (oct - 7);
                                if (freq > 0x1ff)
                                    freq = 0x1ff;
                                oct = 7;
                            }

                            byte octave = (byte)(oct << 1);

                            //keyon
                            byte kon = IsKeyOff ? (byte)0 : (byte)0x10;
                            lastFreqData = (byte)(timbre.SUS << 5 | kon | octave | ((freq >> 8) & 1));

                            parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x10 + Slot), 0, (byte)(0xff & freq));
                            parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x20 + Slot), 0, lastFreqData, false);
                        }
                        break;
                }

                base.OnPitchUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            public void SetTimbre()
            {
                switch (lastToneType)
                {
                    case ToneType.DrumSet:
                    case ToneType.DrumSetEnhanced:
                        break;
                    case ToneType.Custom:
                        YM2413Modulator m = timbre.Modulator;
                        YM2413Career c = timbre.Career;

                        //$00+:
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x00, 0, (byte)((m.AM << 7 | m.VIB << 6 | m.EG << 5 | m.KSR << 4 | m.MUL)));
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x01, 0, (byte)((c.AM << 7 | c.VIB << 6 | c.EG << 5 | c.KSR << 4 | c.MUL)));
                        //$02+:
                        if (!timbre.UseExprForModulator)
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x02, 0, (byte)((m.KSL << 6 | m.TL)));

                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x03, 0, (byte)((c.KSL << 6 | c.DIST << 4 | m.DIST << 3 | timbre.FB)));
                        //$04+:
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x04, 0, (byte)((m.AR << 4 | m.DR)));
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x05, 0, (byte)((c.AR << 4 | c.DR)));
                        //$06+:
                        if (m.SR.HasValue && m.EG == 0)
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x06, 0, (byte)(m.SL << 4 | m.SR.Value));
                        else
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x06, 0, (byte)(m.SL << 4 | m.RR));

                        if (c.SR.HasValue && c.EG == 0)
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x07, 0, (byte)(c.SL << 4 | c.SR.Value));
                        else
                            parentModule.YM2413WriteData(parentModule.UnitNumber, 0x07, 0, (byte)(c.SL << 4 | c.RR));
                        break;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public override void SoundOff()
            {
                base.SoundOff();

                if (lastToneType != ToneType.DrumSet &&
                    lastToneType != ToneType.DrumSetEnhanced)
                {
                    YM2413Modulator m = timbre.Modulator;
                    if (m.SR.HasValue && m.EG == 0)
                    {
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x00, 0, (byte)((m.AM << 7 | m.VIB << 6 | 1 << 5 | m.KSR << 4 | m.MUL)));
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x06, 0, (byte)(m.SL << 4 | m.RR));
                    }
                    YM2413Career c = timbre.Career;
                    if (c.SR.HasValue && c.EG == 0)
                    {
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x07, 0, (byte)(c.SL << 4 | c.RR));
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0x01, 0, (byte)((c.AM << 7 | c.VIB << 6 | 1 << 5 | c.KSR << 4 | c.MUL)));
                    }
                    parentModule.YM2413WriteData(parentModule.UnitNumber, (byte)(0x20 + Slot), 0, (byte)(timbre.SUS << 5 | lastFreqData & 0x0f));
                }
                else if (parentModule.RHY == 1)
                {
                    byte kon = 0;
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
                            kon = 0x10;
                            break;
                        case 37:    //STICK
                        case 38:    //SD
                        case 39:    //CLAP
                        case 40:    //SD

                        case 67:
                        case 68:
                        case 69:
                        case 70:
                            kon = 0x08;
                            break;
                        case 41:    //TOM
                        case 43:    //TOM
                        case 45:    //TOM
                        case 47:    //TOM
                        case 48:    //TOM
                        case 50:    //TOM

                        case 71:
                        case 78:
                            kon = 0x04;
                            break;
                        case 42:    //HH
                        case 44:    //HH

                        case 54:    //BELL
                        case 56:    //BELL
                        case 58:    //BELL
                        case 80:    //BELL

                        case 73:
                        case 79:
                            kon = 0x01;
                            break;
                        case 46:    //HH

                        case 49:    //Cymbal
                        case 51:    //Cymbal
                        case 52:    //Cymbal
                        case 53:    //Cymbal
                        case 55:    //Cymbal
                        case 57:    //Cymbal
                        case 59:    //Cymbal

                        case 81:    //TRIANGLE
                        case 74:
                            kon = 0x02;
                            break;
                    }

                    if (kon != 0)
                    {
                        parentModule.lastDrumKeyOn = (byte)(parentModule.lastDrumKeyOn & ~kon);
                        parentModule.YM2413WriteData(parentModule.UnitNumber, 0xe, 0, (byte)(0x20 | parentModule.lastDrumKeyOn));  //off
                    }
                }
            }


            private ushort[] freqTable1 = new ushort[] {
                326/2,
                173,
                183,
                194,
                205,
                217,
                230,
                244,
                258,
                274,
                290,
                307,
                326,
                173*2,
            };

            private ushort[] freqTable2 = new ushort[] {
                323/2,
                172,
                181,
                192,
                204,
                216,
                229,
                242,
                257,
                272,
                288,
                305,
                323,
                172*2,
            };

            /// <summary>
            /// 
            /// </summary>
            /// <param name="note"></param>
            /// <param name="freq"></param>
            /// <returns></returns>
            private ushort convertFmFrequency(TaggedNoteOnEvent note)
            {
                if (parentModule.FrequencyAccuracyMode)
                    return freqTable1[(int)note.GetNoteName() + 1];
                else
                    return freqTable2[(int)note.GetNoteName() + 1];
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="note"></param>
            /// <param name="freq"></param>
            /// <returns></returns>
            private ushort convertFmFrequency(TaggedNoteOnEvent note, bool plus)
            {
                if (plus)
                {
                    if (parentModule.FrequencyAccuracyMode)
                        return freqTable1[(int)note.GetNoteName() + 2];
                    else
                        return freqTable2[(int)note.GetNoteName() + 2];
                }
                else
                {
                    if (parentModule.FrequencyAccuracyMode)
                        return freqTable1[(int)note.GetNoteName()];
                    else
                        return freqTable2[(int)note.GetNoteName()];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413Timbre>))]
        [DataContract]
        [InstLock]
        public class YM2413Timbre : TimbreBase, IFmTimbre
        {
            [Browsable(false)]
            public override bool AssignMIDIChtoSlotNum
            {
                get;
                set;
            }

            [Browsable(false)]
            public override int AssignMIDIChtoSlotNumOffset
            {
                get;
                set;
            }

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

                //using (var f = new FormYM2413Editor((YM2413)inst, this, true))
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
            [Editor(typeof(YM2413UITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [IgnoreDataMember]
            [JsonIgnore]
            [DisplayName("(FM resisters)")]
            [Description("Open FM register editor for Custom Tone.")]
            [TypeConverter(typeof(OpenEditorTypeConverter))]
            public string Detailed
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                         nameof(FB),
                         nameof(SUS),

                        "Modulator.AR",
                        "Modulator.DR",
                        "Modulator.RR",
                        "Modulator.SL",
                        "Modulator.SR",
                        "Modulator.TL",
                        "Modulator.KSL",
                        "Modulator.KSR",
                        "Modulator.MUL",
                        "Modulator.AM",
                        "Modulator.VIB",
                        "Modulator.EG",
                        "Modulator.DIST",

                        "Career.AR",
                        "Career.DR",
                        "Career.RR",
                        "Career.SL",
                        "Career.SR",
                        "Career.KSL",
                        "Career.KSR",
                        "Career.MUL",
                        "Career.AM",
                        "Career.VIB",
                        "Career.EG",
                        "Career.DIST");
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                         nameof(FB),
                         nameof(SUS),

                        "Modulator.AR",
                        "Modulator.DR",
                        "Modulator.RR",
                        "Modulator.SL",
                        "Modulator.SR",
                        "Modulator.TL",
                        "Modulator.KSL",
                        "Modulator.KSR",
                        "Modulator.MUL",
                        "Modulator.AM",
                        "Modulator.VIB",
                        "Modulator.EG",
                        "Modulator.DIST",

                        "Career.AR",
                        "Career.DR",
                        "Career.RR",
                        "Career.SL",
                        "Career.SR",
                        "Career.KSL",
                        "Career.KSR",
                        "Career.MUL",
                        "Career.AM",
                        "Career.VIB",
                        "Career.EG",
                        "Career.DIST");
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

            private ToneType f_ToneType;

            [DataMember]
            [Category("Sound")]
            [Description("Tone type(YM2413/DS1001 tone names)")]
            [DefaultValue(ToneType.Custom)]
            public ToneType ToneType
            {
                get
                {
                    return f_ToneType;
                }
                set
                {
                    f_ToneType = value;
                }
            }

            private byte f_FB;

            [DataMember]
            [Category("Sound")]
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

            private byte f_SUS;

            /// <summary>
            /// Sustain Mode (0:Disalbe 1:Enable)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Sustain Mode (0:Disalbe 1:Enable)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte SUS
            {
                get
                {
                    return f_SUS;
                }
                set
                {
                    f_SUS = (byte)(value & 1);
                }
            }

            #endregion


            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Operators")]
            public YM2413Modulator Modulator
            {
                get;
                set;
            }

            public virtual bool ShouldSerializeModulator()
            {
                return !string.Equals(JsonConvert.SerializeObject(Modulator, Formatting.Indented), "{}", StringComparison.Ordinal);
            }

            public virtual void ResetModulator()
            {
                Modulator.InjectFrom(new LoopInjection(), new YM2413Modulator());
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Operators")]
            public YM2413Career Career
            {
                get;
                set;
            }

            public virtual bool ShouldSerializeCareer()
            {
                return !string.Equals(JsonConvert.SerializeObject(Career, Formatting.Indented), "{}", StringComparison.Ordinal);
            }

            public virtual void ResetCareer()
            {
                Career.InjectFrom(new LoopInjection(), new YM2413Career());
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Drum Set Custom F-Num")]
            public YM2413DrumFNum DrumFNum
            {
                get;
                set;
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                typeof(UITypeEditor)), Localizable(false)]
            [IgnoreDataMember]
            [JsonIgnore]
            [Category("Sound")]
            [Description("You can copy and paste this text data to other same type timber.\r\n" +
                "FB, AR, DR, RR, SL, SR, TL, KSL, KSR, MUL, AM(AMS), VIB, EG, DIST, AR, DR, RR, SL, SR, KSL, KSR, MUL, AM(AMS), VIB, EG, DIST, SUS")]
            public string MmlSerializeData
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        "FB",

                        "Modulator.AR",
                        "Modulator.DR",
                        "Modulator.RR",
                        "Modulator.SL",
                        "Modulator.SR",
                        "Modulator.TL",
                        "Modulator.KSL",
                        "Modulator.KSR",
                        "Modulator.MUL",
                        "Modulator.AM",
                        "Modulator.VIB",
                        "Modulator.EG",
                        "Modulator.DIST",

                        "Career.AR",
                        "Career.DR",
                        "Career.RR",
                        "Career.SL",
                        "Career.SR",
                        "Career.KSL",
                        "Career.KSR",
                        "Career.MUL",
                        "Career.AM",
                        "Career.VIB",
                        "Career.EG",
                        "Career.DIST",

                        "SUS");
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        "FB",

                        "Modulator.AR",
                        "Modulator.DR",
                        "Modulator.RR",
                        "Modulator.SL",
                        "Modulator.SR",
                        "Modulator.TL",
                        "Modulator.KSL",
                        "Modulator.KSR",
                        "Modulator.MUL",
                        "Modulator.AM",
                        "Modulator.VIB",
                        "Modulator.EG",
                        "Modulator.DIST",

                        "Career.AR",
                        "Career.DR",
                        "Career.RR",
                        "Career.SL",
                        "Career.SR",
                        "Career.KSL",
                        "Career.KSR",
                        "Career.MUL",
                        "Career.AM",
                        "Career.VIB",
                        "Career.EG",
                        "Career.DIST",

                        "SUS");
                }
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

            [DataMember]
            [Category("Chip")]
            [Description("Global Settings")]
            public YM2413GlobalSettings GlobalSettings
            {
                get;
                set;
            }

            public virtual bool ShouldSerializeGlobalSettings()
            {
                return !string.Equals(JsonConvert.SerializeObject(GlobalSettings, Formatting.Indented), "{}", StringComparison.Ordinal);
            }

            public virtual void ResetGlobalSettings()
            {
                GlobalSettings.InjectFrom(new LoopInjection(), new YM2413GlobalSettings());
            }

            /// <summary>
            /// 
            /// </summary>
            public YM2413Timbre()
            {
                Modulator = new YM2413Modulator();
                Career = new YM2413Career();
                DrumFNum = new YM2413DrumFNum();

                GlobalSettings = new YM2413GlobalSettings();
            }

            protected override void InitializeFxS()
            {
                this.SDS.FxS = new YM2413FxSettings();
            }

            public override void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<YM2413Timbre>(serializeData);
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

        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413GlobalSettings>))]
        [DataContract]
        [InstLock]
        public class YM2413GlobalSettings : ContextBoundObject
        {
            [DataMember]
            [Category("Chip")]
            [Description("Override global settings")]
            [DefaultValue(false)]
            public bool Enable
            {
                get;
                set;
            }

            private byte? f_RHY;

            /// <summary>
            /// Vibrato depth (0:7 cent 1:14 cent)
            /// </summary>
            [DataMember]
            [Category("Chip")]
            [Description("Rhythm mode (0:Off(9ch) 1:On(6ch))\r\n" +
            "Set DrumSet to ToneType in Timbre to output drum sound")]
            [DefaultValue(null)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte? RHY
            {
                get
                {
                    return f_RHY;
                }
                set
                {
                    byte? v = value;
                    if (value.HasValue)
                        v = (byte)(value & 1);
                    f_RHY = v;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Editor(typeof(EnumTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(EnumConverter<ToneType>))]
        public enum ToneType
        {
            [Description("Custom(0)")]
            Custom,
            [Description("Violin/Buzzy Bell(1)")]
            Violin,
            [Description("Guitar/Guitar(2)")]
            Guiter,
            [Description("Piano/Wurly(3)")]
            Piano,
            [Description("Flute/Flute(4)")]
            Flute,
            [Description("Clarinet/Clarinet(5)")]
            Clarinet,
            [Description("Oboe/Synth(6)")]
            Oboe,
            [Description("Trumpet/Trumpet(7)")]
            Trumpet,
            [Description("Organ/Organ(8)")]
            Organ,
            [Description("Horn/Bells(9)")]
            Horn,
            [Description("Synthesizer/Vibraphone(10)")]
            Synthesizer,
            [Description("Harpsichord/Tutti(11)")]
            Harpsichord,
            [Description("Vibraphone/Vibes(12)")]
            Vibraphone,
            [Description("SynthesizerBass/Fretless(13)")]
            SynthesizerBass,
            [Description("AcousticBass/SynthBass(14)")]
            AcousticBass,
            [Description("ElectricGuitar/Sweep(15)")]
            ElectricGuitar,
            [Description("DrumSet/-")]
            DrumSet,
            [Description("DrumSetEnhanced *Custom F-Num*/-")]
            DrumSetEnhanced,
        }

        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413Operator>))]
        [DataContract]
        [InstLock]
        public class YM2413Operator : ContextBoundObject
        {

            private byte f_AR;

            /// <summary>
            /// Attack Rate (0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Attack Rate (0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte AR
            {
                get
                {
                    return f_AR;
                }
                set
                {
                    f_AR = (byte)(value & 15);
                }
            }

            private byte f_DR;

            /// <summary>
            /// Decay Rate (0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Decay Rate (0-15)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte DR
            {
                get
                {
                    return f_DR;
                }
                set
                {
                    f_DR = (byte)(value & 15);
                }
            }

            private byte f_RR;

            /// <summary>
            /// release rate(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Release Rate (0-15)\r\n" +
                "When EG = 0, Used both Sustain Rate & Release Rate")]
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
            /// Sustain Level (0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Sustain Level (0-15)")]
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

            private byte? f_SR;

            /// <summary>
            /// Sustain rate(0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("When EG = 0 and value is set, Used as Sustain Rate (0-15) when KOFF")]
            [DefaultValue(null)]
            [SlideParametersAttribute(0, 15)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte? SR
            {
                get
                {
                    return f_SR;
                }
                set
                {
                    if (value.HasValue)
                        f_SR = (byte)(value & 15);
                    else
                        f_SR = value;
                }
            }

            private byte f_KSL;

            /// <summary>
            /// Key Scaling Level(0-3)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Key Scaling Level (00:No Change 10:1.5dB/8ve 01:3dB/8ve 11:6dB/8ve)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 3)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte KSL
            {
                get
                {
                    return f_KSL;
                }
                set
                {
                    f_KSL = (byte)(value & 3);
                }
            }

            private byte f_KSR;

            /// <summary>
            /// Keyboard scaling rate (0-1)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Keyboard Scaling Rate (1: the sound's envelope is foreshortened as it rises in pitch.")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte KSR
            {
                get
                {
                    return f_KSR;
                }
                set
                {
                    f_KSR = (byte)(value & 1);
                }
            }

            private byte f_MUL;

            /// <summary>
            /// Multiply (0-15)
            /// </summary>
            [DataMember]
            [Category("Sound")]
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

            private byte f_AM;

            /// <summary>
            /// Apply amplitude modulation(0-1)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Apply amplitude modulation (0:Off 1:On)")]
            [SlideParametersAttribute(0, 1)]
            [DefaultValue((byte)0)]
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

            private byte f_VIB;

            /// <summary>
            /// Apply vibrato(0-1)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Vibrato (0:Off 1:On)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte VIB
            {
                get
                {
                    return f_VIB;
                }
                set
                {
                    f_VIB = (byte)(value & 1);
                }
            }

            private byte f_EG;

            /// <summary>
            /// EG Type (0-1)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("EG Type (0:the sound begins to decay immediately after hitting the SUSTAIN phase 1:the sustain level of the voice is maintained until released")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte EG
            {
                get
                {
                    return f_EG;
                }
                set
                {
                    f_EG = (byte)(value & 1);
                }
            }

            private byte f_DIST;

            /// <summary>
            /// Distortion (0:Off 1:On)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Distortion(Half Wave) (0:Off 1:On)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 1)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte DIST
            {
                get
                {
                    return f_DIST;
                }
                set
                {
                    f_DIST = (byte)(value & 1);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413Modulator>))]
        [DataContract]
        [InstLock]
        public class YM2413Modulator : YM2413Operator, ISerializeDataSaveLoad
        {

            private byte f_TL;

            /// <summary>
            /// Total Level (0-63)
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Total Level (0-63)")]
            [DefaultValue((byte)0)]
            [SlideParametersAttribute(0, 63)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public byte TL
            {
                get
                {
                    return f_TL;
                }
                set
                {
                    f_TL = (byte)(value & 63);
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
                "AR, DR, RR, SL, SR, TL, KSL, KSR, MUL, AM(AMS), VIB, EG, DIST\r\n" +
                "You can use comma or space chars as delimiter.")]
            public string MmlSerializeData
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        nameof(AR),
                        nameof(DR),
                        nameof(RR),
                        nameof(SL),
                        nameof(SR),
                        nameof(TL),
                        nameof(KSL),
                        nameof(KSR),
                        nameof(MUL),
                        nameof(AM),
                        nameof(VIB),
                        nameof(EG),
                        nameof(DIST));
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        nameof(AR),
                        nameof(DR),
                        nameof(RR),
                        nameof(SL),
                        nameof(SR),
                        nameof(TL),
                        nameof(KSL),
                        nameof(KSR),
                        nameof(MUL),
                        nameof(AM),
                        nameof(VIB),
                        nameof(EG),
                        nameof(DIST));
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
                    var obj = JsonConvert.DeserializeObject<YM2413Modulator>(serializeData);
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

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413Career>))]
        [DataContract]
        [InstLock]
        public class YM2413Career : YM2413Operator, ISerializeDataSaveLoad
        {

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
                "AR, DR), RR, SL, SR, KSL, KSR, MUL, AM(AMS), VIB, EG, DIST\r\n" +
                "You can use comma or space chars as delimiter.")]
            public string MmlSerializeData
            {
                get
                {
                    return SimpleSerializer.SerializeProps(this,
                        nameof(AR),
                        nameof(DR),
                        nameof(RR),
                        nameof(SL),
                        nameof(SR),
                        nameof(KSL),
                        nameof(KSR),
                        nameof(MUL),
                        nameof(AM),
                        nameof(VIB),
                        nameof(EG),
                        nameof(DIST));
                }
                set
                {
                    SimpleSerializer.DeserializeProps(this, value,
                        nameof(AR),
                        nameof(DR),
                        nameof(RR),
                        nameof(SL),
                        nameof(SR),
                        nameof(KSL),
                        nameof(KSR),
                        nameof(MUL),
                        nameof(AM),
                        nameof(VIB),
                        nameof(EG),
                        nameof(DIST));
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
                    var obj = JsonConvert.DeserializeObject<YM2413Career>(serializeData);
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

        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413DrumFNum>))]
        [DataContract]
        [InstLock]
        public class YM2413DrumFNum : ContextBoundObject
        {

            private ushort f_h7 = 0x520;

            /// <summary>
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Ch7 F-Number for DrumSet BD(0-4096)")]
            [DefaultValue(typeof(ushort), "0x520")]
            [SlideParametersAttribute(0, 0xfff)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public ushort Ch7
            {
                get
                {
                    return f_h7;
                }
                set
                {
                    f_h7 = (ushort)(value & 0xfff);
                }
            }

            private ushort f_Ch8 = 0x550;

            /// <summary>
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Ch8 F-Number for DrumSet SD/CYM/HH(0-4096)")]
            [DefaultValue(typeof(ushort), "0x550")]
            [SlideParametersAttribute(0, 0xfff)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public ushort Ch8
            {
                get
                {
                    return f_Ch8;
                }
                set
                {
                    f_Ch8 = (ushort)(value & 0xfff);
                }
            }

            private ushort f_Ch9 = 0x1c0;

            /// <summary>
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("Ch9 F-Number for DrumSet TOM/CYM/HH(0-4096)")]
            [DefaultValue(typeof(ushort), "0x1c0")]
            [SlideParametersAttribute(0, 0xfff)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public ushort Ch9
            {
                get
                {
                    return f_Ch9;
                }
                set
                {
                    f_Ch9 = (ushort)(value & 0xfff);
                }
            }
        }

        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2413FxSettings>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [DataContract]
        [InstLock]
        public class YM2413FxSettings : BasicFxSettings
        {

            private string f_ToneEnvelopes;

            [DataMember]
            [Description("Set ToneType envelop by text. Input ToneType value and split it with space like the FamiTracker.\r\n" +
                       "0-15 \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 15)]
            public string ToneEnvelopes
            {
                get
                {
                    return f_ToneEnvelopes;
                }
                set
                {
                    if (f_ToneEnvelopes != value)
                    {
                        ToneEnvelopesRepeatPoint = -1;
                        ToneEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            ToneEnvelopesNums = new int[] { };
                            f_ToneEnvelopes = string.Empty;
                            return;
                        }
                        f_ToneEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                ToneEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                ToneEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    else if (v > 15)
                                        v = 15;
                                    vs.Add(v);
                                }
                            }
                        }
                        ToneEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < ToneEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (ToneEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (ToneEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < ToneEnvelopesNums.Length)
                                sb.Append(ToneEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_ToneEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeToneEnvelopes()
            {
                return !string.IsNullOrEmpty(ToneEnvelopes);
            }

            public void ResetToneEnvelopes()
            {
                ToneEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] ToneEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int ToneEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int ToneEnvelopesReleasePoint { get; set; } = -1;

            private string f_DrumCh7FNumEnvelopes;

            [DataMember]
            [Description("Set Drum Ch7 F-Number for DrumSet BD envelop by text. Input Drum Ch7 F-Num value and split it with space like the FamiTracker.\r\n" +
                       "0-4095 \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 4095)]
            public string DrumCh7FNumEnvelopes
            {
                get
                {
                    return f_DrumCh7FNumEnvelopes;
                }
                set
                {
                    if (f_DrumCh7FNumEnvelopes != value)
                    {
                        DrumCh7FNumEnvelopesRepeatPoint = -1;
                        DrumCh7FNumEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            DrumCh7FNumEnvelopesNums = new int[] { };
                            f_DrumCh7FNumEnvelopes = string.Empty;
                            return;
                        }
                        f_DrumCh7FNumEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                DrumCh7FNumEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                DrumCh7FNumEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    else if (v > 4095)
                                        v = 4095;
                                    vs.Add(v);
                                }
                            }
                        }
                        DrumCh7FNumEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < DrumCh7FNumEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (DrumCh7FNumEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (DrumCh7FNumEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < DrumCh7FNumEnvelopesNums.Length)
                                sb.Append(DrumCh7FNumEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_DrumCh7FNumEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeDrumCh7FNumEnvelopes()
            {
                return !string.IsNullOrEmpty(DrumCh7FNumEnvelopes);
            }

            public void ResetDrumCh7FNumEnvelopes()
            {
                DrumCh7FNumEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] DrumCh7FNumEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh7FNumEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh7FNumEnvelopesReleasePoint { get; set; } = -1;

            private string f_DrumCh8FNumEnvelopes;

            [DataMember]
            [Description("Set Drum Ch8 F-Number for DrumSet SD/CYM/HH envelop by text. Input Drum Ch8 F-Num value and split it with space like the FamiTracker.\r\n" +
                       "0-4095 \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 4095)]
            public string DrumCh8FNumEnvelopes
            {
                get
                {
                    return f_DrumCh8FNumEnvelopes;
                }
                set
                {
                    if (f_DrumCh8FNumEnvelopes != value)
                    {
                        DrumCh8FNumEnvelopesRepeatPoint = -1;
                        DrumCh8FNumEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            DrumCh8FNumEnvelopesNums = new int[] { };
                            f_DrumCh8FNumEnvelopes = string.Empty;
                            return;
                        }
                        f_DrumCh8FNumEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                DrumCh8FNumEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                DrumCh8FNumEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    else if (v > 4095)
                                        v = 4095;
                                    vs.Add(v);
                                }
                            }
                        }
                        DrumCh8FNumEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < DrumCh8FNumEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (DrumCh8FNumEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (DrumCh8FNumEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < DrumCh8FNumEnvelopesNums.Length)
                                sb.Append(DrumCh8FNumEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_DrumCh8FNumEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeDrumCh8FNumEnvelopes()
            {
                return !string.IsNullOrEmpty(DrumCh8FNumEnvelopes);
            }

            public void ResetDrumCh8FNumEnvelopes()
            {
                DrumCh8FNumEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] DrumCh8FNumEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh8FNumEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh8FNumEnvelopesReleasePoint { get; set; } = -1;

            private string f_DrumCh9FNumEnvelopes;

            [DataMember]
            [Description("Set Drum Ch9 F-Number for DrumSet TOM/CYM/HH envelop by text. Input Drum Ch9 F-Num value and split it with space like the FamiTracker.\r\n" +
                       "0-4095 \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 4095)]
            public string DrumCh9FNumEnvelopes
            {
                get
                {
                    return f_DrumCh9FNumEnvelopes;
                }
                set
                {
                    if (f_DrumCh9FNumEnvelopes != value)
                    {
                        DrumCh9FNumEnvelopesRepeatPoint = -1;
                        DrumCh9FNumEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            DrumCh9FNumEnvelopesNums = new int[] { };
                            f_DrumCh9FNumEnvelopes = string.Empty;
                            return;
                        }
                        f_DrumCh9FNumEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                DrumCh9FNumEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                DrumCh9FNumEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    else if (v > 4095)
                                        v = 4095;
                                    vs.Add(v);
                                }
                            }
                        }
                        DrumCh9FNumEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < DrumCh9FNumEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (DrumCh9FNumEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (DrumCh9FNumEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < DrumCh9FNumEnvelopesNums.Length)
                                sb.Append(DrumCh9FNumEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_DrumCh9FNumEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeDrumCh9FNumEnvelopes()
            {
                return !string.IsNullOrEmpty(DrumCh9FNumEnvelopes);
            }

            public void ResetDrumCh9FNumEnvelopes()
            {
                DrumCh9FNumEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] DrumCh9FNumEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh9FNumEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int DrumCh9FNumEnvelopesReleasePoint { get; set; } = -1;


            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override AbstractFxEngine CreateEngine()
            {
                return new YM2413FxEngine(this);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public class YM2413FxEngine : BasicFxEngine
        {
            private YM2413FxSettings settings;

            /// <summary>
            /// 
            /// </summary>
            public YM2413FxEngine(YM2413FxSettings settings) : base(settings)
            {
                this.settings = settings;
            }

            private uint f_toneCounter;

            /// <summary>
            /// 
            /// </summary>
            public byte? ToneValue
            {
                get;
                private set;
            }

            private ushort f_drumCh7FNumCounter;

            /// <summary>
            /// 
            /// </summary>
            public ushort? DrumCh7FNumValue
            {
                get;
                private set;
            }

            private ushort f_drumCh8FNumCounter;

            /// <summary>
            /// 
            /// </summary>
            public ushort? DrumCh8FNumValue
            {
                get;
                private set;
            }

            private ushort f_drumCh9FNumCounter;

            /// <summary>
            /// 
            /// </summary>
            public ushort? DrumCh9FNumValue
            {
                get;
                private set;
            }

            protected override bool ProcessCore(SoundBase sound, bool isKeyOff, bool isSoundOff)
            {
                bool process = base.ProcessCore(sound, isKeyOff, isSoundOff);

                ToneValue = null;
                if (settings.ToneEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.ToneEnvelopesNums.Length;
                        if (settings.ToneEnvelopesReleasePoint >= 0)
                            vm = settings.ToneEnvelopesReleasePoint;
                        if (f_toneCounter >= vm)
                        {
                            if (settings.ToneEnvelopesRepeatPoint >= 0)
                                f_toneCounter = (uint)settings.ToneEnvelopesRepeatPoint;
                            else
                                f_toneCounter = (uint)vm - 1;
                        }
                    }
                    else
                    {
                        if (f_toneCounter < settings.ToneEnvelopesNums.Length)
                        {
                            if (settings.ToneEnvelopesReleasePoint >= 0 && f_toneCounter <= (uint)settings.ToneEnvelopesReleasePoint)
                                f_toneCounter = (uint)settings.ToneEnvelopesReleasePoint;
                            else if (settings.ToneEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_toneCounter = (uint)settings.ToneEnvelopesNums.Length;
                        }
                    }
                    if (f_toneCounter < settings.ToneEnvelopesNums.Length)
                    {
                        int vol = settings.ToneEnvelopesNums[f_toneCounter++];

                        ToneValue = (byte)vol;
                        process = true;
                    }
                }

                if (settings.DrumCh7FNumEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.DrumCh7FNumEnvelopesNums.Length;
                        if (settings.DrumCh7FNumEnvelopesReleasePoint >= 0)
                            vm = settings.DrumCh7FNumEnvelopesReleasePoint;
                        if (f_drumCh7FNumCounter >= vm)
                        {
                            if (settings.DrumCh7FNumEnvelopesRepeatPoint >= 0)
                                f_drumCh7FNumCounter = (ushort)settings.DrumCh7FNumEnvelopesRepeatPoint;
                            else
                                f_drumCh7FNumCounter = (ushort)(vm - 1);
                        }
                    }
                    else
                    {
                        if (f_drumCh7FNumCounter < settings.DrumCh7FNumEnvelopesNums.Length)
                        {
                            if (settings.DrumCh7FNumEnvelopesReleasePoint >= 0 && f_drumCh7FNumCounter <= (ushort)settings.DrumCh7FNumEnvelopesReleasePoint)
                                f_drumCh7FNumCounter = (ushort)settings.DrumCh7FNumEnvelopesReleasePoint;
                            else if (settings.DrumCh7FNumEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_drumCh7FNumCounter = (ushort)settings.DrumCh7FNumEnvelopesNums.Length;
                        }
                    }
                    if (f_drumCh7FNumCounter < settings.DrumCh7FNumEnvelopesNums.Length)
                    {
                        int vol = settings.DrumCh7FNumEnvelopesNums[f_drumCh7FNumCounter++];

                        DrumCh7FNumValue = (ushort)vol;
                        process = true;
                    }
                }
                if (settings.DrumCh8FNumEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.DrumCh8FNumEnvelopesNums.Length;
                        if (settings.DrumCh8FNumEnvelopesReleasePoint >= 0)
                            vm = settings.DrumCh8FNumEnvelopesReleasePoint;
                        if (f_drumCh8FNumCounter >= vm)
                        {
                            if (settings.DrumCh8FNumEnvelopesRepeatPoint >= 0)
                                f_drumCh8FNumCounter = (ushort)settings.DrumCh8FNumEnvelopesRepeatPoint;
                            else
                                f_drumCh8FNumCounter = (ushort)(vm - 1);
                        }
                    }
                    else
                    {
                        if (f_drumCh8FNumCounter < settings.DrumCh8FNumEnvelopesNums.Length)
                        {
                            if (settings.DrumCh8FNumEnvelopesReleasePoint >= 0 && f_drumCh8FNumCounter <= (ushort)settings.DrumCh8FNumEnvelopesReleasePoint)
                                f_drumCh8FNumCounter = (ushort)settings.DrumCh8FNumEnvelopesReleasePoint;
                            else if (settings.DrumCh8FNumEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_drumCh8FNumCounter = (ushort)settings.DrumCh8FNumEnvelopesNums.Length;
                        }
                    }
                    if (f_drumCh8FNumCounter < settings.DrumCh8FNumEnvelopesNums.Length)
                    {
                        int vol = settings.DrumCh8FNumEnvelopesNums[f_drumCh8FNumCounter++];

                        DrumCh8FNumValue = (ushort)vol;
                        process = true;
                    }
                }
                if (settings.DrumCh9FNumEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.DrumCh9FNumEnvelopesNums.Length;
                        if (settings.DrumCh9FNumEnvelopesReleasePoint >= 0)
                            vm = settings.DrumCh9FNumEnvelopesReleasePoint;
                        if (f_drumCh9FNumCounter >= vm)
                        {
                            if (settings.DrumCh9FNumEnvelopesRepeatPoint >= 0)
                                f_drumCh9FNumCounter = (ushort)settings.DrumCh9FNumEnvelopesRepeatPoint;
                            else
                                f_drumCh9FNumCounter = (ushort)(vm - 1);
                        }
                    }
                    else
                    {
                        if (f_drumCh9FNumCounter < settings.DrumCh9FNumEnvelopesNums.Length)
                        {
                            if (settings.DrumCh9FNumEnvelopesReleasePoint >= 0 && f_drumCh9FNumCounter <= (ushort)settings.DrumCh9FNumEnvelopesReleasePoint)
                                f_drumCh9FNumCounter = (ushort)settings.DrumCh9FNumEnvelopesReleasePoint;
                            else if (settings.DrumCh9FNumEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_drumCh9FNumCounter = (ushort)settings.DrumCh9FNumEnvelopesNums.Length;
                        }
                    }
                    if (f_drumCh9FNumCounter < settings.DrumCh9FNumEnvelopesNums.Length)
                    {
                        int vol = settings.DrumCh9FNumEnvelopesNums[f_drumCh9FNumCounter++];

                        DrumCh9FNumValue = (ushort)vol;
                        process = true;
                    }
                }
                return process;
            }


        }

        private class EnumConverterSoundEngineTypeYM2413 : EnumConverter<SoundEngineType>
        {
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var sc = new StandardValuesCollection(new SoundEngineType[] {
                    SoundEngineType.Software,
                    SoundEngineType.VSIF_SMS,
                    SoundEngineType.VSIF_SMS_FTDI,
                    SoundEngineType.VSIF_MSX_FTDI,
                    SoundEngineType.VSIF_NES_FTDI_VRC6,
                    SoundEngineType.SPFM,
                    SoundEngineType.GIMIC,
                    SoundEngineType.VSIF_MSX_Pi,
                    SoundEngineType.VSIF_MSX_PiTr,
                    SoundEngineType.VSIF_MSX_Pi,
                    SoundEngineType.VSIF_MSX_PiTr,
                    SoundEngineType.VSIF_MSX_Pi,
                    SoundEngineType.VSIF_MSX_PiTr,
                });

                return sc;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public enum OPLLSlotNo
        {
            IO = 0,
            MMIO_1 = 1,
            MMIO_2 = 2,
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
            YM2413Timbre tim = (YM2413Timbre)timbre;

            tim.FB = (byte)tone.FB;
            tim.SUS = 0;

            tim.Modulator.AR = (byte)(tone.aOp[0].AR / 2);
            tim.Modulator.DR = (byte)(tone.aOp[0].DR / 2);
            tim.Modulator.RR = (byte)(tone.aOp[0].RR);
            tim.Modulator.SL = (byte)(tone.aOp[0].SL);
            tim.Modulator.SR = (byte)(tone.aOp[0].SR / 2);
            tim.Modulator.TL = (byte)(tone.aOp[0].TL / 2);
            tim.Modulator.KSL = (byte)(tone.aOp[0].KS);
            tim.Modulator.KSR = (byte)(0);
            tim.Modulator.MUL = (byte)(tone.aOp[0].ML);
            tim.Modulator.AM = (byte)(tone.aOp[0].AM);
            tim.Modulator.VIB = (byte)(0);
            tim.Modulator.EG = (byte)(0);
            tim.Modulator.DIST = (byte)(0);

            tim.Career.AR = (byte)(tone.aOp[1].AR / 2);
            tim.Career.DR = (byte)(tone.aOp[1].DR / 2);
            tim.Career.RR = (byte)(tone.aOp[1].RR);
            tim.Career.SL = (byte)(tone.aOp[1].SL);
            tim.Career.SR = (byte)(tone.aOp[1].SR / 2);
            tim.Career.KSL = (byte)(tone.aOp[1].KS);
            tim.Career.KSR = (byte)(0);
            tim.Career.MUL = (byte)(tone.aOp[1].ML);
            tim.Career.AM = (byte)(tone.aOp[1].AM);
            tim.Career.VIB = (byte)(0);
            tim.Career.EG = (byte)(0);
            tim.Career.DIST = (byte)(0);

            timbre.TimbreName = tone.Name;
        }


        private YM2413CustomToneImporter importer;

        /// <summary>
        /// 
        /// </summary>
        public override CustomToneImporter CustomToneImporter
        {
            get
            {
                if (importer == null)
                {
                    importer = new YM2413CustomToneImporter();
                }
                return importer;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2413CustomToneImporter : FmToneImporter
        {

            /// <summary>
            /// 
            /// </summary>
            public override string ExtensionsFilterExt
            {
                get
                {
                    return "*.moll";
                }
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
                        YM2413Timbre tim = new YM2413Timbre();
                        tim.TimbreName = t.MML[0];
                        tim.Detailed = t.MML[1] + "," + t.MML[2] + "," + t.MML[3];
                        rv.Add(tim);
                    }
                    return rv;
                }
                return null;
            }
        }

    }


}