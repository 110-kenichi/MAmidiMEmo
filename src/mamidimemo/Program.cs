// copyright-holders:K.Ito
using Accessibility;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using MetroFramework;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Instruments;
using zanac.MAmidiMEmo.Mame;
using zanac.MAmidiMEmo.Properties;
using MetroFramework.Components;

namespace zanac.MAmidiMEmo
{
    public static class Program
    {
        /// <summary>
        /// 
        /// </summary>
        public const string FILE_VERSION = "6.4.3.3";

        public const string FILE_COPYRIGHT = @"Virtual chiptune sound MIDI module ""MAmidiMEmo"" Version {0}
Copyright(C) 2019, 2026 Itoken.All rights reserved.";

        public static ISerializationBinder SerializationBinder = new KnownTypesBinder();

        public static readonly JsonSerializerSettings JsonAutoSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = SerializationBinder };

        private static Thread mainThread;

        internal static string RestartRequiredApplication;

        public static event EventHandler ShuttingDown;

#pragma warning disable CS0414
        /// <summary>
        /// ダミー(遅延Assemblyロード回避)
        /// </summary>
        private static readonly MultilineStringEditor dummyEditor = new MultilineStringEditor();

        /// <summary>
        /// ダミー(遅延Assemblyロード回避)
        /// </summary>
        private static readonly AnnoScope dummyAnnoScope = AnnoScope.ANNO_CONTAINER;
#pragma warning restore  CS0414

        private static readonly Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        private static Dictionary<string, Type> assemblieTypes;

        private static Dictionary<string, Type> AssemblieTypes
        {
            get
            {
                if (assemblieTypes == null)
                {
                    assemblieTypes = new Dictionary<string, Type>();
                    Type[] ts = Assembly.GetExecutingAssembly().GetTypes();
                    //ts = AppDomain.CurrentDomain.GetAssemblies()
                    //            .Where(a => a.FullName.StartsWith("MAmidiMEmoUI,"))
                    //            .SelectMany(a => a.GetTypes()).ToArray();
                    foreach (var t in ts)
                    {
                        var attr = t.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault() as DataContractAttribute;
                        if (attr != null)
                            assemblieTypes.Add(t.Name, t);
                        foreach (var nt in GetAllNestedTypes(t))
                        {
                            attr = nt.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault() as DataContractAttribute;
                            if (attr == null)
                                continue;

                            string n = nt.FullName;
                            if (n.Contains("."))
                                n = n.Substring(n.LastIndexOf(".") + 1);
                            assemblieTypes.Add(n, nt);
                        }
                    }
                }
                return assemblieTypes;
            }
        }

