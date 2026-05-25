using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using zanac.VGMPlayer.Properties;
using static System.Net.Mime.MediaTypeNames;

namespace zanac.VGMPlayer
{
    public partial class FormMain : Form
    {
        private SongBase currentSong;

        private ListViewItem currentSongItem;

        public static FormMain TopForm
        {
            get;
            private set;
        }

        public FormMain()
        {
            InitializeComponent();

            comboBoxDCSG.SelectedIndex = 0;
            comboBoxOPLL.SelectedIndex = 0;
            comboBoxOPN2.SelectedIndex = 0;
            comboBoxSCC.SelectedIndex = 0;
            comboBoxSccType.SelectedIndex = 0;
            comboBoxSccSlot.SelectedIndex = 1;
            comboBoxY8910.SelectedIndex = 0;
            comboBoxOPM.SelectedIndex = 0;
            comboBoxOpmSlot.SelectedIndex = 0;
            comboBoxOPL3.SelectedIndex = 0;
            comboBoxOPNA.SelectedIndex = 0;
            comboBoxOPNB.SelectedIndex = 0;
            comboBoxY8950.SelectedIndex = 0;
            comboBoxOPN.SelectedIndex = 0;
            comboBoxNES.SelectedIndex = 0;
            comboBoxMCD.SelectedIndex = 0;
            comboBoxSAA.SelectedIndex = 0;
            comboBoxPCE.SelectedIndex = 0;
            comboBoxPWM.SelectedIndex = 0;

            listViewList.Columns[0].Width = -2;
            SetHeight(listViewList, SystemInformation.MenuHeight);
            listViewList.ListViewItemSorter = new ListViewIndexComparer(listViewList);

            labelSpeed.Text = Settings.Default.PlaybackSpeed.ToString("0.00") + "x";

            TopForm = this;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            checkBoxConnDCSG.Checked = false;
            checkBoxConnOPLL.Checked = false;
            checkBoxConnOPN2.Checked = false;
            checkBoxConnSCC.Checked = false;
            checkBoxConnY8910.Checked = false;
            checkBoxConnOPM.Checked = false;
            checkBoxConnOPL3.Checked = false;
            checkBoxConnOPNA.Checked = false;
            checkBoxConnOPNB.Checked = false;
            checkBoxConnY8950.Checked = false;
            checkBoxConnOPN.Checked = false;
            checkBoxConnNES.Checked = false;
            checkBoxConnMCD.Checked = false;
            checkBoxConnSAA.Checked = false;
            checkBoxConnPCE.Checked = false;
            checkBoxConnPWM.Checked = false;

            //HACK: To avoid layout glith
            tableLayoutPanelPort.Height = tableLayoutPanelPort.Height + 1;
            tableLayoutPanelPort.Height = tableLayoutPanelPort.Height - 1;

            ListViewItem lvi = null;
            try
            {
                listViewList.BeginUpdate();
                if (Settings.Default.Files != null)
                {
                    foreach (string fn in Settings.Default.Files)
                        listViewList.Items.Add(fn);

                    int idx = Settings.Default.FocusedItem;
                    if (0 <= idx && idx < listViewList.Items.Count)
                    {
                        listViewList.Items[idx].Focused = true;
                        listViewList.Items[idx].Selected = true;
                        listViewList.Items[idx].EnsureVisible();
                        lvi = listViewList.Items[idx];
                    }
                }
            }
            finally
            {
                listViewList.EndUpdate();
            }
            if (lvi != null)
                listViewList.TopItem = lvi;

            PcmMixer.DacVolume = (double)Settings.Default.DacVolume;
            PcmMixer.DisableDac = Settings.Default.DisableDAC;
        }

        private static int GET_APPCOMMAND_LPARAM(IntPtr lParam)
        {
            return (int)(lParam.ToInt64() >> 16) & 0xFFF;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeConstants.WM_APPCOMMAND:
                    int cmd = GET_APPCOMMAND_LPARAM(m.LParam);
                    switch ((ApplicationCommand)cmd)
                    {
                        case ApplicationCommand.MediaFastForward:
                            buttonFast.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaRewind:
                            buttonSlow.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaPause:
                            buttonFreeze.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaPlay:
                            buttonPlay.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaPlayPause:
                            buttonPlay.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaNexttrack:
                            buttonNext.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaPrevioustrack:
                            buttonPrev.PerformClick();
                            goto default;
                        case ApplicationCommand.MediaStop:
                            buttonStop.PerformClick();
                            goto default;
                        case ApplicationCommand.Close:
                            Close();
                            goto default;
                        default:
                            /* According to MSDN, when handling
                             * this message, we must return TRUE. */
                            m.Result = new IntPtr(1);
                            base.WndProc(ref m);
                            return;
                    }
            }

            /* Other message handlers here… */

            base.WndProc(ref m);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            stopCurrentSong();

            comPortDCSG?.Dispose();
            comPortOPLL?.Dispose();
            comPortOPN2?.Dispose();
            comPortSCC?.Dispose();
            comPortY8910?.Dispose();
            comPortOPM?.Dispose();
            comPortOPL3?.Dispose();
            comPortOPNA?.Dispose();
            comPortOPNB?.Dispose();
            comPortY8950?.Dispose();
            comPortOPN?.Dispose();
            comPortNES?.Dispose();
            comPortMCD?.Dispose();
            comPortSAA?.Dispose();
            comPortPCE?.Dispose();
            comPortPWM?.Dispose();

            StringCollection sc = new StringCollection();
            foreach (ListViewItem item in listViewList.Items)
                sc.Add(item.Text);
            Settings.Default.Files = sc;

            var fi = listViewList.FocusedItem;
            if (fi != null)
                Settings.Default.FocusedItem = fi.Index;
        }

