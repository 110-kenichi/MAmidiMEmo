// copyright-holders:K.Ito
using FastDelegate.Net;
using FM_SoundConvertor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Instruments.Envelopes;
using zanac.MAmidiMEmo.Instruments.Vst;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.Properties;
using System.Threading.Tasks;

namespace zanac.MAmidiMEmo.Instruments
{
    [InstrumentPropertyTab]
    [JsonConverter(typeof(NoTypeConverterJsonConverter<InstrumentBase>))]
    [TypeConverter(typeof(CustomExpandableObjectConverter))]
    [InstLock]
    [DataContract]
    public abstract class InstrumentBase : ContextBoundObject, IDisposable, ISerializeDataSaveLoad
    {
        public static readonly object VstPluginContextLockObject = new object();

        /// <summary>
        /// 
        /// </summary>
        [Category("General")]
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        [Category("General")]
        public abstract string Group
        {
            get;
        }

        private float f_GainLeft = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Gain Left ch. (0.0-*) of this Instrument.\r\n" +
            "Affected only for Software Engine.")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0d, 10d, 0.1d)]
        public virtual float GainLeft
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

                    DeferredWriteData(SetOutputGain, UnitNumber, SoundInterfaceTagNamePrefix, 0, value);
                }
            }
        }

        public virtual bool ShouldSerializeGainLeft()
        {
            return GainLeft != 1.0f;
        }

        public virtual void ResetGainLeft()
        {
            GainLeft = 1.0f;
        }

        private float f_GainRight = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Gain Right ch. (0.0-*) of this Instrument.\r\n" +
            "Affected only for Software Engine.")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0d, 10d, 0.1d)]
        public virtual float GainRight
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

                    DeferredWriteData(SetOutputGain, UnitNumber, SoundInterfaceTagNamePrefix, 1, value);
                }
            }
        }

        public virtual bool ShouldSerializeGainRight()
        {
            return GainRight != 1.0f;
        }

        public virtual void ResetGainRight()
        {
            GainRight = 1.0f;
        }


        private static float f_MasterGain = 1.0f;

        public static event EventHandler<PropertyChangingEventArgs> StaticPropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public static float MasterGain
        {
            get
            {
                return f_MasterGain;
            }
            set
            {
                if (f_MasterGain != value)
                {
                    f_MasterGain = value;

                    DeferredWriteData(SetOutputGain, uint.MaxValue, "lspeaker", 0, f_MasterGain);
                    DeferredWriteData(SetOutputGain, uint.MaxValue, "rspeaker", 0, f_MasterGain);

                    StaticPropertyChanged?.Invoke(typeof(InstrumentBase), new PropertyChangingEventArgs(nameof(MasterGain)));
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [DefaultValue(FollowerUnit.None)]
        [Description("Enable Follower mode. Share free voice channels with specified leader unit and does not accept any Note On MIDI event.\r\n" +
            "Be sure to set same settings with the leader unit.")]
        public virtual FollowerUnit FollowerMode
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Global Dynamic Arpeggio Settings. This can be overrided by a Timbre settins.")]
        public virtual ArpSettings GlobalARP
        {
            get;
            set;
        }

        public virtual bool ShouldSerializeGlobalARP()
        {
            return !string.Equals(GlobalARP.SerializeData, "{}", StringComparison.Ordinal);
        }

        public virtual void ResetGlobalARP()
        {
            GlobalARP.SerializeData = "{}";
        }


        private double f_ProcessingInterval = 1;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        [Description("Set LFO and Portament processing interval[ms]. If you use a real hardware, please increase to appropriate value to prevent performance hit.")]
        [DefaultValue(1d)]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(1d, 50d, 1d)]
        public virtual double ProcessingInterval
        {
            get
            {
                return f_ProcessingInterval;
            }
            set
            {
                if (value >= 1)
                    f_ProcessingInterval = value;
            }
        }

        private FilterMode f_FilterMode = FilterMode.LowPass;

        [DataMember]
        [Category("Filter")]
        [Description("Audio Filter Type")]
        public virtual FilterMode FilterMode
        {
            get => f_FilterMode;
            set
            {
                if (f_FilterMode != value)
                {
                    f_FilterMode = value;

                    DeferredWriteData(set_filter, UnitNumber, SoundInterfaceTagNamePrefix, f_FilterMode, FilterCutoff, FilterResonance);
                }
            }
        }

        public virtual bool ShouldSerializeFilterMode()
        {
            return FilterMode != FilterMode.LowPass;
        }

        public virtual void ResetFilterMode()
        {
            FilterMode = FilterMode.LowPass;
        }

        private double f_FilterCutOff = 0.99d;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("Filter")]
        [Description("Audio Cutoff Filter (0.1-0.99) of this Instrument.\r\n" +
            "Affected only for Software Engine.")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0.01d, 0.99d, 0.01d)]
        [DefaultValue(0.99d)]
        public virtual double FilterCutoff
        {
            get
            {
                return f_FilterCutOff;
            }
            set
            {
                double v = value;
                if (v < 0.01d)
                    v = 0.01d;
                else if (v > 0.99d)
                    v = 0.99d;
                if (f_FilterCutOff != v)
                {
                    f_FilterCutOff = v;

                    DeferredWriteData(set_filter, UnitNumber, SoundInterfaceTagNamePrefix, FilterMode, f_FilterCutOff, FilterResonance);
                }
            }
        }


        private double f_FilterResonance = 0.01d;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("Filter")]
        [Description("Audio Cutoff Filter (0.01-1.00) of this Instrument.\r\n" +
            "Affected only for Software Engine.")]
        [EditorAttribute(typeof(DoubleSlideEditor), typeof(UITypeEditor))]
        [DoubleSlideParameters(0.01d, 1.00d, 0.01d)]
        [DefaultValue(0.01d)]
        public virtual double FilterResonance
        {
            get
            {
                return f_FilterResonance;
            }
            set
            {
                double v = value;
                if (v < 0.00d)
                    v = 0.00d;
                else if (v > 1.00d)
                    v = 1.00d;
                if (f_FilterResonance != v)
                {
                    f_FilterResonance = v;

                    DeferredWriteData(set_filter, UnitNumber, SoundInterfaceTagNamePrefix, FilterMode, FilterCutoff, f_FilterResonance);
                }
            }
        }


        [DataMember]
        [Description("Graphic Equalizer Settings.\r\n" +
            "This EQ is a peaking filter and its bandwidth is one octave wide.\r\n" +
            "Affected only for Software Engine.")]
        [DisplayName("Graphic Equalizer Settings[GEQ]")]
        [Category("Filter")]
        public virtual GraphicEqualizerSettings GEQ
        {
            get;
            set;
        }

        public virtual bool ShouldSerializeGEQ()
        {
            return !string.Equals(GEQ.SerializeData, "{}", StringComparison.Ordinal);
        }

        public virtual void ResetGEQ()
        {
            GEQ.SerializeData = "{}";
        }

        private VSTPluginCollection f_VSTPlugins;

        [DataMember]
        [Category("Filter")]
        [Description("Set VST effect plugins. Effects are applied in order from the first VST to the last VST.\r\n" +
            "Affected only for Software Engine.")]
        public virtual VSTPluginCollection VSTPlugins
        {
            get
            {
                if (Program.IsVSTiMode())
                    return null;
                return f_VSTPlugins;
            }
            set
            {
                if (!Program.IsVSTiMode())
                    f_VSTPlugins = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Description("Memo")]
        [DefaultValue(null)]
        [Editor(typeof(EnvelopeUITypeEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Memo
        {
            get;
            set;
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
        [Editor(typeof(FormTextUITypeEditor), typeof(UITypeEditor)), Localizable(false)]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [IgnoreDataMember]
        [JsonIgnore]
        [Description("You can copy and paste this text data to other same type Instrument.\r\nNote: Open dropdown editor then copy all text and paste to dropdown editor. Do not copy and paste one liner text.")]
        public string SerializeData
        {
            get
            {
                //return JsonHelper.SerializeToMinimalJson(this);   NG: cant reset child member value
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
            set
            {
                JObject jo = JObject.Parse(value);
                jo["UnitNumber"] = UnitNumber;

                RestoreFrom(jo.ToString());
                if (!Program.IsVSTiMode())
                    serializeVstFx();
                set_device_enable(UnitNumber, SoundInterfaceTagNamePrefix, 1);
            }
        }

        private void serializeVstFx()
        {
            SetVstFxCallback(UnitNumber, SoundInterfaceTagNamePrefix, f_vst_fx_callback);
        }

        public abstract void RestoreFrom(string serializeData);

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public abstract string ImageKey
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public abstract InstrumentType InstrumentType
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        protected abstract string SoundInterfaceTagNamePrefix
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("General")]
        public abstract uint DeviceID
        {
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI")]
        public uint UnitNumber
        {
            get;
            private set;
        }


        [IgnoreDataMember]
        [JsonIgnore]
        [Category(" Timbres")]
        [DisplayName("(Timbre Manager...)")]
        [Description("Opens Timbre Manager.")]
        [TypeConverter(typeof(OpenEditorTypeConverter))]
        [EditorAttribute(typeof(TimbresArrayUITypeEditor), typeof(UITypeEditor))]
        public abstract TimbreBase[] BaseTimbres
        {
            get;
            set;
        }

        public virtual bool ShouldSerializeBaseTimbres()
        {
            return false;
        }

        private TimbreBase[] lastNoteOnTimbres = new TimbreBase[16];

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TimbreBase GetLastNoteOnTimbre(int channel)
        {
            if (lastNoteOnTimbres[channel] != null)
                return lastNoteOnTimbres[channel];

            int pn = (int)ProgramAssignments[ProgramNumbers[channel]];

            if ((pn & 0xffff0000) != 0)
            {
                int ptidx = pn & 0xffff;
                if (ptidx >= CombinedTimbres.Length)
                    ptidx = CombinedTimbres.Length - 1;
                var pts = CombinedTimbres[ptidx];
                return pts;
            }

            int btidx = pn & 0xffff;
            if (btidx >= BaseTimbres.Length)
                btidx = BaseTimbres.Length - 1;
            return BaseTimbres[btidx];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TimbreBase GetLastTimbre(TimbreBase timbre)
        {
            if (timbre is CombinedTimbre)
            {
                var pts = (CombinedTimbre)timbre;
                if (pts.Timbres.Count != 0) //if Timbre assigned
                    return pts.Timbres[pts.Timbres.Count - 1].TimberObject;
                else
                    return null;
            }

            return timbre;
        }

        public TimbreBase[] GetBaseTimbres(TimbreBase timbre)
        {
            List<TimbreBase> ts = new List<TimbreBase>();

            if (timbre is CombinedTimbre)
            {
                var pts = (CombinedTimbre)timbre;
                foreach (var pt in pts.Timbres)
                    ts.Add(pt.TimberObject);
                return ts.ToArray();
            }
            return new TimbreBase[] { timbre };
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CombinedTimbreSettings TryGetBaseTimbreSettings(TaggedNoteOnEvent ev, TimbreBase timbre, int baseTimbreIndex)
        {
            if (ev.CombinedTimbreSettings != null && 0 <= baseTimbreIndex && baseTimbreIndex < ev.CombinedTimbreSettings.Length)
                return ev.CombinedTimbreSettings[baseTimbreIndex];
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TimbreBase GetTimbre(TaggedNoteOnEvent ev)
        {
            var tb = ev.Tag as NoteOnTimbreInfo;
            if (tb != null)
            {
                CombinedTimbre ctb = tb.Timbre as CombinedTimbre;
                if (ctb != null)
                    return ctb;
                else
                    return tb.Timbre;
            }

            switch (ChannelTypes[ev.Channel])
            {
                case ChannelType.Normal:
                    {
                        int pn = (int)ProgramAssignments[ProgramNumbers[ev.Channel]];
                        if ((pn & 0xffff0000) != 0)
                        {
                            int ptidx = pn & 0xffff;
                            if (ptidx >= CombinedTimbres.Length)
                                ptidx = CombinedTimbres.Length - 1;
                            return CombinedTimbres[ptidx];
                        }

                        int btidx = pn & 0xffff;
                        if (btidx >= BaseTimbres.Length)
                            btidx = BaseTimbres.Length - 1;
                        return BaseTimbres[btidx];
                    }
                case ChannelType.Drum:
                case ChannelType.DrumGt:
                    {
                        var dt = DrumTimbres[ev.NoteNumber];
                        if (dt != null && dt.TimbreNumber != null)
                        {
                            int pn = (int)dt.TimbreNumber;
                            if ((pn & 0xffff0000) != 0)
                            {
                                int ptidx = pn & 0xffff;
                                if (ptidx >= CombinedTimbres.Length)
                                    ptidx = CombinedTimbres.Length - 1;
                                return CombinedTimbres[ptidx];
                            }

                            int btidx = pn & 0xffff;
                            if (btidx >= BaseTimbres.Length)
                                btidx = BaseTimbres.Length - 1;
                            return BaseTimbres[btidx];
                        }
                        break;
                    }
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TimbreBase GetTimbre(NoteOnEvent ev)
        {
            switch (ChannelTypes[ev.Channel])
            {
                case ChannelType.Normal:
                    {
                        int pn = (int)ProgramAssignments[ProgramNumbers[ev.Channel]];
                        if ((pn & 0xffff0000) != 0)
                        {
                            int ptidx = pn & 0xffff;
                            if (ptidx >= CombinedTimbres.Length)
                                ptidx = CombinedTimbres.Length - 1;
                            return CombinedTimbres[ptidx];
                        }

                        int btidx = pn & 0xffff;
                        if (btidx >= BaseTimbres.Length)
                            btidx = BaseTimbres.Length - 1;
                        return BaseTimbres[btidx];
                    }
                case ChannelType.Drum:
                case ChannelType.DrumGt:
                    {
                        var dt = DrumTimbres[ev.NoteNumber];
                        if (dt != null && dt.TimbreNumber != null)
                        {
                            int pn = (int)dt.TimbreNumber;
                            if ((pn & 0xffff0000) != 0)
                            {
                                int ptidx = pn & 0xffff;
                                if (ptidx >= CombinedTimbres.Length)
                                    ptidx = CombinedTimbres.Length - 1;
                                return CombinedTimbres[ptidx];
                            }

                            int btidx = pn & 0xffff;
                            if (btidx >= BaseTimbres.Length)
                                btidx = BaseTimbres.Length - 1;
                            return BaseTimbres[btidx];
                        }
                        break;
                    }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TimbreBase[] GetBaseTimbres(TaggedNoteOnEvent ev)
        {
            List<TimbreBase> ts = new List<TimbreBase>();
            List<CombinedTimbreSettings> cts = new List<CombinedTimbreSettings>();
            List<int> tis = new List<int>();

            var tb = ev.Tag as NoteOnTimbreInfo;
            if (tb != null)
            {
                CombinedTimbre ctb = tb.Timbre as CombinedTimbre;
                if (ctb != null)
                {
                    foreach (var tn in ctb.Timbres)
                    {
                        if ((int)tn.TimbreNumber < BaseTimbres.Length)
                        {
                            if ((int)tn.KeyRangeLow <= ev.NoteNumber && ev.NoteNumber <= (int)tn.KeyRangeHigh &&
                                tn.VelocityRangeLow <= ev.Velocity && ev.Velocity <= tn.VelocityRangeHigh)
                            {
                                ts.Add(BaseTimbres[(int)tn.TimbreNumber]);
                                tis.Add((int)tn.TimbreNumber);
                                cts.Add(tn);
                            }
                        }
                    }
                }
                else
                {
                    ts.Add(tb.Timbre);
                    tis.Add(tb.TimbreNo);
                    cts.Add(null);
                }
                ev.BaseTimbreIndexes = tis.ToArray();
                ev.BaseTimbres = ts.ToArray();
                ev.CombinedTimbreSettings = cts.ToArray();
                return ev.BaseTimbres;
            }

            switch (ChannelTypes[ev.Channel])
            {
                case ChannelType.Normal:
                    {
                        int pn = (int)ProgramAssignments[ProgramNumbers[ev.Channel]];
                        if ((pn & 0xffff0000) != 0)
                        {
                            int ptidx = pn & 0xffff;
                            if (ptidx >= CombinedTimbres.Length)
                                ptidx = CombinedTimbres.Length - 1;
                            foreach (var tn in CombinedTimbres[ptidx].Timbres)
                            {
                                if ((int)tn.TimbreNumber < BaseTimbres.Length)
                                {
                                    if ((int)tn.KeyRangeLow <= ev.NoteNumber && ev.NoteNumber <= (int)tn.KeyRangeHigh &&
                                        tn.VelocityRangeLow <= ev.Velocity && ev.Velocity <= tn.VelocityRangeHigh)
                                    {
                                        ts.Add(BaseTimbres[(int)tn.TimbreNumber]);
                                        tis.Add((int)tn.TimbreNumber);
                                        cts.Add(tn);
                                    }
                                }
                            }
                            ev.BaseTimbreIndexes = tis.ToArray();
                            ev.BaseTimbres = ts.ToArray();
                            ev.CombinedTimbreSettings = cts.ToArray();
                            return ev.BaseTimbres;
                        }

                        int btidx = pn & 0xffff;
                        if (btidx >= BaseTimbres.Length)
                            btidx = BaseTimbres.Length - 1;
                        ts.Add(BaseTimbres[btidx]);
                        tis.Add(btidx);
                        cts.Add(null);
                        break;
                    }
                case ChannelType.Drum:
                case ChannelType.DrumGt:
                    {
                        var dt = DrumTimbres[ev.NoteNumber];
                        if (dt != null && dt.TimbreNumber != null)
                        {
                            int pn = (int)dt.TimbreNumber;
                            if ((pn & 0xffff0000) != 0)
                            {
                                int ptidx = pn & 0xffff;
                                if (ptidx >= CombinedTimbres.Length)
                                    ptidx = CombinedTimbres.Length - 1;
                                foreach (var tn in CombinedTimbres[ptidx].Timbres)
                                {
                                    if ((int)tn.TimbreNumber < BaseTimbres.Length)
                                    {
                                        if ((int)tn.KeyRangeLow <= ev.NoteNumber && ev.NoteNumber <= (int)tn.KeyRangeHigh &&
                                            tn.VelocityRangeLow <= ev.Velocity && ev.Velocity <= tn.VelocityRangeHigh)
                                        {
                                            ts.Add(BaseTimbres[(int)tn.TimbreNumber]);
                                            tis.Add((int)tn.TimbreNumber);
                                            cts.Add(tn);
                                        }
                                    }
                                }
                                ev.BaseTimbreIndexes = tis.ToArray();
                                ev.BaseTimbres = ts.ToArray();
                                ev.CombinedTimbreSettings = cts.ToArray();
                                return ev.BaseTimbres;
                            }

                            int btidx = pn & 0xffff;
                            if (btidx >= BaseTimbres.Length)
                                btidx = BaseTimbres.Length - 1;
                            ts.Add(BaseTimbres[btidx]);
                            tis.Add(btidx);
                            cts.Add(null);
                        }
                        break;
                    }
            }

            ev.BaseTimbreIndexes = tis.ToArray();
            ev.BaseTimbres = ts.ToArray();
            ev.CombinedTimbreSettings = cts.ToArray();
            return ev.BaseTimbres;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual int[] GetBaseTimbreIndexes(TaggedNoteOnEvent ev)
        {
            return ev.BaseTimbreIndexes;
        }

        private DrumTimbre[] f_DrumTimbres;

        [DataMember]
        [Category(" Timbres")]
        [Description("Drum ch(usually 10ch) Timbres table")]
        [Editor(typeof(DrumTableUITypeEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableDrumCollectionConverter))]
        public virtual DrumTimbre[] DrumTimbres
        {
            get
            {
                return f_DrumTimbres;
            }
            set
            {
                f_DrumTimbres = value;
                for (int i = 0; i < f_DrumTimbres.Length; i++)
                {
                    f_DrumTimbres[i].NoteNumber = i;
                    f_DrumTimbres[i].KeyName = Midi.MidiManager.GetNoteName((SevenBitNumber)i);
                    f_DrumTimbres[i].Instrument = this;
                }
            }
        }

        public virtual bool ShouldSerializeDrumTimbres()
        {
            foreach (var op in DrumTimbres)
            {
                if (!string.Equals(JsonConvert.SerializeObject(op, Formatting.Indented), "{}"))
                    return true;
            }
            return false;
        }

        public void ResetDrumTimbres()
        {
            for (int i = 0; i < DrumTimbres.Length; i++)
                DrumTimbres[i] = new DrumTimbre(i, null);
        }

        private CombinedTimbre[] f_CombinedTimbres;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category(" Timbres")]
        [Description("Combine multiple Timbres (0-255)\r\n" +
                    "Override PatchTimbres to Timbres when you set binding patch numbers.")]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public virtual CombinedTimbre[] CombinedTimbres
        {
            get
            {
                return f_CombinedTimbres;
            }
            set
            {
                f_CombinedTimbres = value;
            }
        }

        public virtual bool ShouldSerializeCombinedTimbres()
        {
            foreach (var op in CombinedTimbres)
            {
                if (!string.Equals(JsonConvert.SerializeObject(op, Formatting.Indented), "{}"))
                    return true;
            }
            return false;
        }

        public void ResetCombinedTimbres()
        {
            for (int i = 0; i < CombinedTimbres.Length; i++)
                CombinedTimbres[i] = new CombinedTimbre();
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Receving MIDI Port")]
        [DefaultValue(MidiPort.PortAB)]
        public virtual MidiPort MidiPort
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Channel type <MIDI 16ch>")]
        [TypeConverter(typeof(ExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [CollectionDefaultValue(true)]
        public virtual ChannelType[] ChannelTypes
        {
            get;
            set;
        }

        public bool ShouldSerializeChannelTypes()
        {
            for (int i = 0; i < ChannelTypes.Length; i++)
            {
                if (i == 9)
                {
                    if (ChannelTypes[i] != ChannelType.Drum)
                        return true;
                }
                else
                {
                    if (ChannelTypes[i] != ChannelType.Normal)
                        return true;
                }
            }
            return false;
        }

        public void ResetChannelTypes()
        {
            for (int i = 0; i < ChannelTypes.Length; i++)
                ChannelTypes[i] = ChannelType.Normal;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Receving MIDI ch <MIDI 16ch>")]
        [TypeConverter(typeof(ExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [CollectionDefaultValue(true)]
        public virtual bool[] Channels
        {
            get;
            set;
        }

        public bool ShouldSerializeChannels()
        {
            foreach (var dt in Channels)
            {
                if (dt != true)
                    return true;
            }
            return false;
        }

        public void ResetChannels()
        {
            for (int i = 0; i < Channels.Length; i++)
                Channels[i] = true;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI")]
        [Description("Pitch (0 - 8192 - 16383) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(16383)]
        [CollectionDefaultValue((ushort)8192)]
        public virtual ushort[] Pitchs
        {
            get;
            set;
        }

        public bool ShouldSerializePitchs()
        {
            foreach (var dt in Pitchs)
            {
                if (dt != 8192)
                    return true;
            }
            return false;
        }

        public void ResetPitchs()
        {
            for (int i = 0; i < Pitchs.Length; i++)
                Pitchs[i] = 8192;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Pitch bend sensitivity [half note] <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)2)]
        public virtual byte[] PitchBendRanges
        {
            get;
            set;
        }

        public bool ShouldSerializePitchBendRanges()
        {
            foreach (var dt in PitchBendRanges)
            {
                if (dt != 2)
                    return true;
            }
            return false;
        }

        public void ResetPitchBendRanges()
        {
            for (int i = 0; i < PitchBendRanges.Length; i++)
                PitchBendRanges[i] = 2;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Select an algorithm to search for available slots. <MIDI 16ch>")]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableCollectionConverter))]
        public virtual SlotAssignmentType[] SlotAssignAlgorithm
        {
            get;
            set;
        }

        public virtual bool ShouldSerializeSlotAssignAlgorithm()
        {
            for (int i = 0; i < SlotAssignAlgorithm.Length; i++)
            {
                if (SlotAssignAlgorithm[i] != SlotAssignmentType.MostUnusedSlot)
                    return true;
            }
            return false;
        }

        public virtual void ResetProgramSlotAssignAlgorithm()
        {
            for (int i = 0; i < SlotAssignAlgorithm.Length; i++)
                SlotAssignAlgorithm[i] = (SlotAssignmentType)i;
        }


        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Scale Tuning -100～0～100 [cent] <MIDI 16ch>\r\n" +
            "Input scale value of each notes (C C# ... A# B) and split it with space like the FamiTracker.")]
        [TypeConverter(typeof(ExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [CollectionDefaultValue("0 0 0 0 0 0 0 0 0 0 0 0")]
        public virtual ScaleTuning[] ScaleTunings
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI")]
        [Description("FineTune (0 - 8192 - 16383) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(16383)]
        [CollectionDefaultValue((ushort)8192)]
        public virtual ushort[] FineTunes
        {
            get;
            set;
        }

        public bool ShouldSerializeFineTunes()
        {
            foreach (var dt in FineTunes)
            {
                if (dt != 8192)
                    return true;
            }
            return false;
        }

        public void ResetFineTunes()
        {
            for (int i = 0; i < FineTunes.Length; i++)
                FineTunes[i] = 8192;
        }

        [JsonConverter(typeof(NoTypeConverterJsonConverter<ScaleTuning>))]
        [TypeConverter(typeof(CustomExpandableObjectConverter))]
        [InstLock]
        [DataContract]
        public class ScaleTuning : ContextBoundObject
        {
            private string f_Scales = "0 0 0 0 0 0 0 0 0 0 0 0";

            [Description("Scale Tuning -100～0～100 [cent]\r\n" +
                "Input scale value of each notes (C C# ... A# B) and split it with space like the FamiTracker.")]
            [DataMember]
            [DefaultValue("0 0 0 0 0 0 0 0 0 0 0 0")]
            public string Scales
            {
                get
                {
                    return f_Scales;
                }
                set
                {
                    if (f_Scales != value)
                    {
                        string[] vals = value.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (value == null || vals.Length != 12)
                        {
                            ScalesNums = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                            f_Scales = "0 0 0 0 0 0 0 0 0 0 0 0";
                            return;
                        }
                        f_Scales = value;
                        List<int> vs = new List<int>();
                        for (int i = 0; i < vals.Length; i++)
                        {
                            string val = vals[i];
                            int v;
                            if (int.TryParse(val, out v))
                            {
                                if (v < -100)
                                    v = -100;
                                else if (v > 100)
                                    v = 100;
                                vs.Add(v);
                            }
                        }
                        ScalesNums = vs.ToArray();

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < ScalesNums.Length; i++)
                        {
                            if (sb.Length != 0)
                                sb.Append(' ');
                            sb.Append(ScalesNums[i].ToString((IFormatProvider)null));
                        }
                        f_Scales = sb.ToString();
                    }
                }
            }

            [Browsable(false)]
            [JsonIgnore]
            [IgnoreDataMember]
            public int[] ScalesNums { get; set; } = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            /// <summary>
            /// 
            /// </summary>
            public ScaleTuning()
            {
            }
        }

        public bool ShouldSerializeScaleTunings()
        {
            for (int i = 0; i < ScaleTunings.Length; i++)
            {
                if (ScaleTunings[i].Scales != "0 0 0 0 0 0 0 0 0 0 0 0")
                    return true;
            }
            return false;
        }

        public void ResetScaleTunings()
        {
            for (int i = 0; i < ScaleTunings.Length; i++)
                ScaleTunings[i].Scales = "0 0 0 0 0 0 0 0 0 0 0 0";
        }


        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("General Purpose Control Settings <MIDI 16ch>\r\n" +
            "Link Data Entry message value with the Instrument property value (Only the property that has a slider editor)\r\n" +
            "eg 1) \"GainLeft,GainRight\" ... You can change Gain property values dynamically via MIDI Control Change No.16-19,80-83 message.\r\n" +
            "eg 2) \"Timbres[0].ALG\" ... You can change Timbre 0 FM synth algorithm values dynamically via MIDI Control Change No.16-19,80-83 message.")]
        [DisplayName("General Purpose Control Settings[GPCS]")]
        [TypeConverter(typeof(ExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        public GeneralPurposeControlSettings[] GPCS
        {
            get;
            set;
        }

        public bool ShouldSerializeGPCS()
        {
            foreach (var dt in GPCS)
            {
                if (dt.GeneralPurposeControl1 != null ||
                    dt.GeneralPurposeControl2 != null ||
                    dt.GeneralPurposeControl3 != null ||
                    dt.GeneralPurposeControl4 != null ||
                    dt.GeneralPurposeControl5 != null ||
                    dt.GeneralPurposeControl6 != null ||
                    dt.GeneralPurposeControl7 != null ||
                    dt.GeneralPurposeControl8 != null
                    )
                    return true;
            }
            return false;
        }

        public void ResetGPCS()
        {
            for (int i = 0; i < GPCS.Length; i++)
            {
                GPCS[i].GeneralPurposeControl1 = null;
                GPCS[i].GeneralPurposeControl2 = null;
                GPCS[i].GeneralPurposeControl3 = null;
                GPCS[i].GeneralPurposeControl4 = null;
                GPCS[i].GeneralPurposeControl5 = null;
                GPCS[i].GeneralPurposeControl6 = null;
                GPCS[i].GeneralPurposeControl7 = null;
                GPCS[i].GeneralPurposeControl8 = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        [Category("MIDI(Dedicated)")]
        [Description("Assign the Timbre/CombinedTimbre to program number.")]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(ExpandableProgramAssignmentNumberCollectionConverter))]
        public virtual ProgramAssignmentNumber[] ProgramAssignments
        {
            get;
            set;
        }

        public virtual bool ShouldSerializeProgramAssignments()
        {
            for (int i = 0; i < ProgramAssignments.Length; i++)
            {
                if ((int)ProgramAssignments[i] != i)
                    return true;
            }
            return false;
        }

        public virtual void ResetProgramAssignments()
        {
            for (int i = 0; i < ProgramAssignments.Length; i++)
                ProgramAssignments[i] = (ProgramAssignmentNumber)i;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Program number (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChProgramNumbersCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] ProgramNumbers
        {
            get;
            set;
        }

        public bool ShouldSerializeProgramNumbers()
        {
            foreach (var dt in ProgramNumbers)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetProgramNumbers()
        {
            for (int i = 0; i < ProgramNumbers.Length; i++)
                ProgramNumbers[i] = 0;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Volume (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)127)]
        public virtual byte[] Volumes
        {
            get;
            set;
        }

        public bool ShouldSerializeVolumes()
        {
            foreach (var dt in Volumes)
            {
                if (dt != 127)
                    return true;
            }
            return false;
        }

        public void ResetVolumes()
        {
            for (int i = 0; i < Volumes.Length; i++)
                Volumes[i] = 127;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Volume (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)127)]
        public virtual byte[] Expressions
        {
            get;
            set;
        }


        public bool ShouldSerializeExpressions()
        {
            foreach (var dt in Expressions)
            {
                if (dt != 127)
                    return true;
            }
            return false;
        }

        public void ResetExpressions()
        {
            for (int i = 0; i < Expressions.Length; i++)
                Expressions[i] = 127;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Panpot ((L)0-63(C)64-127(R)) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)64)]
        public virtual byte[] Panpots
        {
            get;
            set;
        }

        public bool ShouldSerializePanpots()
        {
            foreach (var dt in Panpots)
            {
                if (dt != 64)
                    return true;
            }
            return false;
        }

        public void ResetPanpots()
        {
            for (int i = 0; i < Panpots.Length; i++)
                Panpots[i] = 64;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Modulation (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] Modulations
        {
            get;
            set;
        }

        public bool ShouldSerializeModulations()
        {
            foreach (var dt in Modulations)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetModulations()
        {
            for (int i = 0; i < Modulations.Length; i++)
                Modulations[i] = 0;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Modulation Rate (0-64-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)64)]
        public virtual byte[] ModulationRates
        {
            get;
            set;
        }


        public bool ShouldSerializeModulationRates()
        {
            foreach (var dt in ModulationRates)
            {
                if (dt != 64)
                    return true;
            }
            return false;
        }

        public void ResetModulationRates()
        {
            for (int i = 0; i < ModulationRates.Length; i++)
                ModulationRates[i] = 64;
        }

        /// <summary>
        /// Hz
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public virtual double GetModulationRateHz(int channel, int offset)
        {
            int val = ModulationRates[channel] + offset;
            if (val < 0)
                val = 0;
            else if (val > 127)
                val = 127;

            double rate = Math.Pow(((double)val / 64d), 2) * 6;

            return rate;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Modulation Depth (0-64-127) <MIDI 16ch>\r\nValues above 65 change the effectiveness.~ ")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)64)]
        public virtual byte[] ModulationDepthes
        {
            get;
            set;
        }

        public bool ShouldSerializeModulationDepthes()
        {
            foreach (var dt in ModulationDepthes)
            {
                if (dt != 64)
                    return true;
            }
            return false;
        }

        public void ResetModulationDepthes()
        {
            for (int i = 0; i < ModulationDepthes.Length; i++)
                ModulationDepthes[i] = 64;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Modulation Delay (0-64-127) <MIDI 16ch>\r\nTypical values are around 80.")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)64)]
        public virtual byte[] ModulationDelays
        {
            get;
            set;
        }

        public bool ShouldSerializeModulationDelays()
        {
            foreach (var dt in ModulationDelays)
            {
                if (dt != 64)
                    return true;
            }
            return false;
        }

        public void ResetModulationDelays()
        {
            for (int i = 0; i < ModulationDelays.Length; i++)
                ModulationDelays[i] = 64;
        }

        /// <summary>
        /// Hz
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public virtual double GetModulationDelaySec(int channel, int offset)
        {
            int val = ModulationDelays[channel] + offset;
            if (val < 0)
                val = 0;
            else if (val > 127)
                val = 127;

            double rate = Math.Pow(((double)val / 64d), 3.25) - 1;
            if (rate < 0)
                rate = 0;

            return rate;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Modulation Depth Range[Half Note] (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] ModulationDepthRangesNote
        {
            get;
            set;
        }

        public bool ShouldSerializeModulationDepthRangesNote()
        {
            foreach (var dt in ModulationDepthRangesNote)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetModulationDepthRangesNote()
        {
            for (int i = 0; i < ModulationDepthRangesNote.Length; i++)
                ModulationDepthRangesNote[i] = 64;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Modulation Depth Range[Cent] (0-64-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)64)]
        public virtual byte[] ModulationDepthRangesCent
        {
            get;
            set;
        }


        public bool ShouldSerializeModulationDepthRangesCent()
        {
            foreach (var dt in ModulationDepthRangesCent)
            {
                if (dt != 0x40)
                    return true;
            }
            return false;
        }

        public void ResetModulationDepthRangesCent()
        {
            for (int i = 0; i < ModulationDepthRangesCent.Length; i++)
                ModulationDepthRangesCent[i] = 64;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Holds (0:Off 64:On) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] Holds
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public virtual byte[] LastHolds
        {
            get;
            set;
        }

        public bool ShouldSerializeHolds()
        {
            foreach (var dt in Holds)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetHolds()
        {
            for (int i = 0; i < Holds.Length; i++)
                Holds[i] = 0;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Portamento (0:Off 64:On) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] Portamentos
        {
            get;
            set;
        }


        public bool ShouldSerializePortamentos()
        {
            foreach (var dt in Portamentos)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetPortamentos()
        {
            for (int i = 0; i < Portamentos.Length; i++)
                Portamentos[i] = 0;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Portamento Time (0-127) <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] PortamentoTimes
        {
            get;
            set;
        }


        public bool ShouldSerializePortamentoTimes()
        {
            foreach (var dt in PortamentoTimes)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetPortamentoTimes()
        {
            for (int i = 0; i < PortamentoTimes.Length; i++)
                PortamentoTimes[i] = 0;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Legato Foot Switch for Mono mode (0-127) 0:Disable 64:Enable <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] LegatoFootSwitch
        {
            get;
            set;
        }

        public bool ShouldSerializeLegatoFootSwitch()
        {
            foreach (var dt in LegatoFootSwitch)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetLegatoFootSwitch()
        {
            for (int i = 0; i < LegatoFootSwitch.Length; i++)
                LegatoFootSwitch[i] = 0;
        }

        [DataMember]
        [Category("MIDI")]
        [Description("Mono mode (0-127) 0:Disable mono mode N:Number of max voices <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [EditorAttribute(typeof(DummyEditor), typeof(UITypeEditor))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] MonoMode
        {
            get;
            set;
        }

        public bool ShouldSerializeMonoMode()
        {
            foreach (var dt in MonoMode)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetMonoMode()
        {
            for (int i = 0; i < MonoMode.Length; i++)
                MonoMode[i] = 0;
        }


        [DataMember]
        [Category("MIDI")]
        [Description("Poly mode (0-127) 0:Disable Poly mode N:Number of reserved voices <MIDI 16ch>")]
        [TypeConverter(typeof(MaskableExpandableMidiChCollectionConverter))]
        [Mask(127)]
        [CollectionDefaultValue((byte)0)]
        public virtual byte[] PolyMode
        {
            get;
            set;
        }


        public bool ShouldSerializePolyMode()
        {
            foreach (var dt in PolyMode)
            {
                if (dt != 0)
                    return true;
            }
            return false;
        }

        public void ResetPolyMode()
        {
            for (int i = 0; i < PolyMode.Length; i++)
                PolyMode[i] = 0;
        }

        [Browsable(false)]
        public byte[] DataLsb
        {
            get;
        }

        [Browsable(false)]
        public byte[] DataMsb
        {
            get;
        }

        [Browsable(false)]
        public byte[] NrpnLsb
        {
            get;
        }

        [Browsable(false)]
        public byte[] NrpnMsb
        {
            get;
        }

        [Browsable(false)]
        public byte[] RpnLsb
        {
            get;
        }

        [Browsable(false)]
        public byte[] RpnMsb
        {
            get;
        }

        private DataEntryType[] lastDateEntryType;

        /// <summary>
        /// 
        /// </summary>
        private enum DataEntryType
        {
            None,
            Nrpn,
            Rpn
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_device_enable(uint unitNumber, string tagName, byte enable);

        private static delegate_set_device_enable set_device_enable;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_device_passthru(uint unitNumber, string tagName, byte enable);

        private static delegate_set_device_passthru set_device_passthru;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="passthru"></param>
        protected void SetDevicePassThru(bool passthru)
        {
            set_device_passthru(UnitNumber, SoundInterfaceTagNamePrefix, passthru ? (byte)1 : (byte)0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void delegate_device_reset(uint unitNumber, string tagName);

        public static delegate_device_reset DeviceReset;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void delegate_set_output_gain(uint unitNumber, string tagName, int channel, float gain);

        public static delegate_set_output_gain SetOutputGain;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_filter(uint unitNumber, string tagName, FilterMode filterMode, double cutoff, double resonance);

        private static delegate_set_filter set_filter;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_clock(uint unitNumber, string tagName, uint clock);

        /// <summary>
        /// 
        /// </summary>
        private static delegate_set_clock set_clock;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitNumber"></param>
        /// <param name="clock"></param>
        protected void SetClock(uint unitNumber, uint clock)
        {
            try
            {
                Program.SoundUpdating();
                set_clock(unitNumber, SoundInterfaceTagNamePrefix, clock);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delg_vst_fx_callback(
            IntPtr buffer,
            int samples);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delegate_set_vst_fx_callback(uint unitNumber, string name, delg_vst_fx_callback callback);

        /// <summary>
        /// 
        /// </summary>
        private static void SetVstFxCallback(uint unitNumber, string name, delg_vst_fx_callback callback)
        {
            DeferredWriteData(set_vst_fx_callback, unitNumber, name, callback);
            /*
            try
            {
                Program.SoundUpdating();
                set_vst_fx_callback(unitNumber, name, callback);
            }
            finally
            {
                Program.SoundUpdated();
            }*/
        }

        /// <summary>
        /// 
        /// </summary>
        private static delegate_set_vst_fx_callback set_vst_fx_callback
        {
            get;
            set;
        }

        private delg_vst_fx_callback f_vst_fx_callback;

        private GCHandle vstHandle;

        #region VGM

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delg_start_vgm_recording_to(uint unitNumber, string tagName, string vgmPath);

        private delg_start_vgm_recording_to start_vgm_recording_to;

        /// <summary>
        /// 
        /// </summary>
        private delg_start_vgm_recording_to StartVgmRecordingToInternal
        {
            get
            {
                if (start_vgm_recording_to == null)
                {
                    IntPtr funcPtr = MameIF.GetProcAddress("start_vgm_recording_to");
                    if (funcPtr != IntPtr.Zero)
                        start_vgm_recording_to = (delg_start_vgm_recording_to)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delg_start_vgm_recording_to));
                }
                return start_vgm_recording_to;
            }
        }

        private bool vgmRecording;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vgmPath"></param>
        public virtual void StartVgmRecordingTo(string vgmPath)
        {
            AllSoundOff();
            ClearWrittenDataCache();

            vgmRecording = true;

            var now = DateTime.Now;
            string op = System.IO.Path.Combine(vgmPath, this.Name + "_" + this.UnitNumber + "_" +
                now.ToShortDateString().Replace('/', '-') + "_" + now.ToLongTimeString().Replace(':', '-'));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(op));
            StartVgmRecordingToInternal(UnitNumber, SoundInterfaceTagNamePrefix, op);

            PrepareSound();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void delg_stop_vgm_recording(uint unitNumber, string tagName);

        private delg_stop_vgm_recording stop_vgm_recording;

        /// <summary>
        /// 
        /// </summary>
        private delg_stop_vgm_recording StopVgmRecordingInternal
        {
            get
            {
                if (stop_vgm_recording == null)
                {
                    IntPtr funcPtr = MameIF.GetProcAddress("stop_vgm_recording");
                    if (funcPtr != IntPtr.Zero)
                        stop_vgm_recording = (delg_stop_vgm_recording)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(delg_stop_vgm_recording));
                }
                return stop_vgm_recording;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void StopVgmRecording()
        {
            vgmRecording = false;
            StopVgmRecordingInternal(UnitNumber, SoundInterfaceTagNamePrefix);
        }

        #endregion

        #region ctor

        /// <summary>
        /// 
        /// </summary>
        static InstrumentBase()
        {
            IntPtr funcPtr = MameIF.GetProcAddress("set_device_enable");
            if (funcPtr != IntPtr.Zero)
                set_device_enable = Marshal.GetDelegateForFunctionPointer<delegate_set_device_enable>(funcPtr);

            funcPtr = MameIF.GetProcAddress("set_device_passthru");
            if (funcPtr != IntPtr.Zero)
                set_device_passthru = Marshal.GetDelegateForFunctionPointer<delegate_set_device_passthru>(funcPtr);

            funcPtr = MameIF.GetProcAddress("set_output_gain");
            if (funcPtr != IntPtr.Zero)
                SetOutputGain = Marshal.GetDelegateForFunctionPointer<delegate_set_output_gain>(funcPtr);

            funcPtr = MameIF.GetProcAddress("device_reset");
            if (funcPtr != IntPtr.Zero)
                DeviceReset = Marshal.GetDelegateForFunctionPointer<delegate_device_reset>(funcPtr);

            funcPtr = MameIF.GetProcAddress("set_filter");
            if (funcPtr != IntPtr.Zero)
                set_filter = Marshal.GetDelegateForFunctionPointer<delegate_set_filter>(funcPtr);

            funcPtr = MameIF.GetProcAddress("set_clock");
            if (funcPtr != IntPtr.Zero)
                set_clock = Marshal.GetDelegateForFunctionPointer<delegate_set_clock>(funcPtr);

            funcPtr = MameIF.GetProcAddress("set_vst_fx_callback");
            if (funcPtr != IntPtr.Zero)
                set_vst_fx_callback = Marshal.GetDelegateForFunctionPointer<delegate_set_vst_fx_callback>(funcPtr);

            deferredWriteData = new List<(Delegate, object[])>();

            cachedWriteFunc = new Dictionary<Delegate, Func<object, object[], object>>();
        }

        public const int DEFAULT_MAX_TIMBRES = 256;

        /// <summary>
        /// 
        /// </summary>
        public InstrumentBase(uint unitNumber)
        {
            writtenDataCache = new Dictionary<uint, uint>();

            UnitNumber = unitNumber;

            DeviceReset(UnitNumber, SoundInterfaceTagNamePrefix);

            SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 0, GainLeft);
            SetOutputGain(UnitNumber, SoundInterfaceTagNamePrefix, 1, GainRight);
            SetDevicePassThru(false);

            GlobalARP = new ArpSettings();

            set_filter(UnitNumber, SoundInterfaceTagNamePrefix, FilterMode, FilterCutoff, FilterResonance);

            GEQ = new GraphicEqualizerSettings();

            vstHandle = GCHandle.Alloc(this);

            if (!Program.IsVSTiMode())
                initVstPlugins();

            CombinedTimbres = new CombinedTimbre[DEFAULT_MAX_TIMBRES];
            for (int i = 0; i < DEFAULT_MAX_TIMBRES; i++)
                CombinedTimbres[i] = new CombinedTimbre();

            f_DrumTimbres = new DrumTimbre[128];
            for (int i = 0; i < f_DrumTimbres.Length; i++)
                f_DrumTimbres[i] = new DrumTimbre(i, null);

            ProgramAssignments = new ProgramAssignmentNumber[128];
            for (int i = 0; i < ProgramAssignments.Length; i++)
                ProgramAssignments[i] = (ProgramAssignmentNumber)i;

            SlotAssignAlgorithm = new SlotAssignmentType[16];

            ScaleTunings = new ScaleTuning[16]{
                new ScaleTuning(), new ScaleTuning(), new ScaleTuning(), new ScaleTuning(),
                new ScaleTuning(), new ScaleTuning(), new ScaleTuning(), new ScaleTuning(),
                new ScaleTuning(), new ScaleTuning(), new ScaleTuning(), new ScaleTuning(),
                new ScaleTuning(), new ScaleTuning(), new ScaleTuning(), new ScaleTuning()
            };

            ChannelTypes = new ChannelType[] {
                    ChannelType.Normal, ChannelType.Normal, ChannelType.Normal,
                    ChannelType.Normal, ChannelType.Normal, ChannelType.Normal,
                    ChannelType.Normal, ChannelType.Normal, ChannelType.Normal,
                    ChannelType.Drum, ChannelType.Normal, ChannelType.Normal,
                    ChannelType.Normal, ChannelType.Normal, ChannelType.Normal,
                    ChannelType.Normal };

            Channels = new bool[] {
                    true, true, true,
                    true, true, true,
                    true, true, true,
                    true, true, true,
                    true, true, true,true };

            ProgramNumbers = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0 };
            Volumes = new byte[] {
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127, 127  };
            Expressions = new byte[] {
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127,
                    127, 127, 127, 127  };
            Panpots = new byte[] {
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64, 64};
            Pitchs = new ushort[] {
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192, 8192};
            FineTunes = new ushort[] {
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192,
                    8192, 8192, 8192, 8192};
            PitchBendRanges = new byte[] {
                    2, 2, 2,
                    2, 2, 2,
                    2, 2, 2,
                    2, 2, 2,
                    2, 2, 2, 2};
            DataLsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            DataMsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            NrpnLsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            NrpnMsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            RpnLsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            RpnMsb = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            Modulations = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            ModulationRates = new byte[] {
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64, 64};
            ModulationDepthes = new byte[] {
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64, 64};
            ModulationDelays = new byte[] {
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64,
                    64, 64, 64, 64};
            ModulationDepthRangesNote = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            ModulationDepthRangesCent = new byte[] {
                    0x40, 0x40, 0x40,
                    0x40, 0x40, 0x40,
                    0x40, 0x40, 0x40,
                    0x40, 0x40, 0x40,
                    0x40, 0x40, 0x40, 0x40};
            Portamentos = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            PortamentoTimes = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            MonoMode = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            LegatoFootSwitch = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            PolyMode = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            Holds = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            LastHolds = new byte[] {
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0,
                    0, 0, 0, 0};
            GPCS = new GeneralPurposeControlSettings[]{
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings(),
                new GeneralPurposeControlSettings()
            };
            lastDateEntryType = new DataEntryType[]
            {
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None,
                DataEntryType.None
            };

            timbreListMenuItem = new ToolStripMenuItem(Resources.TimbreList);
            timbreListMenuItem.Click += timbreListMenuItem_Click;
        }

        private void initVstPlugins()
        {
            f_vst_fx_callback = new delg_vst_fx_callback(vst_fx_callback);
            SetVstFxCallback(UnitNumber, SoundInterfaceTagNamePrefix, f_vst_fx_callback);

            VSTPlugins = new VSTPluginCollection();
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //マネージ状態を破棄します (マネージ オブジェクト)。

                    timbreListMenuItem?.Dispose();
                    timbreListMenuItem = null;
                }
                if (vgmRecording)
                    StopVgmRecordingInternal(UnitNumber, SoundInterfaceTagNamePrefix);

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。
                set_device_enable(UnitNumber, SoundInterfaceTagNamePrefix, 0);

                if (VSTPlugins != null)
                {
                    SetVstFxCallback(UnitNumber, SoundInterfaceTagNamePrefix, null);

                    lock (InstrumentBase.VstPluginContextLockObject)
                    {
                        VSTPlugins.Dispose();
                    }
                }

                if (vstHandle.IsAllocated)
                    vstHandle.Free();

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        ~InstrumentBase()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        [IgnoreDataMember]
        [JsonIgnore]
        [Browsable(false)]
        public bool IsDisposing
        {
            get;
            protected set;
        }

        [IgnoreDataMember]
        [JsonIgnore]
        [Browsable(false)]
        public bool IsDisposed
        {
            get;
            protected set;
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public virtual void Dispose()
        {
            IsDisposing = true;

            AllSoundOff();

            Program.SoundUpdating();

            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            GC.SuppressFinalize(this);

            Program.SoundUpdated();

            IsDisposed = true;
        }

        #endregion

        #region deferredWriteData

        private static List<(Delegate, object[])> deferredWriteData;

        private static Dictionary<Delegate, Func<Object, Object[], Object>> cachedWriteFunc;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delg"></param>
        protected static void RemoveCachedDelegate(Delegate delg)
        {
            try
            {
                Program.SoundUpdating();
                if (cachedWriteFunc.ContainsKey(delg))
                    cachedWriteFunc.Remove(delg);
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        private Dictionary<uint, uint> writtenDataCache;

        /// <summary>
        /// 
        /// </summary>
        protected virtual void ClearWrittenDataCache()
        {
            lock (writtenDataCache)
                writtenDataCache.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iSoundChipType"></param>
        /// <param name="clock"></param>
        /// <returns></returns>
        protected UInt32? GetCachedWrittenData(uint address)
        {
            lock (writtenDataCache)
                if (writtenDataCache.ContainsKey(address))
                    return writtenDataCache[address];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <param name="useCache"></param>
        /// <param name="wtiteAction"></param>
        protected void WriteData(uint address, uint data, bool useCache, Action wtiteAction)
        {
            if (useCache)
            {
                var cacheData = GetCachedWrittenData(address);
                if (cacheData == null || cacheData.Value != data)
                {
                    wtiteAction?.Invoke();
                    lock (writtenDataCache)
                        writtenDataCache[address] = data;
                }
            }
            else
            {
                wtiteAction?.Invoke();
                lock (writtenDataCache)
                    writtenDataCache[address] = data;
            }
        }

        internal virtual void DirectAccessToChip(uint address, uint data)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delg"></param>
        /// <param name="args"></param>
        protected static void DeferredWriteData(Delegate delg, params object[] args)
        {
#if DEBUG

            try
            {
                Program.SoundUpdating();
                invokeFunction(delg, args);
                //delg.DynamicInvoke(args);
            }
            finally
            {
                Program.SoundUpdated();
            }
#else

            lock (deferredWriteData)
            {
                deferredWriteData.Add((delg, args));
                if (deferredWriteData.Count != 1)
                    return;
            }
            void act()
            {
                try
                {
                    Program.SoundUpdating();
                    lock (deferredWriteData)
                    {
                        foreach (var (d, a) in deferredWriteData)
                        {
                            if (Program.HasExited() != 0)
                                break;
                            try
                            {
                                invokeFunction(d, a);
                                //d.DynamicInvoke(a);
                            }
                            catch (Exception ex)
                            {
                                if (ex.GetType() == typeof(Exception))
                                    throw;
                                else if (ex.GetType() == typeof(SystemException))
                                    throw;
                            }
                        }
                        deferredWriteData.Clear();
                    }
                }
                finally
                {
                    Program.SoundUpdated();
                }
            }
            Task.Factory.StartNew(act);
#endif
        }

        private static void invokeFunction(Delegate d, object[] a)
        {
            Func<Object, Object[], Object> func = null;
            if (cachedWriteFunc.ContainsKey(d))
                func = cachedWriteFunc[d];
            else
            {
                func = d.Method.Bind();
                cachedWriteFunc.Add(d, func);
            }
            func.Invoke(d, a);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void FlushDeferredWriteData()
        {
            lock (deferredWriteData)
            {
                if (deferredWriteData.Count == 0)
                    return;
            }

            try
            {
                Program.SoundUpdating();
                lock (deferredWriteData)
                {
                    foreach (var (d, a) in deferredWriteData)
                    {
                        invokeFunction(d, a);
                        //d.DynamicInvoke(a);
                    }
                    deferredWriteData.Clear();
                }
            }
            finally
            {
                Program.SoundUpdated();
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        [IgnoreDataMember]
        [JsonIgnore]
        [Browsable(false)]
        internal bool IsSoundPrepared
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        internal virtual void PrepareSound()
        {
            IsSoundPrepared = true;
            set_device_enable(UnitNumber, SoundInterfaceTagNamePrefix, 1);
        }

        #region MIDI

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        internal void NotifyMidiEvent(MidiEvent midiEvent)
        {
            lock (MidiManager.SoundExclusiveLockObject)
                OnMidiEvent(midiEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnMidiEvent(MidiEvent midiEvent)
        {
            //TODO: key/ch pressure

            switch (midiEvent)
            {
                case SysExEvent sysex:
                    {
                        OnSystemExclusiveEvent(sysex);
                        break;
                    }
            }

            var ce = midiEvent as ChannelEvent;
            if (ce != null && Channels[ce.Channel])
            {
                switch (midiEvent)
                {
                    case ChannelAftertouchEvent caft:
                        {
                            OnChannelAfterTouchEvent(caft);
                            break;
                        }
                    case NoteOnEvent non:
                        {
                            if (non.Velocity == 0)
                            {
                                if (ChannelTypes[non.Channel] != ChannelType.Drum && Holds[ce.Channel] < 64)
                                    OnNoteOffEvent(new NoteOffEvent(non.NoteNumber, (SevenBitNumber)0) { Channel = non.Channel, DeltaTime = non.DeltaTime });
                            }
                            else
                            {
                                if (FollowerMode != FollowerUnit.None)
                                    break;
                                lastNoteOnTimbres[ce.Channel] = GetTimbre(non);
                                OnNoteOnEvent(new TaggedNoteOnEvent(non));
                            }
                            break;
                        }
                    case TaggedNoteOnEvent tnon:
                        {
                            if (tnon.Velocity == 0)
                            {
                                if (ChannelTypes[tnon.Channel] != ChannelType.Drum && Holds[ce.Channel] < 64)
                                    OnNoteOffEvent(new NoteOffEvent(tnon.NoteNumber, (SevenBitNumber)0) { Channel = tnon.Channel, DeltaTime = tnon.DeltaTime });
                            }
                            else
                            {
                                if (FollowerMode != FollowerUnit.None && !tnon.MonitorEvent)
                                    break;
                                var ni = tnon.Tag as NoteOnTimbreInfo;
                                if (ni != null)
                                    lastNoteOnTimbres[ce.Channel] = ni.Timbre;
                                else
                                    lastNoteOnTimbres[ce.Channel] = GetTimbre(tnon);
                                OnNoteOnEvent(tnon);
                            }
                            break;
                        }
                    case NoteOffEvent noff:
                        {
                            if (ChannelTypes[noff.Channel] != ChannelType.Drum && Holds[ce.Channel] < 64)
                                OnNoteOffEvent(noff);
                            break;
                        }
                    case ControlChangeEvent cont:
                        {
                            OnControlChangeEvent(cont);
                            break;
                        }
                    case ProgramChangeEvent prog:
                        {
                            OnProgramChangeEvent(prog);
                            break;
                        }
                    case PitchBendEvent pitch:
                        {
                            OnPitchBendEvent(pitch);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caft"></param>
        protected virtual void OnChannelAfterTouchEvent(ChannelAftertouchEvent caft)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sysex"></param>
        protected virtual void OnSystemExclusiveEvent(SysExEvent sysex)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnNoteOnEvent(TaggedNoteOnEvent midiEvent)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnNoteOffEvent(NoteOffEvent midiEvent)
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnControlChangeEvent(ControlChangeEvent midiEvent)
        {
            switch (midiEvent.ControlNumber)
            {
                case 1:    //Modulation
                    Modulations[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 6:    //Data Entry MSB
                    DataMsb[midiEvent.Channel] = midiEvent.ControlValue;

                    switch (lastDateEntryType[midiEvent.Channel])
                    {
                        case DataEntryType.Nrpn:
                            OnNrpnDataEntered(midiEvent, null);
                            break;
                        case DataEntryType.Rpn:
                            OnRpnDataEntered(midiEvent, null);
                            break;
                    }
                    break;
                case 38:    //Data Entry LSB
                    DataLsb[midiEvent.Channel] = midiEvent.ControlValue;

                    switch (lastDateEntryType[midiEvent.Channel])
                    {
                        case DataEntryType.Nrpn:
                            OnNrpnDataEntered(null, midiEvent);
                            break;
                        case DataEntryType.Rpn:
                            OnRpnDataEntered(null, midiEvent);
                            break;
                    }
                    break;
                case 5:    //Portamento Time
                    PortamentoTimes[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 7:    //Volume
                    Volumes[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 10:    //Panpot
                    Panpots[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 11:    //Expression
                    Expressions[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 64:    //Holds
                    LastHolds[midiEvent.Channel] = Holds[midiEvent.Channel];
                    Holds[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 65:    //Portamento
                    Portamentos[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 68:    //LegatoFootSwitch
                    LegatoFootSwitch[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 76:    //Modulation Rate
                    ModulationRates[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 77:    //Modulation Depth
                    ModulationDepthes[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 78:    //Modulation Delay
                    ModulationDelays[midiEvent.Channel] = midiEvent.ControlValue;
                    break;

                //Sound Control
                case 91:
                case 92:
                case 93:
                case 94:
                case 95:
                    {
                        if (VSTPlugins != null)
                            VSTPlugins.ProcessSoundControl(midiEvent.Channel, midiEvent.ControlNumber, midiEvent.ControlValue, 0);
                    }
                    break;

                case 96:    //Data Increment

                    break;

                case 97:    //Data Decrement

                    break;

                case 98:    //NRPN LSB
                    NrpnLsb[midiEvent.Channel] = midiEvent.ControlValue;
                    lastDateEntryType[midiEvent.Channel] = DataEntryType.Nrpn;
                    break;
                case 99:    //NRPN MSB
                    NrpnMsb[midiEvent.Channel] = midiEvent.ControlValue;
                    lastDateEntryType[midiEvent.Channel] = DataEntryType.Nrpn;
                    break;

                case 100:    //RPN LSB
                    RpnLsb[midiEvent.Channel] = midiEvent.ControlValue;
                    lastDateEntryType[midiEvent.Channel] = DataEntryType.Rpn;
                    break;
                case 101:    //RPN MSB
                    RpnMsb[midiEvent.Channel] = midiEvent.ControlValue;
                    lastDateEntryType[midiEvent.Channel] = DataEntryType.Rpn;
                    break;
                case 121:    //Reset All Controller
                    for (int i = 0; i < 16; i++)
                    {
                        Volumes[i] = 127;
                        Panpots[i] = 64;
                        Expressions[i] = 127;
                        PitchBendRanges[i] = 2;
                        Pitchs[i] = 8192;
                        Holds[i] = 0;
                        LastHolds[i] = 0;
                        Modulations[i] = 0;
                        ModulationRates[i] = 64;
                        ModulationDepthes[i] = 64;
                        ModulationDelays[i] = 64;
                        ModulationDepthRangesNote[i] = 0;
                        ModulationDepthRangesCent[i] = 0x40;
                        Portamentos[i] = 0;
                        PortamentoTimes[i] = 0;
                        FineTunes[i] = 8192;
                        LegatoFootSwitch[i] = 0;

                        MonoMode[i] = 0;
                        PolyMode[i] = 0;
                    }
                    break;
                case 126:    //MONO Mode
                    MonoMode[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
                case 127:    //POLY Mode
                    MonoMode[midiEvent.Channel] = 0;
                    PolyMode[midiEvent.Channel] = midiEvent.ControlValue;
                    break;
            }
        }

        private ControlChangeEvent[] lastDataLsb = new ControlChangeEvent[16];

        private ControlChangeEvent[] lastDataMsb = new ControlChangeEvent[16];

        abstract internal void AllSoundOff();

        abstract internal void ResetAll();

        protected virtual void OnNrpnDataEntered(ControlChangeEvent dataMsb, ControlChangeEvent dataLsb)
        {
            if (dataMsb != null)
            {
                switch (NrpnMsb[dataMsb.Channel])
                {
                    default:
                        break;
                }

                lastDataMsb[dataMsb.Channel] = dataMsb;
            }
            if (dataLsb != null)
            {
                switch (NrpnMsb[dataLsb.Channel])
                {
                    //Sound Control
                    case 91:
                    case 92:
                    case 93:
                    case 94:
                    case 95:
                        if (VSTPlugins != null)
                            VSTPlugins.ProcessSoundControl(dataMsb.Channel, dataMsb.ControlNumber, lastDataMsb[dataMsb.Channel].ControlValue, dataLsb.ControlValue);
                        break;
                    default:
                        break;
                }

                lastDataLsb[dataLsb.Channel] = dataLsb;
            }
        }

        protected virtual void OnRpnDataEntered(ControlChangeEvent dataMsb, ControlChangeEvent dataLsb)
        {
            if (dataMsb != null)
            {
                switch (RpnMsb[dataMsb.Channel])
                {
                    case 0:
                        {
                            switch (RpnLsb[dataMsb.Channel])
                            {
                                case 0: //PitchBendRanges Half Note
                                    {
                                        PitchBendRanges[dataMsb.Channel] = dataMsb.ControlValue;
                                        break;
                                    }
                                case 5: //Mod Depth
                                    {
                                        ModulationDepthRangesNote[dataMsb.Channel] = dataMsb.ControlValue;
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            if (dataLsb != null)
            {
                switch (RpnMsb[dataLsb.Channel])
                {
                    case 0:
                        {
                            switch (RpnLsb[dataLsb.Channel])
                            {
                                case 1: //Master Fine Tune
                                    {
                                        FineTunes[dataLsb.Channel] = (ushort)(((ushort)(DataMsb[dataLsb.Channel]) << 7) | (ushort)dataLsb.ControlValue);
                                        break;
                                    }
                                case 0: //PitchBendRanges Cent
                                    {
                                        break;
                                    }
                                case 5: //Mod Depth
                                    {
                                        ModulationDepthRangesCent[dataLsb.Channel] = dataLsb.ControlValue;
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }

        public static PropertyDescriptor GetPropertyDescriptor(PropertyInfo propertyInfo)
        {
            return TypeDescriptor.GetProperties(propertyInfo.DeclaringType)[propertyInfo.Name];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnProgramChangeEvent(ProgramChangeEvent midiEvent)
        {
            ProgramNumbers[midiEvent.Channel] = midiEvent.ProgramNumber;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="midiEvent"></param>
        protected virtual void OnPitchBendEvent(PitchBendEvent midiEvent)
        {
            Pitchs[midiEvent.Channel] = midiEvent.PitchValue;
        }

        #endregion

        #region VST

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="pos"></param>
        private void vst_fx_callback(IntPtr buffer, int samples)
        {
            bool copied = false;
            int[][] buf = null;
            IntPtr[] pt = null;

            if (GEQ.Enable)
            {
                if (!copied)
                {
                    buf = new int[2][] { new int[samples], new int[samples] };
                    pt = new IntPtr[] { Marshal.ReadIntPtr(buffer), Marshal.ReadIntPtr(buffer + IntPtr.Size) };
                    Marshal.Copy(pt[0], buf[0], 0, samples);
                    Marshal.Copy(pt[1], buf[1], 0, samples);
                    copied = true;
                }
                GEQ.ProcessCallback(buf, samples);
            }

            if (VSTPlugins != null)
            {
                if (!copied)
                {
                    buf = new int[2][] { new int[samples], new int[samples] };
                    pt = new IntPtr[] { Marshal.ReadIntPtr(buffer), Marshal.ReadIntPtr(buffer + IntPtr.Size) };
                    Marshal.Copy(pt[0], buf[0], 0, samples);
                    Marshal.Copy(pt[1], buf[1], 0, samples);
                    copied = true;
                }
                VSTPlugins.ProcessCallback(buf, samples);
            }

            if (copied)
            {
                Marshal.Copy(buf[0], 0, pt[0], samples);
                Marshal.Copy(buf[1], 0, pt[1], samples);
            }
        }

        #endregion

        #region MENU

        private ToolStripMenuItem timbreListMenuItem;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timbreListMenuItem_Click(object sender, EventArgs e)
        {
            FormTimbreList edl = new FormTimbreList(this);
            edl.Show();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal virtual IEnumerable<ToolStripMenuItem> GetInstrumentMenus()
        {
            return new ToolStripMenuItem[] { timbreListMenuItem };
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        [JsonIgnore]
        public virtual bool CanImportToneFile
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timbre"></param>
        /// <param name="tone"></param>
        public virtual void ImportToneFile(TimbreBase timbre, Tone tone)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        [JsonIgnore]
        public virtual bool CanImportBinFile
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        [JsonIgnore]
        public virtual String SupportedBinExts
        {
            get
            {
                return "*.wav";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timbre"></param>
        /// <param name="binFile"></param>
        public virtual void ImportBinFile(TimbreBase timbre, FileInfo binFile)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        [JsonIgnore]
        public virtual CustomToneImporter CustomToneImporter
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Helper function to go from IWaveProvider to a SampleProvider
        /// Must already be PCM or IEEE float
        /// </summary>
        /// <param name="waveProvider">The WaveProvider to convert</param>
        /// <returns>A sample provider</returns>
        public static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider waveProvider)
        {
            ISampleProvider sampleProvider;

            if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                // go to float
                if (waveProvider.WaveFormat.BitsPerSample == 8)
                {
                    sampleProvider = new Pcm8BitToSampleProvider(waveProvider);
                }
                else if (waveProvider.WaveFormat.BitsPerSample == 16)
                {
                    sampleProvider = new Pcm16BitToSampleProvider(waveProvider);
                }
                else if (waveProvider.WaveFormat.BitsPerSample == 24)
                {
                    sampleProvider = new Pcm24BitToSampleProvider(waveProvider);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported operation");
                }
            }
            else if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                sampleProvider = new WaveToSampleProvider(waveProvider);
            }
            else
            {
                throw new ArgumentException("Unsupported source encoding");
            }
            return (sampleProvider);
        }
    }

    public enum FilterMode
    {
        None = 0,
        LowPass,
        HighPass,
        BandPass,
    };
}
