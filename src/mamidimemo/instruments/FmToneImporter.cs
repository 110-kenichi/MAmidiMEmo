using FM_SoundConvertor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.Util.FITOM;
using zanac.MAmidiMEmo.Util.Syx;
using static System.Net.Mime.MediaTypeNames;

namespace zanac.MAmidiMEmo.Instruments
{
    public abstract class FmToneImporter : CustomToneImporter
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public override IEnumerable<Tone> ImportToneFile(string file)
        {
            string ext = System.IO.Path.GetExtension(file);
            IEnumerable<Tone> tones = null;

            var exts = ExtensionsFilterExt.Split(new char[] { ';' });
            string mext = System.IO.Path.GetExtension(exts[0]).ToUpper(CultureInfo.InvariantCulture);
            if (ext.ToUpper(CultureInfo.InvariantCulture).Equals(mext))
            {
                try
                {
                    string txt = System.IO.File.ReadAllText(file);
                    tones = ImportTone(txt);
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
            else
            {
                try
                {
                    string[] importFile = { file.ToLower(CultureInfo.InvariantCulture) };
                    switch (ext.ToUpper(CultureInfo.InvariantCulture))
                    {
                        case ".MUC":
                            tones = Muc.Reader(file);
                            break;
                        case ".DAT":
                            tones = Dat.Reader(file);
                            break;
                        case ".MWI":
                            tones = Fmp.Reader(file);
                            break;
                        case ".MML":
                            tones = Pmd.Reader(file);
                            break;
                        case ".FXB":
                            tones = Vopm.Reader(file);
                            break;
                        case ".GWI":
                            tones = Gwi.Reader(file);
                            break;
                        case ".BNK":
                            tones = BankReader.Read(file);
                            break;
                        case ".SYX":
                            tones = SyxReaderTX81Z.Read(file);
                            break;
                        case ".FF":
                            tones = FF.Reader(file);
                            break;
                        case ".FFOPM":
                            tones = FF.Reader(file);
                            break;
                        case ".VGI":
                            tones = Vgi.Reader(file);
                            break;
                        case ".TFI":
                            tones = Tfi.Reader(file);
                            break;
                        default:

                            break;
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
        /// <param name="txt"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public override IEnumerable<Tone> ImportTone(String txt)
        {
            IEnumerable<Tone> tones = null;
            StringReader rs = new StringReader(txt);

            string ftname = rs.ReadLine().ToUpper(CultureInfo.InvariantCulture);
            var exts = ExtensionsFilterExt.Split(new char[] { ';' });
            string fullTypeName = exts[0];

            if (fullTypeName.ToUpper(CultureInfo.InvariantCulture).Equals(ftname))
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
                        t.MML = lines.ToArray();
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

            return tones;
        }


    }
}
