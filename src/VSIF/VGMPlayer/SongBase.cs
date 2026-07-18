using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using zanac.VGMPlayer.Properties;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static zanac.VGMPlayer.FormMain;
using Timer = System.Timers.Timer;

namespace zanac.VGMPlayer
{
    public abstract class SongBase : IDisposable
    {
        public event EventHandler ProcessLoadOccurred;

        public event EventHandler DacClipOccurred;

        public event EventHandler SpeedChanged;

        public event EventHandler PlayStatusChanged;

        public event EventHandler Finished;

        public bool CanLoadCoverArt
        {
            get; set;
        }

        public String CoverArtFile
        {
            get; set;
        }

        public bool DeleteCoverArtFileAfterStop
        {
            get; set;
        }

        public bool DeleteFileAfterStop
        {
            get; set;
        }

        public bool TemporarySongFile
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string LastLoadedCoverArtFileName
        {
            get;
            private set;
        }

        internal void NotifyProcessLoadOccurred()
        {
            HighLoad = true;
            ProcessLoadOccurred?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifyDacClipOccurred()
        {
            DacClip = true;
            DacClipOccurred?.Invoke(this, EventArgs.Empty);
        }


        protected void NotifyFinished()
        {
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private SoundState state = SoundState.Stopped;

        /// <summary>
        /// 
        /// </summary>
        public virtual SoundState State
        {
            get
            {
                return state;
            }
            protected set
            {
                if (state != value)
                {
                    state = value;
                    PlayStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected SoundState RequestedStat
        {
            set;
            get;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void SetStateRequest(SoundState state)
        {
            RequestedStat = state;
            while (State != state)
                Thread.Sleep(1);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool ConvertChipClock
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool LoopByCount
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int LoopedCount
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual int CurrentLoopedCount
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool LoopByElapsed
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual TimeSpan LoopTimes
        {
            get;
            set;
        }

        private double _playbackSpeed = 1.0f;

        /// <summary>
        /// 
        /// </summary>
        public virtual double PlaybackSpeed
        {
            get
            {
                return _playbackSpeed;
            }
            set
            {
                _playbackSpeed = Math.Min(Math.Max(value, 0.1), 4);
                SpeedChanged.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }

        public bool Accepted
        {
            get;
            protected set;
        }

        public String SongChipInformation
        {
            get;
            protected set;
        }

        public String UseChipInformation
        {
            get;
            protected set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        protected SongBase(string fileName)
        {
            FileName = fileName;
        }

        private Timer playTicTimer;

        private Stopwatch stopwatch;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        private static void drawScaledImage(Graphics g, Bitmap bmp, int maxWidth, int maxHeight)
        {
            //https://efundies.com/scale-an-image-in-c-sharp-preserving-aspect-ratio/
            var ratioX = (double)maxWidth / bmp.Width;
            var ratioY = (double)maxHeight / bmp.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(bmp.Width * ratio);
            var newHeight = (int)(bmp.Height * ratio);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, (maxWidth - newWidth) / 2, (maxHeight - newHeight) / 2, newWidth, newHeight);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Play()
        {
            if (!Accepted)
            {
                FormMain.TopForm.SetStatusText($"The {Path.GetFileName(FileName)} could not be played due to the lack of a compatible chip.");
                NotifyFinished();
                return;
            }

            if (State == SoundState.Playing)
                return;

            if (CanLoadCoverArt && Program.Default.ShowCoverArt)
            {
                LoadCoverArt();
            }

            playTicTimer = new Timer(250);
            playTicTimer.Elapsed += LoopTimer_Elapsed;
            //playTicTimer.Enabled = true;

            stopwatch = new Stopwatch();

            Thread t = new Thread(new ThreadStart(StreamSong));
            t.Priority = ThreadPriority.AboveNormal;
            t.Start();
            playTicTimer.Start();
            stopwatch.Start();

            FormMain.TopForm.SetElapsedTime(new TimeSpan(0));
            FormMain.TopForm.SetStatusText("Playing");
        }


        /// <summary>
        /// 
        /// </summary>
        protected void LoadCoverArt()
        {
            Bitmap img = null;
            Bitmap canvas = null;
            Graphics gp = null;
            try
            {
                string cfile = null;
                String fn = null;
                if (CoverArtFile != null)
                {
                    cfile = CoverArtFile;
                    if (File.Exists(cfile))
                    {
                        img = new Bitmap(cfile);
                        fn = cfile;
                    }
                }
                else
                {
                    cfile = Path.ChangeExtension(FileName, ".png");
                    if (File.Exists(cfile))
                    {
                        img = new Bitmap(cfile);
                        fn = cfile;
                    }
                    else
                    {
                        cfile = Path.ChangeExtension(FileName, ".jpg");
                        if (File.Exists(cfile))
                        {
                            img = new Bitmap(cfile);
                            fn = cfile;
                        }
                    }
                }
                if (img == null)
                {
                    var cfiles = Directory.GetFiles(Path.GetDirectoryName(FileName), "*.png");
                    if (cfiles.Length != 0 && File.Exists(cfiles[0]))
                    {
                        img = new Bitmap(cfiles[0]);
                        fn = cfiles[0];
                    }
                    else
                    {
                        cfiles = Directory.GetFiles(Path.GetDirectoryName(FileName), "*.jpg");
                        if (cfiles.Length != 0 && File.Exists(cfiles[0]))
                        {
                            img = new Bitmap(cfiles[0]);
                            fn = cfiles[0];
                        }
                    }
                }

                if (img != null && !String.Equals(LastLoadedCoverArtFileName, fn, StringComparison.OrdinalIgnoreCase))
                {
                    bool completed = false;
                    foreach (var c in VsifManager.GetVsifClients())
                    {
                        switch (c.SoundModuleType)
                        {
                            //case VsifSoundModuleType.MSX_FTDI:
                            case VsifSoundModuleType.TurboR_FTDI:
                            case VsifSoundModuleType.MSX_PiTR:
                                {
                                    bool cancelled = false;
                                    FormProgress.RunDialog("Loading cover art...", (pd) =>
                                    {
                                        canvas = new Bitmap(256, 212);
                                        gp = Graphics.FromImage(canvas);

                                        drawScaledImage(gp, img, 256, 212);
                                        const byte PORT0 = 0x98;
                                        const byte PORT1 = 0x99;
                                        const byte REGW = 0x80;
                                        const byte CMD = 0x3e;

                                        c.FlushDeferredWriteDataAndWait();

                                        int bbw = (int)(decimal)c.BitBangWait.GetValue(Settings.Default);

                                        //GRAPHIC7 mode
                                        c.WriteData(CMD, PORT1, 0x0e, bbw);
                                        c.WriteData(CMD, PORT1, REGW, bbw);
                                        c.WriteData(CMD, PORT1, 0x40, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 1, bbw);

                                        c.WriteData(CMD, PORT1, 0x80, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 9, bbw);
                                        //Set Pattern Name Table 0
                                        c.WriteData(CMD, PORT1, 0x1f, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 2, bbw);
                                        //Sprite Off
                                        c.WriteData(CMD, PORT1, 0x0a, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 8, bbw);
                                        // BG Black
                                        c.WriteData(CMD, PORT1, 0x00, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 7, bbw);
                                        //Set VRAM 0
                                        c.WriteData(CMD, PORT1, 0x00, bbw);
                                        c.WriteData(CMD, PORT1, REGW | 14, bbw);
                                        c.WriteData(CMD, PORT1, 0x00, bbw);
                                        c.WriteData(CMD, PORT1, 0x40, bbw);

                                        //c.FlushDeferredWriteDataAndWait();

                                        for (int y = 0; y < 212; y++)
                                        {
                                            for (int x = 0; x < 256; x++)
                                            {
                                                var col = canvas.GetPixel(x, y);
                                                // G(3),R(3),B(2)
                                                byte r = col.R;
                                                //if (y % 2 == 1 && r < 256 - 32)
                                                //    r += 32;
                                                byte g = col.G;
                                                //if (y % 2 == 0 && g < 256 - 32)
                                                //    g += 32;
                                                byte b = col.B;
                                                if (y % 2 == 1 && b < 256 - 32)
                                                    b += 32;
                                                byte data = (byte)(((g >> 5) << 5) | ((r >> 5) << 2) | ((b >> 6)));
                                                c.WriteData(CMD, PORT0, data, bbw);

                                                if (RequestedStat == SoundState.Stopped)
                                                    break;
                                            }
                                            //c.FlushDeferredWriteDataAndWait();
                                            FormMain.TopForm.BeginInvoke(new MethodInvoker(() =>
                                            {
                                                pd.Percentage = (100 * y / 212);
                                            }));
                                            if (RequestedStat == SoundState.Stopped)
                                                break;
                                            if (cancelled)
                                                break;
                                        }

                                        c.FlushDeferredWriteDataAndWait();
                                        LastLoadedCoverArtFileName = fn;
                                        completed = true;
                                    },
                                    () => { cancelled = true; });
                                }
                                break;
                        }
                        if (completed)
                            break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                gp?.Dispose();
                canvas?.Dispose();
                img?.Dispose();
            }
        }

        private void LoopTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            FormMain.TopForm.SetElapsedTime(stopwatch.Elapsed);
            if (stopwatch.Elapsed > LoopTimes && LoopByElapsed)
            {
                if (!LoopByCount || (LoopByCount && LoopedCount >= 0 && CurrentLoopedCount >= LoopedCount))
                {
                    Stop();
                    NotifyFinished();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Pause()
        {
            if (State == SoundState.Paused)
                return;

            SetStateRequest(SoundState.Paused);
            playTicTimer?.Stop();
            stopwatch?.Stop();
            FormMain.TopForm.SetStatusText("Paused");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Freeze()
        {
            if (State == SoundState.Freezed)
                return;

            SetStateRequest(SoundState.Freezed);
            playTicTimer?.Stop();
            stopwatch?.Stop();
            FormMain.TopForm.SetStatusText("Freezed");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Resume()
        {
            if (State == SoundState.Playing)
                return;

            SetStateRequest(SoundState.Playing);
            playTicTimer?.Start();
            stopwatch?.Start();
            FormMain.TopForm.SetStatusText("Resumed");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            if (State == SoundState.Stopped)
                return;

            SetStateRequest(SoundState.Stopped);
            playTicTimer?.Stop();
            stopwatch?.Stop();
            FormMain.TopForm.SetStatusText("Stopped");
            FormMain.TopForm.SetElapsedTime(new TimeSpan(0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="volumeOff"></param>
        abstract protected void StopAllSounds(bool volumeOff);

        abstract protected void StreamSong();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                playTicTimer?.Dispose();
                playTicTimer = null;

                if (DeleteFileAfterStop)
                    File.Delete(FileName);
                if (DeleteCoverArtFileAfterStop)
                    File.Delete(CoverArtFile);
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual bool HighLoad
        {
            get;
            set;
        }

        public virtual bool DacClip
        {
            get;
            set;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryPerformanceFrequency(out long frequency);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="frequency_as_hz"></param>
        /// <returns></returns>
        protected static double calcNoteNumberFromFrequency(double frequency)
        {
            return 12.0 * Math.Log(frequency / 440.0, 2) + 69.0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected static double calcFrequencyFromNoteNumber(double noteNum)
        {
            double nn = Math.Pow(2.0, (noteNum - 69.0) / 12.0);
            double freq = 440.0 * nn;
            return freq;
        }

        protected byte convertFromOpmNoteNum(int noteNum)
        {
            byte nn = 0;
            switch (noteNum)
            {
                case 14:
                    nn = 12;
                    break;
                case 0:
                    nn = 1;
                    break;
                case 1:
                    nn = 2;
                    break;
                case 2:
                    nn = 3;
                    break;
                case 4:
                    nn = 4;
                    break;
                case 5:
                    nn = 5;
                    break;
                case 6:
                    nn = 6;
                    break;
                case 8:
                    nn = 7;
                    break;
                case 9:
                    nn = 8;
                    break;
                case 10:
                    nn = 9;
                    break;
                case 12:
                    nn = 10;
                    break;
                case 13:
                    nn = 11;
                    break;
                default:
                    break;
            }

            return nn;
        }

        protected byte convertToOpmNoteNum(int noteNum)
        {
            byte nn = 0;
            switch (noteNum)
            {
                case 0:
                    nn = 14;
                    break;
                case 1:
                    nn = 0;
                    break;
                case 2:
                    nn = 1;
                    break;
                case 3:
                    nn = 2;
                    break;
                case 4:
                    nn = 4;
                    break;
                case 5:
                    nn = 5;
                    break;
                case 6:
                    nn = 6;
                    break;
                case 7:
                    nn = 8;
                    break;
                case 8:
                    nn = 9;
                    break;
                case 9:
                    nn = 10;
                    break;
                case 10:
                    nn = 12;
                    break;
                case 11:
                    nn = 13;
                    break;
            }

            return nn;
        }

        protected (byte Hi, byte Lo, bool noConverted) converSAA1099Frequency(int freqValueOct, int freqValueNote, double chipClock, double dataClock)
        {
            // freq = ((clock / 512) * (2 ^ octave)) / (511 - note)
            var dfreq = (dataClock / 512d) * Math.Pow(2, freqValueOct) / (511d - freqValueNote);

            var octave = (int)Math.Floor(calcNoteNumberFromFrequency(dfreq * dataClock / chipClock) / 12d) - 2;
            if (octave < 0)
                octave = 0;

            int note;
            do
            {
                // freq = ((clock / 512) * (2 ^ octave)) / (511 - note)
                // -> note = 511 - ((clock / 512) * (2 ^ octave))) / freq
                note = 511 - (int)Math.Round(((chipClock / 512d) * Math.Pow(2, octave)) / dfreq);
                if (note < 0)
                    octave--;
                else if (note > 255)
                    octave++;
            } while ((note > 255) || note < 0);

            if (octave < 0)
            {
                octave = 0;
                note = 0;
            }
            else if (octave > 7)
            {
                octave = 7;
                note = 0xff;
            }

            var ret = ((byte)octave, (byte)note, false);
            if (ret.Item1 == freqValueOct && ret.Item2 == freqValueNote)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) converRF5C164Frequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (int)Math.Round((freqValueHi << 8 | freqValueLo) * dataClock / chipClock);

            if (freq > 0xffff)
                freq = 0xffff;

            var ret = ((byte)(freq >> 8), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertAy8910Frequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (int)Math.Round((freqValueHi << 8 | freqValueLo) * chipClock / dataClock);

            if (freq > 0xfff)
                freq = 0xfff;

            var ret = ((byte)(freq >> 8), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertAy8910EnvFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (int)Math.Round((freqValueHi << 8 | freqValueLo) * chipClock / dataClock);

            if (freq > 0xffff)
                freq = 0xffff;

            var ret = ((byte)(freq >> 8), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertOpmFrequency(int KF, int KC, double chipClock, double dataClock)
        {
            var oct = (KC >> 4) & 0x7;
            var note = KC & 0xf;
            var kf = KF >> 2;

            var fout = calcFrequencyFromNoteNumber((oct * 12) + convertFromOpmNoteNum(note));
            fout += (calcFrequencyFromNoteNumber((oct * 12) + convertFromOpmNoteNum(note) + 1) - fout) * kf / 64d;

            //convert
            fout *= dataClock / chipClock;

            var noted = calcNoteNumberFromFrequency(fout);

            oct = (int)noted / 12;
            note = convertToOpmNoteNum((int)noted % 12);
            if (note == 14)
                oct -= 1;
            if (oct < 0)
            {
                oct = 0;
                note = 0;
            }
            if (oct > 7)
            {
                oct = 7;
                note = 14;
            }

            kf = (int)Math.Round((noted - (int)noted) * 64d);

            if (oct > 7)
            {
                oct = 7;
                note = 14;
            }

            var ret = ((byte)(kf << 2), (byte)((oct << 4) | note), false);
            if (ret.Item1 == KF && ret.Item2 == KC)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertOpnFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (freqValueHi << 8) | freqValueLo;
            var block = (freq >> 11) & 0x7;
            var fnum = freq & 0x7ff;

            fnum = (int)Math.Round(fnum * dataClock / chipClock);
            if (fnum > 0x7ff)
            {
                block++;
                fnum = (int)Math.Round(fnum / 2d);
            }
            if (block > 7)
            {
                block = 7;
                fnum = 0x7ff;
            }
            freq = (block << 11) | fnum;

            var ret = ((byte)(freq >> 8), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertDcsgFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (int)Math.Round((freqValueHi << 4 | freqValueLo) * chipClock / dataClock);
            if (freq > 0x3ff)
                freq = 0x3ff;

            var ret = ((byte)(freq >> 4), (byte)(freq & 0xf), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertOpllFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (freqValueHi << 8) | freqValueLo;
            var block = (freq >> 9) & 0x7;
            var fnum = freq & 0x1ff;
            var skon = freqValueHi & 0x30;

            fnum = (int)Math.Round(fnum * dataClock / chipClock);
            if (fnum > 0x1ff)
            {
                block++;
                fnum = (int)Math.Round(fnum / 2d);
            }
            if (block > 7)
            {
                block = 7;
                fnum = 0x1ff;
            }
            freq = (block << 9) | fnum;

            var ret = ((byte)((freq >> 8) | skon), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }


        protected (byte Hi, byte Lo, bool noConverted) convertOplFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (freqValueHi << 8) | freqValueLo;
            var block = (freq >> 10) & 0x7;
            var fnum = freq & 0x3ff;
            var kon = freqValueHi & 0x20;

            fnum = (int)Math.Round(fnum * dataClock / chipClock);
            if (fnum > 0x3ff)
            {
                block++;
                fnum = (int)Math.Round(fnum / 2d);
            }
            if (block > 7)
            {
                block = 7;
                fnum = 0x3ff;
            }
            freq = (block << 10) | fnum;

            var ret = ((byte)((freq >> 8) | kon), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }

        protected (byte Hi, byte Lo, bool noConverted) convertNesFrequency(int freqValueHi, int freqValueLo, double chipClock, double dataClock)
        {
            var freq = (int)Math.Round((freqValueHi << 8 | freqValueLo) * dataClock / chipClock);
            if (freq > 0x7ff)
                freq = 0x7ff;

            var ret = ((byte)(freq >> 8), (byte)(freq & 0xff), false);
            if (ret.Item1 == freqValueHi && ret.Item2 == freqValueLo)
                ret.Item3 = true;
            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="comPortOPNB"></param>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        /// <param name="dclk"></param>
        protected void deferredWriteOPNB_P0(VsifClient comPortOPNB, int adrs, int dt, uint dclk)
        {
            if (adrs == 7)
                dt &= 0x3f;
            comPortOPNB.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0:
                case 2:
                case 4:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910Frequency(comPortOPNB.RegTable[adrs + 1], dt, comPortOPNB.ChipClockHz["OPNB_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                        deferredWriteOPNB_P0(comPortOPNB, adrs + 1, ret.Hi);
                    }
                    break;
                case 1:
                case 3:
                case 5:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910Frequency(dt, comPortOPNB.RegTable[adrs - 1], comPortOPNB.ChipClockHz["OPNB_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNB_P0(comPortOPNB, adrs - 1, ret.Lo);
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                    }
                    break;
                case 6:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB_SSG"] == (double)dclk)
                        goto default;
                    {
                        var data = (int)Math.Round(dt * (dclk) / (double)comPortOPNB.ChipClockHz["OPNB_SSG"]);
                        if (data > 32)
                            data = 32;
                        deferredWriteOPNB_P0(comPortOPNB, adrs, (byte)data);
                    }
                    break;
                case 0xB:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910EnvFrequency(comPortOPNB.RegTable[adrs + 1], dt, comPortOPNB.ChipClockHz["OPNB_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                        deferredWriteOPNB_P0(comPortOPNB, adrs + 1, ret.Hi);
                    }
                    break;
                case 0xC:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910EnvFrequency(dt, comPortOPNB.RegTable[adrs - 1], comPortOPNB.ChipClockHz["OPNB_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNB_P0(comPortOPNB, adrs - 1, ret.Lo);
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                    }
                    break;

                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPNB.RegTable[adrs + 4], dt, comPortOPNB.ChipClockHz["OPNB"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNB_P0(comPortOPNB, adrs + 4, ret.Hi);
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPNB.RegTable[adrs - 4], comPortOPNB.ChipClockHz["OPNB"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                        deferredWriteOPNB_P0(comPortOPNB, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPNB_P0(comPortOPNB, adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPNB_P0(VsifClient comPortOPNB, int adrs, int dt)
        {
            switch (comPortOPNB.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    int type = (int)comPortOPNB.Tag["OPNB.Type"];
                    if (type == 0)
                    {
                        if (adrs == 0x28 && ((dt & 0x7) == 1 || (dt & 0x7) == 4))
                            return; //Skip OPNB Type 0
                    }
                    var slot = (int)0;
                    comPortOPNB.DeferredWriteData(0x18, (byte)SCCType.SIOS_OPNB, (byte)slot, 0);

                    switch (comPortOPNB.SoundModuleType)
                    {
                        case VsifSoundModuleType.MSX_Pi:
                            comPortOPNB.DeferredWriteData(0x1A, (byte)adrs, (byte)dt, 0);
                            break;
                        case VsifSoundModuleType.MSX_PiTR:
                            comPortOPNB.DeferredWriteData(0x3A, (byte)adrs, (byte)dt, -2);
                            break;
                    }
                    break;
            }
        }


        protected void deferredWriteOPNB_P1(VsifClient comPortOPNB, int adrs, int dt, uint dclk)
        {
            comPortOPNB.RegTable[adrs + 0x100] = dt;

            switch (adrs)
            {
                case 0x0e:

                    break;
                case 0xa0:
                case 0xa1:
                case 0xa2:
                case 0xa8:
                case 0xa9:
                case 0xaa:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertOpnFrequency(comPortOPNB.RegTable[adrs + 4 + 0x100], dt, comPortOPNB.ChipClockHz["OPNB"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNB_P1(comPortOPNB, adrs + 4, ret.Hi);
                        deferredWriteOPNB_P1(comPortOPNB, adrs, dt);
                    }
                    break;
                case 0xa4:
                case 0xa5:
                case 0xa6:
                case 0xac:
                case 0xad:
                case 0xae:
                    if (!ConvertChipClock || (double)comPortOPNB.ChipClockHz["OPNB"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertOpnFrequency(dt, comPortOPNB.RegTable[adrs - 4 + 0x100], comPortOPNB.ChipClockHz["OPNB"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNB_P1(comPortOPNB, adrs, dt);
                        deferredWriteOPNB_P1(comPortOPNB, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPNB_P1(comPortOPNB, adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPNB_P1(VsifClient comPortOPNB, int adrs, int dt)
        {
            switch (comPortOPNB.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    //int type = (int)comPortOPNB.Tag["OPNB.Type"];
                    //if (type == 0)
                    //{
                    //}
                    var slot = (int)0;
                    comPortOPNB.DeferredWriteData(0x18, (byte)SCCType.SIOS_OPNB, (byte)slot, 0);
                    switch (comPortOPNB.SoundModuleType)
                    {
                        case VsifSoundModuleType.MSX_Pi:
                            comPortOPNB.DeferredWriteData(0x1B, (byte)adrs, (byte)dt, 0);
                            break;
                        case VsifSoundModuleType.MSX_PiTR:
                            comPortOPNB.DeferredWriteData(0x3B, (byte)adrs, (byte)dt, -2);
                            break;
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
        protected void EnablePseudoDacYM2608(VsifClient comPort, bool enable)
        {
            deferredWriteOPNA_P1(comPort, 0x00, 3, 0x01);  //RESET
            if (enable)
            {
                //https://www.piece-me.org/piece-lab/adpcm/adpcm2.html
                //MAX Attenuation
                byte[] adpcmdata = new byte[32];
                for (int i = 0; i < adpcmdata.Length; i++)
                    adpcmdata[i] = 0x80;
                sendAdpcmDataYM2608(comPort, true, adpcmdata, 0, null);

                //*
                //ADPCM mode
                deferredWriteOPNA_P1(comPort, 0x10, 0x17);   //ENA FLAG BRDY
                deferredWriteOPNA_P1(comPort, 0x10, 0x80);   //RESET FLAGS
                deferredWriteOPNA_P1(comPort, 0x00, 0x80);   //CPU->OPNA
                deferredWriteOPNA_P1(comPort, 0x01, 0xC0);   //LR

                // (f / 55.5) * 65536
                // 8KHz = 9447
                //int f = (int)Math.Round((44.1 / 55.5) * 65536);
                //YM2608WriteData(comPort, 0x09, 0, 3, (byte)(f & 0xff), false);   //14KHz
                //YM2608WriteData(comPort, 0x0A, 0, 3, (byte)((f >> 8) & 0xff), false);   //14KHz

                deferredWriteOPNA_P1(comPort, 0x09, 0xff);   //55.5KHz
                deferredWriteOPNA_P1(comPort, 0x0A, 0xff);   //55.5KHz

                deferredWriteOPNA_P1(comPort, 0x0B, 0x00);   // Volume 0

                //playAdpcmDataYM2608(comPort, 0, 0, false);
                //Thread.Sleep(1);
                playAdpcmDataYM2608(comPort, 0, 0, false);

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
            comPort.FlushDeferredWriteDataAndWait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferData"></param>
        /// <param name="saddr"></param>
        /// <param name="fp"></param>
        protected void EnableDacYM2608(VsifClient comPort, bool enable)
        {
            deferredWriteOPNA_P1(comPort, 0x00, 3, 0x01);  //RESET
            if (enable)
            {
                deferredWriteOPNA_P1(comPort, 0x10, 0x1B);   //ENA FLAG EOS
                deferredWriteOPNA_P1(comPort, 0x10, 0x80);   //RESET FLAGS
                deferredWriteOPNA_P1(comPort, 0x06, 0xF4);   //16KHz
                deferredWriteOPNA_P1(comPort, 0x07, 0x01);   //16KHz
                deferredWriteOPNA_P1(comPort, 0x01, 0xCC);   //Sart
                deferredWriteOPNA_P1(comPort, 0x0B, 0x1f);   // Volume 
            }
        }

        protected void playAdpcmDataYM2608(VsifClient comPortOPNA, uint saddr, uint eaddr, bool loop)
        {
            deferredWriteOPNA_P1(comPortOPNA, 0x10, 0x1B); //CLEAR FLAGS
            deferredWriteOPNA_P1(comPortOPNA, 0x10, 0x80); //IRQ RESET

            deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x20); //ACCESS TO MEM

            //pcm start
            deferredWriteOPNA_P1(comPortOPNA, 0x02, (byte)((saddr >> 5) & 0xff));
            deferredWriteOPNA_P1(comPortOPNA, 0x03, (byte)((saddr >> (5 + 8) & 0xff)));
            //pcm end
            deferredWriteOPNA_P1(comPortOPNA, 0x04, (byte)((eaddr >> 5) & 0xff));
            deferredWriteOPNA_P1(comPortOPNA, 0x05, (byte)((eaddr >> (5 + 8)) & 0xff));
            //limit
            deferredWriteOPNA_P1(comPortOPNA, 0x0C, (byte)(0xff));
            deferredWriteOPNA_P1(comPortOPNA, 0x0D, (byte)(0xff));

            //KeyOn
            deferredWriteOPNA_P1(comPortOPNA, 0x00, (byte)(0x80 | 0x20 | (loop ? (byte)0x10 : (byte)0x00)));   //PLAY, ACCESS TO MEM, LOOP
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferData"></param>
        /// <param name="saddr"></param>
        /// <param name="fp"></param>
        protected void sendAdpcmDataYM2608(VsifClient comPortOPNA, bool ym2608_adpcmbit8, byte[] transferData, int saddr, FormProgress fp)
        {
            //File.WriteAllBytes(transferData.Length.ToString(), transferData);

            deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x01);  //RESET

            //flag
            deferredWriteOPNA_P1(comPortOPNA, 0x10, 0x13);   //CLEAR MASK
            deferredWriteOPNA_P1(comPortOPNA, 0x10, 0x80);   //IRQ RESET
                                                             //Ctrl1
            deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x60);   //REC, EXTMEM
            //Ctrl2
            //START
            if (ym2608_adpcmbit8)
            {
                deferredWriteOPNA_P1(comPortOPNA, 0x01, 0x02);   //LR, 8bit DRAM
                deferredWriteOPNA_P1(comPortOPNA, 0x02, (byte)((saddr >> 5) & 0xff));
                deferredWriteOPNA_P1(comPortOPNA, 0x03, (byte)((saddr >> (5 + 8)) & 0xff));
            }
            else
            {
                deferredWriteOPNA_P1(comPortOPNA, 0x01, 0x00);   //LR, 1bit DRAM
                deferredWriteOPNA_P1(comPortOPNA, 0x02, (byte)((saddr >> 2) & 0xff));
                deferredWriteOPNA_P1(comPortOPNA, 0x03, (byte)((saddr >> (2 + 8)) & 0xff));
            }
            //STOP
            deferredWriteOPNA_P1(comPortOPNA, 0x04, 0xff);
            deferredWriteOPNA_P1(comPortOPNA, 0x05, 0xff);
            //LIMIT
            deferredWriteOPNA_P1(comPortOPNA, 0x0C, 0xff);
            deferredWriteOPNA_P1(comPortOPNA, 0x0D, 0xff);

            //Transfer
            int len = transferData.Length;
            int index = 0;
            int percentage = 0;
            int lastPercentage = -1;
            for (int i = 0; i < len; i++)
            {
                deferredWriteOPNA_P1(comPortOPNA, 0x08, transferData[i]);

                //HACK: WAIT
                switch (comPortOPNA?.SoundModuleType)
                {
                    case VsifSoundModuleType.Spfm:
                    case VsifSoundModuleType.SpfmLight:
                    case VsifSoundModuleType.Gimic:
                        comPortOPNA?.FlushDeferredWriteDataAndWait();
                        break;
                }

                percentage = (100 * index) / len;
                if (percentage != lastPercentage)
                {
                    FormMain.TopForm.SetStatusText("YM2608: Transferring ADPCM(" + percentage + "%)");
                    //fp.Percentage = percentage;
                    comPortOPNA?.FlushDeferredWriteDataAndWait();
                }
                lastPercentage = percentage;
                index++;
                if (RequestedStat == SoundState.Stopped)
                    break;
                else updateStatusForDataTransfer();
            }
            FormMain.TopForm.SetStatusText("YM2608: Transferred ADPCM");

            // Finish
            deferredWriteOPNA_P1(comPortOPNA, 0x10, 0x80);
            deferredWriteOPNA_P1(comPortOPNA, 0x00, 0x01);  //RESET
        }

        /// <summary>
        /// 
        /// </summary>
        protected void updateStatusForDataTransfer()
        {
            if (RequestedStat == SoundState.Paused)
            {
                if (State != SoundState.Paused)
                    State = SoundState.Paused;
            }
            else if (RequestedStat == SoundState.Freezed)
            {
                if (State != SoundState.Freezed)
                    State = SoundState.Freezed;
            }
            else if (RequestedStat == SoundState.Playing)
            {
                if (State != SoundState.Playing)
                    State = SoundState.Freezed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comPortOPNA"></param>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        /// <param name="dclk"></param>
        protected void deferredWriteOPNA_P0(VsifClient comPortOPNA, int adrs, int dt, uint dclk)
        {
            if (adrs == 7)
                dt &= 0x3f;
            comPortOPNA.RegTable[adrs] = dt;

            switch (adrs)
            {
                case 0:
                case 2:
                case 4:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910Frequency(comPortOPNA.RegTable[adrs + 1], dt, comPortOPNA.ChipClockHz["OPNA_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                        deferredWriteOPNA_P0(comPortOPNA, adrs + 1, ret.Hi);
                    }
                    break;
                case 1:
                case 3:
                case 5:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910Frequency(dt, comPortOPNA.RegTable[adrs - 1], comPortOPNA.ChipClockHz["OPNA_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNA_P0(comPortOPNA, adrs - 1, ret.Lo);
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                    }
                    break;
                case 6:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA_SSG"] == (double)dclk)
                        goto default;
                    {
                        var data = (int)Math.Round(dt * (dclk) / (double)comPortOPNA.ChipClockHz["OPNA_SSG"]);
                        if (data > 32)
                            data = 32;
                        deferredWriteOPNA_P0(comPortOPNA, adrs, (byte)data);
                    }
                    break;
                case 0xB:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA_SSG"] == (double)dclk)
                        goto default;
                    {
                        //LO
                        var ret = convertAy8910EnvFrequency(comPortOPNA.RegTable[adrs + 1], dt, comPortOPNA.ChipClockHz["OPNA_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                        deferredWriteOPNA_P0(comPortOPNA, adrs + 1, ret.Hi);
                    }
                    break;
                case 0xC:
                    if (!ConvertChipClock || (double)comPortOPNA.ChipClockHz["OPNA_SSG"] == (double)dclk)
                        goto default;
                    {
                        //HI
                        var ret = convertAy8910EnvFrequency(dt, comPortOPNA.RegTable[adrs - 1], comPortOPNA.ChipClockHz["OPNA_SSG"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNA_P0(comPortOPNA, adrs - 1, ret.Lo);
                        deferredWriteOPNA_P0(comPortOPNA, adrs, dt);
                    }
                    break;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPNA_P0(VsifClient comPortOPNA, int adrs, int dt)
        {
            switch (comPortOPNA.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                    comPortOPNA.DeferredWriteData(0x10, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    break;
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPNA.DeferredWriteData(0x30, (byte)adrs, (byte)dt, -2);
                    break;
                case VsifSoundModuleType.SpfmLight:
                case VsifSoundModuleType.Spfm:
                    comPortOPNA.DeferredWriteData(0x00, (byte)adrs, (byte)dt, 0);
                    break;
                case VsifSoundModuleType.Gimic:
                    if (!comPortOPNA.Tag.ContainsKey("OPN3L"))
                        comPortOPNA.DeferredWriteData(0, (byte)adrs, (byte)dt, 0);
                    else
                        comPortOPNA.DeferredWriteData(3, (byte)adrs, (byte)dt, 0);
                    break;
                case VsifSoundModuleType.PC88_FTDI:
                    if (0x10 <= adrs && adrs <= 0x1f)
                    {
                        comPortOPNA.DeferredWriteData(0x02, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    }
                    else
                    {
                        comPortOPNA.DeferredWriteData(0x00, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    }
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortOPNA.DeferredWriteData(0x52, (byte)adrs, (byte)dt, 0);
                    break;
            }
        }


        protected void deferredWriteOPNA_P1(VsifClient comPortOPNA, int adrs, int dt, uint dclk)
        {
            comPortOPNA.RegTable[adrs + 0x100] = dt;

            switch (adrs)
            {
                case 0x0e:

                    break;
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
                        var ret = convertOpnFrequency(comPortOPNA.RegTable[adrs + 4 + 0x100], dt, comPortOPNA.ChipClockHz["OPNA"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Lo;
                        deferredWriteOPNA_P1(comPortOPNA, adrs + 4, ret.Hi);
                        deferredWriteOPNA_P1(comPortOPNA, adrs, dt);
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
                        var ret = convertOpnFrequency(dt, comPortOPNA.RegTable[adrs - 4 + 0x100], comPortOPNA.ChipClockHz["OPNA"], dclk);
                        if (ret.noConverted)
                            goto default;
                        dt = ret.Hi;
                        deferredWriteOPNA_P1(comPortOPNA, adrs, dt);
                        deferredWriteOPNA_P1(comPortOPNA, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPNA_P1(comPortOPNA, adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPNA_P1(VsifClient comPortOPNA, int adrs, int dt)
        {
            switch (comPortOPNA.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                    {
                        int wait = (int)Program.Default.BitBangWaitOPNA;
                        if (comPortOPNA.SoundModuleType == VsifSoundModuleType.TurboR_FTDI && adrs <= 0x10)
                            wait = (int)(wait * 1.2);
                        comPortOPNA.DeferredWriteData(0x11, (byte)adrs, (byte)dt, wait);
                    }
                    break;
                case VsifSoundModuleType.MSX_Pi:
                    {
                        comPortOPNA.DeferredWriteData(0x11, (byte)adrs, (byte)dt, 0);
                    }
                    break;
                case VsifSoundModuleType.MSX_PiTR:
                    {
                        if (adrs <= 0x10)
                            comPortOPNA.DeferredWriteData(0x31, (byte)adrs, (byte)dt, -3);
                        else
                            comPortOPNA.DeferredWriteData(0x31, (byte)adrs, (byte)dt, -2);
                    }
                    break;
                case VsifSoundModuleType.SpfmLight:
                case VsifSoundModuleType.Spfm:
                    comPortOPNA.DeferredWriteData(0x01, (byte)adrs, (byte)dt, 0);
                    break;
                case VsifSoundModuleType.Gimic:
                    if (!comPortOPNA.Tag.ContainsKey("OPN3L"))
                        comPortOPNA.DeferredWriteData(1, (byte)adrs, (byte)dt, 0);
                    else
                        comPortOPNA.DeferredWriteData(4, (byte)adrs, (byte)dt, 0);
                    break;
                case VsifSoundModuleType.PC88_FTDI:
                    if (adrs == 0x08)
                        comPortOPNA.DeferredWriteData(0x04, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    else if (adrs == 0x0b)
                        comPortOPNA.DeferredWriteData(0x03, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    else if (adrs == 0x0e)
                        comPortOPNA.DeferredWriteData(0x05, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    else
                        comPortOPNA.DeferredWriteData(0x01, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPNA);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortOPNA.DeferredWriteData(0x53, (byte)adrs, (byte)dt, 0);
                    break;
            }
        }

        /// <summary>
        /// PCM値
        /// </summary>
        int pcmValue = 0;
        /// <summary>
        /// 予測変化量
        /// </summary>
        int predictValue = 127;
        /// <summary>
        /// 8bitデータの1つ目(上位4bit)かどうか
        /// </summary>
        bool firstData = true;
        /// <summary>
        /// 実際のADPCM出力値
        /// </summary>
        byte outputValue = 0;

        int lastOpn2DacValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        public void DeferredWriteOPN2_DAC(VsifClient comPortOPN2, int dacValue)
        {
            if (PcmMixer.DisableDac)
                return;

            if (lastOpn2DacValue == dacValue)
                return;

            switch (comPortOPN2.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN2.DeferredWriteData(0x14, (byte)0x2a, (byte)dacValue, (int)Program.Default.BitBangWaitOPN2);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortOPN2.DeferredWriteData(0x52, (byte)0x2a, (byte)dacValue, (int)Program.Default.BitBangWaitOPN2);
                    break;
                default:
                    //Genesis
                    comPortOPN2.DeferredWriteDataPrior(
                        new byte[] { 0, 0 },
                        new byte[] { 1 << 2, 2 << 2 },
                        new byte[] { (byte)0x2a, (byte)dacValue },
                        (int)Program.Default.BitBangWaitOPN2);
                    break;
            }
            lastOpn2DacValue = dacValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="inputValue"></param>
        public void DeferredWriteOPNA_PseudoDAC(VsifClient comPortOPNA, int inputValue)
        {
            if (PcmMixer.DisableDac)
                return;

            switch (comPortOPNA.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    //Set volume for pseudo DAC
                    comPortOPNA.DeferredWriteData(0x13, (byte)0xb, (byte)inputValue, (int)Program.Default.BitBangWaitOPNA);
                    //outputAdpcm(comPort, lastWriteDacValue);
                    break;
                case VsifSoundModuleType.SpfmLight:
                case VsifSoundModuleType.Spfm:
                    comPortOPNA.DeferredWriteData(0x02, 0x0b, (byte)inputValue, 0);
                    break;
                case VsifSoundModuleType.Gimic:
                    if (!comPortOPNA.Tag.ContainsKey("OPN3L"))
                        comPortOPNA.DeferredWriteData(1, 0x0b, (byte)inputValue, 0);
                    else
                        comPortOPNA.DeferredWriteData(4, 0x0b, (byte)inputValue, 0);
                    break;
                case VsifSoundModuleType.PC88_FTDI:
                    //Set volume for pseudo DAC
                    comPortOPNA.DeferredWriteData(0x03, (byte)0xb, (byte)inputValue, (int)Program.Default.BitBangWaitOPNA);
                    break;
            }
        }

        int lastOpnaDacValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dacValue"></param>
        public void DeferredWriteOPNA_DAC(VsifClient comPortOPNA, int dacValue)
        {
            if (PcmMixer.DisableDac)
                return;

            if (lastOpnaDacValue == dacValue)
                return;

            switch (comPortOPNA.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPNA.DeferredWriteData(0x16, (byte)0xe, (byte)dacValue, (int)Program.Default.BitBangWaitOPNA);
                    break;
                case VsifSoundModuleType.SpfmLight:
                case VsifSoundModuleType.Spfm:
                    comPortOPNA.DeferredWriteData(0x01, 0x0e, (byte)dacValue, 0);
                    break;
                case VsifSoundModuleType.Gimic:
                    if (!comPortOPNA.Tag.ContainsKey("OPN3L"))
                        comPortOPNA.DeferredWriteData(1, 0x0e, (byte)dacValue, 0);
                    break;
                case VsifSoundModuleType.PC88_FTDI:
                    deferredWriteOPNA_P1(comPortOPNA, 0x05, (byte)dacValue);
                    //comPortOPNA.DeferredWriteData(0x13, (byte)0xb, (byte)dacValue, (int)Program.Default.BitBangWaitOPNA);
                    break;
            }

            lastOpnaDacValue = dacValue;
        }

        private int lastOPN2DacEn;

        protected void deferredWriteOPN2_P0(VsifClient comPortOPN2, int adrs, int dt, uint dclk)
        {
            //ignore test and unknown registers
            if (adrs < 0x22 || adrs == 0x23 || adrs == 0x29 || (0x2c < adrs && adrs < 0x30))
                return;
            if (adrs > 0xb6)
                return;

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
                        deferredWriteOPN2_P0(comPortOPN2, adrs + 4, ret.Hi);
                        deferredWriteOPN2_P0(comPortOPN2, adrs, dt);
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
                        deferredWriteOPN2_P0(comPortOPN2, adrs, dt);
                        deferredWriteOPN2_P0(comPortOPN2, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPN2_P0(comPortOPN2, adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPN2_P0(VsifClient comPortOPN2, int adrs, int dt)
        {
            if (adrs == 0x2b)
            {
                if (lastOPN2DacEn == (dt & 0x80))
                    return;
                lastOPN2DacEn = (dt & 0x80);
            }

            switch (comPortOPN2.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN2.DeferredWriteData(0x10, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortOPN2.DeferredWriteData(0x52, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                default:
                    //Genesis
                    comPortOPN2.DeferredWriteData(
                        new byte[] { 0, 0 },
                        new byte[] { 1 << 2, 2 << 2 },
                        new byte[] { (byte)adrs, (byte)dt },
                        (int)Program.Default.BitBangWaitOPN2);
                    break;
            }
        }

        protected void deferredWriteOPN2_P1(VsifClient comPortOPN2, int adrs, int dt, uint dclk)
        {
            //ignore test and unknown registers
            if (adrs < 0x22 || adrs == 0x23 || adrs == 0x29 || (0x2c < adrs && adrs < 0x30))
                return;
            if (adrs > 0xb6)
                return;

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
                        deferredWriteOPN2_P1(comPortOPN2, adrs + 4, ret.Hi);
                        deferredWriteOPN2_P1(comPortOPN2, adrs, dt);
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
                        deferredWriteOPN2_P1(comPortOPN2, adrs, dt);
                        deferredWriteOPN2_P1(comPortOPN2, adrs - 4, ret.Lo);
                    }
                    break;
                default:
                    deferredWriteOPN2_P1(comPortOPN2, adrs, dt);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dt"></param>
        protected void deferredWriteOPN2_P1(VsifClient comPortOPN2, int adrs, int dt)
        {
            switch (comPortOPN2.SoundModuleType)
            {
                case VsifSoundModuleType.MSX_FTDI:
                case VsifSoundModuleType.TurboR_FTDI:
                case VsifSoundModuleType.MSX_Pi:
                case VsifSoundModuleType.MSX_PiTR:
                    comPortOPN2.DeferredWriteData(0x11, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                case VsifSoundModuleType.NanoDrive:
                    comPortOPN2.DeferredWriteData(0x53, (byte)adrs, (byte)dt, (int)Program.Default.BitBangWaitOPN2);
                    break;
                default:
                    //Genesis
                    comPortOPN2.DeferredWriteData(
                        new byte[] { 0, 0 },
                        new byte[] { 3 << 2, 4 << 2 },
                        new byte[] { (byte)adrs, (byte)dt },
                        (int)Program.Default.BitBangWaitOPN2);
                    break;
            }
        }

        private void outputAdpcm(VsifClient comPortOPNA, int inputValue)
        {
            //https://www.piece-me.org/piece-lab/adpcm/adpcm2.html
            //変化量 <- PCM入力値 - PCM値
            int deltaValue = (inputValue * 256) - pcmValue;
            //ADPCM値
            int adpcmData = 0;
            //予測変化値に対する変化量の比率によってADPCM値を決定
            if (predictValue * 14 / 8 <= deltaValue)
                adpcmData = 7;
            else if (predictValue * 12 / 8 <= deltaValue && deltaValue < predictValue * 14 / 8)
                adpcmData = 6;
            else if (predictValue * 10 / 8 <= deltaValue && deltaValue < predictValue * 12 / 8)
                adpcmData = 5;
            else if (predictValue * 8 / 8 <= deltaValue && deltaValue < predictValue * 10 / 8)
                adpcmData = 4;
            else if (predictValue * 6 / 8 <= deltaValue && deltaValue < predictValue * 8 / 8)
                adpcmData = 3;
            else if (predictValue * 4 / 8 <= deltaValue && deltaValue < predictValue * 6 / 8)
                adpcmData = 2;
            else if (predictValue * 2 / 8 <= deltaValue && deltaValue < predictValue * 4 / 8)
                adpcmData = 1;
            else if (predictValue * 0 / 8 <= deltaValue && deltaValue < predictValue * 2 / 8)
                adpcmData = 0;
            else if (predictValue * -2 / 8 <= deltaValue && deltaValue < predictValue * 0 / 8)
                adpcmData = 8;
            else if (predictValue * -4 / 8 <= deltaValue && deltaValue < predictValue * -2 / 8)
                adpcmData = 9;
            else if (predictValue * -6 / 8 <= deltaValue && deltaValue < predictValue * -4 / 8)
                adpcmData = 10;
            else if (predictValue * -8 / 8 <= deltaValue && deltaValue < predictValue * -6 / 8)
                adpcmData = 11;
            else if (predictValue * -10 / 8 <= deltaValue && deltaValue < predictValue * -8 / 8)
                adpcmData = 12;
            else if (predictValue * -12 / 8 <= deltaValue && deltaValue < predictValue * -10 / 8)
                adpcmData = 13;
            else if (predictValue * -14 / 8 <= deltaValue && deltaValue < predictValue * -12 / 8)
                adpcmData = 14;
            else if (deltaValue < predictValue * -14 / 8)
                adpcmData = 15;

            //出力
            if (firstData)
            {
                outputValue = (byte)(adpcmData << 4);
                firstData = false;
            }
            else
            {
                outputValue |= (byte)adpcmData;
                comPortOPNA.DeferredWriteData(0x13, (byte)0x8, outputValue, (int)Program.Default.BitBangWaitOPNA);
                outputValue = 0;
                firstData = true;
            }

            //変化率 = ADPCM値のbit2-0
            int factor = adpcmData & 0x7;
            //変化量 <- 予測変化量 x (変化率 x 2 + 1) / 8
            deltaValue = predictValue * (factor * 2 + 1) / 8;
            if ((adpcmData & 0x8) == 0)
            {
                //増加
                pcmValue = pcmValue + deltaValue;
            }
            else
            {
                //減少
                pcmValue = pcmValue - deltaValue;
            }
            //変化率によって次回の予測変化量を更新
            switch (factor)
            {
                case 0:
                    predictValue = predictValue * 57 / 64;
                    break;
                case 1:
                    predictValue = predictValue * 57 / 64;
                    break;
                case 2:
                    predictValue = predictValue * 57 / 64;
                    break;
                case 3:
                    predictValue = predictValue * 57 / 64;
                    break;
                case 4:
                    predictValue = predictValue * 77 / 64;
                    break;
                case 5:
                    predictValue = predictValue * 102 / 64;
                    break;
                case 6:
                    predictValue = predictValue * 128 / 64;
                    break;
                case 7:
                    predictValue = predictValue * 153 / 64;
                    break;
            }
            //
            if (predictValue < 127)
                predictValue = 127;
            else if (predictValue > 24576)
                predictValue = 24576;
        }


        private int lastTurboRDacValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adrs"></param>
        /// <param name="dacValue"></param>
        public void DeferredWriteTurboR_DAC(VsifClient comPortTurboR, int dacValue)
        {
            if (PcmMixer.DisableDac)
                return;

            if (lastTurboRDacValue == dacValue)
                return;

            comPortTurboR.DeferredWriteData(0x15, (byte)0x0, (byte)dacValue, (int)(decimal)comPortTurboR.BitBangWait.GetValue(Settings.Default));

            lastTurboRDacValue = dacValue;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum SoundState
    {
        Playing,
        Stopped,
        Paused,
        Freezed,
    }
}