        private static int f_CurrentSamplingRate;

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentSamplingRate
        {
            get
            {
                return f_CurrentSamplingRate;
            }
            set
            {
                f_CurrentSamplingRate = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static string MAmiDir
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }

        public class InterfaceImplementer : RealProxy, IRemotingTypeInfo
        {
            readonly Type _type;
            readonly Func<MethodInfo, IMethodCallMessage, object> _callback;

            public InterfaceImplementer(Type type, Func<MethodInfo, IMethodCallMessage, object> callback) : base(type)
            {
                _callback = callback;
                _type = type;
            }

            public override IMessage Invoke(IMessage msg)
            {
                var call = msg as IMethodCallMessage;

                if (call == null)
                    throw new NotSupportedException();

                var method = (MethodInfo)call.MethodBase;

                return new ReturnMessage(_callback(method, call), null, 0, call.LogicalCallContext, call);
            }

            public bool CanCastTo(Type fromType, object o) => fromType == _type;

            public string TypeName { get; set; }
        }

        private static float guiScale;

        public static float GuiScale
        {
            get
            {
                return guiScale;
            }
        }

        public static readonly float DpiScale = 1f; // ((new System.Windows.Forms.Form()).CreateGraphics().DpiX) / 96;

        public static readonly float DpiScaleResolver = ((new System.Windows.Forms.Form()).CreateGraphics().DpiX) / 96;

        static object ResolveFont(MethodInfo info, IMethodCallMessage msg)
        {
            float sz = (float)msg.Args[1];
            //sz = System.Drawing.SystemFonts.DefaultFont.Size;
            sz *= ((100f + (float)Settings.Default.GuiScale) / 100f);
            sz *= DpiScaleResolver;
            //sz = sz + (sz * ((((DpiScaleResolver * 100f) - 100f) + (float)Settings.Default.GuiScale) / 100f));

            return new Font((String)msg.Args[0], sz,
                (System.Drawing.FontStyle)msg.Args[2], (GraphicsUnit)msg.Args[3]);
        }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        /// <param name="parentModule">親モジュール</param>
        public static void Main(IntPtr parentModule)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            System.Resources.ResourceManager rm =
                new System.Resources.ResourceManager("System", typeof(UriFormat).Assembly);
            string dummy = rm.GetString("Arg_EmptyOrNullString");

            ThreadPool.SetMaxThreads(250, 1000);

            MameIF.Initialize(parentModule);
            var threadStart = new ManualResetEvent(false);
            mainThread = new Thread(new ThreadStart(() =>
            {
                threadStart.Set();
                Settings.Default.Reload();

                if (string.IsNullOrWhiteSpace(Settings.Default.OutputDir))
                    Settings.Default.OutputDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                f_CurrentSamplingRate = 48000;
                int.TryParse(Settings.Default.SampleRate, out f_CurrentSamplingRate);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                guiScale = ((((DpiScale * 100f) - 100f) + (float)Settings.Default.GuiScale) / 100f);
                //if (guiScale != 0)
                {
                    var internalType = typeof(MetroFonts).Assembly.GetType("MetroFramework.MetroFonts+IMetroFontResolver");
                    var result = new InterfaceImplementer(internalType, ResolveFont).GetTransparentProxy();

                    var field = typeof(MetroFonts).GetField("FontResolver", BindingFlags.Static | BindingFlags.NonPublic);
                    field.SetValue(typeof(MetroFonts), result);
                }
                //MetroStyleManager.Default.Theme = MetroFramework.MetroThemeStyle.Dark;

                using (var fs = new FormSplash())
                {
                    fs.Show();
                    if (!IsVSTiMode())
                    {
                        while (fs.Opacity != 1)
                        {
                            fs.Opacity += 0.1;
                            Thread.Sleep(50);
                            fs.Refresh();
                        }
                    }
                    else
                    {
                        formSplash = fs;
                        fs.Opacity += 1;
                        fs.Refresh();
                    }

                    try
                    {
                        var fm = new FormMain();
                        fm.Shown += (_, __) =>
                        {
                            if (!IsVSTiMode())
                            {
                                fm.BeginInvoke(new MethodInvoker(() => { fs.Close(); }));

                                if (!string.IsNullOrEmpty(Settings.Default.EnvironmentSettings))
                                {
                                    try
                                    {
                                        var dso = StringCompressionUtility.Decompress(Settings.Default.EnvironmentSettings);
                                        InstrumentManager.ClearAllInstruments();
                                        var settings = JsonConvert.DeserializeObject<EnvironmentSettings>(dso, JsonAutoSettings);
                                        InstrumentManager.RestoreSettings(settings);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex.GetType() == typeof(Exception))
                                            throw;
                                        else if (ex.GetType() == typeof(SystemException))
                                            throw;

                                        MessageBox.Show(ex.ToString());
                                    }
                                }
                            }
                        };
                        //if (IsVSTiMode())
                        //    Application.Idle += (_, __) => MameIF.ParameterAutomated();
                        Application.Run(fm);
                        if (!IsVSTiMode())
                        {
                            var so = JsonConvert.SerializeObject(SaveEnvironmentSettings(), Formatting.Indented, JsonAutoSettings);
                            Settings.Default.EnvironmentSettings = StringCompressionUtility.Compress(so);
                        }
                        Settings.Default.Save();
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType() == typeof(Exception))
                            throw;
                        else if (ex.GetType() == typeof(SystemException))
                            throw;

                        MessageBox.Show(ex.ToString());
                    }

                    ShuttingDown?.Invoke(typeof(Program), EventArgs.Empty);
                }
            }))
            {
                Priority = ThreadPriority.BelowNormal
            };
            mainThread.SetApartmentState(ApartmentState.STA);
            mainThread.Start();
            threadStart.WaitOne();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static EnvironmentSettings SaveEnvironmentSettings()
        {
            var es = new EnvironmentSettings();
            try
            {
                InstrumentManager.InstExclusiveLockObject.EnterReadLock();

                InstrumentManager.SaveSettings(es);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                InstrumentManager.InstExclusiveLockObject.ExitReadLock();
            }
            return es;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void RestartApplication()
        {
            if (RestartRequiredApplication != null)
                Process.Start(RestartRequiredApplication);
        }


        /// <summary>
        /// 
        /// </summary>
        public static void CloseApplication()
        {
            var so = JsonConvert.SerializeObject(SaveEnvironmentSettings(), Formatting.Indented, JsonAutoSettings);
            Settings.Default.EnvironmentSettings = StringCompressionUtility.Compress(so);
            InstrumentManager.ClearAllInstruments();

            FormMain.AppliactionForm?.ForceClose();
            /*
            Application.Exit();
            */
        }

        private static IntPtr saveDataPtr;

        unsafe public static int SaveData(void** saveBuf)
        {
            try
            {
                InstrumentManager.InstExclusiveLockObject.EnterReadLock();

                FormMain.AppliactionForm?.SaveWindowStatus();

                var es = Program.SaveEnvironmentSettings();
                string data = JsonConvert.SerializeObject(es, Formatting.Indented, Program.JsonAutoSettings);
                string comdata = StringCompressionUtility.Compress(data);
                byte[] buf = Encoding.Unicode.GetBytes(comdata);

                if (saveDataPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(saveDataPtr);
                saveDataPtr = Marshal.AllocHGlobal(buf.Length);
                Marshal.Copy(buf, 0, saveDataPtr, buf.Length);

                *saveBuf = saveDataPtr.ToPointer();
                return buf.Length;
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

            }
            finally
            {
                InstrumentManager.InstExclusiveLockObject.ExitReadLock();
            }
            return 0;
        }

        unsafe public static void LoadData(byte* data, int length)
        {
            try
            {
                string text = StringCompressionUtility.Decompress(Encoding.Unicode.GetString(data, length));
                InstrumentManager.ClearAllInstruments();
                var settings = JsonConvert.DeserializeObject<EnvironmentSettings>(text, Program.JsonAutoSettings);
                InstrumentManager.RestoreSettings(settings);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                MessageBox.Show(ex.ToString());

                try
                {
                    string text2 = Encoding.Unicode.GetString(data, length);
                    string tmp = Path.GetTempFileName();
                    File.WriteAllText(tmp, text2);
                    MessageBox.Show($"Please send the {tmp} file for debugging.");
                }
                catch (Exception ex2)
                {
                    if (ex.GetType() == typeof(Exception))
                        throw;
                    else if (ex.GetType() == typeof(SystemException))
                        throw;

                    MessageBox.Show(ex2.ToString());
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int HasExited()
        {
            var ret = mainThread.IsAlive ? 0 : 1;
            return ret;
        }

        public static FormSplash formSplash;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static void VstStarted()
        {
            formSplash?.BeginInvoke(new MethodInvoker(()
                =>
            {
                if (formSplash.IsDisposed)
                    return;

                formSplash.Close();
            }));
        }

        private static ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// MAMEとMAmiの排他制御
        /// MAmiがMAMEのレジスタを書き換えるときに呼び出す必要がある
        /// </summary>
        public static void SoundUpdating()
        {
            lockSlim.EnterWriteLock();
        }

        /// <summary>
        /// MAMEとMAmiの排他制御
        /// MAmiがMAMEのレジスタを書き換えたあとに呼び出す必要がある
        /// </summary>
        public static void SoundUpdated()
        {
            lockSlim.ExitWriteLock();
        }


        private static bool vstiMode;

        /// <summary>
        /// 
        /// </summary>
        public static void SetVSTiMode()
        {
            vstiMode = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public static bool IsVSTiMode()
        {
            return vstiMode;
        }

        /// <summary>
        /// MAMEとMAmiの排他制御
        /// MAmiがMAMEのレジスタを書き換えるときに呼び出す必要がある
        /// </summary>
        public static bool TryEnterWriteLock(int wait)
        {
            return lockSlim.TryEnterWriteLock(wait);
        }

        /// <summary>
        /// </summary>
        public static bool IsWriteLockHeld()
        {
            return lockSlim.IsWriteLockHeld;
        }

        private class KnownTypesBinder : ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName)
            {
                Type t = Type.GetType(typeName);
                if (t != null)
                    return t;

                if (typeName.Contains("."))
                    typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);

                switch (typeName)
                {
                    case "DeltaPcmSound":
                        typeName = "DeltaPcmTimbre";
                        break;
                    case "AdpcmSoundTable":
                        typeName = "YM2610BAdpcmSoundTable";
                        break;
                    case "AdpcmTimbre":
                        typeName = "YM2610BAdpcmTimbre";
                        break;
                }
                if (AssemblieTypes.ContainsKey(typeName))
                    return AssemblieTypes[typeName];

                return null;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (serializedType.Assembly == Assembly.GetExecutingAssembly())
                {
                    assemblyName = null;
                    typeName = serializedType.Name;
                }
                else
                {
                    typeName = serializedType.FullName;
                    assemblyName = serializedType.Assembly.FullName;
                }
            }
        }

        private static Type[] GetAllNestedTypes(Type type)
        {
            List<Type> types = new List<Type>();
            AddNestedTypesRecursively(types, type);
            return types.ToArray();
        }

        private static void AddNestedTypesRecursively(List<Type> types, Type type)
        {
            Type[] nestedTypes = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            foreach (Type nestedType in nestedTypes)
            {
                types.Add(nestedType);
                AddNestedTypesRecursively(types, nestedType);
            }
        }

        public static string GetToneLibLastDir()
        {
            string dir = Settings.Default.ToneLibLastDir;
            try
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    dir = Path.Combine(dir, "MAmi");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(Settings.Default.ToneLibLastDir);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(Exception))
                    throw;
                else if (ex.GetType() == typeof(SystemException))
                    throw;

                dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            }
            return dir;
        }
    }
}
