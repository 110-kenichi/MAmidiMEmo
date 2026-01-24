// copyright-holders:K.Ito
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;
using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Instruments.Envelopes;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.VSIF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static zanac.MAmidiMEmo.Instruments.Chips.RP2A03;

//http://bifi.msxnet.org/msxnet/tech/scc.html

namespace zanac.MAmidiMEmo.Instruments.Chips
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    [InstLock]
    public class SCC1 : InstrumentBase
    {

        public override string Name => "SCC1";

        public override string Group => "WSG";

        public override InstrumentType InstrumentType => InstrumentType.SCC1;

        [Browsable(false)]
        public override string ImageKey => "SCC1";

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected override string SoundInterfaceTagNamePrefix => "scc1_";

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
                return 7;
            }
        }


        private PortId portId = PortId.No1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set FTDI No for \"VSIF - MSX\"\r\n" +
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

        private SoundEngineType f_SoundEngineType;

        private SoundEngineType f_CurrentSoundEngineType;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Select a sound engine type.\r\n" +
            "Supports \"Software\" and \"VSIF - MSX\"")]
        [DefaultValue(SoundEngineType.Software)]
        [TypeConverter(typeof(EnumConverterSoundEngineTypeSCC))]
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
                            enableScc(SCCChipType, ExtSCCSlot, true);
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
                            enableScc(SCCChipType, ExtSCCSlot, true);
                            SetDevicePassThru(true);
                        }
                        else
                        {
                            f_CurrentSoundEngineType = SoundEngineType.Software;
                            enableScc(SCCChipType, ExtSCCSlot, true);
                            SetDevicePassThru(false);
                        }
                        break;
                }
            }
            ClearWrittenDataCache();
            PrepareSound();
        }

        private SCCSlotNo f_extSCCSlot = SCCSlotNo.Id0;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [DefaultValue(SCCSlotNo.Id0)]
        [Description("Specify the SCC/SCC-I ID or slot number for VSIF(MSX).\r\n" +
            "*WANRING* Be sure to specify a valid slot to avoid crashing.")]
        public SCCSlotNo ExtSCCSlot
        {
            get
            {
                return f_extSCCSlot;
            }
            set
            {
                if (f_extSCCSlot != value)
                {
                    f_extSCCSlot = value;
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.VSIF_MSX_FTDI:
                        case SoundEngineType.VSIF_MSX_Pi:
                        case SoundEngineType.VSIF_MSX_PiTr:
                            enableScc(f_sccType, f_extSCCSlot, true);
                            break;
                    }
                }
            }
        }

        private SCCType f_sccType = SCCType.SCC1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Select SCC chip type.\r\n" +
            "SCC1(SCC-I) can outputs 5ch sounds." +
            "Others can outputs 4ch sounds only.")]
        [DefaultValue(SCCType.SCC1)]
        public SCCType SCCChipType
        {
            get
            {
                return f_sccType;
            }
            set
            {
                if (f_sccType != value)
                {
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.VSIF_MSX_FTDI:
                        case SoundEngineType.VSIF_MSX_Pi:
                        case SoundEngineType.VSIF_MSX_PiTr:
                            enableScc(value, f_extSCCSlot, true);
                            break;
                    }
                    f_sccType = value;
                }
            }
        }

        private void enableScc(SCCType type, SCCSlotNo slot)
        {
            enableScc(type, slot, false);
        }

        private void enableScc(SCCType type, SCCSlotNo slot, bool clearCache)
        {
            lock (sndEnginePtrLock)
            {
                if ((int)slot < 0)
                    vsifClient?.WriteData(3, (byte)(type), (byte)(-((int)slot + 1)), f_ftdiClkWidth);    //自動選択方式
                else
                    vsifClient?.WriteData(3, (byte)(type + 4), (byte)(slot), f_ftdiClkWidth);   //従来方式
                if (clearCache)
                    vsifClient?.ClearDataCache();
            }
            if (clearCache)
                ClearWrittenDataCache();
        }

        protected override void ClearWrittenDataCache()
        {
            base.ClearWrittenDataCache();

            enableScc(f_sccType, f_extSCCSlot, false);
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
        public override TimbreBase[] BaseTimbres
        {
            get
            {
                return Timbres;
            }
            set
            {
                Timbres = (SCC1Timbre[])value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category(" Timbres")]
        [Description("Timbres")]
        [EditorAttribute(typeof(TimbresArrayUITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public SCC1Timbre[] Timbres
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializeData"></param>
        public override void RestoreFrom(string serializeData)
        {
            using (var obj = JsonConvert.DeserializeObject<SCC1>(serializeData))
                this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_SCC1_w(uint unitNumber, uint address, byte data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_SCC1_w_array(uint unitNumber, uint address, sbyte[] data, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte delegate_SCC1_r(uint unitNumber, uint address);

        private static delegate_SCC1_w_array SCC1_waveform_w;

        private static delegate_SCC1_w SCC1_volume_w;

        private static delegate_SCC1_w SCC1_frequency_w;

        private static delegate_SCC1_w SCC1_keyonoff_w;

        private static delegate_SCC1_r SCC1_keyonoff_r;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        internal override void DirectAccessToChip(uint address, uint data)
        {
            byte type = 0;
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.VSIF_MSX_FTDI:
                    case SoundEngineType.VSIF_MSX_Pi:
                    case SoundEngineType.VSIF_MSX_PiTr:
                        switch (SCCChipType)
                        {
                            case SCCType.SCC1:
                                type = 4;
                                break;
                            case SCCType.SCC1_Compat:
                            case SCCType.SCC:
                                type = 5;
                                break;
                        }
                        break;
                }
            }
            WriteData(address, data, true, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    if (CurrentSoundEngine != SoundEngineType.Software)
                    {
                        enableScc(SCCChipType, ExtSCCSlot, false);
                        vsifClient.WriteData(type, (byte)(address & 0xff), (byte)data, f_ftdiClkWidth);
                    }
                }
                if (address < 0x100)
                {
                    //SCC
                    if (address < 0x80)
                    {
                        // 0x00..0x7F : write wave form 1..4
                        DeferredWriteData(SCC1_waveform_w, UnitNumber, (uint)address, new sbyte[] { (sbyte)data }, 1);
                    }
                    else if (address < 0x8a)
                    {
                        // 0x80..0x9F : freq volume block
                        DeferredWriteData(SCC1_frequency_w, UnitNumber, (uint)(address - 0x80), (byte)data);
                    }
                    else if (address < 0x8f)
                    {
                        DeferredWriteData(SCC1_volume_w, UnitNumber, (uint)(address - 0x8a), (byte)data);
                    }
                    else if (address == 0x8f)
                    {
                        DeferredWriteData(SCC1_keyonoff_w, UnitNumber, (uint)0, (byte)data);
                    }
                    else
                    {
                        // 0xA0..0xDF : no function
                        // 0xE0..0xFF : deformation register
                    }
                }
                else
                {
                    //SCC+
                    address = address & 0xff;
                    if (address < 0xa0)
                    {
                        DeferredWriteData(SCC1_waveform_w, UnitNumber, (uint)address, new sbyte[] { (sbyte)data }, 1);
                    }
                    else if (address < 0xaa)
                    {
                        DeferredWriteData(SCC1_frequency_w, UnitNumber, (uint)(address - 0xa0), (byte)data);
                    }
                    else if (address < 0xaf)
                    {
                        DeferredWriteData(SCC1_volume_w, UnitNumber, (uint)(address - 0xaa), (byte)data);
                    }
                    else if (address == 0xaf)
                    {
                        DeferredWriteData(SCC1_keyonoff_w, UnitNumber, (uint)0, (byte)data);
                    }
                    else
                    {
                        // 0xA0..0xDF : no function
                        // 0xE0..0xFF : deformation register
                    }
                }
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        private void Scc1VolumeWriteData(uint unitNumber, uint offset, byte data)
        {
            byte address = 0;
            byte type = 0;
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.Software:
                    case SoundEngineType.VSIF_MSX_FTDI:
                    case SoundEngineType.VSIF_MSX_Pi:
                    case SoundEngineType.VSIF_MSX_PiTr:
                        switch (SCCChipType)
                        {
                            case SCCType.SCC1:
                                type = 4;
                                address = (byte)(0xaa + offset);
                                break;
                            case SCCType.SCC1_Compat:
                            case SCCType.SCC:
                                type = 5;
                                address = (byte)(0x8a + offset);
                                break;
                        }
                        break;
                }
            }
            WriteData(address, data, true, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    if (CurrentSoundEngine != SoundEngineType.Software)
                    {
                        enableScc(SCCChipType, ExtSCCSlot, false);
                        vsifClient.WriteData(type, address, data, f_ftdiClkWidth);
                    }
                }
                DeferredWriteData(SCC1_volume_w, unitNumber, offset, data);
            }));
            /*
            try
            {
                Program.SoundUpdating();
                SCC1_volume_w(unitNumber, address, data);
            }
            finally
            {
                Program.SoundUpdated();
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        private void Scc1FrequencyWriteData(uint unitNumber, uint offset, uint freq)
        {
            byte address = 0;
            byte type = 0;
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.Software:
                    case SoundEngineType.VSIF_MSX_FTDI:
                    case SoundEngineType.VSIF_MSX_Pi:
                    case SoundEngineType.VSIF_MSX_PiTr:
                        switch (SCCChipType)
                        {
                            case SCCType.SCC1:
                                type = 6;
                                address = (byte)(0xa0 + offset);
                                break;
                            case SCCType.SCC1_Compat:
                            case SCCType.SCC:
                                type = 7;
                                address = (byte)(0x80 + offset);
                                break;
                        }
                        break;
                }
            }
            WriteData(address, freq, true, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    if (CurrentSoundEngine != SoundEngineType.Software)
                    {
                        lock (vsifClient.LockObject)
                        {
                            enableScc(SCCChipType, ExtSCCSlot, false);
                            vsifClient.WriteData(type, address, (byte)(freq & 0xff), f_ftdiClkWidth);
                            vsifClient.FlushDeferredWriteDataAndWait();
                            byte freq_h = (byte)((freq >> 8) & 0xf);
                            vsifClient.RawWriteData(new byte[] {
                                (byte)((freq_h    >> 4) | 0x00),
                                (byte)((freq_h &  0x0f) | 0x10),
                            }, new int[] { f_ftdiClkWidth, f_ftdiClkWidth });
                        }
                    }
                }
                DeferredWriteData(SCC1_frequency_w, unitNumber, offset + 0, (byte)(freq & 0xff));
                DeferredWriteData(SCC1_frequency_w, unitNumber, offset + 1, (byte)((freq >> 8) & 0xf));
            }));
            /*
            try
            {
                Program.SoundUpdating();
                SCC1_frequency_w(unitNumber, address, data);
            }
            finally
            {
                Program.SoundUpdated();
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        private void Scc1KeyOnOffWriteData(uint unitNumber, byte data)
        {
            byte address = 0;
            byte type = 0;
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.Software:
                    case SoundEngineType.VSIF_MSX_FTDI:
                    case SoundEngineType.VSIF_MSX_Pi:
                    case SoundEngineType.VSIF_MSX_PiTr:
                        switch (SCCChipType)
                        {
                            case SCCType.SCC1:
                                type = 4;
                                address = (byte)(0xaf);
                                break;
                            case SCCType.SCC1_Compat:
                            case SCCType.SCC:
                                type = 5;
                                address = (byte)(0x8f);
                                break;
                        }
                        break;
                }
            }
            WriteData(address, data, true, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    if (CurrentSoundEngine != SoundEngineType.Software)
                    {
                        enableScc(SCCChipType, ExtSCCSlot, false);
                        vsifClient.WriteData(type, address, data, f_ftdiClkWidth);
                    }
                }
                DeferredWriteData(SCC1_keyonoff_w, unitNumber, (uint)0, data);
            }));
            /*
            try
            {
                Program.SoundUpdating();
                SCC1_keyonoff_w(unitNumber, 0, data);
            }
            finally
            {
                Program.SoundUpdated();
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        private byte Scc1KeyOnOffReadData(uint unitNumber)
        {
            byte address = 0;
            switch (SCCChipType)
            {
                case SCCType.SCC1:
                    address = (byte)(0xaf);
                    break;
                case SCCType.SCC1_Compat:
                case SCCType.SCC:
                    address = (byte)(0x8f);
                    break;
            }
            var dt = GetCachedWrittenData(address);
            if (dt == null)
                return 0;
            else
                return (byte)dt.Value;
            //try
            //{
            //    Program.SoundUpdating();
            //    FlushDeferredWriteData();

            //    return SCC1_keyonoff_r(unitNumber, 0);
            //}
            //finally
            //{
            //    Program.SoundUpdated();
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        private void Scc1WriteWaveData(uint unitNumber, uint offset, sbyte[] data, int hashCode)
        {
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.VSIF_MSX_FTDI:
                    case SoundEngineType.VSIF_MSX_Pi:
                    case SoundEngineType.VSIF_MSX_PiTr:
                        {
                            byte type = 0;
                            switch (SCCChipType)
                            {
                                case SCCType.SCC1:
                                    type = 8;
                                    break;
                                case SCCType.SCC1_Compat:
                                case SCCType.SCC:
                                    type = 9;
                                    break;
                            }
                            if (type != 0)
                            {
                                WriteData(offset, (uint)hashCode, true, new Action(() =>
                                {
                                    lock (vsifClient.LockObject)
                                    {
                                        byte address = (byte)(0x00 + offset);
                                        enableScc(SCCChipType, ExtSCCSlot, false);
                                        vsifClient.WriteData(type, address, (byte)data[0], f_ftdiClkWidth);
                                        vsifClient.FlushDeferredWriteDataAndWait();

                                        for (int i = 1; i < data.Length; i++)
                                        {
                                            var dt = (byte)data[i];
                                            vsifClient.RawWriteData(new byte[] {
                                                (byte)((dt    >> 4) | 0x00),
                                                (byte)((dt &  0x0f) | 0x10),
                                            }, new int[] { f_ftdiClkWidth, f_ftdiClkWidth });
                                        }
                                    }
                                }));
                            }
                        }
                        break;
                }
            }
            DeferredWriteData(SCC1_waveform_w, unitNumber, offset, data, data.Length);

            /*
            try
            {
                Program.SoundUpdating();

                SCC1_waveform_w(unitNumber, address, data, data.Length);
            }
            finally
            {
                Program.SoundUpdated();
            }//*/
        }

        /// <summary>
        /// 
        /// </summary>
        static SCC1()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("SCC1_waveform_w");
            if (funcPtr != IntPtr.Zero)
                SCC1_waveform_w = (delegate_SCC1_w_array)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_SCC1_w_array));
            funcPtr = MameIF.GetProcAddress("SCC1_volume_w");
            if (funcPtr != IntPtr.Zero)
                SCC1_volume_w = (delegate_SCC1_w)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_SCC1_w));
            funcPtr = MameIF.GetProcAddress("SCC1_frequency_w");
            if (funcPtr != IntPtr.Zero)
                SCC1_frequency_w = (delegate_SCC1_w)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_SCC1_w));
            funcPtr = MameIF.GetProcAddress("SCC1_keyonoff_w");
            if (funcPtr != IntPtr.Zero)
                SCC1_keyonoff_w = (delegate_SCC1_w)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_SCC1_w));
            funcPtr = MameIF.GetProcAddress("SCC1_keyonoff_r");
            if (funcPtr != IntPtr.Zero)
                SCC1_keyonoff_r = (delegate_SCC1_r)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_SCC1_r));
        }

        private SCC1SoundManager soundManager;

        private const float DEFAULT_GAIN = 0.7f;

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
        public SCC1(uint unitNumber) : base(unitNumber)
        {
            GainLeft = DEFAULT_GAIN;
            GainRight = DEFAULT_GAIN;

            Timbres = new SCC1Timbre[InstrumentBase.DEFAULT_MAX_TIMBRES];
            for (int i = 0; i < InstrumentBase.DEFAULT_MAX_TIMBRES; i++)
                Timbres[i] = new SCC1Timbre();
            setPresetInstruments();

            this.soundManager = new SCC1SoundManager(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            soundManager?.Dispose();

            lock (sndEnginePtrLock)
            {
                if (vsifClient != null)
                    vsifClient.Dispose();
            }

            base.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        private void setPresetInstruments()
        {
            Timbres[0].WsgData = new sbyte[] { 8 * 16 - 127, 9 * 16 - 127, 11 * 16 - 127, 12 * 16 - 127, 13 * 16 - 127, 14 * 16 - 127, 15 * 16 - 127, 15 * 16 - 127, 15 * 16 - 127, 15 * 16 - 127, 14 * 16 - 127, 14 * 16 - 127, 13 * 16 - 127, 11 * 16 - 127, 10 * 16 - 127, 9 * 16 - 127, 7 * 16 - 127, 6 * 16 - 127, 4 * 16 - 127, 3 * 16 - 127, 2 * 16 - 127, 1 * 16 - 127, 0 * 16 - 127, 0 * 16 - 127, 0 * 16 - 127, 0 * 16 - 127, 1 * 16 - 127, 1 * 16 - 127, 2 * 16 - 127, 4 * 16 - 127, 5 * 16 - 127, 6 };
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
        private class SCC1SoundManager : SoundManagerBase
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

            private static SoundList<SCC1Sound> sccOnSounds = new SoundList<SCC1Sound>(5);

            private SCC1 parentModule;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public SCC1SoundManager(SCC1 parent) : base(parent)
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
                foreach (SCC1Timbre timbre in parentModule.GetBaseTimbres(note))
                {
                    tindex++;
                    var emptySlot = searchEmptySlot(note);
                    if (emptySlot.slot < 0)
                        continue;

                    SCC1Sound snd = new SCC1Sound(emptySlot.inst, this, timbre, tindex - 1, note, emptySlot.slot);
                    sccOnSounds.Add(snd);

                    FormMain.OutputDebugLog(parentModule, "KeyOn SCC ch" + emptySlot + " " + note.ToString());
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
            private (SCC1 inst, int slot) searchEmptySlot(TaggedNoteOnEvent note)
            {
                if (parentModule.SCCChipType == SCCType.SCC1)
                    return SearchEmptySlotAndOffForLeader(parentModule, sccOnSounds, note, 5);
                else
                    return SearchEmptySlotAndOffForLeader(parentModule, sccOnSounds, note, 4);
            }

            internal override void ProcessAllSoundOff()
            {
                var me = new ControlChangeEvent((SevenBitNumber)120, (SevenBitNumber)0);
                ProcessControlChange(me);

                for (int i = 0; i < 5; i++)
                    parentModule.Scc1KeyOnOffWriteData(parentModule.UnitNumber, 0);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private class SCC1Sound : SoundBase
        {

            private SCC1 parentModule;

            private SCC1Timbre timbre;

            private int wsgDataHashCode;

            private sbyte[] wsgData;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parentModule"></param>
            /// <param name="noteOnEvent"></param>
            /// <param name="programNumber"></param>
            /// <param name="slot"></param>
            public SCC1Sound(SCC1 parentModule, SCC1SoundManager manager, TimbreBase timbre, int tindex, TaggedNoteOnEvent noteOnEvent, int slot) : base(parentModule, manager, timbre, tindex, noteOnEvent, slot)
            {
                this.parentModule = parentModule;
                this.timbre = (SCC1Timbre)timbre;
                wsgDataHashCode = this.timbre.GetWsgDataHashCode();
                wsgData = this.timbre.WsgData;
            }

            /// <summary>
            /// 
            /// </summary>
            public override void KeyOn()
            {
                base.KeyOn();

                SetTimbre();
                //Freq
                OnPitchUpdated();
                //Volume
                OnVolumeUpdated();

                byte data = parentModule.Scc1KeyOnOffReadData(parentModule.UnitNumber);
                data |= (byte)(1 << Slot);
                parentModule.Scc1KeyOnOffWriteData(parentModule.UnitNumber, data);
            }

            /// <summary>
            /// 
            /// </summary>
            public void SetTimbre()
            {
                parentModule.Scc1WriteWaveData(parentModule.UnitNumber,
                    (uint)(Slot << 5), wsgData, wsgDataHashCode);
            }

            /// <summary>
            /// 
            /// </summary>
            protected override void OnProcessFx()
            {
                updateWsgData();

                base.OnProcessFx();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnSoundParamsUpdated()
            {
                base.OnSoundParamsUpdated();

                updateWsgData();

                OnPitchUpdated();

                OnVolumeUpdated();
            }

            private void updateWsgData()
            {
                if (FxEngine != null && FxEngine.Active)
                {
                    var eng = (SccFxEngine)FxEngine;
                    if (eng.MorphValue != null)
                    {
                        if (eng.MorphValue.Value <= timbre.WsgDataMorphs.Count)
                        {
                            var no = eng.MorphValue.Value;
                            {
                                if (no != 0 && no - 1 < timbre.WsgDataMorphs.Count)
                                {
                                    wsgData = timbre.WsgDataMorphs[no - 1].WsgData;
                                    wsgDataHashCode = timbre.WsgDataMorphs[no - 1].GetWsgDataHashCode();
                                }
                                else
                                {
                                    wsgData = timbre.WsgData;
                                    wsgDataHashCode = timbre.GetWsgDataHashCode();
                                }
                                parentModule.Scc1WriteWaveData(parentModule.UnitNumber, (uint)(Slot << 5),
                                    wsgData, wsgDataHashCode);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnVolumeUpdated()
            {
                byte fv = (byte)((int)Math.Round(15 * CalcCurrentVolume()) & 0xf);

                parentModule.Scc1VolumeWriteData(parentModule.UnitNumber, (uint)Slot, fv);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            public override void OnPitchUpdated()
            {
                double freq = CalcCurrentFrequency();

                /*
                 *                fclock
                 *     ftone = -------------
                 *             32 * (TP + 1)
                 *             
                 *     fclock is the clock frequency of the computer. 3,579,545 Hz
                 */
                // TP = (fclock / (32 * ftone))-1
                freq = Math.Round((3579545 / (32 * freq)) - 1);
                if (freq > 0xfff)
                    freq = 0xfff;
                else if (freq < 0x8)
                    freq = 0x8;
                uint n = (uint)freq;
                parentModule.Scc1FrequencyWriteData(parentModule.UnitNumber, (uint)((Slot << 1)), n);
                base.OnPitchUpdated();
            }

            public override void SoundOff()
            {
                base.SoundOff();

                byte data = parentModule.Scc1KeyOnOffReadData(parentModule.UnitNumber);
                data &= (byte)~(1 << Slot);
                parentModule.Scc1KeyOnOffWriteData(parentModule.UnitNumber, data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<SCC1Timbre>))]
        [DataContract]
        [InstLock]
        public class SCC1Timbre : TimbreBase
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

            private sbyte[] wsgData = new sbyte[32];

            [TypeConverter(typeof(ArrayConverter))]
            [Editor(typeof(WsgUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [WsgBitWideAttribute(8)]
            [DataMember]
            [Category("Sound")]
            [Description("Wave Table (32 samples, 8 bit signed data)")]
            public sbyte[] WsgData
            {
                get
                {
                    return wsgData;
                }
                set
                {
                    wsgData = value;
                    calcWsgDataHashCode();
                }
            }

            private int wsgDataHashCode;

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public int GetWsgDataHashCode()
            {
                if (wsgDataHashCode == 0)
                    calcWsgDataHashCode();
                return wsgDataHashCode;
            }

            private void calcWsgDataHashCode()
            {
                wsgDataHashCode = WsgData.Length;
                foreach (int val in WsgData)
                    wsgDataHashCode = unchecked(wsgDataHashCode * 314159 + val);
            }

            public bool ShouldSerializeWsgData()
            {
                foreach (var dt in WsgData)
                {
                    if (dt != 0)
                        return true;
                }
                return false;
            }

            public void ResetWsgData()
            {
                WsgData = new sbyte[32];
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor)), Localizable(false)]
            [Category("Sound")]
            [Description("Wave Table (32 samples, 8 bit signed data)")]
            [IgnoreDataMember]
            [JsonIgnore]
            public string WsgDataSerializeData
            {
                get
                {
                    return createWsgDataSerializeData(WsgData);
                }
                set
                {
                    applyWsgSerializeData(value, WsgData);
                    calcWsgDataHashCode();
                }
            }

            private void applyWsgSerializeData(string value, sbyte[] data)
            {
                string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var vs = new List<sbyte>();
                foreach (var val in vals)
                {
                    sbyte v = 0;
                    if (sbyte.TryParse(val, out v))
                        vs.Add(v);
                }
                for (int i = 0; i < Math.Min(data.Length, vs.Count); i++)
                    data[i] = vs[i];
            }

            private static string createWsgDataSerializeData(sbyte[] data)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    if (sb.Length != 0)
                        sb.Append(' ');
                    sb.Append(data[i].ToString((IFormatProvider)null));
                }
                return sb.ToString();
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor)), Localizable(false)]
            [Category("Sound")]
            [Description("MML Data")]
            [IgnoreDataMember]
            [JsonIgnore]
            public string WsgDataMmlData
            {
                get
                {
                    return CreateWsgDataMmlData(WsgData);
                }
                set
                {
                    ApplyWsgMmlData(value, WsgData);
                    calcWsgDataHashCode();
                }
            }

            public static string CreateWsgDataMmlData(sbyte[] data)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    if (sb.Length != 0)
                        sb.Append(' ');
                    sb.Append(data[i].ToString("X2", (IFormatProvider)null));
                }
                return sb.ToString();
            }

            public static void ApplyWsgMmlData(string value, sbyte[] data)
            {
                string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var vs = new List<sbyte>();
                foreach (var val in vals)
                {
                    sbyte v = 0;
                    if (val.Length > 2)
                    {
                        foreach (Match m in Regex.Matches(val, "(..)"))
                        {
                            if (sbyte.TryParse(m.Value, System.Globalization.NumberStyles.HexNumber, null, out v))
                                vs.Add(v);
                        }
                    }
                    else
                    {
                        if (sbyte.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out v))
                            vs.Add(v);
                    }
                }
                for (int i = 0; i < Math.Min(data.Length, vs.Count); i++)
                    data[i] = vs[i];
            }

            [Browsable(false)]
            [DataMember]
            [DefaultValue(null)]
            [Obsolete]
            public SCCWsgMorphData[] WsgMorphData2
            {
                get
                {
                    return null;
                }
                set
                {
                    // for compatibility
                    if (value != null)
                    {
                        foreach (var i in value)
                        {
                            if (i != null)
                                WsgDataMorphs.Add(i);
                        }
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Category("Sound")]
            [Description("WSG Morph Table")]
            public SCCWsgMorphDataCollection WsgDataMorphs
            {
                get;
                set;
            } = new SCCWsgMorphDataCollection();

            public bool ShouldSerializeWsgDataMorphs()
            {
                return WsgDataMorphs.Count != 0;
            }

            public void ResetWsgDataMorphs()
            {
                WsgDataMorphs.Clear();
            }



            public SCC1Timbre()
            {
                calcWsgDataHashCode();
            }

            protected override void InitializeFxS()
            {
                this.SDS.FxS = new SccFxSettings();
            }

            public override void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<SCC1Timbre>(serializeData);
                    this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
                    calcWsgDataHashCode();
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

        [Editor(typeof(SccMorphUITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        [RefreshProperties(RefreshProperties.All)]
        public class SCCWsgMorphDataCollection : IList<SCCWsgMorphData>, IList
        {
            private List<SCCWsgMorphData> f_list = new List<SCCWsgMorphData>();

            /// <summary>
            /// 
            /// </summary>
            public SCCWsgMorphDataCollection()
            {
            }


            public int IndexOf(SCCWsgMorphData item)
            {
                return f_list.IndexOf(item);
            }

            public void Insert(int index, SCCWsgMorphData item)
            {
                lock (InstrumentBase.VstPluginContextLockObject)
                    f_list.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                lock (InstrumentBase.VstPluginContextLockObject)
                    f_list.RemoveAt(index);
            }

            public SCCWsgMorphData this[int index]
            {
                get
                {
                    return f_list[index];
                }
                set
                {
                    f_list[index] = value;
                }
            }

            public void Add(SCCWsgMorphData item)
            {
                lock (InstrumentBase.VstPluginContextLockObject)
                    f_list.Add(item);
            }

            public void Clear()
            {
                lock (InstrumentBase.VstPluginContextLockObject)
                    f_list.Clear();
            }

            public bool Contains(SCCWsgMorphData item)
            {
                return f_list.Contains(item);
            }

            public void CopyTo(SCCWsgMorphData[] array, int arrayIndex)
            {
                f_list.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get
                {
                    return f_list.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((IList)f_list).IsReadOnly;
                }
            }

            public bool Remove(SCCWsgMorphData item)
            {
                lock (InstrumentBase.VstPluginContextLockObject)
                    return f_list.Remove(item);
            }

            public IEnumerator<SCCWsgMorphData> GetEnumerator()
            {
                return f_list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            int IList.Add(object value)
            {
                int index = Count;
                Add((SCCWsgMorphData)value);
                return index;
            }

            bool IList.Contains(object value)
            {
                return Contains((SCCWsgMorphData)value);
            }

            int IList.IndexOf(object value)
            {
                return IndexOf((SCCWsgMorphData)value);
            }

            void IList.Insert(int index, object value)
            {
                Insert(index, (SCCWsgMorphData)value);
            }

            bool IList.IsFixedSize
            {
                get
                {
                    return ((IList)f_list).IsFixedSize;
                }
            }

            bool IList.IsReadOnly
            {
                get
                {
                    return ((IList)f_list).IsReadOnly;
                }
            }

            void IList.Remove(object value)
            {
                Remove((SCCWsgMorphData)value);
            }

            object IList.this[int index]
            {
                get
                {
                    return this[index];
                }
                set
                {
                    this[index] = (SCCWsgMorphData)value;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array is SCCWsgMorphData[])
                    CopyTo((SCCWsgMorphData[])array, index);
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return ((ICollection)f_list).IsSynchronized;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return ((ICollection)f_list).SyncRoot;
                }
            }

        }

        [JsonConverter(typeof(NoTypeConverterJsonConverter<SCCWsgMorphData>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [DataContract]
        [InstLock]
        public class SCCWsgMorphData : ContextBoundObject
        {
            private sbyte[] wsgData;

            [TypeConverter(typeof(ArrayConverter))]
            [Editor(typeof(WsgUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [WsgBitWideAttribute(8)]
            [DataMember]
            [Category("Sound")]
            [Description("Morphing Wave Table 1 (32 samples, 8 bit signed data)")]
            public sbyte[] WsgData
            {
                get
                {
                    return wsgData;
                }
                set
                {
                    for (int i = 0; i < wsgData.Length; i++)
                        wsgData[i] = value[i];
                    calcWsgDataHashCode();
                }
            }

            private int wsgDataHashCode;

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public int GetWsgDataHashCode()
            {
                if (wsgDataHashCode == 0)
                    calcWsgDataHashCode();
                return wsgDataHashCode;
            }

            private void calcWsgDataHashCode()
            {
                wsgDataHashCode = WsgData.Length;
                foreach (int val in WsgData)
                    wsgDataHashCode = unchecked(wsgDataHashCode * 314159 + val);
            }

            public bool ShouldSerializeWsgData()
            {
                foreach (var dt in WsgData)
                {
                    if (dt != 0)
                        return true;
                }
                return false;
            }

            public void ResetWsgData()
            {
                for (int i = 0; i < wsgData.Length; i++)
                    wsgData[i] = 0;
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor)), Localizable(false)]
            [Category("Sound")]
            [Description("Morphing Wave Table (32 samples, 8 bit signed data)")]
            [IgnoreDataMember]
            [JsonIgnore]
            public string WsgDataSerializeData
            {
                get
                {
                    return createWsgDataSerializeData(WsgData);
                }
                set
                {
                    applyWsgSerializeData(value, WsgData);
                    calcWsgDataHashCode();
                }
            }

            private static void applyWsgSerializeData(string value, sbyte[] data)
            {
                string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                var vs = new List<sbyte>();
                foreach (var val in vals)
                {
                    sbyte v = 0;
                    if (sbyte.TryParse(val, out v))
                        vs.Add(v);
                }
                for (int i = 0; i < Math.Min(data.Length, vs.Count); i++)
                    data[i] = vs[i];
            }

            private static string createWsgDataSerializeData(sbyte[] data)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    if (sb.Length != 0)
                        sb.Append(' ');
                    sb.Append(data[i].ToString((IFormatProvider)null));
                }
                return sb.ToString();
            }

            [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
typeof(UITypeEditor)), Localizable(false)]
            [Category("Sound")]
            [Description("MML Data")]
            [IgnoreDataMember]
            [JsonIgnore]
            public string WsgDataMmlData
            {
                get
                {
                    return SCC1Timbre.CreateWsgDataMmlData(WsgData);
                }
                set
                {
                    SCC1Timbre.ApplyWsgMmlData(value, WsgData);
                    calcWsgDataHashCode();
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public SCCWsgMorphData()
            {
                this.wsgData = new sbyte[32];
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="wsgData"></param>
            public SCCWsgMorphData(sbyte[] wsgData)
            {
                this.wsgData = wsgData;
            }
        }


        [JsonConverter(typeof(NoTypeConverterJsonConverter<SccFxSettings>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [DataContract]
        [InstLock]
        public class SccFxSettings : BasicFxSettings
        {

            private string f_MorphEnvelopes;

            [DataMember]
            [Description("Set wave table number by text. Input wave table number and split it with space like the FamiTracker.\r\n" +
                       "0-3(0 is normal table) \"|\" is repeat point. \"/\" is release point.")]
            [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [EnvelopeEditorAttribute(0, 3)]
            public string MorphEnvelopes
            {
                get
                {
                    return f_MorphEnvelopes;
                }
                set
                {
                    if (f_MorphEnvelopes != value)
                    {
                        MorphEnvelopesRepeatPoint = -1;
                        MorphEnvelopesReleasePoint = -1;
                        if (value == null)
                        {
                            MorphEnvelopesNums = new int[] { };
                            f_MorphEnvelopes = string.Empty;
                            return;
                        }
                        f_MorphEnvelopes = value;
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            if (val.Equals("|", StringComparison.Ordinal))
                                MorphEnvelopesRepeatPoint = vs.Count;
                            else if (val.Equals("/", StringComparison.Ordinal))
                                MorphEnvelopesReleasePoint = vs.Count;
                            else
                            {
                                int v;
                                if (int.TryParse(val, out v))
                                {
                                    if (v < 0)
                                        v = 0;
                                    vs.Add(v);
                                }
                            }
                        }
                        MorphEnvelopesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < MorphEnvelopesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            if (MorphEnvelopesRepeatPoint == i)
                                sb.Append("| ");
                            if (MorphEnvelopesReleasePoint == i)
                                sb.Append("/ ");
                            if (i < MorphEnvelopesNums.Length)
                                sb.Append(MorphEnvelopesNums[i].ToString((IFormatProvider)null));
                        }
                        f_MorphEnvelopes = sb.ToString();
                    }
                }
            }

            public bool ShouldSerializeMorphEnvelopesEnvelopes()
            {
                return !string.IsNullOrEmpty(MorphEnvelopes);
            }

            public void ResetMorphEnvelopesEnvelopes()
            {
                MorphEnvelopes = null;
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] MorphEnvelopesNums { get; set; } = new int[] { };

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int MorphEnvelopesRepeatPoint { get; set; } = -1;

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            [DefaultValue(-1)]
            public int MorphEnvelopesReleasePoint { get; set; } = -1;

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override AbstractFxEngine CreateEngine()
            {
                return new SccFxEngine(this);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public class SccFxEngine : BasicFxEngine
        {
            private SccFxSettings settings;

            /// <summary>
            /// 
            /// </summary>
            public SccFxEngine(SccFxSettings settings) : base(settings)
            {
                this.settings = settings;
            }

            private uint f_morphCounter;

            public int? MorphValue
            {
                get;
                private set;
            }

            protected override bool ProcessCore(SoundBase sound, bool isKeyOff, bool isSoundOff)
            {
                bool process = base.ProcessCore(sound, isKeyOff, isSoundOff);

                MorphValue = null;
                if (settings.MorphEnvelopesNums.Length > 0)
                {
                    if (!isKeyOff)
                    {
                        var vm = settings.MorphEnvelopesNums.Length;
                        if (settings.MorphEnvelopesReleasePoint >= 0)
                            vm = settings.MorphEnvelopesReleasePoint;
                        if (f_morphCounter >= vm)
                        {
                            if (settings.MorphEnvelopesRepeatPoint >= 0)
                                f_morphCounter = (uint)settings.MorphEnvelopesRepeatPoint;
                            else
                                f_morphCounter = (uint)vm - 1;
                        }
                    }
                    else
                    {
                        if (f_morphCounter < settings.MorphEnvelopesNums.Length)
                        {
                            if (settings.MorphEnvelopesReleasePoint >= 0 && f_morphCounter <= (uint)settings.MorphEnvelopesReleasePoint)
                                f_morphCounter = (uint)settings.MorphEnvelopesReleasePoint;
                            else if (settings.MorphEnvelopesReleasePoint < 0 && settings.KeyOffStop)
                                f_morphCounter = (uint)settings.MorphEnvelopesNums.Length;
                        }
                    }
                    if (f_morphCounter < settings.MorphEnvelopesNums.Length)
                    {
                        int vol = settings.MorphEnvelopesNums[f_morphCounter++];

                        MorphValue = vol;
                        process = true;
                    }
                }

                return process;
            }
        }

        private class EnumConverterSoundEngineTypeSCC : EnumConverter<SoundEngineType>
        {
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var sc = new StandardValuesCollection(new SoundEngineType[] {
                    SoundEngineType.Software,
                    SoundEngineType.VSIF_MSX_FTDI,
                    SoundEngineType.VSIF_MSX_Pi,
                    SoundEngineType.VSIF_MSX_PiTr,
                });

                return sc;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public enum SCCType
        {
            SCC1 = 1,
            SCC1_Compat = 2,
            SCC = 3,
            SIOS_OPNB = 4,
        }

        /// <summary>
        /// 
        /// </summary>
        public enum SCCSlotNo
        {
            Id0 = -1,
            Id1 = -2,
            Slot0_Basic = 0b0000_0000,
            Slot0_Ext0 = 0b1000_0000,
            Slot0_Ext1 = 0b1000_0100,
            Slot0_Ext2 = 0b1000_1000,
            Slot0_Ext3 = 0b1000_1100,

            Slot1_Basic = 0b0000_0001,
            Slot1_Ext0 = 0b1000_0001,
            Slot1_Ext1 = 0b1000_0101,
            Slot1_Ext2 = 0b1000_1001,
            Slot1_Ext3 = 0b1000_1101,

            Slot2_Basic = 0b0000_0010,
            Slot2_Ext0 = 0b1000_0010,
            Slot2_Ext1 = 0b1000_0110,
            Slot2_Ext2 = 0b1000_1010,
            Slot2_Ext3 = 0b1000_1110,

            Slot3_Basic = 0b0000_0011,
            Slot3_Ext0 = 0b1000_0011,
            Slot3_Ext1 = 0b1000_0111,
            Slot3_Ext2 = 0b1000_1011,
            Slot3_Ext3 = 0b1000_1111,
        }

    }
}