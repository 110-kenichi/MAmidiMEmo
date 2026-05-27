// copyright-holders:K.Ito
using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zanac.MAmidiMEmo.ComponentModel;
using zanac.MAmidiMEmo.Gui;
using zanac.MAmidiMEmo.Instruments;
using zanac.MAmidiMEmo.Properties;
using zanac.MAmidiMEmo.TurboLink;
using zanac.MAmidiMEmo.VSIF;

namespace zanac.MAmidiMEmo.VSIF
{

    public static class VsifManager
    {
        //http://analoghome.blogspot.com/2017/08/ftdi-ft232r-usb-to-serial-bridge.html
        //The maximum BAUD rate for the FT232R chip is 3M BAUD

        public const int FTDI_BAUDRATE_GEN = 11;
        public const int FTDI_BAUDRATE_GEN_CLK_WIDTH = 9;

        public const int FTDI_BAUDRATE_NES = 352941;
        public const int FTDI_BAUDRATE_NES_CLK_WIDTH = 11;

        public const int FTDI_BAUDRATE_C64 = 72289;
        public const int FTDI_BAUDRATE_C64_CLK_WIDTH = 25;

        public const int FTDI_BAUDRATE_MSX = 240000;
        public const int FTDI_BAUDRATE_MSX_CLK_WIDTH = 25;

        public const int FTDI_BAUDRATE_88 = 240000;
        public const int FTDI_BAUDRATE_88_CLK_WIDTH = 25;

        private static object lockObject = new object();

