using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace Uzycie_Manipulatora
{

    class MyEventArgs : EventArgs//argumenty eventu
    {
        public Urzadzenie urzadzenie { get; set; }
    }


    ///////////////////////////////////////////////////////////////////////////////
    /// \file    Manipulator.cs 
    ///	\brief   This class includes HID communication methods.
    /// \author  Krzysztof Brzostowski
    ///	\date    17.04.2012
    ///	\version 0.1
    /// \remarks By using constructor of this class we invoking HID communication process
    /// \remarks 64 bytes buffer is used for reading method
    /// \todo    Add method to write to HID device
    ///////////////////////////////////////////////////////////////////////////////
    class Manipulator : IDisposable
    {
        private ObservableCollection<LED> m_LedCollection = new ObservableCollection<LED>();
        public ObservableCollection<LED> LedCollection
        {
            get { return m_LedCollection; }
        }

        private strTrackSerialNumbers[] serialNums;
        private Window1 m_window1;


        private readonly object eventLock = new object();
        private event EventHandler<MyEventArgs> fooHandler_HID_ENC;

        public event EventHandler<MyEventArgs> HID_ENC_EventHandler
        {
            add
            {
                lock (eventLock)
                {
                    fooHandler_HID_ENC += value;
                }
            }
            remove
            {
                lock (eventLock)
                {
                    fooHandler_HID_ENC -= value;
                }
            }
        }








        public event EventHandler<MyEventArgs> HID_SWT_EventHandler;
        public event EventHandler<MyEventArgs> HID_OFN_EventHandler;
        public event EventHandler<MyEventArgs> HID_KBD_EventHandler;


        public ENC[] tab_ENC = new ENC[10];//zamiana na ilosc urządzen.
        public SWT[] tab_SWT = new SWT[10];
        public OFN[] tab_OFN = new OFN[10];
        //public KBD[] tab_KBD = new KBD[10];
        public KBD kbd = new KBD();

        //Komunikaty wysylane do HID
        //REPORT_FULL_REQUEST - WYSYLA STAN WSZYSTKICH
        //REPORT_ENUM_REQUEST - WYSYŁA DANE OPISOWE, KTORE MOGA BYC UZYTE DO OPISU W GUI

        public Manipulator(Window1 window1Control)
        {
            m_window1 = window1Control;
            m_Dispatcher = Dispatcher.CurrentDispatcher;
            r = new Regex(@"^(\w+)\[(\d+)\](\.)*(\w+)*=(.*)$");

            HwndSource source = PresentationSource.FromVisual(window1Control) as HwndSource;
            source.AddHook(WndProc);

            deviceHandler = new strHidDevice();
            HidAdapter.HID_Init(ref deviceHandler);

            serialNums = new strTrackSerialNumbers[6];
            for (int index = 0; index < (int)serialNums.Length; index++)
            {
                serialNums[index].deviceNum = index;
                serialNums[index].serialNum = new char[40];
                for (int y = 0; y < serialNums[index].serialNum.Length; ++y)
                    serialNums[index].serialNum[y] = (char)1;
            }

            ThreadCounter = 0;
            number_of_HID_MSP430_in_OS = 0;
            number_of_HID_Interfaces_in_OS = 0;


            for (int i = 0; i < 10; i++)
            {
                tab_ENC[i] = new ENC();
                tab_SWT[i] = new SWT();
                tab_OFN[i] = new OFN();
                //tab_KBD[i] = new KBD();
            }

            for (int i = 0; i < 10; i++)
            {
                tab_ENC[i].nr_urzadzenia = i;
                tab_SWT[i].nr_urzadzenia = i;
                tab_OFN[i].nr_urzadzenia = i;
                //tab_KBD[i].nr_urzadzenia = i;
            }

            kbd.nr_urzadzenia = 0;

            //HID lub
            //FillLedCollection();
            //Init();

            //Atrapa
            m_readingThread_1 = new Thread(readingMethod_1);
            m_readingThread_1.IsBackground = true;
            doLoop_1 = true;
            FillLedCollection();
            m_readingThread_1.Start();


        }

        private enum PacketsKinds
        {
            NONE, RAPORT_ENUM, RAPORT_INC, RAPORT_FULL
        };

        //private enum DataKinds
        //{
        //    NONE, ENC, SWT, SLI, OFN, DS3, KBD, LED, LSS
        //};

        ///////////////////////////////////////////////////////////////////////////////
        /// Open HID device and, depend on result invoke reading thread
        ///
        /// \param[in] deviceGUID Device interface GUID.
        /// \param[out] devicePath Device path.
        /// \param[in] bufLen devicePath maximum length.
        /// \return The operation result.
        /// \retval TRUE No error.
        /// \retval FALSE Error occured.
        ///////////////////////////////////////////////////////////////////////////////

        private object m_BuferLock = new object();
        Thread m_readingThread;
        Thread m_readingThread_1;
        public void Init()
        {
            Int32 open_res;

            number_of_HID_MSP430_in_OS = HidAdapter.HID_GetSerNums((UInt16)pid, (UInt16)vid, serialNums);

            if (number_of_HID_MSP430_in_OS == 1)
            {

                open_res = hid_Open();
                if (open_res != 0) return;

                m_readingThread = new Thread(readingMethod);
                m_readingThread.IsBackground = true;
                doLoop = true;
                m_readingThread.Start();
                ThreadCounter++;
            }
        }

        private void ThreadClose()
        {
            //m_readingThread.Join();
            //ThreadCounter--;
        }


        private PacketsKinds ProvidePacketKind(string word)
        {
            if (word.Equals("RAPORT_INC"))
            {
                return PacketsKinds.RAPORT_INC;
            }
            else if (word.Equals("RAPORT_FULL"))
            {
                return PacketsKinds.RAPORT_FULL;
            }
            else if (word.Equals("RAPORT_ENUM"))
            {
                return PacketsKinds.RAPORT_ENUM;
            }

            return PacketsKinds.NONE;
        }




        private int[] tab_timeScale = new int[3];
        private int status_enc_Timescale;

        public static string glob_buffer;
        //Method run by m_Dispatcher
        private Regex r;
        private void BufferReady(string buffer)
        {

            PacketsKinds PacketKind;
            //bufferWriteToFile(buffer);

            //RAPORT_ENUM

            string[] words = buffer.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            PacketKind = ProvidePacketKind(words[0]);
            if (PacketKind == PacketsKinds.NONE) return;

            //bo raport przyjdzie np taki:
            //SWT[3]=BTN 2,ENC[0]=ENCODER
            //a tutaj takiego pakietu nie chcemy
            if (PacketKind == PacketsKinds.RAPORT_ENUM) return;

            //uruchom Powiadomienie
            //AppHelper.WyslijPowiadomienieBuforDostepny();
            glob_buffer = buffer;
            glob_buffer = string.Empty;

            for (int i = 4; i < words.Length; i++)
            {
                Match m = r.Match(words[i]);
                if (m.Success)
                {
                    string strData = m.Groups[1].Value;
                    int dev_no = int.Parse(m.Groups[2].Value);

                    //KBD[0]=0x123/0x312
                    //rodzaj Danych    = m.Groups[1].Value;
                    //NR urzadzenia    = m.Groups[2].Value;//dev_no
                    //NIE UZYWANE(.)   = m.Groups[3].Value;
                    //REJEST (NP. AX)  = m.Groups[4].Value;
                    //WARTOSC          = m.Groups[5].Value;

                    if (strData.Contains("SWT"))
                    {


                        tab_SWT[dev_no].obecnosc = true;
                        tab_SWT[dev_no].P_press = false;
                        tab_SWT[dev_no].R_press = false;

                        string val = m.Groups[5].Value;

                        if (val.Equals("P") == true) { tab_SWT[dev_no].P_press = true; }
                        if (val.Equals("R") == true) { tab_SWT[dev_no].R_press = true; }

                        if (tab_SWT[dev_no].P_press == true)
                        {
                            if (tab_SWT[dev_no].stan == true) tab_SWT[dev_no].stan = false;
                            else tab_SWT[dev_no].stan = true;
                        }


                        //if (raport_enum==true) wtedy wykonaj ponizszalinijke
                        //tab_SWT[dev_no].opis = "abc";

                        if (HID_SWT_EventHandler != null)
                            HID_SWT_EventHandler(this, new MyEventArgs() { urzadzenie = tab_SWT[dev_no] });

                    }
                    else if (strData.Contains("ENC"))
                    {
                        //AppHelper.WyslijPowiadomienie_PokretloPokrecone //(dev_no, int.Parse(m.Groups[5].Value));
                        tab_ENC[dev_no].obecnosc = true;
                        tab_ENC[dev_no].obrot = int.Parse(m.Groups[5].Value);
                        tab_ENC[dev_no].wartosc += int.Parse(m.Groups[5].Value);

                        if (fooHandler_HID_ENC != null)
                            fooHandler_HID_ENC(this, new MyEventArgs() { urzadzenie = tab_ENC[dev_no] });

                    }
                    else if (strData.Contains("SLI"))
                    {

                    }
                    else if (strData.Contains("OFN")) //Myszka
                    {
                        string rejestr = m.Groups[4].Value;
                        string val = m.Groups[5].Value;

                        //zeruj
                        tab_OFN[dev_no].Single_Tap_Detected_Event = false;
                        tab_OFN[dev_no].Double_Tap_Detected_Event = false;
                        tab_OFN[dev_no].Tap_And_Hold_Detected_Event = false;
                        tab_OFN[dev_no].Finger_Presence_Detected_Event = false;
                        tab_OFN[dev_no].Button_Pressed_Event = false;
                        tab_OFN[dev_no].Button_Release_Event = false;
                        //tab_OFN[dev_no].DX = 0;//Tego nie chcemy zerowac
                        //tab_OFN[dev_no].DY = 0;//Tego nie chcemy zerowac
                        tab_OFN[dev_no].EV = string.Empty;

                        //wykonaj dla DX
                        if (rejestr.Equals("DX") == true)
                        {
                            tab_OFN[dev_no].DX += int.Parse(val);
                        }
                        else if (rejestr.Equals("DY") == true)
                        {
                            tab_OFN[dev_no].DY += int.Parse(val);
                        }
                        else if (rejestr.Equals("EV") == true)
                        {

                            if (val.Equals("STap") == true)
                                tab_OFN[dev_no].Single_Tap_Detected_Event = true;
                            else if (val.Equals("DTap") == true)
                                tab_OFN[dev_no].Single_Tap_Detected_Event = true;
                            else if (val.Equals("TnH") == true)
                                tab_OFN[dev_no].Tap_And_Hold_Detected_Event = true;
                            else if (val.Equals("FP") == true)
                                tab_OFN[dev_no].Finger_Presence_Detected_Event = true;
                            else if (val.Equals("P") == true)
                                tab_OFN[dev_no].Button_Pressed_Event = true;
                            else if (val.Equals("R") == true)
                                tab_OFN[dev_no].Button_Release_Event = true;
                        }

                        if (HID_OFN_EventHandler != null)
                            HID_OFN_EventHandler(this, new MyEventArgs() { urzadzenie = tab_OFN[dev_no] });

                    }
                    else if (strData.Contains("3DS"))
                    {


                    }
                    else if (strData.Contains("KBD"))
                    {
                        string bufor_na_klawisze = m.Groups[5].Value;
                        string[] klawisze = bufor_na_klawisze.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                        kbd.klawisz1 = Convert.ToInt32(klawisze[0], 16);
                        kbd.klawisz2 = Convert.ToInt32(klawisze[1], 16);

                        if (HID_KBD_EventHandler != null)
                            HID_KBD_EventHandler(this, new MyEventArgs() { urzadzenie = kbd });


                        //KBD[0]=0x123/0x312
                        //rodzaj Danych    = m.Groups[1].Value;
                        //NR urzadzenia    = m.Groups[2].Value;
                        //NIE UZYWANE(.)   = m.Groups[3].Value;
                        //REJEST (NP. AX)  = m.Groups[4].Value;
                        //WARTOSC          = m.Groups[5].Value;

                        ////KBD[0]=0x123/0x312
                        ////KBD [##]=SEQ	SEQ – opis sekwencji klawiszy wraz z ich stanem rozdzielonych /
                        //    class KBD : Urzadzenie
                        //    {
                        //        public KBD()
                        //        {
                        //          klawisz1=0;
                        //          klawisz2=0;
                        //          klawisze=string.Empty;
                        //        }
                        //        public int klawisz1;
                        //        public int klawisz2;
                        //        public string klawisze;

                        //        string bufor_na_klawisze = m.Groups[5].Value;
                        //        string[] klawisze = bufor_na_klawisze.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                        //        //AppHelper.WyslijPowiadomienie_WcisnietyKlawiszKlawiatury(dev_no, int.Parse(klawisze[0]), int.Parse(klawisze[1]));


                    }
                    else if (strData.Contains("LED"))
                    {

                    }
                    if (strData.Contains("LSS"))
                    {

                    }
                    else
                    {
                        //return
                    }


                }//if
            }//for
        }

        //private uint device_connected_HID_GetSerNums;
        private Int32 openResult;
        private strHidDevice deviceHandler;

        //MSP
        private uint pid = 0x2047;
        private uint vid = 0x0301;

        //joyStick
        //private uint pid = 1133;
        //private uint vid = 49675;

        public uint number_of_HID_MSP430_in_OS;
        public uint number_of_HID_Interfaces_in_OS;

        uint provide_number_HID_MSP430_in_OS()
        {
            uint number_of_devices;

            number_of_devices = HidAdapter.HID_GetSerNums((UInt16)pid, (UInt16)vid, serialNums);

            return number_of_devices;
        }

        private void FillLedCollection()
        {
            // TODO: This is temporary code inserting some Led controls.
            // TODO: Send full report request
            // Wait for answer and check number of LEDs

            int ledCount = 6;
            for (int ledIndex = 0; ledIndex < ledCount; ledIndex++)
            {
                LED led = new LED();
                led.LedIndex = ledIndex;
                led.LedIndex_str = "LED " + String.Format(CultureInfo.CurrentUICulture, "{0}", ledIndex);

                led.PropertyChanged +=
                    new System.ComponentModel.PropertyChangedEventHandler
                        (led_PropertyChanged);
                LedCollection.Add(led);
            }

            //LedCollection[0].IsOn

        }

        object _object = new object();
        void led_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LED led = (LED)sender;
            String command_string = String.Format(CultureInfo.CurrentUICulture, "DEVREQUEST=LED[0]=S.{0}", led.LedIndex+1);

            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            Byte[] buf_write = encoding.GetBytes(command_string);

            ////DEVREQUEST=LED[k]=S.1/S.2/R.3
            //Byte[] buf_write = encoding.GetBytes("");

            UInt32 ile = (UInt32) buf_write.Length;
            byte wynik_zapisu;



            lock (_object)
            {
                ///TODO: Sprawdzic, czy urzadzenie otwarte
                wynik_zapisu = HidAdapter.HID_WriteFile(ref deviceHandler, buf_write, ile);
            }




//LED - tu bedzie inaczej, bo to sa dane wysylane do HID
//LED[ ##]=S	LED jest włączony ( SET )
//LED[ ##]=C	LED jest wyłączony ( CLEAR )
//k - nr szyny w HID. Na jednej szynie jest 8 diod.
//DEVREQUEST=LED[k]=S.1/S.2/R.3


//TX --> REPORT_FULL_REQUEST
//Wysyla stan wszystkich

//        RX -->
//        RAPORT_FULL,DS=106,V0.01,TS=5016225,SWT[0]=R,SWT[1]=R,SWT[2]=R

//        RX -->
//        ,SWT[3]=R,ENC[0]=0,ENC[1]=0,ENC[2]=0,ENC[3]=0,ENC[4]=0,ENC[5]=

//        RX --> 0
       

//        TX --> REPORT_ENUM_REQUEST
//Wysyla listing wszystkich i opisy(!!!)

//        RX --> RAPORT_ENUM,DS=172,V0.01,TS=5025462,SWT[0]=S1
//        EVB,SWT[1]=S2 EV

//        RX --> B,SWT[2]=BTN 1,SWT[3]=BTN 2,ENC[0]=ENCODER
//        1,ENC[1]=ENCODER 2,

//        RX --> ENC[2]=ENCODER 3,ENC[3]=ENCODER 4,ENC[4]=ENCODER
//        5,ENC[5]=ENCO

//        RX --> DER 6




            // TODO: Tutaj będziemy pisać do urządzenia
        }




        private Int32 hid_Open()
        {
            uint newNumOfHIDDevices_ = HidAdapter.HID_GetNumOfInterfaces((UInt16)pid, (UInt16)vid,
                number_of_HID_MSP430_in_OS /* totalNumSerNums*/);

            byte openResult_local_var = HidAdapter.HID_Open(ref deviceHandler,
                                         (UInt16)pid, (UInt16)vid,
                                         0, //Index of the device.If only one HID is connected, deviceIndex is 0.
                                         serialNums[0].serialNum,
                                         1, //DWORD totalDevNum
                                         1); //DWORD totalSerNum

            openResult = openResult_local_var;
            //if (openResult == 0) return 0;
            FillLedCollection();
            return openResult_local_var;
        }


        void packet_getting_simulate()
        {
        }

        void bufferWriteToFile(string pakiet)
        {
            pakiet += Environment.NewLine;
            using (StreamWriter writer = new StreamWriter("_dane.txt", true))
            {
                writer.Write(pakiet);
            }
        }



        private void readingMethod_1()
        {
            //zmienne kontrolne
            int control_val_even_odd = -1;
            int swt_0 = -1;
            int swt_1 = -1;
            int swt_2 = -1;
            int swt_3 = -1;
            int swt_4 = -1;
            int swt_5 = -1;
            int swt_6 = -1;
            int swt_7 = -1;
            int swt_8 = -1;
            int swt_9 = -1;
            int ofn_0 = -1;
            int ofn_1 = -1;

            //FillLedCollection();

            string strPacket = String.Empty;
            while (doLoop_1)
            {
                Thread.Sleep(10);
                control_val_even_odd++;
                swt_0++; swt_1++; swt_2++; swt_3++; swt_4++; swt_5++; swt_6++; swt_7++; swt_8++; swt_9++;
                ofn_0++; ofn_1++;

                if (control_val_even_odd == 0)
                {
                    strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[0]=5";
                    m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                    if (swt_0 % 2 == 0)
                        strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,SWT[0]=P";
                    else
                        strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,SWT[0]=R";
                    m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                }
                else
                    if (control_val_even_odd == 1)
                    {
                        strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[1]=-5";
                        m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        if (swt_1 % 2 == 0)
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,SWT[1]=P";
                        else
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,SWT[1]=R";
                        m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);

                    }
                    else
                        if (control_val_even_odd == 2)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[2]=5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 3)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[3]=-5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 4)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[4]=-5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 5)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[5]=5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 6)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[6]=-5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 7)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[7]=5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 8)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[8]=-5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 9)
                        {
                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,ENC[9]=5";
                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                        }
                        else if (control_val_even_odd == 10)
                        {//OFN[0].DX=12,OFN[0].DY=1

                            if (ofn_0 % 2 == 0)
                                strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,OFN[0].DX=-5";
                            else
                                strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,OFN[0].DY=-5";

                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);

                        }
                        else if (control_val_even_odd == 11)
                        {//KBD

                            strPacket = "RAPORT_INC,V0.01,DS=138,TS=1236700,KBD[0]=0x123/0x312";

                            m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);

                        }










                //m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                if (control_val_even_odd == 12)
                {
                    control_val_even_odd = -1;
                    swt_0 = -1; swt_1 = -1; swt_2 = -1; swt_3 = -1; swt_4 = -1; swt_5 = -1; swt_6 = -1; swt_7 = -1; swt_8 = -1; swt_9 = -1;
                }

            }
        }


        public bool doLoop = false;
        public bool doLoop_1 = false;

        Dispatcher m_Dispatcher;
        public int ThreadCounter;
        private void readingMethod()
        {

            if ((openResult == 0) || (openResult == 3))
            {
                byte wynik_czytania;
                byte[] buf_write = new byte[64];
                byte[] buf_read = new byte[64];
                UInt32 bytesRet = 0;
                string string_bufor;
                string strPacket = string.Empty;

                while (doLoop)
                {
                    bytesRet = 0;
                    for (int i = 0; i < 64; i++) buf_read[i] = (byte)0;


                    try
                    {
                        lock (_object)
                        {

                            wynik_czytania = HidAdapter.HID_ReadFile(ref deviceHandler,
                                                                     buf_read, //array of bytes
                                                                     (uint) 64, ref bytesRet);
                        }
                    }
                    catch (Exception)
                    {

                        doLoop = false;
                        ThreadCounter--;
                        return;
                    }



                    if (bytesRet > 0)
                    {
                        //StringBuilder
                        string_bufor = ASCIIEncoding.GetEncoding("Latin1").GetString(buf_read);
                        int dlug = string_bufor.Length;

                        for (int i = 0; i < bytesRet; i++)
                        {
                            char ch = string_bufor[i];
                            if (ch.Equals('\n') == false)
                            {
                                strPacket += string_bufor[i];
                            }
                            else
                            {
                                m_Dispatcher.Invoke(new Action<string>(BufferReady), strPacket);
                                strPacket = string.Empty;
                            }
                        }

                    }//if bytesRet
                }//while true
                //HidAdapter.HID_Close(ref deviceHandler);
            }// if open success
        }


        #region IDisposable Members

        public void Dispose()
        {
            //HidAdapter.HID_Close(ref deviceHandler);
            //m_readingThread.Abort();
        }

        #endregion


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0219)
            {
                Int32 open_res = -1;
                Thread.Sleep(100);

                number_of_HID_MSP430_in_OS = HidAdapter.HID_GetSerNums((UInt16)pid, (UInt16)vid, serialNums);
                number_of_HID_Interfaces_in_OS = HidAdapter.HID_GetNumOfInterfaces((UInt16)pid, (UInt16)vid, 1
                    /*number_of_HID_MSP430_in_OS*/ /* totalNumSerNums*/);

                if (number_of_HID_Interfaces_in_OS == 1)
                {
                    if (ThreadCounter == 0)
                    {
                        ThreadCounter++;

                        open_res = hid_Open();

                        if ((open_res != 0))
                        {
                            handled = true;
                            return IntPtr.Zero;
                        }

                        m_readingThread = new Thread(readingMethod);
                        m_readingThread.IsBackground = true;
                        doLoop = true;
                        m_readingThread.Start();
                    }
                }
                else
                    if (number_of_HID_Interfaces_in_OS == 0)
                    {
                        doLoop = false;
                        m_readingThread.Join();
                        ThreadCounter--;
                        HidAdapter.HID_Close(ref deviceHandler);

                    }

                handled = true;
            }
            return IntPtr.Zero;
        }

    }
}

