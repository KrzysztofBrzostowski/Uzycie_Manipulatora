using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Uzycie_Manipulatora
{
    public class Urzadzenie
    {
        public Urzadzenie()
        {
            nr_urzadzenia = 0;
            obecnosc = false;
            opis = string.Empty;
        }

        public int nr_urzadzenia { get; set; }
        public bool obecnosc { get; set; }
        public string opis { get; set; }
    }

    class SWT : Urzadzenie
    {
        public SWT()
        {
            P_press = false;
            R_press = false;
            stan = false;
        }
        public bool P_press { get; set; }
        public bool R_press { get; set; }
        public bool stan { get; set; }
    }

    class ENC : Urzadzenie
    {
        public ENC()
        {
            obrot = 0;
            wartosc = 0;
        }
        public int obrot { get; set; }
        public int wartosc { get; set; }
    }


    //LED - tu bedzie inaczej, bo to sa dane wysylane do HID
    //LED[ ##]=S	LED jest włączony ( SET )
    //LED[ ##]=C	LED jest wyłączony ( CLEAR )
    //k - nr szyny w HID. Na jednej szynie jest 8 diod.
    //DEVREQUEST=LED[k]=S.1/S.2/R.3


    //OFN[##].DX=VAL	VAL = Przyrost pozycji w osi X ze znakiem
    //OFN[##].DY=VAL	VAL = Przyrost pozycji w osi Y ze znakiem
    //OFN[##].EV=STap	Event Single Tap – A TO CO ?
    //OFN[##].EV=DTap	Event Double Tap  -``-
    //OFN[##].EV=TnH	Event Tap’N’Hold
    //OFN[##].EV=FP	Event Finger Presence detected
    //OFN[##].EV=P	Event Button Pressed
    //OFN[##].EV=R	Event Button Released
    class OFN : Urzadzenie
    {
        public OFN()
        {
            DX = 0;
            DY = 0;
            EV = string.Empty;
            Single_Tap_Detected_Event = false;
            Double_Tap_Detected_Event = false;
            Tap_And_Hold_Detected_Event = false;
            Finger_Presence_Detected_Event = false;
            Button_Pressed_Event = false;
            Button_Release_Event = false;
        }

        public int DX;
        public int DY;
        public string EV;

        public bool Single_Tap_Detected_Event;
        public bool Double_Tap_Detected_Event;
        public bool Tap_And_Hold_Detected_Event;
        public bool Finger_Presence_Detected_Event;
        public bool Button_Pressed_Event;
        public bool Button_Release_Event;
    }

    //KBD[0]=0x123/0x312
    //KBD [##]=SEQ	SEQ – opis sekwencji klawiszy wraz z ich stanem rozdzielonych /
    class KBD : Urzadzenie
    {
        public KBD()
        {
            klawisz1 = 0;
            klawisz2 = 0;
            klawisze = string.Empty;
        }
        public int klawisz1;
        public int klawisz2;
        public string klawisze;
    }

    public class LED : Urzadzenie, INotifyPropertyChanged
    {
        public int LedIndex { get; set; }

        public string LedIndex_str { get; set; }

        public bool IsOn
        {
            get { return m_IsOn; }
            set
            {
                m_IsOn = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IsOn"));
            }
        }
        private bool m_IsOn;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;


        #endregion
    }
}