        private static List<VsifClient> vsifClients = new List<VsifClient>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iSoundChipType"></param>
        /// <param name="clock"></param>
        /// <returns></returns>
        public static VsifClient[] TryGetConnectedVSIF(VsifSoundModuleType soundModule)
        {
            List<VsifClient> list = new List<VsifClient>();

            lock (lockObject)
            {
                foreach (var c in vsifClients)
                {
                    if (c.SoundModuleType == soundModule)
                        list.Add(c);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iSoundChipType"></param>
        /// <param name="clock"></param>
        /// <returns></returns>
        public static VsifClient TryToConnectVSIF(VsifSoundModuleType soundModule, PortId comPort)
        {
            return TryToConnectVSIF(soundModule, comPort, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iSoundChipType"></param>
        /// <param name="clock"></param>
        /// <returns></returns>
        public static VsifClient TryToConnectVSIF(VsifSoundModuleType soundModule, PortId comPort, bool shareOnly)
        {
            lock (lockObject)
            {
                foreach (var c in vsifClients)
                {
                    if (c.SoundModuleType == soundModule)
                    {
                        if (c.DataWriter.PortName.Equals("COM" + (int)(comPort + 1)))
                        {
                            c.ReferencedCount++;
                            return c;
                        }
                        if (c.DataWriter.PortName.Equals("FTDI_COM" + (int)comPort))
                        {
                            c.ReferencedCount++;
                            return c;
                        }
                    }
                }
                if (shareOnly)
                    return null;

                try
                {
                    switch (soundModule)
                    {
                        case VsifSoundModuleType.SMS:
                            {
                                SerialPort sp = null;
                                sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.WriteTimeout = 100;
                                sp.ReadTimeout = 100;
                                sp.BaudRate = 115200;
                                sp.StopBits = StopBits.Two;
                                //sp.BaudRate = 57600;
                                //sp.StopBits = StopBits.One;
                                sp.Parity = Parity.None;
                                sp.DataBits = 8;
                                sp.Handshake = Handshake.None;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterGenesis(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.SMS_FTDI:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    ftdi.SetBaudRate(FTDI_BAUDRATE_MSX);
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);

                                    var client = new VsifClient(soundModule, new PortWriterSms(ftdi, comPort));
                                    client.WriteData(0, 0, 0, (int)100);  //Dummy

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.Genesis:
                            {
                                SerialPort sp = null;
                                sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.WriteTimeout = 500;
                                sp.ReadTimeout = 500;
                                //sp.BaudRate = 230400;
                                sp.BaudRate = 163840;
                                sp.StopBits = StopBits.One;
                                sp.Parity = Parity.None;
                                sp.DataBits = 8;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterGenesis(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.Genesis_Low:
                            {
                                SerialPort sp = null;
                                sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.WriteTimeout = 500;
                                sp.ReadTimeout = 500;
                                sp.BaudRate = 115200;
                                sp.StopBits = StopBits.One;
                                sp.Parity = Parity.None;
                                sp.DataBits = 8;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterGenesis(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.Genesis_FTDI:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    var ofst = FTDI_BAUDRATE_GEN;
                                    if (ofst < 0)
                                        ofst = 0;
                                    ftdi.SetBaudRate((uint)(3000000 / (ofst + 0.5)));
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);

                                    var client = new VsifClient(soundModule, new PortWriterGenesis(ftdi, comPort));

                                    for (int i = 0; i < 2; i++)
                                    {
                                        client.WriteData(0, 0x14, (byte)(0x80 | 0x1f), (int)11);
                                        client.WriteData(0, 0x14, (byte)(0x80 | 0x3f), (int)11);
                                        client.WriteData(0, 0x14, (byte)(0x80 | 0x5f), (int)11);
                                        client.WriteData(0, 0x14, (byte)(0x80 | 0x7f), (int)11);
                                    }

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.MSX_FTDI:
                        case VsifSoundModuleType.TurboR_FTDI:
                        case VsifSoundModuleType.P6_FTDI:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    ftdi.SetBaudRate(FTDI_BAUDRATE_MSX);
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);

                                    var client = new VsifClient(soundModule, new PortWriterMsx(ftdi, comPort));
                                    client.WriteData(0, 0, 0, (int)100);  //Dummy

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.PC88_FTDI:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    ftdi.SetBaudRate(FTDI_BAUDRATE_88);
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);

                                    var client = new VsifClient(soundModule, new PortWriterPC88(ftdi, comPort));
                                    client.WriteData(0, 0, 0, (int)100);  //Dummy

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.NES_FTDI_INDIRECT:
                        case VsifSoundModuleType.NES_FTDI_DIRECT:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    ftdi.SetBaudRate(FTDI_BAUDRATE_NES);
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);
                                    byte ps = 0;
                                    ftdi.GetPinStates(ref ps);
                                    {
                                        uint dummy = 0;
                                        ftdi.Write(new byte[] { 0xff }, 1, ref dummy);
                                    }

                                    VsifClient client = null;
                                    switch (soundModule)
                                    {
                                        case VsifSoundModuleType.NES_FTDI_DIRECT:
                                            client = new VsifClient(soundModule, new PortWriterNesDirect(ftdi, comPort));
                                            break;
                                        case VsifSoundModuleType.NES_FTDI_INDIRECT:
                                            client = new VsifClient(soundModule, new PortWriterNesIndirect(ftdi, comPort));
                                            break;
                                    }

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.C64_FTDI:
                            {
                                var ftdi = new FTD2XX_NET.FTDI();
                                var stat = ftdi.OpenByIndex((uint)comPort);
                                if (stat == FTDI.FT_STATUS.FT_OK)
                                {
                                    ftdi.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                                    ftdi.SetBitMode(0xff, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                                    ftdi.SetBaudRate(FTDI_BAUDRATE_C64);
                                    ftdi.SetTimeouts(500, 500);
                                    ftdi.SetLatency(0);
                                    {
                                        uint dummy = 0;
                                        ftdi.Write(new byte[] { 0x1f }, 1, ref dummy);
                                    }

                                    VsifClient client = new VsifClient(soundModule, new PortWriterC64(ftdi, comPort));

                                    client.Disposed += Client_Disposed;
                                    vsifClients.Add(client);
                                    return client;
                                }
                            }
                            break;
                        case VsifSoundModuleType.TurboEverDrive:
                            {
                                SerialPort sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.ReadTimeout = 300;
                                sp.WriteTimeout = 300;
                                sp.WriteBufferSize = 2;
                                sp.Open();
                                TurboEvedriveManager.getStatus(sp);
                                var client = new VsifClient(soundModule, new PortWriterTurboEverDrive(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.NanoDrive:
                            {
                                SerialPort sp = null;
                                sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.WriteTimeout = 100;
                                sp.ReadTimeout = 100;
                                sp.BaudRate = 921600;
                                sp.StopBits = StopBits.Two;
                                sp.Parity = Parity.None;
                                sp.DataBits = 8;
                                sp.Handshake = Handshake.None;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterNanoDrive(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.AMIGA:
                            {
                                SerialPort sp = null;
                                sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.WriteTimeout = 500;
                                sp.ReadTimeout = 500;
                                sp.BaudRate = 31250;
                                sp.StopBits = StopBits.One;
                                sp.Parity = Parity.None;
                                sp.DataBits = 8;
                                sp.Handshake = Handshake.None;
                                //sp.Handshake = Handshake.RequestToSend;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterAMIGA(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                        case VsifSoundModuleType.MSX_Pi:
                        case VsifSoundModuleType.MSX_PiTR:
                            {
                                SerialPort sp = new SerialPort("COM" + ((int)comPort + 1));
                                sp.Handshake = Handshake.RequestToSend;
                                sp.ReadTimeout = 1000;
                                sp.WriteTimeout = 1000;
                                sp.WriteBufferSize = 2;
                                sp.BaudRate = 921600;
                                sp.Open();
                                var client = new VsifClient(soundModule, new PortWriterMsxPi(sp));
                                client.Disposed += Client_Disposed;
                                vsifClients.Add(client);
                                return client;
                            }
                    }

                    //sp.Write(new byte[] { (byte)'M', (byte)'a', (byte)'M', (byte)'i' }, 0, 4);
                    //sp.BaseStream.WriteByte((byte)soundModule);
                    //Thread.Sleep(100);
                    //var ret = sp.BaseStream.ReadByte();
                    //if (ret == 0x0F)   //OK
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(Exception))
                        throw;
                    else if (ex.GetType() == typeof(SystemException))
                        throw;
                }
                return null;
            }
        }

        private static void Client_Disposed(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                foreach (var c in vsifClients)
                {
                    if (c == sender)
                    {
                        vsifClients.Remove(c);
                        break;
                    }
                }
            }
        }
    }

    public enum VsifSoundModuleType
    {
        None,
        SMS,
        Genesis,
        Genesis_FTDI,
        Genesis_Low,
        NES_FTDI_INDIRECT,
        NES_FTDI_DIRECT,
        MSX_FTDI,
        C64_FTDI,
        P6_FTDI,
        PC88_FTDI,
        SMS_FTDI,
        TurboR_FTDI,
        TurboEverDrive,
        NanoDrive,
        AMIGA,
        MSX_Pi,
        MSX_PiTR,
    }


}
