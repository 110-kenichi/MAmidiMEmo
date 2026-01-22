// copyright-holders:K.Ito
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using FM_SoundConvertor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using NAudio.Gui;
using NAudio.SoundFont;
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
using zanac.MAmidiMEmo.VSIF;

//http://www.ajworld.net/neogeodev/ym2610am2.html
//https://wiki.neogeodev.org/index.php?title=YM2610_registers

namespace zanac.MAmidiMEmo.Instruments.Chips
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    [InstLock]
    public class YM2610B : InstrumentBase
    {

        public override string Name => "YM2610B";

        public override string Group => "FM";

        public override InstrumentType InstrumentType => InstrumentType.YM2610B;

        [Browsable(false)]
        public override string ImageKey => "YM2610B";

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected override string SoundInterfaceTagNamePrefix => "ym2610b_";

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
                return 19;
            }
        }

        private PortId portId = PortId.No1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set COM Port No. for \"VSIF - MSXπ UART\".")]
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
            "Supports Software and MSXπ(UART)")]
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
                    SoundEngineType.VSIF_MSX_Pi,
                    SoundEngineType.VSIF_MSX_PiTr,
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

        /// <summary>
        /// 
        /// </summary>
        public enum ChipType
        {
            YM2610B,
            YM2610,
        }

        private ChipType f_ChipType = ChipType.YM2610B;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Chip variation")]
        [DefaultValue(ChipType.YM2610B)]
        public ChipType ChipVariation
        {
            get
            {
                return f_ChipType;
            }
            set
            {
                if (f_ChipType != value)
                {
                    f_ChipType = value;
                }
            }
        }

        private YM2610BAdpcmSoundTable adpcmASoundTable;

        [DataMember]
        [Category("Chip(ADPCM)")]
        [Description("YM2610 ADPCM-A DATA. 18.5 kHz sampling rate at 12-bit from 4-bit data.")]
        [Editor(typeof(OpnbPcmTableUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [PcmTableEditor("ADPCM Audio File(*.pcma, *.wav)|*.pcma;*.wav")]
        [TypeConverter(typeof(CustomObjectTypeConverter))]
        public YM2610BAdpcmSoundTable AdpcmASoundTable
        {
            get
            {
                return adpcmASoundTable;
            }
            set
            {
                if (adpcmASoundTable != value)
                {
                    adpcmASoundTable = value;
                    if (adpcmASoundTable != null)
                    {
                        adpcmASoundTable.Parent = this;
                        adpcmASoundTable.Submit();
                    }
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
                    YM2610BWriteData(UnitNumber, 0x0b, 0, 0, (byte)f_EnvelopeFrequencyCoarse);
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
                    YM2610BWriteData(UnitNumber, 0x0c, 0, 0, (byte)f_EnvelopeFrequencyFine);
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
                    YM2610BWriteData(UnitNumber, 0x0d, 0, 0, (byte)f_EnvelopeType);
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
                    YM2610BWriteData(UnitNumber, 0x22, 0, 0, (byte)(LFOEN << 3 | LFRQ));
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
                    YM2610BWriteData(UnitNumber, 0x22, 0, 0, (byte)(LFOEN << 3 | LFRQ));
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
                Timbres = (YM2610BTimbre[])value;
            }
        }

        [DataMember]
        [Category(" Timbres")]
        [Description("Timbres")]
        [EditorAttribute(typeof(YM2610BUITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public YM2610BTimbre[] Timbres
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
            try
            {
                using (var obj = JsonConvert.DeserializeObject<YM2610B>(serializeData))
                    this.InjectFrom(new LoopInjection(new[] { "SerializeData", "SerializeDataSave", "SerializeDataLoad" }), obj);
                YM2610BSetCallback(UnitNumber, f_read_byte_callback);
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
        private delegate void delegate_YM2610B_write(uint unitNumber, uint address, byte data);


        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2610B_write YM2610B_write
        {
            get;
            set;
        }

        private void YM2610BWriteData(uint unitNumber, byte address, int op, int slot, byte data)
        {
            YM2610BWriteData(unitNumber, address, op, slot, data, true);
        }
        /// <summary>
        /// 
        /// </summary>
        private void YM2610BWriteData(uint unitNumber, byte address, int op, int slot, byte data, bool useCache)
        {
            YM2610BWriteData(unitNumber, address, op, slot, data, useCache, false);
        }

        /// <summary>
        /// 
        /// </summary>
        private void YM2610BWriteData(uint unitNumber, byte address, int op, int slot, byte data, bool useCache, bool internalOnly)
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
                            case SoundEngineType.VSIF_MSX_Pi:
                                if (adrs < 0x100)
                                    vsifClient.WriteData(0x1A, (byte)adrs, (byte)data, 0);
                                else
                                    vsifClient.WriteData(0x1B, (byte)(adrs & 0xff), (byte)data, 0);
                                break;
                            case SoundEngineType.VSIF_MSX_PiTr:
                                if (adrs < 0x100)
                                    vsifClient.WriteData(0x3A, (byte)adrs, (byte)data, -2);
                                else
                                    vsifClient.WriteData(0x3B, (byte)(adrs & 0xff), (byte)data, -2);
                                break;
                        }
                    }
                }

                uint yreg = (uint)(slot / 3) * 2;
                DeferredWriteData(YM2610B_write, unitNumber, yreg + 0, (byte)(address + (op * 4) + (slot % 3)));
                DeferredWriteData(YM2610B_write, unitNumber, yreg + 1, data);

                //try
                //{
                //    Program.SoundUpdating();
                //    YM2610B_write(unitNumber, reg + 0, (byte)(address + (op * 4) + (slot % 3)));
                //    YM2610B_write(unitNumber, reg + 1, data);
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
        private delegate byte delegate_YM2610B_read(uint unitNumber, uint address);


        /// <summary>
        /// 
        /// </summary>
        private static delegate_YM2610B_read YM2610B_read
        {
            get;
            set;
        }


        /// <summary>
        /// 
        /// </summary>
        private static byte YM2610BReadData(uint unitNumber, byte address, int op, int slot)
        {
            uint reg = (uint)(slot / 3) * 2;
            try
            {
                Program.SoundUpdating();
                FlushDeferredWriteData();

                YM2610B_write(unitNumber, reg + 0, (byte)(address + (op * 4) + (slot % 3)));
                return YM2610B_read(unitNumber, reg + 1);
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
        private static void YM2610BSetCallback(uint unitNumber, delg_adpcm_callback callback)
        {
            try
            {
                Program.SoundUpdating();
                set_callback(unitNumber, callback);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_set_adpcm_callback set_callback
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        static YM2610B()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("ym2610b_write");
            if (funcPtr != IntPtr.Zero)
            {
                YM2610B_write = (delegate_YM2610B_write)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2610B_write));
            }
            funcPtr = MameIF.GetProcAddress("ym2610b_read");
            if (funcPtr != IntPtr.Zero)
            {
                YM2610B_read = (delegate_YM2610B_read)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_YM2610B_read));
            }
            funcPtr = MameIF.GetProcAddress("ym2610b_set_adpcm_callback");
            if (funcPtr != IntPtr.Zero)
                set_callback = (delegate_set_adpcm_callback)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_set_adpcm_callback));
        }

        private delg_adpcm_callback f_read_byte_callback;

        private YM2610BSoundManager soundManager;

        /// <summary>
        /// 
        /// </summary>
        private const int ADPCM_A = 0;

        /// <summary>
        /// 
        /// </summary>
        private const int ADPCM_B = 1;

        private YM2610BPcmManager[] adpcmManagers = new YM2610BPcmManager[2];

        /// <summary>
        /// 
        /// </summary>
        public YM2610B(uint unitNumber) : base(unitNumber)
        {
            GainLeft = DEFAULT_GAIN;
            GainRight = DEFAULT_GAIN;

            adpcmManagers[ADPCM_A] = new YM2610BPcmManager(this, ADPCM_A);
            adpcmManagers[ADPCM_B] = new YM2610BPcmManager(this, ADPCM_B);

            AdpcmASoundTable = new YM2610BAdpcmSoundTable(this);

            Timbres = new YM2610BTimbre[128];
            for (int i = 0; i < 128; i++)
                Timbres[i] = new YM2610BTimbre();
            setPresetInstruments();

            this.soundManager = new YM2610BSoundManager(this);

            f_read_byte_callback = new delg_adpcm_callback(read_byte_callback);
            YM2610BSetCallback(UnitNumber, f_read_byte_callback);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            soundManager?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryType"></param>
        /// <param name="address"></param>
        private byte read_byte_callback(byte memoryType, int address)
        {
            lock (adpcmManagers[memoryType])
            {
                //Debug.WriteLine(" address=" + address.ToString("X6"));
                return adpcmManagers[memoryType].ReadFlashMemory(address);
            }
        }

        internal override void PrepareSound()
        {
            base.PrepareSound();

            initGlobalRegisters();
        }

        private bool ignoreUpdatePcmData;

        private void initGlobalRegisters()
        {
            //SSG OFF
            YM2610BWriteData(UnitNumber, (byte)(7), 0, 0, (byte)(0x3f));
            //ADPCMA TOTAL LEVEL MAX
            YM2610BWriteData(UnitNumber, (byte)(1), 0, 3, (byte)(0x3f));
            //LFO
            YM2610BWriteData(UnitNumber, 0x22, 0, 0, 0x00);

            if (!IsDisposing && !ignoreUpdatePcmData)
            {
                adpcmManagers[ADPCM_A].Reset();
                adpcmManagers[ADPCM_B].Reset();

                for (int i = 0; i < Timbres.Length; i++)
                {
                    if (Timbres[i] is YM2610BTimbre t)
                    {
                        adpcmManagers[ADPCM_B].DeletePCM(i);
                        t.PcmAddressStart = 0;
                        t.PcmAddressEnd = 0;
                        if (t.PcmData != null && t.PcmData.Length != 0)
                        {
                            var pe = adpcmManagers[ADPCM_B].RegisterPCM(i, t.PcmData);
                            if (pe != null)
                            {
                                t.PcmAddressStart = (uint)pe.StartAddress;
                                t.PcmAddressEnd = (uint)(pe.StartAddress + pe.Size - 1);
                            }
                        }
                    }
                }
                for (int i = 0; i < AdpcmASoundTable.PcmTimbres.Length; i++)
                {
                    if (AdpcmASoundTable.PcmTimbres[i] is YM2610BAdpcmTimbre t)
                    {
                        adpcmManagers[ADPCM_A].DeletePCM(i);
                        t.PcmAddressStart = 0;
                        t.PcmAddressEnd = 0;
                        if (t.PcmData != null && t.PcmData.Length != 0)
                        {
                            var pe = adpcmManagers[ADPCM_A].RegisterPCM(i, t.PcmData);
                            if (pe != null)
                            {
                                t.PcmAddressStart = (uint)pe.StartAddress;
                                t.PcmAddressEnd = (uint)(pe.StartAddress + pe.Size - 1);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vgmPath"></param>
        public override void StartVgmRecordingTo(string vgmPath)
        {
            base.StartVgmRecordingTo(vgmPath);

            initGlobalRegisters();
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

        public class YM2610BPcmEntry
        {
            public bool Valid;
            public int StartAddress;
            public int Size;
            public int Id;
        }

        public enum MemoryState
        {
            Unused,
            Used,
            Dirty
        }

        public class YM2610BPcmManager
        {
            private static readonly int FlashSize = 16 * 1024 * 1024; // 16MB
            private static readonly int BlockSize = 64 * 1024;       // 64KB
            private static readonly int PageSize = 256;              // 256B
            private static readonly int MaxBlocks = FlashSize / BlockSize;

            private byte[] flashMemory = new byte[FlashSize]; // 仮想フラッシュメモリ
            private MemoryState[] pageUsed = new MemoryState[FlashSize / PageSize];
            private bool[] blockErased = new bool[MaxBlocks];

            private Dictionary<int, YM2610BPcmEntry> pcmEntries = new Dictionary<int, YM2610BPcmEntry>();

            private YM2610B parentInst;

            /// <summary>
            /// 0: ADPCM-A 1: ADPCM-B
            /// </summary>
            private int memoryType;

            private int getBlockIndex(int address) => address / BlockSize;
            private int getBlockStart(int address) => address & ~(BlockSize - 1);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="inst"></param>
            /// <param name="memoryType">0:ADPCM-A, 1:ADPCM-B</param>
            public YM2610BPcmManager(YM2610B inst, int memoryType)
            {
                parentInst = inst;
                this.memoryType = memoryType;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Reset()
            {
                Array.Clear(flashMemory, 0, flashMemory.Length);
                Array.Clear(pageUsed, 0, pageUsed.Length);
                Array.Clear(blockErased, 0, blockErased.Length);
                pcmEntries.Clear();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="address"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            public byte ReadFlashMemory(int address)
            {
                if (address < 0 || address >= flashMemory.Length)
                    throw new ArgumentOutOfRangeException(nameof(address), "Address out of range");
                return flashMemory[address];
            }

            private void eraseBlock(int blockIdx)
            {
                for (int i = 0; i < BlockSize; i++)
                    flashMemory[blockIdx + i] = 0xFF;

                int pageIdx = blockIdx / PageSize;
                for (int i = 0; i < BlockSize / PageSize; i++)
                {
                    pageUsed[pageIdx + i] = MemoryState.Unused;
                }

                blockErased[getBlockIndex(blockIdx)] = true;
            }

            private bool safeWrite(int address, byte[] data)
            {
                if (address % PageSize != 0)// || data.Length % PageSize != 0)
                    throw new ArgumentException("Write must be 256-byte aligned and sized");

                //int startBlock = GetBlockIndex(address);
                //int endBlock = GetBlockIndex(address + data.Length - 1);

                //for (int blk = startBlock; blk <= endBlock; blk++)
                //{
                //    if (!blockErased[blk])
                //    {
                //        EraseBlock(blk * BlockSize);
                //    }
                //}

                for (int i = 0; i < data.Length; i += PageSize)
                {
                    int saddr = address + i;

                    int pageIndex = saddr / PageSize;
                    if (pageUsed[pageIndex] == MemoryState.Used)
                    {
                        Debug.WriteLine("Page already used: cannot overwrite at " + saddr);
                        return false;
                    }

                    bool erase = !blockErased[getBlockIndex(saddr)];
                    if (erase)
                        eraseBlock(getBlockStart(saddr));

                    byte[] chunkData = new byte[PageSize];
                    // Fill with 0x80 to silence
                    for (int j = 0; j < PageSize; j++)
                        chunkData[j] = 0x80;
                    int sz = Math.Min(PageSize, data.Length - i);

                    Array.Copy(data, i, chunkData, 0, sz);
                    Array.Copy(chunkData, 0, flashMemory, address + i, PageSize);

                    //Write to the flash memory
                    switch (parentInst.SoundEngine)
                    {
                        case SoundEngineType.VSIF_MSX_Pi:
                        case SoundEngineType.VSIF_MSX_PiTr:
                            parentInst.vsifClient.DataWriter.
                                RawWrite(new byte[] { 0x19, (byte)(memoryType + 1), (byte)((saddr) >> 16), (byte)(saddr >> 8), (byte)(erase ? 1 : 0) }, null);
                            parentInst.vsifClient.DataWriter.
                                RawWrite(chunkData, null);
                            break;
                    }
                    if (erase)
                        Debug.WriteLine($"Erase Page:{pageIndex}-{pageIndex + (BlockSize / PageSize) - 1} Adr:{saddr}-{saddr + 0xffff}");
                    Debug.WriteLine($"Write PCM Type:{memoryType} Page:{pageIndex} Adr:{saddr}");

                    pageUsed[pageIndex] = MemoryState.Used;


                }

                return true;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="size"></param>
            /// <param name="gc"></param>
            /// <returns>Start address</returns>
            private int findFreeRegion(int size, bool gc)
            {
                int requiredPages = (size + PageSize - 1) / PageSize;
                int totalPages = flashMemory.Length / PageSize;

                for (int pageIdx = 0; pageIdx <= totalPages - requiredPages; pageIdx++)
                {
                    bool spaceAvailable = true;
                    for (int i = 0; i < requiredPages; i++)
                    {
                        if (pageUsed[pageIdx + i] != MemoryState.Unused)
                        {
                            spaceAvailable = false;
                            break;
                        }
                    }

                    if (spaceAvailable)
                    {
                        //同じバンクのみ可能
                        if (((pageIdx * PageSize) & 0xF00000) ==
                            (((pageIdx + requiredPages - 1) * PageSize) & 0xF00000))
                        {
                            Debug.WriteLine($"Found Page:{pageIdx} - {pageIdx + (requiredPages - 1)}");
                            return pageIdx * PageSize;
                        }
                    }
                }

                if (gc) // If no space found and GC is allowed
                {
                    //GC
                    bool rescanAvailable = false;
                    var mb = MaxBlocks;
                    switch (parentInst.SoundEngine)
                    {
                        case SoundEngineType.Software:
                            break;
                        case SoundEngineType.VSIF_MSX_Pi:
                        case SoundEngineType.VSIF_MSX_PiTr:
                            mb = 8 * 1024 * 1024 / BlockSize;
                            break;
                    }

                    for (int blockIdx = 0; blockIdx < mb; blockIdx++)
                    {
                        int pageIdx = blockIdx / PageSize;

                        if (blockErased[blockIdx])
                        {
                            bool canErase = true;
                            for (int i = 0; i < BlockSize / PageSize; i++)
                            {
                                if (pageUsed[pageIdx + i] == MemoryState.Used)
                                {
                                    canErase = false;
                                    break;
                                }
                            }
                            if (canErase)
                            {
                                for (int i = 0; i < BlockSize / PageSize; i++)
                                    pageUsed[pageIdx + i] = MemoryState.Unused;
                                blockErased[blockIdx] = false;
                                rescanAvailable = true;
                                Debug.WriteLine($"GCed Block:{blockIdx}");
                            }
                        }
                    }

                    if (rescanAvailable)
                        return findFreeRegion(size, false);
                }

                return -1;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public YM2610BPcmEntry RegisterPCM(int id, byte[] data)
            {
                if (data.Length == 0)
                    return null;

                int address = findFreeRegion(data.Length, true);
                if (address == -1)
                {
                    Debug.WriteLine("No space for PCM");
                    return null;
                }

                if (!safeWrite(address, data))
                {
                    Debug.WriteLine("Write failed");
                    return null;
                }

                DeletePCM(id);
                pcmEntries[id] = new YM2610BPcmEntry { Id = id, StartAddress = address, Size = data.Length, Valid = true };
                int requiredPages = (data.Length + PageSize - 1) / PageSize;
                Debug.WriteLine($"Registered PCM No:{id} Type:{memoryType} Page:{address / PageSize}-{(address / PageSize) + (requiredPages - 1)} Adr:{address} Sz:{data.Length}");

                return pcmEntries[id];
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool DeletePCM(int id)
            {
                if (!pcmEntries.ContainsKey(id))
                    return false;

                var entry = pcmEntries[id];
                if (entry == null || !entry.Valid)
                    return false;

                int startPageIdx = entry.StartAddress / PageSize;
                int pageCount = entry.Size / PageSize;

                for (int i = 0; i < pageCount; i++)
                {
                    pageUsed[startPageIdx + i] = MemoryState.Dirty;
                    Debug.WriteLine($"Page{startPageIdx + i} = Dirty");
                }

                entry.Valid = false;
                return true;
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
            soundManager.ProcessAllSoundOff();
        }

        internal override void ResetAll()
        {
            ClearWrittenDataCache();
            PrepareSound();
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2610BSoundManager : SoundManagerBase
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

            private static SoundList<YM2610BSound> fmOnSounds = new SoundList<YM2610BSound>(6);

            private static SoundList<YM2610BSound> ssgOnSounds = new SoundList<YM2610BSound>(3);

            private static SoundList<YM2610BSound> pcmaOnSounds = new SoundList<YM2610BSound>(6);

            private static SoundList<YM2610BSound> pcmbOnSounds = new SoundList<YM2610BSound>(1);

            private YM2610B parentModule;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public YM2610BSoundManager(YM2610B parent) : base(parent)
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
                    YM2610BTimbre timbre = (YM2610BTimbre)bts[i];

                    tindex++;
                    var emptySlot = searchEmptySlot(note, timbre);
                    if (emptySlot.slot < 0)
                        continue;

                    YM2610BSound snd = new YM2610BSound(emptySlot.inst, this, timbre, tindex - 1, note, emptySlot.slot, ids[i]);
                    switch (timbre.ToneType)
                    {
                        case ToneType.FM:
                            fmOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn FM ch" + emptySlot + " " + note.ToString());
                            break;
                        case ToneType.ADPCM_A:
                            pcmaOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn PCM-A ch" + emptySlot + " " + note.ToString());
                            break;
                        case ToneType.ADPCM_B:
                            pcmbOnSounds.Add(snd);
                            FormMain.OutputDebugLog(parentModule, "KeyOn PCM-B ch" + emptySlot + " " + note.ToString());
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
            private (YM2610B inst, int slot) searchEmptySlot(TaggedNoteOnEvent note, YM2610BTimbre timbre)
            {
                var emptySlot = (parentModule, -1);

                switch (timbre.ToneType)
                {
                    case ToneType.FM:
                        {
                            emptySlot = SearchEmptySlotAndOffForLeader(parentModule, fmOnSounds, note, parentModule.ChipVariation == ChipType.YM2610B ? 6 : 4);
                            break;
                        }
                    case ToneType.ADPCM_A:
                        {
                            emptySlot = SearchEmptySlotAndOffForLeader(parentModule, pcmaOnSounds, note, 6);
                            break;
                        }
                    case ToneType.ADPCM_B:
                        {
                            emptySlot = SearchEmptySlotAndOffForLeader(parentModule, pcmbOnSounds, note, 1);
                            break;
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
                    parentModule.YM2610BWriteData(parentModule.UnitNumber, 0x28, 0, 0, (byte)(0x00 | (reg << 1) | (byte)(i % 3)));

                    for (int op = 0; op < 4; op++)
                        parentModule.YM2610BWriteData(parentModule.UnitNumber, 0x40, op, i, 127);
                }
                parentModule.YM2610BWriteData(parentModule.UnitNumber, 7, 0, 0, (byte)0x3f);
                parentModule.YM2610BWriteData(parentModule.UnitNumber, (byte)(0), 0, 3, (byte)0xff);
                parentModule.YM2610BWriteData(parentModule.UnitNumber, (byte)(0x10), 0, 0, (byte)(0x00));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private class YM2610BSound : SoundBase
        {

            private YM2610B parentModule;

            private uint unitNumber;

            private int timbreIndex;

            private YM2610BTimbre timbre;

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
            public YM2610BSound(YM2610B parentModule, YM2610BSoundManager manager, TimbreBase timbre, int tindex, TaggedNoteOnEvent noteOnEvent, int slot, int timbreIndex) : base(parentModule, manager, timbre, tindex, noteOnEvent, slot)
            {
                this.parentModule = parentModule;
                this.timbreIndex = timbreIndex;
                this.timbre = (YM2610BTimbre)timbre;
                this.unitNumber = parentModule.UnitNumber;

                lastToneType = this.timbre.ToneType;
                lastSoundType = this.timbre.SsgSoundType;
                baseFreq = this.timbre.BaseFreqency;
            }

            public int ConvertSlot(int slot)
            {
                if (parentModule.ChipVariation == ChipType.YM2610)
                {
                    if (slot >= 0)
                        slot++;
                    if (slot >= 3)
                        slot++;
                }
                return slot;
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
                            uint reg = (uint)(ConvertSlot(Slot) / 3) * 2;
                            byte op = (byte)(timbre.Ops[0].Enable << 4 | timbre.Ops[1].Enable << 5 | timbre.Ops[2].Enable << 6 | timbre.Ops[3].Enable << 7);
                            parentModule.YM2610BWriteData(unitNumber, 0x28, 0, 0, (byte)(op | (reg << 1) | (byte)(ConvertSlot(Slot) % 3)));
                        }
                        break;
                    case ToneType.SSG:
                        {
                            OnPitchUpdated();
                            OnVolumeUpdated();

                            if (((int)lastSoundType & 4) != 0)
                                parentModule.YM2610BWriteData(unitNumber, (byte)(13), 0, 0, parentModule.EnvelopeType, false);
                        }
                        break;
                    case ToneType.ADPCM_A:
                        {
                            int nn = NoteOnEvent.NoteNumber;
                            YM2610BAdpcmTimbre pt = (YM2610BAdpcmTimbre)parentModule.AdpcmASoundTable.PcmTimbres[nn];
                            var pd = pt.PcmData;
                            if (pd == null)
                                break;
                            ushort len = 0;
                            if (pd != null && pd.Length > 0)
                                len = (ushort)(((pd.Length - 1) & 0xffffff) >> 8);
                            if (len == 0)
                                break;

                            //Volume
                            OnVolumeUpdated();
                            //Pan
                            OnPanpotUpdated();

                            //pcm start
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x10 + Slot), 0, 3, (byte)(pt.PcmAddressStart >> 8));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x18 + Slot), 0, 3, (byte)(pt.PcmAddressStart >> 16));
                            //pcm end
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x20 + Slot), 0, 3, (byte)(pt.PcmAddressEnd >> 8));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x28 + Slot), 0, 3, (byte)(pt.PcmAddressEnd >> 16));
                            //KeyOn
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0), 0, 3, (byte)(1 << Slot), false);
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            var pd = timbre.PcmData;
                            if (pd == null)
                                break;
                            ushort len = 0;
                            if (pd != null && pd.Length > 0)
                                len = (ushort)(((pd.Length - 1) & 0xffffff) >> 8);
                            if (len == 0)
                                break;

                            //Pitch
                            OnPitchUpdated();
                            //Volume
                            OnVolumeUpdated();
                            //Pan
                            OnPanpotUpdated();

                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x16), 0, 0, (byte)ADPCM_B, false, true);   //HACK:
                            //pcm start
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x12), 0, 0, (byte)(timbre.PcmAddressStart >> 8));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x13), 0, 0, (byte)(timbre.PcmAddressStart >> 16));
                            //pcm end
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x14), 0, 0, (byte)(timbre.PcmAddressEnd >> 8));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x15), 0, 0, (byte)(timbre.PcmAddressEnd >> 16));
                            //KeyOn
                            byte loop = timbre.LoopEnable ? (byte)0x10 : (byte)0x00;
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x10), 0, 0, (byte)(0x00));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x10), 0, 0, (byte)(0x80 | loop), false);
                        }
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
                            parentModule.YM2610BWriteData(unitNumber, 0x30, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].DT1 << 4 | timbre.Ops[op].MUL)));
                            //$40+: total level
                            switch (timbre.ALG)
                            {
                                case 0:
                                    if (op != 3)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 1:
                                    if (op != 3)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 2:
                                    if (op != 3)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 3:
                                    if (op != 3)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 4:
                                    if (op != 1 && op != 3)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 5:
                                    if (op == 4)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 6:
                                    if (op == 4)
                                        parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                                    break;
                                case 7:
                                    break;
                            }
                            //$50+: attack rate and rate scaling
                            parentModule.YM2610BWriteData(unitNumber, 0x50, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].RS << 6 | timbre.Ops[op].AR)));
                            //$60+: 1st decay rate and AM enable
                            parentModule.YM2610BWriteData(unitNumber, 0x60, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].AM << 7 | timbre.Ops[op].D1R)));
                            //$70+: 2nd decay rate
                            parentModule.YM2610BWriteData(unitNumber, 0x70, op, ConvertSlot(Slot), (byte)timbre.Ops[op].D2R);
                            //$80+: release rate and sustain level
                            parentModule.YM2610BWriteData(unitNumber, 0x80, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].SL << 4 | timbre.Ops[op].RR)));
                            //$90+: SSG-EG
                            parentModule.YM2610BWriteData(unitNumber, 0x90, op, ConvertSlot(Slot), (byte)timbre.Ops[op].SSG_EG);
                        }

                        //$B0+: algorithm and feedback
                        parentModule.YM2610BWriteData(unitNumber, 0xB0, 0, ConvertSlot(Slot), (byte)(timbre.FB << 3 | timbre.ALG));

                        if (!IsKeyOff)
                        {
                            //On
                            uint reg = (uint)(ConvertSlot(Slot) / 3) * 2;
                            byte open = (byte)(timbre.Ops[0].Enable << 4 | timbre.Ops[1].Enable << 5 | timbre.Ops[2].Enable << 6 | timbre.Ops[3].Enable << 7);
                            parentModule.YM2610BWriteData(unitNumber, 0x28, 0, 0, (byte)(open | (reg << 1) | (byte)(ConvertSlot(Slot) % 3)));
                        }

                        break;
                    case ToneType.SSG:
                        break;
                    case ToneType.ADPCM_A:
                        break;
                    case ToneType.ADPCM_B:
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
                            parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)((127 / velo) - Math.Round(((127 / velo) - (timbre.Ops[op].TL / velo)) * v)));
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
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)vol);
                            }
                        }
                        break;
                    case ToneType.SSG:
                        updatePsgVolume();
                        break;
                    case ToneType.ADPCM_A:
                        byte fv = (byte)(((byte)Math.Round(31 * CalcCurrentVolume()) & 0x1f));
                        byte pan = CalcCurrentPanpot();
                        if (pan < 32)
                            pan = 0x2;
                        else if (pan > 96)
                            pan = 0x1;
                        else
                            pan = 0x3;
                        parentModule.YM2610BWriteData(unitNumber, (byte)(0x08 + Slot), 0, 3, (byte)(pan << 6 | fv));
                        break;
                    case ToneType.ADPCM_B:
                        parentModule.YM2610BWriteData(unitNumber, (byte)(0x1b), 0, 0, (byte)(Math.Round(127 * CalcCurrentVolume())));
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
                    parentModule.YM2610BWriteData(unitNumber, (byte)(0x08 + Slot), 0, 0, (byte)(fv & 0xf));
                else
                    //Envelope
                    parentModule.YM2610BWriteData(unitNumber, (byte)(0x08 + Slot), 0, 0, (byte)(0x10 | fv));

                //key on
                byte data = YM2610BReadData(unitNumber, (byte)(7), 0, 0);
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
                parentModule.YM2610BWriteData(unitNumber, (byte)(7), 0, 0, data);
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

                            parentModule.YM2610BWriteData(unitNumber, 0xa4, 0, ConvertSlot(Slot), (byte)(octave | ((dfreq >> 8) & 7)), false);
                            parentModule.YM2610BWriteData(unitNumber, 0xa0, 0, ConvertSlot(Slot), (byte)(0xff & dfreq), false);
                        }
                        break;
                    case ToneType.SSG:
                        {
                            var st = lastSoundType;

                            //key on
                            byte data = YM2610BReadData(unitNumber, (byte)(7), 0, 0);
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
                            parentModule.YM2610BWriteData(unitNumber, (byte)(7), 0, 0, data);

                            if (((int)st & 1) == 1)
                                updatePsgPitch();

                            if (((int)st & 2) == 2)
                                updateNoisePitch();

                            if (((int)st & 4) != 0)
                            {

                                parentModule.YM2610BWriteData(unitNumber, (byte)(12), 0, 0, parentModule.EnvelopeFrequencyCoarse);
                                parentModule.YM2610BWriteData(unitNumber, (byte)(11), 0, 0, parentModule.EnvelopeFrequencyFine);
                                parentModule.YM2610BWriteData(unitNumber, (byte)(13), 0, 0, parentModule.EnvelopeType, false);
                                break;
                            }
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            uint freq = (uint)Math.Round((CalcCurrentFrequency() / baseFreq) * 65536d);
                            if (freq > 0xffff)
                                freq = 0xffff;

                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x19), 0, 0, (byte)(freq & 0xff));
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x1a), 0, 0, (byte)(freq >> 8));
                        }
                        break;
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

                freq = Math.Round(8000000 / 72 / 2 / freq); //HACK: Sync with FM sample rate
                if (freq > 0xfff)
                    freq = 0xfff;
                ushort tp = (ushort)freq;

                parentModule.YM2610BWriteData(unitNumber, (byte)(0 + (Slot * 2)), 0, 0, (byte)(tp & 0xff));
                parentModule.YM2610BWriteData(unitNumber, (byte)(1 + (Slot * 2)), 0, 0, (byte)((tp >> 8) & 0xf));
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

                parentModule.YM2610BWriteData(unitNumber, (byte)(6), 0, 0, (byte)v);
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnPanpotUpdated()
            {
                int pan = CalcCurrentPanpot();
                if (lastToneType == ToneType.ADPCM_A)
                {
                    var pct = parentModule.AdpcmASoundTable.PcmTimbres[NoteOnEvent.NoteNumber];
                    pan += pct.PanShift;
                    if (pan < 0)
                        pan = 0;
                    else if (pan > 127)
                        pan = 127;
                }

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
                        parentModule.YM2610BWriteData(unitNumber, 0xB4, 0, ConvertSlot(Slot), (byte)(pan << 6 | (timbre.AMS << 4) | timbre.FMS));
                        break;
                    case ToneType.ADPCM_A:
                        byte fv = (byte)(((byte)Math.Round(31 * CalcCurrentVolume()) & 0x1f));
                        parentModule.YM2610BWriteData(unitNumber, (byte)(0x08 + Slot), 0, 3, (byte)(pan << 6 | fv));
                        break;
                    case ToneType.ADPCM_B:
                        parentModule.YM2610BWriteData(unitNumber, (byte)(0x11), 0, 0, (byte)(pan << 6));
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
                    parentModule.YM2610BWriteData(unitNumber, 0x30, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].DT1 << 4 | timbre.Ops[op].MUL)));
                    //$40+: total level
                    switch (timbre.ALG)
                    {
                        case 0:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 1:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 2:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 3:
                            if (op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 4:
                            if (op != 1 && op != 3 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 5:
                            if (op == 0 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 6:
                            if (op == 0 && !timbre.UseExprForModulator)
                                parentModule.YM2610BWriteData(unitNumber, 0x40, op, ConvertSlot(Slot), (byte)timbre.Ops[op].TL);
                            break;
                        case 7:
                            break;
                    }
                    //$50+: attack rate and rate scaling
                    parentModule.YM2610BWriteData(unitNumber, 0x50, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].RS << 6 | timbre.Ops[op].AR)));
                    //$60+: 1st decay rate and AM enable
                    parentModule.YM2610BWriteData(unitNumber, 0x60, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].AM << 7 | timbre.Ops[op].D1R)));
                    //$70+: 2nd decay rate
                    parentModule.YM2610BWriteData(unitNumber, 0x70, op, ConvertSlot(Slot), (byte)timbre.Ops[op].D2R);
                    //$80+: release rate and sustain level
                    parentModule.YM2610BWriteData(unitNumber, 0x80, op, ConvertSlot(Slot), (byte)((timbre.Ops[op].SL << 4 | timbre.Ops[op].RR)));
                    //$90+: SSG-EG
                    parentModule.YM2610BWriteData(unitNumber, 0x90, op, ConvertSlot(Slot), (byte)timbre.Ops[op].SSG_EG);
                }

                //$B0+: algorithm and feedback
                parentModule.YM2610BWriteData(unitNumber, 0xB0, 0, ConvertSlot(Slot), (byte)(timbre.FB << 3 | timbre.ALG));

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
                        uint reg = (uint)(ConvertSlot(Slot) / 3) * 2;
                        parentModule.YM2610BWriteData(unitNumber, 0x28, 0, 0, (byte)(0x00 | (reg << 1) | (byte)(ConvertSlot(Slot) % 3)));
                        break;
                    case ToneType.SSG:
                        byte data = YM2610BReadData(unitNumber, 7, 0, 0);
                        data |= (byte)((1 | 8) << Slot);
                        parentModule.YM2610BWriteData(unitNumber, 7, 0, 0, (byte)data);

                        parentModule.YM2610BWriteData(unitNumber, (byte)(8 + Slot), 0, 0, (byte)0);
                        break;
                    case ToneType.ADPCM_A:
                        {
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0), 0, 3, (byte)(0x80 | (1 << Slot)));
                        }
                        break;
                    case ToneType.ADPCM_B:
                        {
                            parentModule.YM2610BWriteData(unitNumber, (byte)(0x10), 0, 0, (byte)(0x01));
                        }
                        break;
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
                var rv = (144 * freq * Math.Pow(2, 20) / 8000000) / Math.Pow(2, oct - 1);

                return rv;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2610BTimbre>))]
        [DataContract]
        [InstLock]
        public class YM2610BTimbre : TimbreBase, IFmTimbre
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

            public ToneType toneType;

            [DataMember]
            [Category("Sound")]
            [Description("Sound Type")]
            [DefaultValue(ToneType.FM)]
            public ToneType ToneType
            {
                get
                {
                    return toneType;
                }
                set
                {
                    if (toneType != value)
                        toneType = value;
                }
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

                //using (var f = new FormYM2610BEditor((YM2610B)inst, this, true))
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
            [Editor(typeof(YM2610BUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
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
            public YM2610BOperator[] Ops
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
                var ops = new YM2610BOperator[] {
                    new YM2610BOperator(),
                    new YM2610BOperator(),
                    new YM2610BOperator(),
                    new YM2610BOperator() };
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
            [Description("YM2610 ADPCM-B DATA. 55.5 kHz sampling rate at 12-bit from 4-bit data.\r\n" +
                "Or, wave file 16bit mono. MAX 16MB. (NEOTRON is 8MB) ")]
            [PcmFileLoaderEditor("Audio File(*.pcmb, *.wav)|*.pcmb;*.wav", 0, 16, 1, 16777215)]
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
                        PcmAddressStart = 0;
                        PcmAddressEnd = 0;
                        if (Instrument != null && Instrument.IsSoundPrepared)
                        {
                            var inst = (YM2610B)this.Instrument;
                            if (inst != null)
                            {
                                inst.adpcmManagers[ADPCM_B].DeletePCM(Index);
                                var pe = inst.adpcmManagers[ADPCM_B].RegisterPCM(Index, PcmData);
                                if (pe != null)
                                {
                                    PcmAddressStart = (uint)pe.StartAddress;
                                    PcmAddressEnd = (uint)(pe.StartAddress + pe.Size - 1);
                                }
                            }
                        }
                    }
                }
            }

            public bool ShouldSerializePcmData()
            {
                return f_PcmData.Length != 0;
            }

            public void ResetPcmData()
            {
                f_PcmData = new byte[0];
            }

            private String pcmDataInfo;

            [DataMember]
            [Category("Sound(ADPCM-B)")]
            [Description("PcmData information.")]
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
            [Category("Chip")]
            [Description("Global Settings")]
            public YM2610BGlobalSettings GlobalSettings
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
                GlobalSettings.InjectFrom(new LoopInjection(), new YM2610BGlobalSettings());
            }

            /// <summary>
            /// 
            /// </summary>
            public YM2610BTimbre()
            {
                Ops = new YM2610BOperator[] {
                    new YM2610BOperator(),
                    new YM2610BOperator(),
                    new YM2610BOperator(),
                    new YM2610BOperator() };
                GlobalSettings = new YM2610BGlobalSettings();
                SsgSoundType = SsgSoundType.PSG;
            }

            public override void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<YM2610BTimbre>(serializeData);
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

        /// <summary>
        /// 
        /// </summary>
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2610BOperator>))]
        [DataContract]
        [InstLock]
        public class YM2610BOperator : ContextBoundObject, ISerializeDataSaveLoad
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
                    var obj = JsonConvert.DeserializeObject<YM2610BOperator>(serializeData);
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
        [JsonConverter(typeof(NoTypeConverterJsonConverter<YM2610BGlobalSettings>))]
        [DataContract]
        [InstLock]
        public class YM2610BGlobalSettings : ContextBoundObject
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
        [Editor(typeof(EnumTypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(EnumConverter<ToneType>))]
        public enum ToneType
        {
            [Description("FM")]
            FM,
            [Description("SSG")]
            SSG,
            [Description("ADPCM-A(Use AdpcmASoundTable)")]
            ADPCM_A,
            [Description("ADPCM-B(Use PcmData)")]
            ADPCM_B,
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
            YM2610BTimbre tim = (YM2610BTimbre)timbre;

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


        private YM2610BCustomToneImporter importer;

        /// <summary>
        /// 
        /// </summary>
        public override CustomToneImporter CustomToneImporter
        {
            get
            {
                if (importer == null)
                {
                    importer = new YM2610BCustomToneImporter();
                }
                return importer;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class YM2610BCustomToneImporter : FmToneImporter
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
                        YM2610BTimbre tim = new YM2610BTimbre();
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
        [DataContract]
        [InstLock]
        public class YM2610BAdpcmSoundTable : PcmTimbreTableBase
        {
            private YM2610B parent;

            public YM2610B Parent
            {
                get
                {
                    return parent;
                }
                set
                {
                    parent = value;
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public YM2610BAdpcmSoundTable(YM2610B inst)
            {
                this.parent = inst;
                for (int i = 0; i < 128; i++)
                {
                    var pt = new YM2610BAdpcmTimbre(inst, i);
                    PcmTimbres[i] = pt;
                }
            }

            /// <summary>
            /// Submit changes to the PCM timbre table.
            /// </summary>
            public override void Submit()
            {
                if (Parent == null || !Parent.IsSoundPrepared)
                    return;

                for (int i = 0; i < PcmTimbres.Length; i++)
                {
                    var t = (YM2610BAdpcmTimbre)PcmTimbres[i];

                    Parent?.adpcmManagers[ADPCM_A].DeletePCM(i);
                    t.PcmAddressStart = 0;
                    t.PcmAddressEnd = 0;
                    if (t.PcmData != null && t.PcmData.Length != 0)
                    {
                        var pe = Parent?.adpcmManagers[ADPCM_A].RegisterPCM(i, t.PcmData);
                        if (pe != null)
                        {
                            t.PcmAddressStart = (uint)pe.StartAddress;
                            t.PcmAddressEnd = (uint)(pe.StartAddress + pe.Size - 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataContract]
        [InstLock]
        public class YM2610BAdpcmTimbre : PcmTimbreBase
        {
            private YM2610B parent;

            private YM2610B Parent
            {
                get
                {
                    if (parent == null)
                    {
                        bool found = false;
                        foreach (YM2610B inst in InstrumentManager.GetInstruments(19))
                        {
                            if (found)
                                break;
                            Parallel.ForEach(inst.AdpcmASoundTable.PcmTimbres, (t, state) =>
                            {
                                if (t == this)
                                {
                                    parent = inst;
                                    found = true;
                                    state.Break();
                                }
                            });
                        }
                    }
                    return parent;
                }
                set
                {
                    parent = value;
                }
            }

            private byte[] f_PcmData;

            /// <summary>
            /// 
            /// </summary>
            [DataMember]
            [Browsable(false)]
            public override byte[] PcmData
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="noteNumber"></param>
            public YM2610BAdpcmTimbre(YM2610B inst, int noteNumber) : base(noteNumber)
            {
                this.parent = inst;
            }
        }

    }
}