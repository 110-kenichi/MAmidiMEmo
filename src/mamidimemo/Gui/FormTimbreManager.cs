using FM_SoundConvertor;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using NAudio.SoundFont;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Instruments;
using zanac.MAmidiMEmo.Midi;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.Util;
using zanac.MAmidiMEmo.Util.FITOM;
using zanac.MAmidiMEmo.Util.Syx;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace zanac.MAmidiMEmo.Gui
{
    public partial class FormTimbreManager : FormBase
    {
        public InstrumentBase Instrument
        {
            get;
            private set;
        }

        private Type timbreType;

        private Type drumTimbreType;

        private List<TimbreBase> originalTimbres;

        public static Dictionary<InstrumentBase, FormTimbreManager> TimbreManagers = new Dictionary<InstrumentBase, FormTimbreManager>();

        /// <summary>
        /// 
        /// </summary>
        public FormTimbreManager()
        {
            InitializeComponent();
        }


        /// <summary>
        /// 
        /// </summary>
        public FormTimbreManager(InstrumentBase inst)
        {
            InitializeComponent();

            Size = Settings.Default.TimbreManagerWinSize;

            this.Instrument = inst;

            for (int nn = 0; nn < 128; nn++)
            {
                toolStripComboBoxNote.Items.Add(MidiManager.GetNoteName((SevenBitNumber)nn) + "(" + nn + ")");
                toolStripComboBoxVelo.Items.Add(nn);
                comboBoxDrumKeyNo.Items.Add(MidiManager.GetNoteName((SevenBitNumber)nn) + "(" + nn + ")");
            }
            comboBoxDrumKeyNo.SelectedIndex = 0;

            Settings.Default.SettingsLoaded += Default_SettingsLoaded;
            toolStripButtonPlay.Checked = Settings.Default.FmPlayOnEdit;
            toolStripButtonHook.Checked = Settings.Default.FmHook;
            toolStripComboBoxVelo.SelectedIndex = Settings.Default.FmVelocity;
            toolStripComboBoxGate.SelectedIndex = Settings.Default.FmGateTime;
            toolStripComboBoxNote.SelectedIndex = Settings.Default.FmNote;
            toolStripComboBoxCC.SelectedIndex = Settings.Default.FmCC;
            toolStripComboBoxCh.SelectedIndex = Settings.Default.FmCh;
            if (inst.Channels[toolStripComboBoxCh.SelectedIndex] == false)
            {
                for (int i = 0; i < inst.Channels.Length; i++)
                {
                    if (inst.Channels[i])
                    {
                        toolStripComboBoxCh.SelectedIndex = i;
                        break;
                    }
                }
            }

            //pianoControl1.TargetTimbres = new TimbreBase[] { timbre };

            timbreType = Instrument.BaseTimbres[0].GetType();
            drumTimbreType = Instrument.DrumTimbres[0].GetType();

            originalTimbres = new List<TimbreBase>();
            for (int i = 0; i < Instrument.BaseTimbres.Length; i++)
            {
                originalTimbres.Add((TimbreBase)JsonConvert.DeserializeObject(inst.BaseTimbres[i].SerializeData, timbreType));
                var tim = new TimbreItem(inst.BaseTimbres[i], i);
                var lvi = new ListViewItem(new string[] { i.ToString(), tim.Timbre.TimbreName, tim.Timbre.Memo });
                lvi.Tag = tim;
                listViewCurrentTimbres.Items.Add(lvi);
            }
            listViewCurrentTimbres.Items[0].Selected = true;

            lastFocusedListView = listViewCurrentTimbres;

            setTitle();

            InstrumentManager.InstrumentChanged += InstrumentManager_InstrumentChanged;
            InstrumentManager.InstrumentRemoved += InstrumentManager_InstrumentRemoved;

            pianoControl1.NoteOn += PianoControl1_NoteOn;
            pianoControl1.NoteOff += PianoControl1_NoteOff;
            pianoControl1.EntryDataChanged += PianoControl1_EntryDataChanged;

            Midi.MidiManager.MidiEventHooked += MidiManager_MidiEventHooked;

            List<string> extList = new List<string>(new string[] { ".MSD", ".MSDS" });
            if (inst.CanImportToneFile)
            {
                var texts = Tone.SupportedExts.Split(';');
                for (int i = 0; i < texts.Length; i++)
                    texts[i] = texts[i].Replace("*", "");
                extList.AddRange(texts);
            }
            if (inst.CanImportBinFile)
            {
                var texts = inst.SupportedBinExts.Split(';');
                for (int i = 0; i < texts.Length; i++)
                    texts[i] = texts[i].Replace("*", "");
                extList.AddRange(texts);
            }
            if (inst.CustomToneImporter != null)
            {
                var texts = inst.CustomToneImporter.ExtensionsFilterExt.Split(';');
                for (int i = 0; i < texts.Length; i++)
                    texts[i] = texts[i].Replace("*", "");
                extList.AddRange(texts);
            }

            fileFolderList1.FilterExts = extList.ToArray();
            fileFolderList1.FileValidator = new Func<FileSystemInfo, bool>((fi) =>
            {
                FileInfo ffi = fi as FileInfo;
                if (ffi != null &&
                (string.Equals(ffi.Extension, ".MSD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ffi.Extension, ".MSDS", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        string fullTypeName = timbreType.FullName;
                        string ftname = System.IO.File.OpenText(ffi.FullName).ReadLine();
                        if (string.Equals(fullTypeName, ftname, StringComparison.Ordinal))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                    }
                    return false;
                }
                else
                    return true;
            });
            fileFolderList1.ItemSelectionChanged += fileFolderList1_SelectedIndexChanged;
            System.Windows.Forms.Application.Idle += Application_Idle;

            if (Settings.Default.ToneLibMRU == null)
                Settings.Default.ToneLibMRU = new System.Collections.Specialized.StringCollection();
            foreach (String item in Settings.Default.ToneLibMRU)
                comboBoxDir.Items.Add(item);

            TimbreManagers.Add(inst, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            listViewFilesTimbres.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(listViewFilesTimbres_ItemSelectionChanged);

            listViewCurrentTimbres.ItemSelectionChanged += new ListViewItemSelectionChangedEventHandler(listViewCurrentTimbres_ItemSelectionChanged);
            foreach (ColumnHeader c in listViewCurrentTimbres.Columns)
                c.Width = -1;

            fileFolderList1.CurrentDirectory = Program.GetToneLibLastDir();

            try
            {
                comboBoxDir.Text = fileFolderList1.CurrentDirectory;
                fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

            }

            toolStripComboBoxCh.Focus();
            base.OnShown(e);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            TimbreManagers.Remove(Instrument);

            Midi.MidiManager.MidiEventHooked -= MidiManager_MidiEventHooked;

            System.Windows.Forms.Application.Idle -= new EventHandler(Application_Idle);

            Settings.Default.SettingsLoaded -= Default_SettingsLoaded;
            InstrumentManager.InstrumentChanged -= InstrumentManager_InstrumentChanged;
            InstrumentManager.InstrumentRemoved -= InstrumentManager_InstrumentRemoved;

            Settings.Default.FmPlayOnEdit = toolStripButtonPlay.Checked;
            Settings.Default.FmHook = toolStripButtonHook.Checked;
            Settings.Default.FmVelocity = toolStripComboBoxVelo.SelectedIndex;
            Settings.Default.FmGateTime = toolStripComboBoxGate.SelectedIndex;
            Settings.Default.FmNote = toolStripComboBoxNote.SelectedIndex;
            Settings.Default.FmCC = toolStripComboBoxCC.SelectedIndex;
            Settings.Default.FmCh = toolStripComboBoxCh.SelectedIndex;

            Settings.Default.TimbreManagerWinSize = Size;

            base.OnClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.W | Keys.Control) || keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            Settings.Default.ToneLibLastDir = fileFolderList1.CurrentDirectory;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Instrument.BaseTimbres.Length; i++)
                Instrument.BaseTimbres[i] = originalTimbres[i];

            DialogResult = DialogResult.Cancel;
        }

        private void Default_SettingsLoaded(object sender, System.Configuration.SettingsLoadedEventArgs e)
        {
            toolStripButtonPlay.Checked = Settings.Default.FmPlayOnEdit;
            toolStripButtonHook.Checked = Settings.Default.FmHook;
            toolStripComboBoxVelo.SelectedIndex = Settings.Default.FmVelocity;
            toolStripComboBoxGate.SelectedIndex = Settings.Default.FmGateTime;
            toolStripComboBoxNote.SelectedIndex = Settings.Default.FmNote;
            toolStripComboBoxCC.SelectedIndex = Settings.Default.FmCC;
            toolStripComboBoxCh.SelectedIndex = Settings.Default.FmCh;
        }

        private void setTitle()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Instrument.Name + "(" + Instrument.UnitNumber + ")");
            sb.Append(" - " + timbreType.Name);

            this.Text = sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstrumentManager_InstrumentRemoved(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed)
                return;

            foreach (var inst in InstrumentManager.GetAllInstruments())
            {
                if (Instrument.DeviceID == inst.DeviceID && Instrument.UnitNumber == inst.UnitNumber)
                    return;
            }
            DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstrumentManager_InstrumentChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed)
                return;

            foreach (var inst in InstrumentManager.GetAllInstruments())
            {
                if (Instrument.DeviceID == inst.DeviceID && Instrument.UnitNumber == inst.UnitNumber)
                    return;
            }
            DialogResult = DialogResult.Cancel;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MidiManager_MidiEventHooked(object sender, CancelMidiEventReceivedEventArgs e)
        {
            if (!this.IsHandleCreated || ActiveForm != this)
                return;

            if (this.IsDisposed)
                return;

            this.Invoke(new MethodInvoker(() =>
            {
                if (this.IsDisposed)
                    return;

                if (toolStripButtonHook.Checked)
                {
                    switch (e.Event.Event)
                    {
                        case NoteOnEvent non:
                            if (non.Velocity != 0)
                                PianoControl1_NoteOn(null, new TaggedNoteOnEvent(non) { MonitorEvent = true });
                            else
                                PianoControl1_NoteOff(null, new NoteOffEvent(non.NoteNumber, (SevenBitNumber)0) { Channel = non.Channel, DeltaTime = non.DeltaTime });
                            break;
                        case NoteOffEvent noff:
                            PianoControl1_NoteOff(null, noff);
                            break;
                        default:
                            Instrument.NotifyMidiEvent(e.Event.Event);
                            break;
                    }
                    e.Cancel = true;
                }
            }));
        }

        private object playing;
        private SevenBitNumber ni;
        private SevenBitNumber vi;

        private int ignorePlayingFlag;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void testStop()
        {
            if (playing != null)
            {
                PianoControl1_NoteOff(null, new NoteOffEvent(ni, vi));
                playing = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task testPlay()
        {
            if (playing != null)
            {
                PianoControl1_NoteOff(null, new NoteOffEvent(ni, vi));
                playing = null;
            }

            ni = (SevenBitNumber)toolStripComboBoxNote.SelectedIndex;
            vi = (SevenBitNumber)toolStripComboBoxVelo.SelectedIndex;
            PianoControl1_NoteOn(null, new TaggedNoteOnEvent(new NoteOnEvent(ni, vi)) { MonitorEvent = true });
            playing = new object();
            object _playing = playing;

            int wait = 500;
            switch (toolStripComboBoxGate.SelectedIndex)
            {
                case 0:
                    //500ms
                    wait = 500;
                    break;
                case 1:
                    //1000ms
                    wait = 1000;
                    break;
                case 2:
                    //2000ms
                    wait = 2000;
                    break;
                case 3:
                    //5000ms
                    wait = 5000;
                    break;
            }
            await Task.Delay(wait);

            if (playing == _playing)
            {
                if (IsDisposed)
                    return;

                PianoControl1_NoteOff(null, new NoteOffEvent(ni, vi));
                playing = null;
            }
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < 16; i++)
                pianoControl1.SetReceiveChannel(i, false);
            pianoControl1.SetReceiveChannel(toolStripComboBoxCh.SelectedIndex, true);
        }

        private void PianoControl1_NoteOn(object sender, TaggedNoteOnEvent e)
        {
            try
            {
                //InstrumentManager.ExclusiveLockObject.EnterUpgradeableReadLock();
                if (lastFocusedListView != null && lastFocusedListView.SelectedItems.Count != 0)
                {
                    TimbreItem tim = (TimbreItem)lastFocusedListView.SelectedItems[0].Tag;
                    TimbreItem timNo = (TimbreItem)listViewCurrentTimbres.Items[0].Tag;
                    if (listViewCurrentTimbres.SelectedItems.Count != 0)
                        timNo = (TimbreItem)listViewCurrentTimbres.SelectedItems[0].Tag;

                    e.Tag = new NoteOnTimbreInfo(tim.Timbre, timNo.Number);
                    e.Channel = (FourBitNumber)(toolStripComboBoxCh.SelectedIndex & 0xf);
                    Instrument.NotifyMidiEvent(e);
                }
            }
            finally
            {
                //InstrumentManager.ExclusiveLockObject.ExitUpgradeableReadLock();
            }
        }

        private void PianoControl1_NoteOff(object sender, NoteOffEvent e)
        {
            try
            {
                //InstrumentManager.ExclusiveLockObject.EnterUpgradeableReadLock();
                e.Channel = (FourBitNumber)(toolStripComboBoxCh.SelectedIndex & 0xf);
                Instrument.NotifyMidiEvent(e);
            }
            finally
            {
                //InstrumentManager.ExclusiveLockObject.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PianoControl1_EntryDataChanged(object sender, EventArgs e)
        {
            try
            {
                //InstrumentManager.ExclusiveLockObject.EnterUpgradeableReadLock();

                var cce = new ControlChangeEvent
                    ((SevenBitNumber)toolStripComboBoxCC.SelectedIndex,
                    (SevenBitNumber)pianoControl1.EntryDataValue);
                cce.Channel = (FourBitNumber)(toolStripComboBoxCh.SelectedIndex);
                Instrument.NotifyMidiEvent(cce);
            }
            finally
            {
                //InstrumentManager.ExclusiveLockObject.ExitUpgradeableReadLock();
            }
        }

        private bool ignoreMetroComboBoxTimbres_SelectedIndexChanged;

        private bool listViewCurrentTimbresItemSelectionChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewCurrentTimbres_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
                return;
            if (ignoreMetroComboBoxTimbres_SelectedIndexChanged)
                return;
            if (listViewCurrentTimbres.SelectedItems.Count == 0)
                return;

            listViewCurrentTimbresItemSelectionChanged = true;
        }

        private bool listViewFilesTimbresItemSelectionChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewFilesTimbres_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
                return;
            if (ignoreMetroComboBoxTimbres_SelectedIndexChanged)
                return;
            if (listViewCurrentTimbres.SelectedItems.Count == 0)
                return;

            listViewFilesTimbresItemSelectionChanged = true;
        }

        private bool fileFolderListItemSelectionChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidDataException"></exception>
        private async void Application_Idle(object sender, EventArgs e)
        {
            if (listViewCurrentTimbresItemSelectionChanged)
            {
                listViewCurrentTimbresItemSelectionChanged = false;

                TimbreItem ti = (TimbreItem)listViewCurrentTimbres.SelectedItems[0].Tag;
                if (ti != null)
                {
                    if (toolStripButtonPlay.Checked && ignorePlayingFlag == 0 && Visible)
                        await testPlay();
                }
            }

            if (listViewFilesTimbresItemSelectionChanged)
            {
                listViewFilesTimbresItemSelectionChanged = false;

                TimbreItem ti = (TimbreItem)listViewFilesTimbres.SelectedItems[0].Tag;
                if (ti != null)
                {
                    if (listViewCurrentTimbres.SelectedItems.Count != 0)
                    {
                        if (toolStripButtonPlay.Checked && ignorePlayingFlag == 0 && Visible)
                            await testPlay();
                    }
                }
            }

            if (fileFolderListItemSelectionChanged)
            {
                fileFolderListItemSelectionChanged = false;

                var dlDestFilePath = fileFolderList1.SelectedPaths;
                if (dlDestFilePath == null)
                    return;

                List<ListViewItem> items = new List<ListViewItem>();
                int no = 0;
                try
                {
                    listViewFilesTimbres.BeginUpdate();
                    listViewFilesTimbres.Items.Clear();

                    for (int i = 0; i < dlDestFilePath.Length; i++)
                    {
                        string file = dlDestFilePath[i];
                        if (!System.IO.File.Exists(file))
                            continue;

                        switch (Path.GetExtension(file).ToUpper(CultureInfo.InvariantCulture))
                        {
                            case ".MSD":
                                try
                                {
                                    string txt = System.IO.File.ReadAllText(file);
                                    StringReader rs = new StringReader(txt);

                                    string ftname = rs.ReadLine();
                                    if (!string.Equals(timbreType.FullName, ftname, StringComparison.Ordinal))
                                        return;
                                    string ver = rs.ReadLine();
                                    if (ver != "1.0")
                                        return;

                                    string serializeData = rs.ReadToEnd();

                                    TimbreBase t = (TimbreBase)JsonConvert.DeserializeObject(serializeData, timbreType);
                                    TimbreItem tim = new TimbreItem(t, no);
                                    no++;

                                    var lvi = new ListViewItem(new string[] { i.ToString(), tim.Timbre.TimbreName, tim.Timbre.Memo });
                                    lvi.Tag = tim;
                                    items.Add(lvi);
                                }
                                catch (Exception ex)
                                {
                                    if (ex.GetType() == typeof(Exception))
                                        throw;
                                    else if (ex.GetType() == typeof(SystemException))
                                        throw;

                                }
                                break;
                            case ".MSDS":
                                {
                                    string txt = System.IO.File.ReadAllText(file);
                                    StringReader rs = new StringReader(txt);

                                    string ftname = rs.ReadLine();
                                    if (ftname == timbreType.FullName)
                                    {
                                        string ver = rs.ReadLine();
                                        if (ver != "1.0")
                                            throw new InvalidDataException();
                                        int num = int.Parse(rs.ReadLine());
                                        StringBuilder lines = new StringBuilder();
                                        while (true)
                                        {
                                            try
                                            {
                                                string line = rs.ReadLine();
                                                if (line == null || line == "-")
                                                {
                                                    if (lines.Length == 0)
                                                        break;

                                                    TimbreBase t = (TimbreBase)JsonConvert.DeserializeObject(lines.ToString(), timbreType);
                                                    TimbreItem tim = new TimbreItem(t, no);

                                                    var lvi = new ListViewItem(new string[] { no.ToString(), tim.Timbre.TimbreName, tim.Timbre.Memo });
                                                    no++;
                                                    lvi.Tag = tim;
                                                    items.Add(lvi);

                                                    lines.Clear();
                                                    if (line == null)
                                                        break;
                                                    continue;
                                                }
                                                lines.AppendLine(line);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex.GetType() == typeof(Exception))
                                                    throw;
                                                else if (ex.GetType() == typeof(SystemException))
                                                    throw;

                                            }
                                        }
                                    }
                                    break;
                                }
                            case ".MUC":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Muc.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".DAT":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Dat.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".MWI":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Fmp.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".MML":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Pmd.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".FXB":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Vopm.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".GWI":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Gwi.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".BNK":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = BankReader.Read(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".SYX":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = SyxReaderTX81Z.Read(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".FF":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = FF.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".FFOPM":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = FF.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".VGI":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Vgi.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            case ".TFI":
                                if (Instrument.CanImportToneFile)
                                {
                                    var tones = Tfi.Reader(file);
                                    no = loadTones(no, tones);
                                }
                                break;
                            default:
                                if (Instrument.CanImportBinFile)
                                {
                                    var texts = Instrument.SupportedBinExts.Split(';');
                                    for (int ei = 0; ei < texts.Length; ei++)
                                    {
                                        String ext = texts[ei].Replace("*", "");
                                        if (Path.GetExtension(file).ToUpper().Equals(ext.ToUpper(CultureInfo.InvariantCulture)))
                                        {
                                            try
                                            {
                                                TimbreBase t = (TimbreBase)Activator.CreateInstance(timbreType);
                                                Instrument.ImportBinFile(t, new FileInfo(file));
                                                var tim = new TimbreItem(t, no);

                                                var lvi = new ListViewItem(new string[] { no.ToString(), t.TimbreName, t.Memo });
                                                no++;
                                                lvi.Tag = tim;
                                                items.Add(lvi);
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex.GetType() == typeof(Exception))
                                                    throw;
                                                else if (ex.GetType() == typeof(SystemException))
                                                    throw;

                                                System.Windows.Forms.MessageBox.Show(ex.Message);
                                            }
                                        }
                                    }
                                }
                                if (Instrument.CustomToneImporter != null)
                                {
                                    var texts = Instrument.CustomToneImporter.ExtensionsFilterExt.Split(';');
                                    for (int ei = 0; ei < texts.Length; ei++)
                                    {
                                        String ext = texts[ei].Replace("*", "");
                                        if (Path.GetExtension(file).ToUpper().Equals(ext.ToUpper(CultureInfo.InvariantCulture)))
                                        {
                                            try
                                            {
                                                var tims = Instrument.CustomToneImporter.ImportToneFileAsTimbre(file);
                                                if (tims != null)
                                                {
                                                    foreach (var t in tims)
                                                    {
                                                        var tim = new TimbreItem(t, no);

                                                        var lvi = new ListViewItem(new string[] { no.ToString(), t.TimbreName, t.Memo });
                                                        no++;
                                                        lvi.Tag = tim;
                                                        items.Add(lvi);
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                if (ex.GetType() == typeof(Exception))
                                                    throw;
                                                else if (ex.GetType() == typeof(SystemException))
                                                    throw;

                                                System.Windows.Forms.MessageBox.Show(ex.Message);
                                            }
                                            break;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    listViewFilesTimbres.Items.AddRange(items.ToArray());
                    if (listViewFilesTimbres.Items.Count > 0)
                    {
                        listViewFilesTimbres.Items[0].Selected = true;
                        lastFocusedListView = listViewFilesTimbres;
                    }
                }
                finally
                {
                    foreach (ColumnHeader c in listViewFilesTimbres.Columns)
                        c.Width = -1;

                    listViewFilesTimbres.EndUpdate();
                }
                //fileFolderList1.SelectedPath
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileFolderList1_SelectedIndexChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected)
                return;

            fileFolderListItemSelectionChanged = true;
        }

        private int loadTones(int no, IEnumerable<Tone> tones)
        {
            foreach (var tns in tones)
            {
                TimbreBase t = (TimbreBase)Activator.CreateInstance(timbreType);
                Instrument.ImportToneFile(t, tns);
                var tim = new TimbreItem(t, no);

                var lvi = new ListViewItem(new string[] { no.ToString(), t.TimbreName, t.Memo });
                no++;
                lvi.Tag = tim;
                listViewFilesTimbres.Items.Add(lvi);
            }

            return no;
        }

        //internal static string NormalizePath(string path, bool fullCheck)

        private bool ignore_metroTextBox1_TextChanged;

        private void metroTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (ignore_metroTextBox1_TextChanged)
                return;

            try
            {
                string dir = comboBoxDir.Text;
                if (System.IO.Directory.Exists(dir))
                {
                    fileFolderList1.Browse(dir);

                    dir = updateDirMRU(dir);
                }
            }
            catch { }
        }

        private string updateDirMRU(string dir)
        {
            MethodInfo mi = typeof(Path).GetMethod("NormalizePath", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(String), typeof(Boolean) }, null);
            dir = (String)mi.Invoke(null, new object[] { dir, true });
            if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                dir += Path.DirectorySeparatorChar.ToString();

            if (Settings.Default.ToneLibMRU.Contains(dir))
                Settings.Default.ToneLibMRU.Remove(dir);
            Settings.Default.ToneLibMRU.Insert(0, dir);
            for (; Settings.Default.ToneLibMRU.Count > 10;)
                Settings.Default.ToneLibMRU.RemoveAt(10);

            comboBoxDir.Items.Clear();
            foreach (String item in Settings.Default.ToneLibMRU)
                comboBoxDir.Items.Add(item);
            return dir;
        }


        /// <summary>
        /// 
        /// </summary>
        private class TimbreItem
        {
            public TimbreBase Timbre
            {
                get;
                private set;
            }

            public int Number
            {
                get;
                private set;
            }

            public TimbreItem(TimbreBase timbre, int number)
            {
                Timbre = timbre;
                Number = number;
            }

            public override string ToString()
            {
                return "No " + Number.ToString() + " " + Timbre.TimbreName + " " + Timbre.Memo;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class DrumTimbreItem
        {
            public DrumTimbre Timbre
            {
                get;
                private set;
            }

            public int Number
            {
                get;
                private set;
            }

            public DrumTimbreItem(DrumTimbre timbre, int number)
            {
                Timbre = timbre;
                Number = number;
            }

            public override string ToString()
            {
                return "No " + Number.ToString() + " " + Timbre.TimbreName + " " + Timbre.KeyName;
            }
        }


        private void listViewFilesTimbres_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ListViewItem[] items = new ListViewItem[listViewFilesTimbres.SelectedItems.Count];
                listViewFilesTimbres.SelectedItems.CopyTo(items, 0);
                listViewFilesTimbres.DoDragDrop(items, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        private void listViewFilesTimbres_DragDrop(object sender, DragEventArgs e)
        {

        }

        private void listViewFilesTimbres_DragOver(object sender, DragEventArgs e)
        {

        }

        private void listViewFilesTimbres_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void listViewCurrentTimbres_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ListViewItem[] items = new ListViewItem[listViewCurrentTimbres.SelectedItems.Count];
                listViewCurrentTimbres.SelectedItems.CopyTo(items, 0);
                listViewCurrentTimbres.DoDragDrop(items, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        private void listViewCurrentTimbres_DragDrop(object sender, DragEventArgs e)
        {
            var items = e.Data.GetData(typeof(ListViewItem[])) as ListViewItem[];
            if (items != null && items.Length != 0)
            {
                Point cp = listViewCurrentTimbres.PointToClient(new Point(e.X, e.Y));
                ListViewItem dragToItem = listViewCurrentTimbres.GetItemAt(cp.X, cp.Y);
                if (dragToItem != null)
                {
                    bool exchange = true;
                    if (listViewCurrentTimbres != items[0].ListView || (e.KeyState & 8) == 8)
                        exchange = false;

                    bool setDrumTimbre = checkBoxSetDrumKeyNo.Checked;
                    bool setDrumTimbreName = checkBoxSetDrumTimbreName.Checked;
                    int drumStartIdx = dragToItem.Index;
                    for (int i = dragToItem.Index; i < Math.Min(dragToItem.Index + items.Length, listViewCurrentTimbres.Items.Count); i++)
                    {
                        TimbreItem tim = items[i - dragToItem.Index].Tag as TimbreItem;
                        if (tim != null)
                        {
                            string ss = tim.Timbre.SerializeData;
                            TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.Items[i].Tag);
                            if (exchange)
                            {
                                string tss = ttim.Timbre.SerializeData;

                                ttim.Timbre.SerializeData = ss;
                                tim.Timbre.SerializeData = tss;

                                listViewCurrentTimbres.Items[tim.Number].SubItems[1].Text = tim.Timbre.TimbreName;
                                listViewCurrentTimbres.Items[tim.Number].SubItems[2].Text = tim.Timbre.Memo;
                            }
                            else
                            {
                                ttim.Timbre.SerializeData = ss;

                                if (listViewCurrentTimbres != items[0].ListView)
                                {
                                    if (setDrumTimbre)
                                    {
                                        int idx = comboBoxDrumKeyNo.SelectedIndex + drumStartIdx;
                                        if (idx <= 127)
                                        {
                                            this.Instrument.DrumTimbres[idx].TimbreNumber = (ProgramAssignmentNumber)ttim.Number;
                                            if (setDrumTimbreName)
                                            {
                                                this.Instrument.DrumTimbres[idx].TimbreName = ttim.Timbre.TimbreName;
                                            }
                                            drumStartIdx++;
                                        }
                                    }
                                }
                            }
                            listViewCurrentTimbres.Items[i].SubItems[1].Text = ttim.Timbre.TimbreName;
                            listViewCurrentTimbres.Items[i].SubItems[2].Text = ttim.Timbre.Memo;
                        }
                    }
                }
            }
        }

        private void listViewCurrentTimbres_DragOver(object sender, DragEventArgs e)
        {
            var items = e.Data.GetData(typeof(ListViewItem[])) as ListViewItem[];
            if (items != null && items.Length != 0)
            {
                if (listViewCurrentTimbres != items[0].ListView || (e.KeyState & 8) == 8)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.Move;
                try
                {
                    ignorePlayingFlag++;
                    ignoreMetroComboBoxTimbres_SelectedIndexChanged = true;
                    listViewCurrentTimbres.SelectedItems.Clear();

                    Point cp = listViewCurrentTimbres.PointToClient(new Point(e.X, e.Y));
                    ListViewItem dragToItem = listViewCurrentTimbres.GetItemAt(cp.X, cp.Y);
                    if (dragToItem != null)
                    {
                        var index = dragToItem.Index;
                        var maxIndex = listViewCurrentTimbres.Items.Count - 1;
                        var scrollZoneHeight = listViewCurrentTimbres.Font.Height;
                        if (index > 0 && cp.Y < scrollZoneHeight)
                            listViewCurrentTimbres.Items[index - 1].EnsureVisible();
                        else if (index < maxIndex && cp.Y > listViewCurrentTimbres.Height - scrollZoneHeight)
                            listViewCurrentTimbres.Items[index + 1].EnsureVisible();

                        for (int i = dragToItem.Index; i < Math.Min(dragToItem.Index + items.Length, listViewCurrentTimbres.Items.Count); i++)
                            listViewCurrentTimbres.Items[i].Selected = true;
                    }
                }
                finally
                {
                    ignorePlayingFlag--;
                    ignoreMetroComboBoxTimbres_SelectedIndexChanged = false;
                }
            }
        }

        private void metroButtonNewDir_Click(object sender, EventArgs e)
        {
            using (var f = new FormRename())
            {
                var result = f.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(f.InputText))
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(fileFolderList1.CurrentDirectory, f.InputText));
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(Exception))
                                throw;
                            else if (ex.GetType() == typeof(SystemException))
                                throw;

                            MessageBox.Show(ex.Message);
                        }
                        fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
                    }
                }
            }
        }

        private void metroButtonDelete_Click(object sender, EventArgs e)
        {
            if (fileFolderList1.SelectedPaths.Length == 0)
                return;

            var result = MessageBox.Show(this, "Are you sure you want to delete selected files/directories?",
                "Qeuestion", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.OK)
            {
                for (int i = 0; i < fileFolderList1.SelectedPaths.Length; i++)
                {
                    try
                    {
                        string path = Path.Combine(fileFolderList1.CurrentDirectory, fileFolderList1.SelectedPaths[i]);
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path);
                        }
                        else if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                        MessageBox.Show(ex.Message);
                    }
                }
                fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
            }
        }

        private void metroButtonExport_Click(object sender, EventArgs e)
        {
            if (listViewCurrentTimbres.SelectedItems.Count == 0)
                return;

            if (listViewCurrentTimbres.SelectedItems.Count == 1)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.SelectedItems[0].Tag);

                    string dir = fileFolderList1.CurrentDirectory;
                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        dir = Path.Combine(dir, "MAmi");
                    }
                    saveFileDialog.InitialDirectory = dir;

                    saveFileDialog.DefaultExt = "*.msd";
                    saveFileDialog.Filter = "MAmi Serialize Data File(*.msd)|*.msd";
                    string fname = null;
                    try
                    {
                        fname = ttim.Timbre.TimbreName;
                    }
                    catch (Exception ex1)
                    {
                        if (ex1.GetType() == typeof(Exception))
                            throw;
                        else if (ex1.GetType() == typeof(SystemException))
                            throw;

                        try
                        {
                            if (ttim.Timbre.Memo?.Trim() != null)
                            {
                                StringReader rs = new StringReader(ttim.Timbre.Memo);
                                while (rs.Peek() > -1)
                                {
                                    fname = rs.ReadLine();
                                    break;
                                }
                            }
                        }
                        catch (Exception ex2)
                        {
                            if (ex2.GetType() == typeof(Exception))
                                throw;
                            else if (ex2.GetType() == typeof(SystemException))
                                throw;
                        }
                    }

                    fname = fname?.Trim();
                    if (string.IsNullOrWhiteSpace(fname))
                        fname = timbreType.Name + ".mds";

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                        fname = fname.Replace(invalidChar.ToString(), "");

                    fname = Path.ChangeExtension(fname, ".msd");

                    saveFileDialog.FileName = Utility.MakeUniqueFileName(dir, fname);
                    saveFileDialog.SupportMultiDottedExtensions = true;

                    DialogResult res = saveFileDialog.ShowDialog(this);
                    if (res == DialogResult.OK)
                    {
                        fname = saveFileDialog.FileName;
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(timbreType.FullName);
                        sb.AppendLine("1.0");

                        TimbreItem ttimi = (TimbreItem)(listViewCurrentTimbres.SelectedItems[0].Tag);
                        string sd = ttimi.Timbre.SerializeData;
                        sb.AppendLine(sd);

                        try
                        {
                            File.WriteAllText(fname, sb.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(Exception))
                                throw;
                            else if (ex.GetType() == typeof(SystemException))
                                throw;

                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
            else
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.SelectedItems[0].Tag);

                    string dir = fileFolderList1.CurrentDirectory;
                    if (string.IsNullOrWhiteSpace(dir))
                    {
                        dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        dir = Path.Combine(dir, "MAmi");
                    }
                    saveFileDialog.InitialDirectory = dir;

                    saveFileDialog.DefaultExt = "*.msds";
                    saveFileDialog.Filter = "MAmi Serialize Data Files(*.msds)|*.msds";
                    string fname = null;
                    try
                    {
                        fname = ttim.Timbre.TimbreName;
                    }
                    catch (Exception ex1)
                    {
                        if (ex1.GetType() == typeof(Exception))
                            throw;
                        else if (ex1.GetType() == typeof(SystemException))
                            throw;

                        try
                        {
                            if (ttim.Timbre.Memo?.Trim() != null)
                            {
                                StringReader rs = new StringReader(ttim.Timbre.Memo);
                                while (rs.Peek() > -1)
                                {
                                    fname = rs.ReadLine();
                                    break;
                                }
                            }
                        }
                        catch (Exception ex2)
                        {
                            if (ex2.GetType() == typeof(Exception))
                                throw;
                            else if (ex2.GetType() == typeof(SystemException))
                                throw;
                        }
                    }

                    fname = fname?.Trim();
                    if (string.IsNullOrWhiteSpace(fname))
                        fname = timbreType.Name + ".msds";

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                        fname = fname.Replace(invalidChar.ToString(), "");

                    fname = Path.ChangeExtension(fname, ".msds");

                    saveFileDialog.FileName = Utility.MakeUniqueFileName(dir, fname);
                    saveFileDialog.SupportMultiDottedExtensions = true;

                    DialogResult res = saveFileDialog.ShowDialog(this);
                    if (res == DialogResult.OK)
                    {
                        fname = saveFileDialog.FileName;
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(timbreType.FullName);
                        sb.AppendLine("1.0");
                        sb.AppendLine(listViewCurrentTimbres.SelectedItems.Count.ToString());

                        for (int i = 0; i < listViewCurrentTimbres.SelectedItems.Count; i++)
                        {
                            TimbreItem ttimi = (TimbreItem)(listViewCurrentTimbres.SelectedItems[i].Tag);
                            string sd = ttimi.Timbre.SerializeData;
                            sb.AppendLine(sd);
                            sb.AppendLine("-");
                        }

                        try
                        {
                            File.WriteAllText(fname, sb.ToString());
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(Exception))
                                throw;
                            else if (ex.GetType() == typeof(SystemException))
                                throw;

                            MessageBox.Show(ex.Message);
                        }
                    }
                }

                /*
                bool? overwrite = null;
                for (int i = 0; i < listViewCurrentTimbres.SelectedItems.Count; i++)
                {
                    TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.SelectedItems[i].Tag);
                    string sd = ttim.Timbre.SerializeData;

                    StringBuilder serializeData = new StringBuilder();
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(timbreType.FullName);
                    sb.AppendLine("1.0");
                    sb.AppendLine(sd);

                    string fname = ttim.Timbre.TimbreName;
                    if (string.IsNullOrEmpty(fname))
                    {
                        if (ttim.Timbre.Memo?.Trim() != null)
                        {
                            StringReader rs = new StringReader(ttim.Timbre.Memo);
                            while (rs.Peek() > -1)
                            {
                                fname = rs.ReadLine();
                                break;
                            }
                        }
                    }

                    fname = fname?.Trim();
                    if (string.IsNullOrWhiteSpace(fname))
                        fname = timbreType.FullName;

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                        fname = fname.Replace(invalidChar.ToString(), "");

                    fname = Path.ChangeExtension(fname, ".msd");

                    fname = Path.Combine(fileFolderList1.CurrentDirectory, fname);
                    if (File.Exists(fname))
                    {
                        if (overwrite == null)
                        {
                            var result = MessageBox.Show(this, "Are you sure you want to overrite files?", "Qeuestion", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                            if (result == DialogResult.OK)
                                overwrite = true;
                            else
                                overwrite = false;
                        }
                        if (!overwrite.HasValue || !overwrite.Value)
                            continue;
                    }
                    try
                    {
                        File.WriteAllText(fname, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                        MessageBox.Show(ex.Message);
                    }
                }*/
            }
            fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
        }

        private ListView lastFocusedListView;

        private void listViewFilesTimbres_Enter(object sender, EventArgs e)
        {
            lastFocusedListView = listViewFilesTimbres;
        }

        private void listViewCurrentTimbres_Enter(object sender, EventArgs e)
        {
            lastFocusedListView = listViewCurrentTimbres;
        }

        private void fileFolderList1_CurrentDirectoryChanged(object sender, EventArgs e)
        {
            try
            {
                ignore_metroTextBox1_TextChanged = true;
                comboBoxDir.Text = fileFolderList1.CurrentDirectory;

                updateDirMRU(fileFolderList1.CurrentDirectory);
            }
            finally
            {
                ignore_metroTextBox1_TextChanged = false;
            }
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            betterFolderBrowser1.RootFolder = Settings.Default.ToneLibLastDir;

            var dr = betterFolderBrowser1.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                comboBoxDir.Text = betterFolderBrowser1.SelectedFolder;
                Settings.Default.ToneLibLastDir = betterFolderBrowser1.SelectedFolder;
            }
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.Items[0].Tag);

                string dir = fileFolderList1.CurrentDirectory;
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    dir = Path.Combine(dir, "MAmi");
                }
                saveFileDialog.InitialDirectory = dir;

                saveFileDialog.DefaultExt = "*.msds";
                saveFileDialog.Filter = "MAmi Serialize Data Files(*.msds)|*.msds";
                string fname = null;
                try
                {
                    fname = ttim.Timbre.TimbreName;
                }
                catch (Exception ex1)
                {
                    if (ex1.GetType() == typeof(Exception))
                        throw;
                    else if (ex1.GetType() == typeof(SystemException))
                        throw;

                    try
                    {
                        if (ttim.Timbre.Memo?.Trim() != null)
                        {
                            StringReader rs = new StringReader(ttim.Timbre.Memo);
                            while (rs.Peek() > -1)
                            {
                                fname = rs.ReadLine();
                                break;
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        if (ex2.GetType() == typeof(Exception))
                            throw;
                        else if (ex2.GetType() == typeof(SystemException))
                            throw;
                    }
                }

                fname = fname?.Trim();
                if (string.IsNullOrWhiteSpace(fname))
                    fname = timbreType.Name + ".msds";

                foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    fname = fname.Replace(invalidChar.ToString(), "");

                fname = Path.ChangeExtension(fname, ".msds");

                saveFileDialog.FileName = Utility.MakeUniqueFileName(dir, fname);
                saveFileDialog.SupportMultiDottedExtensions = true;

                DialogResult res = saveFileDialog.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    fname = saveFileDialog.FileName;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(timbreType.FullName);
                    sb.AppendLine("1.0");
                    sb.AppendLine(listViewCurrentTimbres.Items.Count.ToString());

                    for (int i = 0; i < listViewCurrentTimbres.Items.Count; i++)
                    {
                        TimbreItem ttimi = (TimbreItem)(listViewCurrentTimbres.Items[i].Tag);
                        string sd = ttimi.Timbre.SerializeData;
                        sb.AppendLine(sd);
                        sb.AppendLine("-");
                    }

                    try
                    {
                        File.WriteAllText(fname, sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                        MessageBox.Show(ex.Message);
                    }
                }
            }
            try
            {
                fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                MessageBox.Show(ex.Message);
            }
        }

        private void metroButtonRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                fileFolderList1.Browse(fileFolderList1.CurrentDirectory);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                MessageBox.Show(ex.Message);
            }
        }

        private void metroButtonExplorer_Click(object sender, EventArgs e)
        {
            String path = null;
            var dlDestFilePath = fileFolderList1.SelectedPaths;
            if (dlDestFilePath.Length != 0)
                path = dlDestFilePath[0];
            else
                path = fileFolderList1.CurrentDirectory;

            Task.Run(new Action(() =>
            {
                Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void metroButtonClear_Click(object sender, EventArgs e)
        {
            if (listViewCurrentTimbres.SelectedItems.Count == 0)
                return;

            var result = MessageBox.Show(this, "Are you sure you want to reset selected Timbres to default?",
    "Qeuestion", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.OK)
            {
                for (int i = 0; i < listViewCurrentTimbres.SelectedItems.Count; i++)
                {
                    TimbreItem ttim = (TimbreItem)(listViewCurrentTimbres.SelectedItems[i].Tag);
                    ttim.Timbre.SerializeData = ((TimbreBase)Activator.CreateInstance(timbreType)).SerializeData;
                    //ttim = new TimbreItem((TimbreBase)Activator.CreateInstance(timbreType), ttim.Number);
                    //listViewCurrentTimbres.SelectedItems[i].Tag = ttim;
                    listViewCurrentTimbres.SelectedItems[i].SubItems[1].Text = ttim.Timbre.TimbreName;
                    listViewCurrentTimbres.SelectedItems[i].SubItems[2].Text = ttim.Timbre.Memo;
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            metroButtonClear_Click(sender, e);
        }


        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            ListViewHitTestInfo info = listViewCurrentTimbres.HitTest(listViewCurrentTimbres.PointToClient(contextMenuStrip1.Bounds.Location));

            TimbreItem ttim = (TimbreItem)info.Item.Tag;

            editToolStripMenuItem.Enabled = ttim.Timbre.CanOpenTimbreEditor(Instrument);
        }

        private void listViewCurrentTimbres_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = listViewCurrentTimbres.HitTest(e.Location);
            if (info.Item != null)
            {
                TimbreItem ttim = (TimbreItem)info.Item.Tag;

                testStop();

                ttim.Timbre.OpenTimbreEditor(Instrument);
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewHitTestInfo info = listViewCurrentTimbres.HitTest(listViewCurrentTimbres.PointToClient(contextMenuStrip1.Bounds.Location));
            if (info.SubItem == null)
                return;
            if (info.Item.SubItems[0] == info.SubItem)
                return;

            TimbreItem ttim = (TimbreItem)info.Item.Tag;
            using (var f = new FormRename())
            {
                if (info.Item.SubItems[1] == info.SubItem)
                {
                    f.InputText = ttim.Timbre.TimbreName;
                    f.TitleText = "Specify the Timbre name";
                }
                else if (info.Item.SubItems[2] == info.SubItem)
                {
                    f.InputText = ttim.Timbre.Memo;
                    f.TitleText = "Specify the Timbre memo";
                }

                var dr = f.ShowDialog(this);
                if (dr == DialogResult.OK)
                {
                    info.SubItem.Text = f.InputText;
                    if (info.Item.SubItems[1] == info.SubItem)
                        ttim.Timbre.TimbreName = f.InputText;
                    else if (info.Item.SubItems[2] == info.SubItem)
                        ttim.Timbre.Memo = f.InputText;
                }
            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewHitTestInfo info = listViewCurrentTimbres.HitTest(listViewCurrentTimbres.PointToClient(contextMenuStrip1.Bounds.Location));
            if (info.Item != null)
            {
                TimbreItem ttim = (TimbreItem)info.Item.Tag;

                testStop();

                ttim.Timbre.OpenTimbreEditor(Instrument);
            }
        }

        private void propertyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListViewHitTestInfo info = listViewCurrentTimbres.HitTest(listViewCurrentTimbres.PointToClient(contextMenuStrip1.Bounds.Location));
            if (info.Item != null)
            {
                TimbreItem ttim = (TimbreItem)info.Item.Tag;

                testStop();

                ttim.Timbre.OpenPropEditor(Instrument);
            }
        }

        private void fileFolderList1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                try
                {
                    fileFolderList1.BeginUpdate();
                    foreach (ListViewItem item in fileFolderList1.Items)
                    {
                        item.Selected = true;
                    }
                }
                finally
                {
                    fileFolderList1.EndUpdate();
                }
            }
        }

        private void listViewFilesTimbres_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                try
                {
                    listViewFilesTimbres.BeginUpdate();
                    foreach (ListViewItem item in listViewFilesTimbres.Items)
                    {
                        item.Selected = true;
                    }
                }
                finally
                {
                    listViewFilesTimbres.EndUpdate();
                }
            }
        }

        private void listViewCurrentTimbres_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                try
                {
                    listViewCurrentTimbres.BeginUpdate();
                    foreach (ListViewItem item in listViewCurrentTimbres.Items)
                    {
                        item.Selected = true;
                    }
                }
                finally
                {
                    listViewCurrentTimbres.EndUpdate();
                }
            }
        }
    }
}