        private void SetHeight(ListView listView, int height)
        {
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, height);
            listView.SmallImageList = imgList;
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (currentSong == null ||
                listViewList.SelectedItems.Count != 0 &&
                listViewList.SelectedItems[0] != currentSongItem)
            {
                playSelectedItem();
            }
            else
            {
                if (currentSong?.State == SoundState.Paused || currentSong?.State == SoundState.Freezed)
                    currentSong?.Resume();
                else if (currentSong?.State == SoundState.Playing)
                    currentSong?.Pause();
                else
                    currentSong?.Play();
            }
        }


        private void buttonFreeze_Click(object sender, EventArgs e)
        {
            if (currentSong?.State == SoundState.Playing)
                currentSong?.Freeze();
            else
                currentSong?.Resume();
        }

        public event EventHandler StopRequested;

        private void buttonStop_Click(object sender, EventArgs e)
        {
            stopCurrentSong();
        }

        private void buttonSlow_Click(object sender, EventArgs e)
        {
            Settings.Default.PlaybackSpeed = Math.Max(Settings.Default.PlaybackSpeed - 0.1f, 0.1);
            if (currentSong != null)
                currentSong.PlaybackSpeed = Settings.Default.PlaybackSpeed;
            labelSpeed.Text = Settings.Default.PlaybackSpeed.ToString("0.00") + "x";
        }

        private void buttonFast_Click(object sender, EventArgs e)
        {
            Settings.Default.PlaybackSpeed = Math.Min(Settings.Default.PlaybackSpeed + 0.1f, 4);
            if (currentSong != null)
                currentSong.PlaybackSpeed = Settings.Default.PlaybackSpeed;
            labelSpeed.Text = Settings.Default.PlaybackSpeed.ToString("0.00") + "x";
        }


        private void checkBoxLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (currentSong != null)
                currentSong.LoopByCount = checkBoxLoop.Checked;
        }

        private void listViewList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                addFilesToList((string[])e.Data.GetData(DataFormats.FileDrop, false));
            }
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listViewList_ItemDrag(object sender, ItemDragEventArgs e)
        {
        }

        private void listViewList_DragLeave(object sender, EventArgs e)
        {
        }

        private void listViewList_DragOver(object sender, DragEventArgs e)
        {
        }

        private ListViewItem addFilesToList(string[] files)
        {
            ListViewItem lvi = null;
            try
            {
                listViewList.BeginUpdate();

                listViewList.SelectedItems.Clear();
                lvi = addAllFiles(files, lvi);
            }
            finally
            {
                listViewList.EndUpdate();
                lvi?.EnsureVisible();
            }
            return lvi;
        }

        private List<ListViewItem> readM3U(string m3uPath)
        {

            var playlist = new List<ListViewItem>();
            bool ext = false;
            string inf = "";
            int nr = 0;
            foreach (string line in File.ReadAllLines(m3uPath))
            {
                nr++;
                if (line.ToLower() == "#extm3u")
                    ext = true;
                else if (ext && line.ToLower().StartsWith("#extinf:"))
                    inf = line.Substring(8);
                else if (line.StartsWith("#") || line == "")
                    continue;
                else
                {
                    string fp = line;

                    string exts = Path.GetExtension(fp);
                    switch (exts.ToUpper())
                    {
                        case ".VGM":
                        case ".VGZ":
                        case ".XGM":
                        case ".KSS":
                        case ".MGS":
                            //string length = "";
                            //string artist = "";
                            //string title = "";
                            //if (inf != "")
                            //{
                            //    if (!inf.Contains(","))
                            //        continue;
                            //    string[] split = inf.Split(',');
                            //    length = split[0];
                            //    if (split[1].Contains("-"))
                            //    {
                            //        artist = split[1].Split('-')[0];
                            //        title = split[1].Split('-')[1];
                            //    }
                            //    else
                            //        title = split[1];
                            //}
                            //string t = title;
                            //if (!string.IsNullOrWhiteSpace(artist))
                            //    t = artist + "," + t;
                            //if (!string.IsNullOrWhiteSpace(length))
                            //    t = length + "," + t;
                            playlist.Add(new ListViewItem(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(m3uPath), fp))));
                            break;
                    }
                }
            }
            return playlist;
        }

        private ListViewItem addAllFiles(IEnumerable<string> files, ListViewItem lvi)
        {
            foreach (var fileName in files)
            {
                var uri = new Uri(fileName);
                if (uri.Scheme == Uri.UriSchemeFile)
                {
                    var fp = Path.GetFullPath(fileName);
                    if (File.Exists(fp))
                    {
                        string ext = Path.GetExtension(fp);
                        switch (ext.ToUpper())
                        {
                            case ".VGM":
                            case ".VGZ":
                            case ".XGM":
                            case ".KSS":
                            case ".MGS":
                                lvi = new ListViewItem(fp);
                                listViewList.Items.Add(lvi);
                                lvi.Selected = true;
                                break;
                            case ".M3U":
                                {
                                    var m3u = readM3U(fp);
                                    foreach (ListViewItem lvi2 in m3u)
                                    {
                                        lvi = lvi2;
                                        listViewList.Items.Add(lvi);
                                        lvi.Selected = true;
                                    }
                                    break;
                                }
                        }
                    }
                    if (Directory.Exists(fp))
                    {
                        string[] allfiles = Directory.GetFiles(fp, "*.*", SearchOption.AllDirectories);
                        lvi = addAllFiles(allfiles, lvi);
                    }
                }
                else
                {
                    string ext = Path.GetExtension(fileName);
                    switch (ext.ToUpper())
                    {
                        case ".VGM":
                        case ".VGZ":
                        case ".XGM":
                        case ".KSS":
                        case ".MGS":
                            lvi = new ListViewItem(fileName);
                            listViewList.Items.Add(lvi);
                            lvi.Selected = true;
                            break;
                    }
                }
            }

            return lvi;
        }

        private void buttonPrev_Click(object sender, EventArgs e)
        {
            int idx = 0;
            if (currentSongItem != null)
            {
                idx = currentSongItem.Index;
                TimeSpan? ts = labelElapsed.Tag as TimeSpan?;
                if (ts != null && ts.Value.Seconds > 3)
                {
                    playItem(currentSongItem.Index);
                    return;
                }
            }
            if (idx < 0 && listViewList.Items.Count != 0)
            {
                playItem(0);
            }
            else
            {
                idx--;
                if (idx < 0)
                    idx = listViewList.Items.Count - 1;
                playItem(idx);
            }
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            int idx = 0;
            if (currentSongItem != null)
                idx = currentSongItem.Index;
            if (idx < 0 && listViewList.Items.Count != 0)
            {
                playItem(0);
            }
            else
            {
                idx++;
                if (idx >= listViewList.Items.Count)
                    idx = 0;
                playItem(idx);
            }
        }

        private void playSelectedItem()
        {
            if (listViewList.SelectedItems.Count != 0)
            {
                currentSongItem = listViewList.SelectedItems[0];
                listViewList.SelectedItems.Clear();

                stopCurrentSong();
                playFile(currentSongItem.Text);
            }
        }

        private void playItem(int idx)
        {
            if (idx >= listViewList.Items.Count)
                idx = listViewList.Items.Count - 1;
            if (idx < 0)
                return;

            currentSongItem = listViewList.Items[idx];
            listViewList.SelectedItems.Clear();

            stopCurrentSong();
            playFile(currentSongItem.Text);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="files"></param>
        /// <param name="play"></param>
        public void AddFilesToList(String[] files, bool play)
        {
            if (preparingPlay)
                return;

            ListViewItem lvi = addFilesToList(files);
            if (play && lvi != null)
            {
                playItem(lvi.Index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameOrUrl"></param>
        public void PlayFileDirect(string fileNameOrUrl, string coverArtFileNameUrUrl = null)
        {
            if (preparingPlay)
                return;

            stopCurrentSong();
            playFile(fileNameOrUrl, true, coverArtFileNameUrUrl);
        }

        private bool preparingPlay;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileNameOrUrl"></param>
        private void playFile(string fileNameOrUrl, bool isTempFile = false, string coverArtFileNameUrUrl = null)
        {
            if (preparingPlay)
                return;

            String vgmFileName = fileNameOrUrl;
            String coverArtFileName = coverArtFileNameUrUrl;
            bool cancelled = false;
            try
            {
                preparingPlay = true;
                String tempVgmFile = null;
                String tempArtFile = null;
                {
                    var uri = new Uri(fileNameOrUrl);
                    if (String.Equals(uri.Scheme, Uri.UriSchemeHttp) || String.Equals(uri.Scheme, Uri.UriSchemeHttps))
                    {
                        using (WebClient client = new WebClient())
                        {
                            FormProgress.RunDialog("Downloading track data...", (pd) =>
                            {
                                try
                                {
                                    tempVgmFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.AbsoluteUri));
                                    vgmFileName = tempVgmFile;
                                    client.DownloadProgressChanged += (sender, e) =>
                                    {
                                        if (cancelled)
                                            client.CancelAsync();
                                        try
                                        {
                                            pd.Percentage = e.ProgressPercentage;
                                        }
                                        catch { }
                                    };
                                    client.DownloadFileCompleted += (sender, e) =>
                                    {
                                        if (e.Cancelled)
                                            File.Delete(tempVgmFile);
                                    };
                                    var task = client.DownloadFileTaskAsync(uri, tempVgmFile);
                                    task.Wait();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                    cancelled = true;
                                }
                            }, () =>
                            {
                                cancelled = true;
                            });
                        }
                        if (cancelled)
                            return;
                        if (Program.Default.ShowCoverArt && coverArtFileNameUrUrl == null)
                        {
                            if (uri.AbsoluteUri.StartsWith("https://vgmrips.net/packs/vgm", StringComparison.OrdinalIgnoreCase))
                            {
                                //https://vgmrips.net/packs/vgm/Arcade/Battle_Garegga_%28Toaplan_2%29/14%20Marginal%20Consciousness%20%5BStage%207%20Airport%5D.vgz
                                //https://vgmrips.net/packs/images/large/Arcade/Battle_Garegga_%28Toaplan_2%29.png

                                //https://vgmrips.net/packs/vgm/Arcade/SegaSys/Out_Run_%28Arcade%29/01%20Magical%20Sound%20Shower.vgz
                                //https://vgmrips.net/packs/images/large/Arcade/SegaSys/Out_Run_%28Arcade%29.png
                                //https://vgmrips.net/packs/images/large/SegaSys/Out_Run_%28Arcade%29.png

                                String urltext = "https://vgmrips.net/packs/images/large/";
                                for (int i = 3; i < uri.Segments.Length - 1; i++)
                                    urltext += uri.Segments[i];
                                urltext = urltext.Substring(0, urltext.Length - 1);
                                urltext += ".png";
                                uri = new Uri(urltext);
                                using (WebClient client = new WebClient())
                                {
                                    FormProgress.RunDialog("Downloading cover art data...", (pd) =>
                                    {
                                        try
                                        {
                                            tempArtFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), uri.Segments[uri.Segments.Length - 1]), Path.GetExtension(".png"));
                                            coverArtFileName = tempArtFile;
                                            client.DownloadProgressChanged += (sender, e) =>
                                            {
                                                if (cancelled)
                                                    client.CancelAsync();
                                                try
                                                {
                                                    pd.Percentage = e.ProgressPercentage;
                                                }
                                                catch { }
                                            };
                                            client.DownloadFileCompleted += (sender, e) =>
                                            {
                                                if (e.Cancelled)
                                                    File.Delete(tempArtFile);
                                            };
                                            var task = client.DownloadFileTaskAsync(uri, tempArtFile);
                                            task.Wait();
                                        }
                                        catch
                                        {
                                            tempArtFile = null;
                                            coverArtFileName = null;
                                        }
                                    }, () =>
                                    {
                                        cancelled = true;
                                    });
                                }
                            }
                        }
                    }
                    else if (String.Equals(uri.Scheme, Uri.UriSchemeFile))
                    {
                        fileNameOrUrl = uri.LocalPath;
                    }
                    if (cancelled)
                        return;

                    if (Settings.Default.ShowCoverArt)
                    {
                        if (coverArtFileNameUrUrl != null)
                        {
                            uri = new Uri(coverArtFileNameUrUrl);
                            if (String.Equals(uri.Scheme, Uri.UriSchemeHttp) || String.Equals(uri.Scheme, Uri.UriSchemeHttps))
                            {
                                using (WebClient client = new WebClient())
                                {
                                    FormProgress.RunDialog("Downloading cover art data...", (pd) =>
                                    {
                                        try
                                        {
                                            tempArtFile = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(fileNameOrUrl));
                                            coverArtFileName = tempArtFile;
                                            client.DownloadProgressChanged += (sender, e) =>
                                            {
                                                if (cancelled)
                                                    client.CancelAsync();
                                                try
                                                {
                                                    pd.Percentage = e.ProgressPercentage;
                                                }
                                                catch { }
                                            };
                                            client.DownloadFileCompleted += (sender, e) =>
                                            {
                                                if (e.Cancelled)
                                                    File.Delete(tempArtFile);
                                            };
                                            var task = client.DownloadFileTaskAsync(uri, tempArtFile);
                                            task.Wait();
                                        }
                                        catch
                                        {
                                            tempArtFile = null;
                                            coverArtFileName = null;
                                        }
                                    }, () =>
                                    {
                                        cancelled = true;
                                    });
                                }
                            }
                            else if (String.Equals(uri.Scheme, Uri.UriSchemeFile))
                            {
                                coverArtFileNameUrUrl = uri.LocalPath;
                            }
                        }
                    }
                    if (cancelled)
                        return;
                }

                string ext = Path.GetExtension(vgmFileName);
                try
                {
                    switch (ext.ToUpper())
                    {
                        case ".VGM":
                        case ".VGZ":
                            currentSong = new VGMSong(vgmFileName);
                            break;
                        case ".XGM":
                            currentSong = new XGMSong(vgmFileName);
                            break;
                        case ".KSS":
                        case ".MGS":
                            currentSong = new MGSSong(vgmFileName);
                            break;
                    }
                    if (tempVgmFile != null)
                    {
                        currentSong.CanLoadCoverArt = coverArtFileName != null ? true : false;
                        currentSong.DeleteFileAfterStop = true;
                        currentSong.TemporarySongFile = isTempFile;
                    }
                    currentSong.CoverArtFile = coverArtFileName;
                    currentSong.DeleteCoverArtFileAfterStop = tempArtFile != null ? true : false;

                    currentSong.ConvertChipClock = checkBoxCnvClk.Checked;
                    currentSong.LoopByCount = checkBoxLoop.Checked;
                    currentSong.LoopedCount = (int)numericUpDownLooped.Value;
                    currentSong.LoopByElapsed = checkBoxLoopTimes.Checked;
                    currentSong.LoopTimes = new TimeSpan(dateTimePickerLoopTimes.Value.Hour, dateTimePickerLoopTimes.Value.Minute, dateTimePickerLoopTimes.Value.Second);
                    currentSong.ProcessLoadOccurred += CurrentSong_ProcessLoadOccurred;
                    currentSong.PlayStatusChanged += CurrentSong_PlayStatusChanged;
                    currentSong.SpeedChanged += CurrentSong_SpeedChanged;
                    currentSong.Finished += CurrentSong_Finished;
                    currentSong.PlaybackSpeed = Settings.Default.PlaybackSpeed;
                    //labelSpeed.Text = currentSong.PlaybackSpeed.ToString("0.00") + "x";
                    textBoxTitle.Text = currentSong.FileName;
                    currentSong.Play();
                    textBoxSongSystem.Text = currentSong.SongChipInformation;
                    textBoxUseSystem.Text = currentSong.UseChipInformation;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            finally
            {
                preparingPlay = false;
            }
        }

        private async void CurrentSong_Finished(object sender, EventArgs e)
        {
            if (sender != currentSong)
                return;

            var cs = currentSong;

            await Task.Delay(500);

            if (this.IsDisposed)
                return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                if (this.IsDisposed)
                    return;
                if (cs != currentSong)
                    return;
                if (currentSong.TemporarySongFile)
                    return;

                buttonNext_Click(null, null);
            }));
        }

        private void CurrentSong_SpeedChanged(object sender, EventArgs e)
        {
            if (sender != currentSong)
                return;

            //labelSpeed.Text = Settings.Default.PlaybackSpeed.ToString("0.00") + "x";
        }

        private void CurrentSong_PlayStatusChanged(object sender, EventArgs e)
        {
            if (sender != currentSong)
                return;

            if (currentSong.State != SoundState.Playing)
                buttonPlay.ImageIndex = 0;
            else
                buttonPlay.ImageIndex = 1;
        }

        private void CurrentSong_ProcessLoadOccurred(object sender, EventArgs e)
        {
            if (sender != currentSong)
                return;

            if (this.IsDisposed)
                return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                if (progressBarLoad.IsDisposed)
                    return;
                if (progressBarLoad.Value <= 40)
                    setProgressBarLoadValue(50);
                else if (progressBarLoad.Value <= 90)
                    addProgressBarLoadValue(10);
            }));
        }

        private void stopCurrentSong()
        {
            StopRequested?.Invoke(this, EventArgs.Empty);

            if (currentSongItem != null)
            {
                currentSongItem.Selected = true;
                currentSongItem.EnsureVisible();
            }
            if (currentSong != null)
            {
                currentSong.Stop();
                currentSong.ProcessLoadOccurred -= CurrentSong_ProcessLoadOccurred;
                currentSong.PlayStatusChanged -= CurrentSong_PlayStatusChanged;
                currentSong.SpeedChanged -= CurrentSong_SpeedChanged;
                currentSong.Finished -= CurrentSong_Finished;
                currentSong.Dispose();
                currentSong = null;
                textBoxTitle.Text = string.Empty;
            }
        }

        private void listViewList_KeyDown(object sender, KeyEventArgs e)
        {
            // Deleteキーが押されたら項目を削除


            if (e.KeyData == Keys.Delete ||
                e.KeyData == Keys.Back ||
                (e.KeyCode == Keys.X && e.Control))
            {
                removeSelectedItem();
            }
            if (e.KeyCode == Keys.Enter)
            {
                playSelectedItem();
            }
            if (e.KeyCode == Keys.A && e.Control)
            {
                try
                {
                    listViewList.BeginUpdate();
                    foreach (ListViewItem item in listViewList.Items)
                        item.Selected = true;
                }
                finally
                {
                    listViewList.EndUpdate();
                }
            }
            if (e.KeyCode == Keys.V && e.Control)
            {
                System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList();
                if (files != null)
                {
                    ListViewItem lvi = null;
                    try
                    {
                        listViewList.BeginUpdate();

                        listViewList.SelectedItems.Clear();
                        lvi = addAllFiles(files.Cast<string>().ToArray(), lvi);
                    }
                    finally
                    {
                        listViewList.EndUpdate();
                        lvi?.EnsureVisible();
                    }
                }
            }
        }

        private void removeSelectedItem()
        {
            try
            {
                listViewList.BeginUpdate();
                int index = 0;
                for (int i = 0; i < listViewList.SelectedItems.Count; i++)
                {
                    // 現在選択している行のインデックスを取得
                    index = listViewList.SelectedItems[0].Index;
                    if ((0 <= index) && (index < listViewList.Items.Count))
                    {
                        listViewList.Items.RemoveAt(index);
                        i--;
                    }
                }
                if (index < listViewList.Items.Count)
                {
                    listViewList.Items[index].Selected = true;
                    listViewList.Items[index].EnsureVisible();
                }
                else if (listViewList.Items.Count != 0)
                {
                    listViewList.Items[listViewList.Items.Count - 1].Selected = true;
                    listViewList.Items[listViewList.Items.Count - 1].EnsureVisible();
                }
            }
            finally
            {
                listViewList.EndUpdate();
            }
        }

        private void listViewList_DoubleClick(object sender, EventArgs e)
        {
            playSelectedItem();
        }

        private void listViewList_SizeChanged(object sender, EventArgs e)
        {
            listViewList.Columns[0].Width = -2;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            PcmMixer.DacVolume = (double)Settings.Default.DacVolume;
            PcmMixer.DisableDac = Settings.Default.DisableDAC;
            PcmMixer.DacClipping = Settings.Default.DACClipping;

            if (currentSong?.HighLoad == true && currentSong?.State == SoundState.Playing)
            {
                if (progressBarLoad.Value <= 90)
                    addProgressBarLoadValue(10);
            }
            else if (progressBarLoad.Value >= 10)
                addProgressBarLoadValue(-10);

            if (currentSong?.DacClip == true && currentSong?.State == SoundState.Playing)
            {
                checkBoxClip.BackColor = Color.Red;
                if (currentSong != null)
                    currentSong.DacClip = false;
            }
            else
            {
                checkBoxClip.BackColor = SystemColors.Control;
            }

        }

        private void setProgressBarLoadValue(int value)
        {
            progressBarLoad.Value = value;
            progressBarLoad.ForeColor =
                System.Drawing.Color.FromArgb(255, (255 * (100 - progressBarLoad.Value)) / 100, 0);
        }

        private void addProgressBarLoadValue(int value)
        {
            progressBarLoad.Value += value;
            progressBarLoad.ForeColor =
                System.Drawing.Color.FromArgb(255, (255 * (100 - progressBarLoad.Value)) / 100, 0);
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control)
            {
                switch (e.KeyCode)
                {
                    //矢印キーが押されたことを表示する
                    case Keys.Z:
                        buttonPrev_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.X:
                        buttonPlay_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.C:
                        buttonNext_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.V:
                        buttonStop_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.B:
                        buttonFreeze_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.N:
                        buttonSlow_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.M:
                        buttonFast_Click(null, null);
                        e.Handled = true;
                        break;
                    case Keys.Oemcomma:
                        checkBoxLoop.Checked = !checkBoxLoop.Checked;
                        e.Handled = true;
                        break;
                }
            }
        }

        private VsifClient comPortPWM;

        private void checkBoxConnPWM_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnPWM.Checked)
            {
                switch (Settings.Default.PWM_IF)
                {
                    case 0:
                        comPortPWM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                            (PortId)Settings.Default.PWM_Port, (int)Settings.Default.PWM_Div, false);
                        break;
                }

                checkBoxConnPWM.Checked = comPortPWM != null;
                comboBoxPWM.Enabled = comPortPWM == null;
                comboBoxPortPWM.Enabled = comPortPWM == null;
                numericUpDownPWMDiv.Enabled = comPortPWM == null;
            }
            else
            {
                comboBoxPWM.Enabled = true;
                comboBoxPortPWM.Enabled = true;
                numericUpDownPWMDiv.Enabled = true;

                comPortPWM?.Dispose();
            }
        }

        private VsifClient comPortPCE;

        private void checkBoxConnPCE_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnPCE.Checked)
            {
                switch (Settings.Default.PCE_IF)
                {
                    case 0:
                        comPortPCE= VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboEverDrive,
                            (PortId)Settings.Default.PCE_Port, (int)Settings.Default.PCE_Div, false);
                        break;
                }

                checkBoxConnPCE.Checked = comPortPCE != null;
                comboBoxPCE.Enabled = comPortPCE == null;
                comboBoxPortPCE.Enabled = comPortPCE == null;
                //numericUpDownPCEDiv.Enabled = comPortPCE == null;
            }
            else
            {
                comboBoxPCE.Enabled = true;
                comboBoxPortPCE.Enabled = true;
                //numericUpDownSAADiv.Enabled = true;

                comPortPCE?.Dispose();
            }
        }

        private VsifClient comPortSAA;

        private void checkBoxCnnSAA_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnSAA.Checked)
            {
                switch (Settings.Default.SAA_IF)
                {
                    case 0:
                        comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.SAA_Port, (int)Settings.Default.SAA_Div, false);
                        break;
                    case 1:
                        comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.SAA_Port, (int)Settings.Default.SAA_Div, false);
                        if (comPortSAA != null)
                            comPortSAA.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitSAA);
                        break;
                    case 2:
                        comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.SAA_Port, 0, false);
                        break;
                    case 3:
                        comPortSAA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.SAA_Port, 0, false);
                        if (comPortSAA != null)
                            comPortSAA.DeferredWriteData(0x15, (byte)0x0, (byte)127, 0);
                        break;
                }

                checkBoxConnSAA.Checked = comPortSAA != null;
                comboBoxSAA.Enabled = comPortSAA == null;
                comboBoxPortSAA.Enabled = comPortSAA == null;
                numericUpDownSAADiv.Enabled = comPortSAA == null;
            }
            else
            {
                comboBoxSAA.Enabled = true;
                comboBoxPortSAA.Enabled = true;
                numericUpDownSAADiv.Enabled = true;

                comPortSAA?.Dispose();
            }
        }

        private VsifClient comPortMCD;

        private void checkBoxConnMCD_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnMCD.Checked)
            {
                switch (Settings.Default.MCD_IF)
                {
                    case 0:
                        comPortMCD = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                            (PortId)Settings.Default.MCD_Port, (int)Settings.Default.MCD_Div, false);
                        break;
                }

                checkBoxConnMCD.Checked = comPortMCD != null;
                comboBoxMCD.Enabled = comPortMCD == null;
                comboBoxPortMCD.Enabled = comPortMCD == null;
                numericUpDownMCDDiv.Enabled = comPortMCD == null;
            }
            else
            {
                comboBoxMCD.Enabled = true;
                comboBoxPortMCD.Enabled = true;
                numericUpDownMCDDiv.Enabled = true;

                comPortMCD?.Dispose();
            }
        }

        private VsifClient comPortDCSG;

        private void checkBoxConnDCSG_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnDCSG.Checked)
            {
                switch (Settings.Default.DCSG_IF)
                {
                    case 0:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 1:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 2:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 3:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_Low,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 4:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 5:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS_FTDI,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 6:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        if (comPortDCSG != null)
                            comPortDCSG.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitDCSG);
                        break;
                    case 7:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NanoDrive,
                            (PortId)Settings.Default.DCSG_Port, (int)Settings.Default.DCSGDiv, false);
                        break;
                    case 8:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.DCSG_Port, 0, false);
                        break;
                    case 9:
                        comPortDCSG = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.DCSG_Port, 0, false);
                        if (comPortDCSG != null)
                            comPortDCSG.DeferredWriteData(0x15, (byte)0x0, (byte)127, 0);
                        break;
                }

                checkBoxConnDCSG.Checked = comPortDCSG != null;
                comboBoxDCSG.Enabled = comPortDCSG == null;
                comboBoxPortSN76489.Enabled = comPortDCSG == null;
                numericUpDownDCSGDiv.Enabled = comPortDCSG == null;
            }
            else
            {
                comboBoxDCSG.Enabled = true;
                comboBoxPortSN76489.Enabled = true;
                numericUpDownDCSGDiv.Enabled = true;

                comPortDCSG?.Dispose();
            }
        }

        private VsifClient comPortOPLL;

        private void checkBoxConnOPLL_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPLL.Checked)
            {
                switch (Settings.Default.OPLL_IF)
                {
                    case 0:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS,
                            (PortId)Settings.Default.OPLL_Port, (int)Settings.Default.OPLLDiv, false);
                        break;
                    case 1:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPLL_Port, (int)Settings.Default.OPLLDiv, false);
                        if (comPortOPLL != null)
                            comPortOPLL.Tag["OPLL.Slot"] = comboBoxOpllSlot.SelectedIndex;
                        break;
                    case 2:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SMS_FTDI,
                            (PortId)Settings.Default.OPLL_Port, (int)Settings.Default.OPLLDiv, false);
                        break;
                    case 3:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPLL_Port, (int)Settings.Default.OPLLDiv, false);
                        if (comPortOPLL != null)
                            comPortOPLL.Tag["OPLL.Slot"] = comboBoxOpllSlot.SelectedIndex;
                        if (comPortOPLL != null)
                            comPortOPLL.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPLL);
                        break;
                    case 4:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPLL_Port, (int)0, false);
                        if (comPortOPLL != null)
                            comPortOPLL.Tag["OPLL.Slot"] = comboBoxOpllSlot.SelectedIndex;
                        break;
                    case 5:
                        comPortOPLL = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPLL_Port, (int)0, false);
                        if (comPortOPLL != null)
                            comPortOPLL.Tag["OPLL.Slot"] = comboBoxOpllSlot.SelectedIndex;
                        if (comPortOPLL != null)
                            comPortOPLL.DeferredWriteData(0x15, (byte)0x0, (byte)127, 0);
                        break;
                }

                checkBoxConnOPLL.Checked = comPortOPLL != null;
                comboBoxOPLL.Enabled = comPortOPLL == null;
                comboBoxPortYm2413.Enabled = comPortOPLL == null;
                comboBoxOpllSlot.Enabled = comPortOPLL == null;
                numericUpDownOPLLDiv.Enabled = comPortOPLL == null;
            }
            else
            {
                comboBoxOPLL.Enabled = true;
                comboBoxPortYm2413.Enabled = true;
                comboBoxOpllSlot.Enabled = true;
                numericUpDownOPLLDiv.Enabled = true;

                comPortOPLL?.Dispose();
            }
        }

        private VsifClient comPortOPN2;

        private void checkBoxConnOPN2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPN2.Checked)
            {
                switch (Settings.Default.OPN2_IF)
                {
                    case 0:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        break;
                    case 1:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_FTDI,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        break;
                    case 2:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Genesis_Low,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        break;
                    case 3:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        break;
                    case 4:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        if (comPortOPN2 != null)
                            comPortOPN2.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPN2);
                        break;
                    case 5:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NanoDrive,
                            (PortId)Settings.Default.OPN2_Port, (int)Settings.Default.OPN2Div, false);
                        break;
                    case 6:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPN2_Port, (int)0, false);
                        break;
                    case 7:
                        comPortOPN2 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPN2_Port, (int)0, false);
                        if (comPortOPN2 != null)
                            comPortOPN2.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnOPN2.Checked = comPortOPN2 != null;
                comboBoxOPN2.Enabled = comPortOPN2 == null;
                comboBoxPortYM2612.Enabled = comPortOPN2 == null;
                numericUpDownOPN2Div.Enabled = comPortOPN2 == null;

            }
            else
            {
                comboBoxOPN2.Enabled = true;
                comboBoxPortYM2612.Enabled = true;
                numericUpDownOPN2Div.Enabled = true;

                comPortOPN2?.Dispose();
            }
        }

        private VsifClient comPortOPN;

        private void checkBoxConnOPN_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPN.Checked)
            {
                switch (Settings.Default.OPN_IF)
                {
                    case 0:
                        comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPN_Port, (int)Settings.Default.OPNDiv, false);
                        break;
                    case 1:
                        comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.PC88_FTDI,
                            (PortId)Settings.Default.OPN_Port, (int)Settings.Default.OPNDiv, false);
                        break;
                    case 2:
                        comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPN_Port, (int)Settings.Default.OPNDiv, false);
                        if (comPortOPN != null)
                            comPortOPN.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPN);
                        break;
                    case 3:
                        comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPN_Port, (int)0, false);
                        break;
                    case 4:
                        comPortOPN = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPN_Port, (int)0, false);
                        break;
                }
                checkBoxConnOPN.Checked = comPortOPN != null;
                comboBoxOPN.Enabled = comPortOPN == null;
                comboBoxPortOPN.Enabled = comPortOPN == null;
                numericUpDownOPNDiv.Enabled = comPortOPN == null;
            }
            else
            {
                comboBoxOPN.Enabled = true;
                comboBoxPortOPN.Enabled = true;
                numericUpDownOPNDiv.Enabled = true;

                comPortOPN?.Dispose();
            }
        }

        private VsifClient comPortSCC;

        private void checkBoxConnSCC_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnSCC.Checked)
            {
                switch (Settings.Default.SCC_IF)
                {
                    case 0:
                        comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.SCC_Port, (int)Settings.Default.SCCDiv, false);
                        if (comPortSCC != null)
                        {
                            comPortSCC.Tag["SCC.Type"] = (SCCType)(comboBoxSccType.SelectedIndex + 1);
                            if (comboBoxSccSlot.SelectedIndex < 2)
                                comPortSCC.Tag["SCC.Slot"] = comboBoxSccSlot.SelectedIndex - 2;
                            else
                                comPortSCC.Tag["SCC.Slot"] = SCCSlotNo[comboBoxSccSlot.SelectedIndex - 2];
                        }
                        break;
                    case 1:
                        comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.SCC_Port, (int)Settings.Default.SCCDiv, false);
                        if (comPortSCC != null)
                        {
                            comPortSCC.Tag["SCC.Type"] = (SCCType)(comboBoxSccType.SelectedIndex + 1);
                            if (comboBoxSccSlot.SelectedIndex < 2)
                                comPortSCC.Tag["SCC.Slot"] = comboBoxSccSlot.SelectedIndex - 2;
                            else
                                comPortSCC.Tag["SCC.Slot"] = SCCSlotNo[comboBoxSccSlot.SelectedIndex - 2];
                        }
                        if (comPortSCC != null)
                            comPortSCC.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitSCC);
                        break;
                    case 2:
                        comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.SCC_Port, (int)0, false);
                        if (comPortSCC != null)
                        {
                            comPortSCC.Tag["SCC.Type"] = (SCCType)(comboBoxSccType.SelectedIndex + 1);
                            if (comboBoxSccSlot.SelectedIndex < 2)
                                comPortSCC.Tag["SCC.Slot"] = comboBoxSccSlot.SelectedIndex - 2;
                            else
                                comPortSCC.Tag["SCC.Slot"] = SCCSlotNo[comboBoxSccSlot.SelectedIndex - 2];
                        }
                        break;
                    case 3:
                        comPortSCC = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.SCC_Port, (int)0, false);
                        if (comPortSCC != null)
                        {
                            comPortSCC.Tag["SCC.Type"] = (SCCType)(comboBoxSccType.SelectedIndex + 1);
                            if (comboBoxSccSlot.SelectedIndex < 2)
                                comPortSCC.Tag["SCC.Slot"] = comboBoxSccSlot.SelectedIndex - 2;
                            else
                                comPortSCC.Tag["SCC.Slot"] = SCCSlotNo[comboBoxSccSlot.SelectedIndex - 2];
                        }
                        if (comPortSCC != null)
                            comPortSCC.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnSCC.Checked = comPortSCC != null;
                comboBoxSCC.Enabled = comPortSCC == null;
                comboBoxPortSCC.Enabled = comPortSCC == null;
                comboBoxSccSlot.Enabled = comPortSCC == null;
                comboBoxSccType.Enabled = comPortSCC == null;
                numericUpDownSCCDiv.Enabled = comPortSCC == null;
            }
            else
            {
                comboBoxSCC.Enabled = true;
                comboBoxPortSCC.Enabled = true;
                comboBoxSccSlot.Enabled = true;
                comboBoxSccType.Enabled = true;
                numericUpDownSCCDiv.Enabled = true;

                comPortSCC?.Dispose();
            }
        }

        private int[] SCCSlotNo = new int[]
        {
            0b0000_0000,
            0b1000_0000,
            0b1000_0100,
            0b1000_1000,
            0b1000_1100,

            0b0000_0001,
            0b1000_0001,
            0b1000_0101,
            0b1000_1001,
            0b1000_1101,

            0b0000_0010,
            0b1000_0010,
            0b1000_0110,
            0b1000_1010,
            0b1000_1110,

            0b0000_0011,
            0b1000_0011,
            0b1000_0111,
            0b1000_1011,
            0b1000_1111,
        };

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

        private VsifClient comPortY8910;

        private void checkBoxConnY8910_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnY8910.Checked)
            {
                switch (Settings.Default.Y8910_IF)
                {
                    case 0:
                        comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.Y8910_Port, (int)Settings.Default.PSGDiv, false);
                        break;
                    case 1:
                        comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.Y8910_Port, (int)Settings.Default.PSGDiv, false);
                        if (comPortY8910 != null)
                            comPortY8910.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitAY8910);
                        break;
                    case 2:
                        comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.Y8910_Port, (int)Settings.Default.PSGDiv, false);
                        break;
                    case 3:
                        comPortY8910 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.Y8910_Port, (int)Settings.Default.PSGDiv, false);
                        if (comPortY8910 != null)
                            comPortY8910.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitAY8910);
                        break;
                }
                checkBoxConnY8910.Checked = comPortY8910 != null;
                comboBoxY8910.Enabled = comPortY8910 == null;
                comboBoxPortY8910.Enabled = comPortY8910 == null;
                numericUpDownPSGDiv.Enabled = comPortY8910 == null;
            }
            else
            {
                comboBoxY8910.Enabled = true;
                comboBoxPortY8910.Enabled = true;
                numericUpDownPSGDiv.Enabled = true;

                comPortY8910?.Dispose();
            }
        }



        private VsifClient comPortOPM;

        private void checkBoxConnOPM_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPM.Checked)
            {
                switch (Settings.Default.OPM_IF)
                {
                    case 0:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPM_Port, (int)Settings.Default.OPMDiv, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                            comPortOPM.DeferredWriteData(0xd, 0, (byte)comboBoxOpmSlot.SelectedIndex, (int)Settings.Default.BitBangWaitOPM);
                        }
                        break;
                    case 1:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SpfmLight,
                            (PortId)Settings.Default.OPM_Port, (int)Settings.Default.OPMDiv, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                        }
                        break;
                    case 2:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Spfm,
                            (PortId)Settings.Default.OPM_Port, (int)Settings.Default.OPMDiv, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                        }
                        break;
                    case 3:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Gimic,
                            (PortId)Settings.Default.OPM_Port, (int)Settings.Default.OPMDiv, false);
                        break;
                    case 4:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPM_Port, (int)Settings.Default.OPMDiv, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                            comPortOPM.DeferredWriteData(0xd, 0, (byte)comboBoxOpmSlot.SelectedIndex, (int)Settings.Default.BitBangWaitOPM);
                        }
                        if (comPortOPM != null)
                            comPortOPM.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPM);
                        break;
                    case 5:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPM_Port, (int)0, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                            comPortOPM.DeferredWriteData(0xd, 0, (byte)comboBoxOpmSlot.SelectedIndex, (int)0);
                        }
                        break;
                    case 6:
                        comPortOPM = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPM_Port, (int)0, false);
                        if (comPortOPM != null)
                        {
                            comPortOPM.Tag["OPM.Slot"] = comboBoxOpmSlot.SelectedIndex;
                            comPortOPM.DeferredWriteData(0xd, 0, (byte)comboBoxOpmSlot.SelectedIndex, (int)0);
                        }
                        if (comPortOPM != null)
                            comPortOPM.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnOPM.Checked = comPortOPM != null;
                comboBoxOPM.Enabled = comPortOPM == null;
                comboBoxPortOPM.Enabled = comPortOPM == null;
                comboBoxOpmSlot.Enabled = comPortOPM == null;
                numericUpDownOPMDiv.Enabled = comPortOPM == null;
            }
            else
            {
                comboBoxOPM.Enabled = true;
                comboBoxPortOPM.Enabled = true;
                comboBoxOpmSlot.Enabled = true;
                numericUpDownOPMDiv.Enabled = true;

                comPortOPM?.Dispose();
            }
        }

        private VsifClient comPortOPL3;

        private void checkBoxConnOPL3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPL3.Checked)
            {
                switch (Settings.Default.OPL3_IF)
                {
                    case 0:
                        comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPL3_Port, (int)Settings.Default.OPL3Div, false);
                        break;
                    case 1:
                        comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPL3_Port, (int)Settings.Default.OPL3Div, false);
                        if (comPortOPL3 != null)
                            comPortOPL3.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPL3);
                        break;
                    case 2:
                        comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPL3_Port, (int)0, false);
                        break;
                    case 3:
                        comPortOPL3 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPL3_Port, (int)0, false);
                        if (comPortOPL3 != null)
                            comPortOPL3.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnOPL3.Checked = comPortOPL3 != null;
                comboBoxOPL3.Enabled = comPortOPL3 == null;
                comboBoxPortOPL3.Enabled = comPortOPL3 == null;
                numericUpDownOPL3Div.Enabled = comPortOPL3 == null;
            }
            else
            {
                comboBoxOPL3.Enabled = true;
                comboBoxPortOPL3.Enabled = true;
                numericUpDownOPL3Div.Enabled = true;

                comPortOPL3?.Dispose();
            }
        }

        private VsifClient comPortOPNA;

        private void checkBoxOPNA_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPNA.Checked)
            {
                switch (Settings.Default.OPNA_IF)
                {
                    case 0:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        break;
                    case 1:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.SpfmLight,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        break;
                    case 2:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Spfm,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        break;
                    case 3:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.Gimic,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        break;
                    case 4:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.PC88_FTDI,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        break;
                    case 5:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.OPNA_Port, (int)Settings.Default.OPNADiv, false);
                        if (comPortOPNA != null)
                            comPortOPNA.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitOPNA);
                        break;
                    case 6:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPNA_Port, (int)0, false);
                        break;
                    case 7:
                        comPortOPNA = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPNA_Port, (int)0, false);
                        if (comPortOPNA != null)
                            comPortOPNA.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnOPNA.Checked = comPortOPNA != null;
                comboBoxOPNA.Enabled = comPortOPNA == null;
                comboBoxPortOPNA.Enabled = comPortOPNA == null;
                numericUpDownOPNADiv.Enabled = comPortOPNA == null;

            }
            else
            {
                comboBoxOPNA.Enabled = true;
                comboBoxPortOPNA.Enabled = true;
                numericUpDownOPNADiv.Enabled = true;

                comPortOPNA?.Dispose();
            }
        }


        private VsifClient comPortOPNB;

        private void checkBoxConnOPNB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnOPNB.Checked)
            {
                switch (Settings.Default.OPNB_IF)
                {
                    case 0:
                        comPortOPNB = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.OPNB_Port, (int)0, false);
                        if (comPortOPNB != null)
                            comPortOPNB.Tag["OPNB.Type"] = comboBoxOPNBType.SelectedIndex;
                        break;
                    case 1:
                        comPortOPNB = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.OPNB_Port, (int)0, false);
                        if(comPortOPNB != null)
                            comPortOPNB.Tag["OPNB.Type"] = comboBoxOPNBType.SelectedIndex;
                        if (comPortOPNB != null)
                            comPortOPNB.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnOPNB.Checked = comPortOPNB != null;
                comboBoxOPNB.Enabled = comPortOPNB == null;
                comboBoxPortOPNB.Enabled = comPortOPNB == null;
                //numericUpDownOPNBDiv.Enabled = comPortOPNB == null;

            }
            else
            {
                comboBoxOPNB.Enabled = true;
                comboBoxPortOPNB.Enabled = true;
                //numericUpDownOPNBDiv.Enabled = true;

                comPortOPNB?.Dispose();
            }
        }

        private VsifClient comPortY8950;

        private void checkBoxConnY8950_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnY8950.Checked)
            {
                switch (Settings.Default.Y8950_IF)
                {
                    case 0:
                        comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_FTDI,
                            (PortId)Settings.Default.Y8950_Port, (int)Settings.Default.Y8950Div, false);
                        if (comPortY8950 != null)
                            comPortY8950.Tag["Y8950.Slot"] = comboBoxY8950Slot.SelectedIndex;
                        break;
                    case 1:
                        comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.TurboR_FTDI,
                            (PortId)Settings.Default.Y8950_Port, (int)Settings.Default.Y8950Div, false);
                        if (comPortY8950 != null)
                            comPortY8950.Tag["Y8950.Slot"] = comboBoxY8950Slot.SelectedIndex;
                        if (comPortY8950 != null)
                            comPortY8950.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)Settings.Default.BitBangWaitY8950);
                        break;
                    case 2:
                        comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_Pi,
                            (PortId)Settings.Default.Y8950_Port, (int)0, false);
                        if (comPortY8950 != null)
                            comPortY8950.Tag["Y8950.Slot"] = comboBoxY8950Slot.SelectedIndex;
                        break;
                    case 3:
                        comPortY8950 = VsifManager.TryToConnectVSIF(VsifSoundModuleType.MSX_PiTR,
                            (PortId)Settings.Default.Y8950_Port, (int)0, false);
                        if (comPortY8950 != null)
                            comPortY8950.Tag["Y8950.Slot"] = comboBoxY8950Slot.SelectedIndex;
                        if (comPortY8950 != null)
                            comPortY8950.DeferredWriteData(0x15, (byte)0x0, (byte)127, (int)0);
                        break;
                }
                checkBoxConnY8950.Checked = comPortY8950 != null;
                comboBoxY8950.Enabled = comPortY8950 == null;
                comboBoxPortY8950.Enabled = comPortY8950 == null;
                comboBoxY8950Slot.Enabled = comPortY8950 == null;
                numericUpDown8950Div.Enabled = comPortY8950 == null;
            }
            else
            {
                comboBoxY8950.Enabled = true;
                comboBoxPortY8950.Enabled = true;
                comboBoxY8950Slot.Enabled = true;
                numericUpDown8950Div.Enabled = true;

                comPortY8950?.Dispose();
            }
        }


        private VsifClient comPortNES;

        private void checkBoxConnNES_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnNES.Checked)
            {
                switch (Settings.Default.NES_IF)
                {
                    case 0:
                        comPortNES = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NES_FTDI_DIRECT,
                            (PortId)Settings.Default.NES_Port, (int)Settings.Default.NESDiv, false);
                        break;
                    case 1:
                        comPortNES = VsifManager.TryToConnectVSIF(VsifSoundModuleType.NES_FTDI_INDIRECT,
                            (PortId)Settings.Default.NES_Port, (int)Settings.Default.NESDiv, false);
                        break;
                }

                checkBoxConnNES.Checked = comPortNES != null;
                comboBoxNES.Enabled = comPortNES == null;
                comboBoxPortNES.Enabled = comPortNES == null;
                numericUpDownNESDiv.Enabled = comPortNES == null;
            }
            else
            {
                comboBoxNES.Enabled = true;
                comboBoxPortNES.Enabled = true;
                numericUpDownNESDiv.Enabled = true;

                comPortNES?.Dispose();
            }
        }

        private void listViewList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            switch (listViewList.Sorting)
            {
                case SortOrder.Ascending:
                    listViewList.Sorting = SortOrder.Descending;
                    listViewList.Columns[0].Text = "File name ▼";
                    break;
                case SortOrder.Descending:
                    listViewList.Sorting = SortOrder.None;
                    listViewList.Columns[0].Text = "File name";
                    break;
                case SortOrder.None:
                    listViewList.Sorting = SortOrder.Ascending;
                    listViewList.Columns[0].Text = "File name ▲";
                    break;
            }
        }

        private void buttonPlay_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void buttonPlay_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            addFilesAndPlay(files);
        }

        private void addFilesAndPlay(string[] files)
        {
            try
            {
                listViewList.BeginUpdate();

                listViewList.Items.Clear();

                ListViewItem lvi = null;
                try
                {
                    listViewList.BeginUpdate();

                    listViewList.SelectedItems.Clear();
                    lvi = addAllFiles(files, lvi);
                }
                finally
                {
                    listViewList.EndUpdate();
                    lvi?.EnsureVisible();
                }
            }
            finally
            {
                listViewList.EndUpdate();
            }

            playSelectedItem();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure want to clear all list items?", "Confirmation", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.OK)
            {
                try
                {
                    listViewList.BeginUpdate();
                    listViewList.Items.Clear();
                }
                finally
                {
                    listViewList.EndUpdate();
                }
            }
        }

        private void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            playSelectedItem();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            removeSelectedItem();
        }

        private void explorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = listViewList.FocusedItem;
            if (item != null)
            {
                Task.Run(new Action(() =>
                {
                    Process.Start("explorer.exe", "/select,\"" + item.Text + "\"");
                }));
            }
        }

        private void buttonEject_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(openFileDialog.InitialDirectory))
            {
                if (String.IsNullOrEmpty(Settings.Default.LastDir) || !Directory.Exists(Settings.Default.LastDir))
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                else
                    openFileDialog.InitialDirectory = Settings.Default.LastDir;
            }

            //ダイアログを表示する
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                addFilesToList(openFileDialog.FileNames);

                openFileDialog.InitialDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                Settings.Default.LastDir = openFileDialog.InitialDirectory;
            }
        }

        private class ListViewIndexComparer : System.Collections.IComparer
        {
            private ListView listView;

            public ListViewIndexComparer(ListView listView)
            {
                this.listView = listView;
            }

            public int Compare(object x, object y)
            {
                if (listView.Sorting == SortOrder.Ascending)
                    return ((ListViewItem)x).Text.CompareTo(((ListViewItem)y).Text);
                else if (listView.Sorting == SortOrder.Descending)
                    return ((ListViewItem)y).Text.CompareTo(((ListViewItem)x).Text);
                else
                    return 0;
            }
        }

        private void aBOUTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout ab = new FormAbout();
            ab.Text = this.Text;
            ab.ShowDialog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetStatusText(string text)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                if (!IsDisposed)
                {
                    toolStripStatusLabel.Text = text;
                }
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetElapsedTime(TimeSpan timeSpan)
        {
            labelElapsed.BeginInvoke(new MethodInvoker(() =>
            {
                if (!labelElapsed.IsDisposed)
                {
                    labelElapsed.Text = timeSpan.ToString(@"hh\:mm\:ss");
                    labelElapsed.Tag = timeSpan;
                    //toolStripStatusLabel.Text = "Playing";
                }
            }));
        }

        private void numericUpDownLooped_ValueChanged(object sender, EventArgs e)
        {
            if (currentSong != null)
                currentSong.LoopedCount = (int)numericUpDownLooped.Value;
        }

        private void dateTimePickerLoopTimes_ValueChanged(object sender, EventArgs e)
        {
            if (currentSong != null)
                currentSong.LoopTimes = new TimeSpan(dateTimePickerLoopTimes.Value.Hour, dateTimePickerLoopTimes.Value.Minute, dateTimePickerLoopTimes.Value.Second);
        }

        private void checkBoxLoopTimes_CheckedChanged(object sender, EventArgs e)
        {
            if (currentSong != null)
                currentSong.LoopByElapsed = checkBoxLoopTimes.Checked;
        }

        private void label3_Click(object sender, EventArgs e)
        {
            int row = tableLayoutPanelPort.GetRow((Control)sender);
            CheckBox cb = (CheckBox)tableLayoutPanelPort.GetControlFromPosition(0, row);
            cb.Checked = !cb.Checked;
        }

        private void label3_DoubleClick(object sender, EventArgs e)
        {
            int row = tableLayoutPanelPort.GetRow((Control)sender);
            CheckBox cb = (CheckBox)tableLayoutPanelPort.GetControlFromPosition(0, row);
            cb.Checked = !cb.Checked;
        }

        private void label8_DoubleClick(object sender, EventArgs e)
        {
            for (int i = 0; i < tableLayoutPanelPort.RowCount; i++)
            {
                CheckBox cb = tableLayoutPanelPort.GetControlFromPosition(0, i) as CheckBox;
                if (cb != null)
                    cb.Checked = !cb.Checked;
            }
        }

        private void checkBoxCnvClk_CheckedChanged(object sender, EventArgs e)
        {
            if (currentSong != null)
                currentSong.ConvertChipClock = checkBoxCnvClk.Checked;
        }


        private void loadPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(openFileDialog.InitialDirectory))
            {
                if (String.IsNullOrEmpty(Settings.Default.LastDir) || !Directory.Exists(Settings.Default.LastDir))
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                else
                    openFileDialog.InitialDirectory = Settings.Default.LastDir;
            }

            //ダイアログを表示する
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                addFilesAndPlay(openFileDialog.FileNames);

                openFileDialog.InitialDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                Settings.Default.LastDir = openFileDialog.InitialDirectory;
            }
        }

        private void addPlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            buttonEject_Click(null, null);
        }

        private void saveAsThePlaylistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(saveFileDialogM3U.InitialDirectory))
            {
                if (String.IsNullOrEmpty(Settings.Default.LastDir) || !Directory.Exists(Settings.Default.LastDir))
                    saveFileDialogM3U.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                else
                    saveFileDialogM3U.InitialDirectory = Settings.Default.LastDir;
            }

            var r = saveFileDialogM3U.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                string f = saveFileDialogM3U.FileName;
                using (var sw = File.CreateText(f))
                {
                    foreach (ListViewItem item in listViewList.Items)
                        sw.WriteLine(item.Text);
                }

                saveFileDialogM3U.InitialDirectory = Path.GetDirectoryName(saveFileDialogM3U.FileName);
                Settings.Default.LastDir = saveFileDialogM3U.InitialDirectory;
            }
        }

        private void oPTIONSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form f = new FormSettings())
            {
                f.ShowDialog(this);
            }
        }

        private void vgmripsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var web = new FormWeb(this);
            web.Location = new Point(Location.X + Width, Location.Y);
            web.Show(this);
        }

    }

    internal static class NativeConstants
    {
        public const int WM_APPCOMMAND = 0x0319;
    }

    internal enum ApplicationCommand
    {
        VolumeMute = 8,
        VolumeDown = 9,
        VolumeUp = 10,
        MediaNexttrack = 11,
        MediaPrevioustrack = 12,
        MediaStop = 13,
        MediaPlayPause = 14,
        Close = 31,
        MediaPlay = 46,
        MediaPause = 47,
        MediaFastForward = 49,
        MediaRewind = 50
    }
}
