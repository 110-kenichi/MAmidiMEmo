//https://github.com/kuma4649/MDPlayer
//kumatan
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using zanac.VGMPlayer.Properties;
using static System.Windows.Forms.AxHost;

namespace zanac.VGMPlayer
{
    public class DacStream : IDisposable
    {
        private bool disposedValue;

        private SongBase parentSong;

        public enum DacProxyType
        {
            OPNA,
            OPN2,
            TurboR,
            NES,
            PCE,
            PWM
        }

        private DacProxyType dacProxyType;
        private VsifClient vsifClient;
        private OKIM6258 okim6258 = null;

        private byte pp;
        private byte cc;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentSong"></param>
        /// <param name="dacProxyType"></param>
        /// <param name="vsifClient"></param>
        /// <param name="okim6258"></param>
        public DacStream(SongBase parentSong, DacProxyType dacProxyType, VsifClient vsifClient, OKIM6258 okim6258) :
            this(parentSong, dacProxyType, vsifClient, okim6258, 0, 0)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentSong"></param>
        /// <param name="dacProxyType"></param>
        /// <param name="vsifClient"></param>
        /// <param name="okim6258"></param>
        /// <param name="pp"></param>
        /// <param name="cc"></param>
        public DacStream(SongBase parentSong, DacProxyType dacProxyType, VsifClient vsifClient, OKIM6258 okim6258, byte pp, byte cc)
        {
            this.parentSong = parentSong;

            this.dacProxyType = dacProxyType;
            this.vsifClient = vsifClient;
            this.okim6258 = okim6258;

            this.pp = pp;
            this.cc = cc;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern bool QueryPerformanceFrequency(out long frequency);


        public object lockObject = new object();

        bool streaming;

        private PlayDataInfo playData;


        /// <summary>
        /// 
        /// </summary>
        public void StreamSong()
        {
            double sampleRate = 15626;
            int multiply = 1;

            int[][] outputs = new int[2][];

            long freq, before, after;
            QueryPerformanceFrequency(out freq);

            while (true)
            {
                if (disposedValue)
                    break;

                if (parentSong.State == SoundState.Stopped)
                {
                    break;
                }
                else if (parentSong.State == SoundState.Paused)
                {
                    Thread.Sleep(1);
                    QueryPerformanceCounter(out before);
                    if (playData != null)
                        playData.BeforeTime = before;
                    continue;
                }
                else if (parentSong.State == SoundState.Freezed)
                {
                    Thread.Sleep(1);
                    QueryPerformanceCounter(out before);
                    if (playData != null)
                        playData.BeforeTime = before;
                    continue;
                }

                if (!streaming)
                    continue;

                PlayDataInfo pd = this.playData;

                if (pd.StreamIdxDir > 0)
                {
                    if (pd.StreamIdx >= pd.CurrentStreamParam.Offset + pd.CurrentStreamParam.Length)
                    {
                        if ((pd.CurrentStreamParam.Mode & StreamModes.Loop) != StreamModes.Loop)
                            streaming = false;
                        else
                            pd.StreamIdx = pd.CurrentStreamParam.Offset;
                    }
                }
                else
                {
                    if (pd.StreamIdx < pd.CurrentStreamParam.Offset)
                    {
                        if ((pd.CurrentStreamParam.Mode & StreamModes.Loop) != StreamModes.Loop)
                            streaming = false;
                        else
                            pd.StreamIdx = pd.CurrentStreamParam.Offset + pd.CurrentStreamParam.Length - 1;
                    }
                }

                if (!streaming)
                    continue;

                byte data = pd.DacData[pd.StreamIdx];
                pd.StreamIdx += pd.StreamIdxDir;

                switch (pd.ChipType)
                {
                    case 0x2:   //OPN2
                        switch (dacProxyType)
                        {
                            case DacProxyType.OPN2:
                                parentSong.DeferredWriteOPN2_DAC(vsifClient, data);
                                break;
                            case DacProxyType.OPNA:
                                data = (byte)Math.Round((double)data * (double)PcmMixer.DacVolume / 100d);
                                parentSong.DeferredWriteOPNA_PseudoDAC(vsifClient, data);
                                break;
                            case DacProxyType.TurboR:
                                data = (byte)Math.Round((double)data * (double)PcmMixer.DacVolume / 100d);
                                parentSong.DeferredWriteTurboR_DAC(vsifClient, data);
                                break;
                        }
                        sampleRate = pd.CurrentStreamData.Frequency;
                        break;
                    case 17:    //PWM
                        {
                            byte dataHi = pd.DacData[pd.StreamIdx];
                            pd.StreamIdx += pd.StreamIdxDir;

                            ((VGMSong)parentSong).DeferredWritePWMReg(0x40 + (dataHi & 0xf), data);
                            sampleRate = pd.CurrentStreamData.Frequency;
                        }
                        break;
                    case 20:    //NES
                        ((VGMSong)parentSong).DeferredWriteNES(0x11, data);
                        sampleRate = pd.CurrentStreamData.Frequency;
                        break;
                    case 23:    //okim6258
                        {
                            if (okim6258 != null)
                            {
                                if (!pd.Oki6285Adpcm2ndNibble)
                                {
                                    var ddata = okim6258.decode(data & 0xf);
                                    ddata = (int)Math.Round((double)ddata * (double)PcmMixer.DacVolume / 100d);

                                    switch (dacProxyType)
                                    {
                                        case DacProxyType.OPN2:
                                            {
                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                parentSong.DeferredWriteOPN2_DAC(vsifClient, data);
                                            }
                                            break;
                                        case DacProxyType.OPNA:
                                            {
                                                byte bdata = (byte)((ddata >> 8));
                                                parentSong.DeferredWriteOPNA_DAC(vsifClient, bdata);
                                            }
                                            break;
                                        case DacProxyType.TurboR:
                                            {
                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                parentSong.DeferredWriteTurboR_DAC(vsifClient, bdata);
                                            }
                                            break;
                                    }

                                    pd.StreamIdx -= pd.StreamIdxDir;
                                    pd.Oki6285Adpcm2ndNibble = true;
                                }
                                else
                                {
                                    var ddata = okim6258.decode(data >> 4);
                                    ddata = (int)Math.Round((double)ddata * (double)PcmMixer.DacVolume / 100d);

                                    switch (dacProxyType)
                                    {
                                        case DacProxyType.OPN2:
                                            {
                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                parentSong.DeferredWriteOPN2_DAC(vsifClient, bdata);
                                            }
                                            break;
                                        case DacProxyType.OPNA:
                                            {
                                                byte bdata = (byte)((ddata >> 8));
                                                parentSong.DeferredWriteOPNA_DAC(vsifClient, bdata);
                                            }
                                            break;
                                        case DacProxyType.TurboR:
                                            {
                                                byte bdata = (byte)((ddata >> 8) + 128);
                                                parentSong.DeferredWriteTurboR_DAC(vsifClient, bdata);
                                            }
                                            break;
                                    }

                                    pd.Oki6285Adpcm2ndNibble = false;
                                }
                            }
                        }
                        sampleRate = pd.CurrentStreamData.Frequency << 1;
                        break;
                    case 27:    //PCE
                        if (pp != 0xff)
                            ((VGMSong)parentSong).DeferredWritePCE(pp, cc);
                        ((VGMSong)parentSong).DeferredWritePCE(0x06, data);
                        sampleRate = pd.CurrentStreamData.Frequency;
                        break;
                }

                QueryPerformanceCounter(out after);
                double nextTime = pd.BeforeTime + ((double)freq / (sampleRate / (double)multiply));
                while (after < nextTime)
                    QueryPerformanceCounter(out after);
                pd.BeforeTime = nextTime;
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
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~SegaPcm()
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comPortOPN2"></param>
        /// <param name="comPortOPNA"></param>
        /// <param name="okim6258"></param>
        /// <param name="currentStreamParam"></param>
        /// <param name="currentStreamData"></param>
        /// <param name="streamChipType"></param>
        /// <param name="dacData"></param>
        public void Play(StreamParam currentStreamParam, StreamData currentStreamData, int streamChipType, List<byte> dacData)
        {
            PlayDataInfo pd = new PlayDataInfo();
            pd.CurrentStreamParam = currentStreamParam;
            pd.CurrentStreamData = currentStreamData;

            pd.ChipType = streamChipType;
            pd.DacData = dacData;

            if ((currentStreamParam.Mode & StreamModes.Reverse) != StreamModes.Reverse)
            {
                pd.StreamIdx = currentStreamParam.Offset;
                pd.StreamIdxDir = 1;
            }
            else
            {
                pd.StreamIdx = currentStreamParam.Offset + currentStreamParam.Length - 1;
                pd.StreamIdxDir = -1;
            }
            long before;
            QueryPerformanceCounter(out before);
            pd.BeforeTime = before;

            this.playData = pd;
            streaming = true;
        }

        public void Stop()
        {
            streaming = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private class PlayDataInfo
        {
            public int ChipType;

            public StreamData CurrentStreamData = null;
            public StreamParam CurrentStreamParam = null;

            public int StreamIdxDir;
            public int StreamIdx;

            public List<byte> DacData;

            public double BeforeTime;

            public bool Oki6285Adpcm2ndNibble;
        }

    }


    [Flags]
    public enum StreamModes
    {
        Loop = 0x01,
        Reverse = 0x02,
    }

    public class StreamParam
    {
        public int StreamID;
        public int BlockID;
        public int Offset;
        public int Length;
        public StreamModes Mode;
    }
}
