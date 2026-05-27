// copyright-holders:K.Ito
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FM_SoundConvertor;
using Kermalis.SoundFont2;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;
using Omu.ValueInjecter;
using Omu.ValueInjecter.Injections;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gimic;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Instruments.Envelopes;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.Scci;
using zanac.MAmidiMEmo.VSIF;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

//https://segaretro.org/images/2/2d/MCDHardware_Manual_PCM_Sound_Source.pdf
//https://www.retrodev.com/RF5C68A.pdf

namespace zanac.MAmidiMEmo.Instruments.Chips
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    [InstLock]
    public class RF5C164 : InstrumentBase
    {
        public override string Name => "RF5C164";

        public override string Group => "PCM";

        public override InstrumentType InstrumentType => InstrumentType.RF5C164;

        [Browsable(false)]
        public override string ImageKey => "RF5C164";

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected override string SoundInterfaceTagNamePrefix => "rf5c164_";

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
                return 32;
            }
        }


        private PortId portId = PortId.No1;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [Description("Set FTDI or COM Port No for \"VSIF - Genesis\".\r\n" +
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
            "Supports \"Software\" and \"VSIF\"")]
        [DefaultValue(SoundEngineType.Software)]
        [TypeConverter(typeof(EnumConverterSoundEngineTypeRF5C164))]
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
                    case SoundEngineType.VSIF_Genesis_FTDI:
                        vsifClient = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI, PortId, false);
                        if (vsifClient != null)
                        {
                            if (vsifClient.DataWriter.FtdiDeviceType == FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232R)
                            {
                                if (FtdiClkWidth < 9)
                                    FtdiClkWidth = 9;
                            }
                            else
                            {
                                if (FtdiClkWidth < 8)
                                    FtdiClkWidth = 8;
                            }

                            //HACK: sync clk
                            RF5C164RegWriteData(UnitNumber, 8, (byte)0xff, false); // dummy key off
                            RF5C164RegWriteData(UnitNumber, 8, (byte)0xff, false); // dummy key off

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

        private int f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;

        [DataMember]
        [Category("Chip(Dedicated)")]
        [SlideParametersAttribute(1, 100)]
        [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Description("Set FTDI Clock Width[%].\r\n" +
            "FT232R:8~\r\n" +
            "FT232H:9~")]
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
            return f_ftdiClkWidth != VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;
        }

        public void ResetFtdiClkWidth()
        {
            f_ftdiClkWidth = VsifManager.FTDI_BAUDRATE_GEN_CLK_WIDTH;
        }

        /// <summary>
        /// 
        /// </summary>
        public enum MasterClockType : uint
        {
            Default = 12500000,
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
                    f_MasterClock = value;
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
                Timbres = (RF5C164Timbre[])value;
            }
        }

        private RF5C164Timbre[] f_Timbres;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category(" Timbres")]
        [Description("Timbres")]
        [EditorAttribute(typeof(TimbresArrayUITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public RF5C164Timbre[] Timbres
        {
            get
            {
                return f_Timbres;
            }
            set
            {
                f_Timbres = value;
            }
        }

        private const float DEFAULT_GAIN = 1.0f;

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
        /// <param name="serializeData"></param>
        public override void RestoreFrom(string serializeData)
        {
            try
            {
                using (var obj = JsonConvert.DeserializeObject<RF5C164>(serializeData))
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
        private delegate void delegate_rf5c164_device_w(uint unitNumber, byte address, byte data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte delegate_rf5c164_device_r(uint unitNumber, byte address);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_rf5c164_device_mem_w(uint unitNumber, uint address, byte data);

        private byte keyOnData;

        /// <summary>
        /// 
        /// </summary>
        private void RF5C164RegWriteData(uint unitNumber, byte reg, byte data, bool useCache)
        {
            if (reg == 8)
                keyOnData = data;


            WriteData(reg, data, useCache, new Action(() =>
            {
                lock (sndEnginePtrLock)
                {
                    switch (CurrentSoundEngine)
                    {
                        case SoundEngineType.VSIF_Genesis_FTDI:
                            ushort adrs = (ushort)((reg << 1) + 1);
                            sendData(adrs, data, true);
                            break;
                    }
                }

                try
                {
                    Program.SoundUpdating();
                    multipcm_reg_write(unitNumber, (byte)reg, (byte)data);
                }
                finally
                {
                    Program.SoundUpdated();
                }
            }
            ));
        }

        /// <summary>
        /// 
        /// </summary>
        private void RF5C164MemWriteData(uint unitNumber, uint address, byte data)
        {
            lock (sndEnginePtrLock)
            {
                switch (CurrentSoundEngine)
                {
                    case SoundEngineType.VSIF_Genesis_FTDI:

                        ushort adrs = (ushort)((0x2000 + (address << 1)) + 1);
                        sendData(adrs, data, false);
                        break;
                }
            }

            try
            {
                Program.SoundUpdating();
                multipcm_mem_write(unitNumber, address, data);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        private byte mcdClk = 0x80;

        private void sendData(ushort address, byte data, bool wait)
        {
            List<PortWriteData> list = new List<PortWriteData>();
            list.Add(new PortWriteData() { Type = 0, Address = 10 << 2, Data = data, Wait = f_ftdiClkWidth });
            list.Add(new PortWriteData() { Type = 0, Address = 9 << 2, Data = (byte)(address & 0xff), Wait = f_ftdiClkWidth });
            list.Add(new PortWriteData() { Type = 0, Address = 8 << 2, Data = (byte)(((address >> 8) & 0x3f) | mcdClk), Wait = f_ftdiClkWidth });

            mcdClk ^= 0x80;

            if (wait)
            {
                list.Add(new PortWriteData() { Type = 0xff, Address = 0, Data = 0, Wait = f_ftdiClkWidth });
                list.Add(new PortWriteData() { Type = 0xff, Address = 0, Data = 0, Wait = f_ftdiClkWidth });
                list.Add(new PortWriteData() { Type = 0xff, Address = 0, Data = 0, Wait = f_ftdiClkWidth });
            }
            vsifClient.WriteData(list.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_rf5c164_device_w multipcm_reg_write
        {
            get;
            set;
        }

        private static delegate_rf5c164_device_mem_w multipcm_mem_write
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        static RF5C164()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("rf5c68_device_w");
            if (funcPtr != IntPtr.Zero)
                multipcm_reg_write = (delegate_rf5c164_device_w)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_rf5c164_device_w));

            funcPtr = MameIF.GetProcAddress("rf5c68_device_mem_w");
            if (funcPtr != IntPtr.Zero)
                multipcm_mem_write = (delegate_rf5c164_device_mem_w)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delegate_rf5c164_device_mem_w));
        }

        private RF5C164SoundManager soundManager;

        /// <summary>
        /// 
        /// </summary>
        public RF5C164(uint unitNumber) : base(unitNumber)
        {
            SetDevicePassThru(false);

            MasterClock = (uint)MasterClockType.Default;

            GainLeft = DEFAULT_GAIN;
            GainRight = DEFAULT_GAIN;

            readSoundFontForTimbre = new ToolStripMenuItem(Resources.ImportSF2Timbre);
            readSoundFontForTimbre.Click += ReadSoundFontForTimbre_Click;

            readSoundFontForDrumTimbre = new ToolStripMenuItem(Resources.ImportSF2Drum);
            readSoundFontForDrumTimbre.Click += ReadSoundFontForDrumTimbre_Click;

            Timbres = new RF5C164Timbre[256];
            for (int i = 0; i < 256; i++)
                Timbres[i] = new RF5C164Timbre();

            setPresetInstruments();

            this.soundManager = new RF5C164SoundManager(this);
        }

        #region IDisposable Support

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //マネージ状態を破棄します (マネージ オブジェクト)。
                    soundManager?.Dispose();
                    soundManager = null;
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        ~RF5C164()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public override void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);

            base.Dispose();

            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        internal override void PrepareSound()
        {
            base.PrepareSound();

            initGlobalRegisters();
        }

        private void initGlobalRegisters()
        {
            lock (sndEnginePtrLock)
                lastTransferPcmData = new byte[] { };

            if (!IsDisposing)
            {
                //koff
                RF5C164RegWriteData(UnitNumber, 8, 0xFF, true);

                updatePcmData(null);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void setPresetInstruments()
        {
        }

        private int waveMemorySize = 64 * 1024;

        /// <summary>
        /// 
        /// </summary>
        private void updatePcmData(RF5C164Timbre timbre)
        {
            List<byte> pcmData = new List<byte>();
            uint nextStartAddress = (uint)0x0;

            for (int i = 0; i < Timbres.Length; i++)
            {
                var tim = Timbres[i];

                tim.PcmAddressStart = 0;
                if (tim.PcmData.Length != 0)
                {
                    int tlen = tim.PcmData.Length + 32;
                    int pad = (0x100 - (tlen & 0xff)) & 0xff;    //256 byte pad (contains PCM end data)

                    if (nextStartAddress + tlen + pad - 1 < waveMemorySize)   //MAX 256KB
                    {
                        tim.PcmAddressStart = nextStartAddress;
                        tim.PcmAddressEnd = (uint)(nextStartAddress + tlen + pad - 1);

                        //Write PCM data
                        for (int j = 0; j < tim.PcmData.Length; j++)
                        {
                            var data = (byte)(Math.Round(((double)tim.PcmData[j] / 255d) * 253d) + 0);
                            if (data < 127)
                                data = (byte)(~data & 0x7f);
                            else
                                data = (byte)(data + 1);
                            pcmData.Add(data);
                        }

                        //Add pad
                        for (int j = 0; j < pad + 32; j++)
                            pcmData.Add(0xff);  //Adds end data

                        nextStartAddress = tim.PcmAddressEnd + 1;
                    }
                    else
                    {
                        MessageBox.Show(Resources.AdpcmBufferExceeded, "Warning", MessageBoxButtons.OK);
                        break;
                    }

                }
            }
            FormMain.OutputLog(this, "Remaining PCM RAM capacity is " + (waveMemorySize - nextStartAddress) + " bytes");

            if (pcmData.Count != 0)
            {
                string timbreName = null;
                if(timbre != null)
                    timbreName = timbre.DisplayName;
                else
                    timbreName = "All";

                FormMain.OutputLog(this, Resources.UpdatingADPCM + " (" + timbreName + ")");
                try
                {
                    FormMain.AppliactionForm.Enabled = false;
                    using (FormProgress f = new FormProgress())
                    {
                        f.StartPosition = FormStartPosition.CenterScreen;
                        f.Message = Resources.UpdatingADPCM + " (" + timbreName + ")";
                        f.Show();
                        //transferPcmOnlyDiffData(pcmData.ToArray(), f);
                        sendPcmData(pcmData.ToArray(), 0, f);
                    }
                }
                finally
                {
                    FormMain.AppliactionForm.Enabled = true;
                }
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
            int endAddress = transferData.Length;
            if (endAddress > waveMemorySize)
                endAddress = waveMemorySize;

            //Transfer
            int startAddress = i;
            int len = endAddress - startAddress;
            int index = 0;
            int percentage = 0;
            int lastPercentage = 0;
            uint baseAddress = 0x0000;

            for (int adr = startAddress; adr < endAddress; adr++)
            {
                uint wadr = (uint)(baseAddress + adr);

                //Write mode and select mem bank
                RF5C164RegWriteData(UnitNumber, 0x7, (byte)(0x00 | ((wadr >> 12) & 0xf)), true);
                //Write PCM DATA
                RF5C164MemWriteData(UnitNumber, (uint)(wadr & 0xfff), (byte)transferData[adr]);

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
                        case SoundEngineType.VSIF_Genesis_FTDI:
                            vsifClient.FlushDeferredWriteDataAndWait();
                            break;
                    }
                }
                lastPercentage = percentage;
                index++;
            }
            switch (CurrentSoundEngine)
            {
                case SoundEngineType.VSIF_Genesis_FTDI:
                    vsifClient.FlushDeferredWriteDataAndWait();
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
        }

        internal override void ResetAll()
        {
            ClearWrittenDataCache();
            PrepareSound();
        }

        /// <summary>
        /// 
        /// </summary>
        private class RF5C164SoundManager : SoundManagerBase
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

            private static SoundList<RF5C164Sound> instOnSounds = new SoundList<RF5C164Sound>(8);

            private RF5C164 parentModule;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            public RF5C164SoundManager(RF5C164 parent) : base(parent)
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
                    RF5C164Timbre timbre = (RF5C164Timbre)bts[i];

                    tindex++;
                    var emptySlot = searchEmptySlot(note);
                    if (emptySlot.slot < 0)
                        continue;

                    RF5C164Sound snd = new RF5C164Sound(emptySlot.inst, this, timbre, tindex - 1, note, emptySlot.slot, (byte)ids[i]);
                    instOnSounds.Add(snd);

                    FormMain.OutputDebugLog(parentModule, "KeyOn INST ch" + emptySlot + " " + note.ToString());
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
            private (RF5C164 inst, int slot) searchEmptySlot(TaggedNoteOnEvent note)
            {
                return SearchEmptySlotAndOffForLeader(parentModule, instOnSounds, note, 8);
            }

            internal override void ProcessAllSoundOff()
            {
                var me = new ControlChangeEvent((SevenBitNumber)120, (SevenBitNumber)0);
                ProcessControlChange(me);

                //koff
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 8, 0xFF, true);
            }

        }


        /// <summary>
        /// 
        /// </summary>
        private class RF5C164Sound : SoundBase
        {

            private RF5C164 parentModule;

            private byte timbreIndex;

            private RF5C164Timbre timbre;

            private double baseFreq;

            private uint loopPoint;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="parentModule"></param>
            /// <param name="noteOnEvent"></param>
            /// <param name="programNumber"></param>
            /// <param name="slot"></param>
            public RF5C164Sound(RF5C164 parentModule, RF5C164SoundManager manager, TimbreBase timbre, int tindex, TaggedNoteOnEvent noteOnEvent, int slot, byte timbreIndex) : base(parentModule, manager, timbre, tindex, noteOnEvent, slot)
            {
                this.parentModule = parentModule;
                this.timbreIndex = timbreIndex;
                this.timbre = (RF5C164Timbre)timbre;

                baseFreq = this.timbre.BaseFreqency;
                loopPoint = this.timbre.LoopPoint;
            }

            /// <summary>
            /// 
            /// </summary>
            public override void KeyOn()
            {
                base.KeyOn();

                uint adrs = (uint)(timbreIndex * 12);
                //start address
                if (timbre.PcmData.Length == 0)
                    return;

                //Select ch
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x7, (byte)(0xc0 | Slot), true);

                //PCM Start Address(Hi 8bit)
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 6, (byte)((timbre.PcmAddressStart >> 8) & 0xff), false);

                //loop address
                if (timbre.LoopEnable)
                {
                    parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 5, (byte)((timbre.LoopPoint >> 8) & 0xff), false);
                    parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 4, (byte)((timbre.LoopPoint) & 0xff), false);
                }
                else
                {
                    parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 5, (byte)(((timbre.PcmAddressStart + timbre.PcmData.Length - 1) >> 8) & 0xff), false);
                    parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 4, (byte)(((timbre.PcmAddressStart + timbre.PcmData.Length - 1)) & 0xff), false);
                }

                OnVolumeUpdated();
                OnPanpotUpdated();

                OnPitchUpdated();

                //KON
                byte bitPos = (byte)~(1 << Slot);
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 8, (byte)(bitPos & parentModule.keyOnData), true);
            }


            public override void OnSoundParamsUpdated()
            {
                //OnVolumeUpdated();
                //OnPanpotUpdated();
                //OnPitchUpdated();

                base.OnSoundParamsUpdated();
            }


            /// <summary>
            /// 
            /// </summary>
            public override void OnPanpotUpdated()
            {
                //Pan
                int pan = CalcCurrentPanpot();

                byte left = 0xf;
                byte right = 0xf;
                if (pan > 64)
                    left = (byte)((64 - (pan - 64)) >> 2);
                if (pan < 64)
                    right = (byte)(pan >> 2);

                //Select ch
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x7, (byte)(0xc0 | Slot), false);
                //Write Pan
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x1, (byte)(right << 4 | left), false);
            }

            /// <summary>
            /// 
            /// </summary>
            public override void OnVolumeUpdated()
            {
                var vol = (byte)Math.Round(255 * CalcCurrentVolume());

                //Select ch
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x7, (byte)(0xc0 | Slot), false);
                //Write Vol
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x0, vol, false);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="slot"></param>
            public override void OnPitchUpdated()
            {
                var cfreq = (CalcCurrentFrequency() / baseFreq) * (parentModule.MasterClock / 384) / timbre.SampleRate;

                int freq = (int)Math.Round(cfreq * 1024);
                if (freq > 0xffff)
                    freq = 0xffff;

                //Select ch
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x7, (byte)(0xc0 | Slot), false);

                //Write Freq L
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x2, (byte)(freq & 0xff), false);
                //Write Freq H
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 0x3, (byte)(freq >> 8), false);

                base.OnPitchUpdated();
            }

            /// <summary>
            /// 
            /// </summary>
            public override void SoundOff()
            {
                base.SoundOff();

                //KOFF
                byte bitPos = (byte)(1 << Slot);
                parentModule.RF5C164RegWriteData(parentModule.UnitNumber, 8, (byte)(bitPos | parentModule.keyOnData), true);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(NoTypeConverterJsonConverter<RF5C164Timbre>))]
        [DataContract]
        [InstLock]
        public class RF5C164Timbre : TimbreBase
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

            [DataMember]
            [Category("Sound")]
            [Description("PCM base frequency @ 32552Hz [Hz]")]
            [DefaultValue(typeof(double), "440")]
            [DoubleSlideParametersAttribute(100, 2000, 1)]
            [EditorAttribute(typeof(DoubleSlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public double BaseFreqency
            {
                get;
                set;
            } = 440;

            private uint f_SampleRate = 32552;

            [DataMember]
            [Category("Sound")]
            [Description("Set PCM samplerate [Hz]")]
            [DefaultValue(typeof(uint), "32552")]
            [SlideParametersAttribute(4000, 32552)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public uint SampleRate
            {
                get => f_SampleRate;
                set
                {
                    f_SampleRate = value;
                }
            }

            private bool f_LoopEnable;

            [DataMember]
            [Category("Sound")]
            [Description("Loop point enable")]
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

            private ushort f_LoopPoint;

            [DataMember]
            [Category("Sound")]
            [Description("Loop start offset address")]
            [DefaultValue(typeof(ushort), "0")]
            [SlideParametersAttribute(0, 65535)]
            [EditorAttribute(typeof(SlideEditor), typeof(System.Drawing.Design.UITypeEditor))]
            public ushort LoopPoint
            {
                get => f_LoopPoint;
                set
                {
                    f_LoopPoint = (ushort)(value & 0xffff);
                }
            }

            private byte[] f_pcmData = new byte[0];

            [TypeConverter(typeof(LoadDataTypeConverter))]
            [Editor(typeof(PcmFileLoaderUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
            [DataMember]
            [Category("Sound")]
            [Description("Unsigned 8bit Mono Wave Data (MAX 64kb)")]
            [PcmFileLoaderEditor("Audio File(*.wav)|*.wav", 0, 8, 1, 65535)]
            public byte[] PcmData
            {
                get
                {
                    return f_pcmData;
                }
                set
                {
                    f_pcmData = value;

                    var inst = (RF5C164)this.Instrument;
                    if (inst != null)
                        inst.updatePcmData(this);
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

            private String pcmDataInfo;

            [DataMember]
            [Category("Sound")]
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

            /*
            [DataMember]
            [Category("Chip")]
            [Description("Global Settings")]
            public RF5C164GlobalSettings GlobalSettings
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
                GlobalSettings.InjectFrom(new LoopInjection(), new RF5C164GlobalSettings());
            }
            */

            /// <summary>
            /// 
            /// </summary>
            public RF5C164Timbre()
            {
                //GlobalSettings = new RF5C164GlobalSettings();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="serializeData"></param>
            public override void RestoreFrom(string serializeData)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<RF5C164Timbre>(serializeData);
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
        [JsonConverter(typeof(NoTypeConverterJsonConverter<RF5C164GlobalSettings>))]
        [DataContract]
        [InstLock]
        public class RF5C164GlobalSettings : ContextBoundObject
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

        }

        #region MENU

        private ToolStripMenuItem readSoundFontForTimbre;

        private ToolStripMenuItem readSoundFontForDrumTimbre;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override IEnumerable<ToolStripMenuItem> GetInstrumentMenus()
        {
            List<ToolStripMenuItem> menus = new System.Collections.Generic.List<ToolStripMenuItem>(base.GetInstrumentMenus());

            menus.AddRange(new ToolStripMenuItem[] {
                readSoundFontForTimbre,
                readSoundFontForDrumTimbre
            });

            return menus.ToArray();
        }

        private System.Windows.Forms.OpenFileDialog openFileDialog;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadSoundFontForTimbre_Click(object sender, EventArgs e)
        {
            try
            {
                int offset = 0;
                using (openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog.SupportMultiDottedExtensions = true;
                    openFileDialog.Title = "Select a SoundFont v2.0 file";
                    openFileDialog.Filter = "SoundFont v2.0 File(*.sf2)|*.sf2";

                    var fr = openFileDialog.ShowDialog(null);
                    if (fr != DialogResult.OK)
                        return;

                    loadPcm(openFileDialog.FileName, offset, false);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadSoundFontForDrumTimbre_Click(object sender, EventArgs e)
        {
            try
            {
                int offset = 128;
                using (openFileDialog = new System.Windows.Forms.OpenFileDialog())
                {
                    openFileDialog.SupportMultiDottedExtensions = true;
                    openFileDialog.Title = "Select a SoundFont v2.0 file";
                    openFileDialog.Filter = "SoundFont v2.0 File(*.sf2)|*.sf2";

                    var fr = openFileDialog.ShowDialog(null);
                    if (fr != DialogResult.OK)
                        return;

                    loadPcm(openFileDialog.FileName, offset, true);
                }
                for (int i = 0; i < 128; i++)
                {
                    var tim = (RF5C164Timbre)Timbres[i + 128];

                    DrumTimbres[i].TimbreNumber = (ProgramAssignmentNumber)(i + 128);
                    DrumTimbres[i].BaseNote =
                        (NoteNames)(byte)Math.Round(MidiManager.CalcNoteNumberFromFrequency(tim.BaseFreqency));
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        private void loadPcm(string fileName, int offset, bool drum)
        {
            var sf2 = new SF2(fileName);

            var spl = sf2.SoundChunk.SMPLSubChunk.Samples;
            int tn = 0;
            int num = 0;
            foreach (var s in sf2.HydraChunk.SHDRSubChunk.Samples)
            {
                if (s.SampleType == SF2SampleLink.MonoSample ||
                    s.SampleType == SF2SampleLink.LeftSample)
                {
                    var tim = new RF5C164Timbre();

                    double baseFreq = 440.0 * Math.Pow(2.0, (((double)s.OriginalKey - 69.0) / 12.0) + (double)(s.PitchCorrection / 100));
                    tim.BaseFreqency = baseFreq;
                    tim.SampleRate = s.SampleRate;

                    uint start = s.Start;
                    uint end = s.End;
                    if (s.LoopEnd < end && s.LoopStart < s.LoopEnd)
                        end = s.LoopEnd;

                    uint len = end - start + 1;
                    if (len > 65535)
                        len = 65535;
                    uint loopP = s.LoopStart - s.Start;
                    if (loopP > 65535)
                        loopP = 65535;

                    {
                        byte[] samples = new byte[len];
                        for (uint i = 0; i < len; i++)
                            samples[i] = (byte)((spl[start + i] >> 8) + 127);
                        tim.PcmData = samples;

                        tim.LoopPoint = (ushort)loopP;
                        if (s.LoopStart == s.LoopEnd)
                        {
                            tim.LoopPoint = (ushort)(len - 1);
                        }
                    }

                    if (drum)
                    {
                        DrumTimbres[tn].TimbreNumber = (ProgramAssignmentNumber)(tn + offset);
                        DrumTimbres[tn].BaseNote =
                            (NoteNames)(byte)Math.Round(MidiManager.CalcNoteNumberFromFrequency(tim.BaseFreqency));
                    }

                    Timbres[tn + offset] = tim;
                    num++;

                    var nidx = s.SampleName.IndexOf('\0');
                    if (nidx >= 0)
                        tim.TimbreName = s.SampleName.Substring(0, nidx);
                    else
                        tim.TimbreName = s.SampleName;

                    tn++;
                    if (tn == 128)
                        break;
                }
            }
            updatePcmData(null);
            MessageBox.Show(string.Format(Resources.TimbreLoaded, num));
        }

        private class EnumConverterSoundEngineTypeRF5C164 : EnumConverter<SoundEngineType>
        {
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var sc = new StandardValuesCollection(new SoundEngineType[] {
                    SoundEngineType.Software,
                    SoundEngineType.VSIF_Genesis_FTDI,
                });

                return sc;
            }
        }

        #endregion
    }

}
