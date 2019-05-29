using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

///////////////////////////////////////////////////////////////////////////////
///	\brief   Project shows usage of HID device in C#
/// \author  K.B.
///	\date    18.04.2012
///	\version 0.1
///////////////////////////////////////////////////////////////////////////////

namespace Uzycie_Manipulatora
{



    //! \brief  Class contains events.
    //!
    //!         Events in this class are invoked when HID device is in use,
    //!         for example: event "OnWcisnietyKlawiszKlawiatury" is invoked,
    //!         when HID KeyBoard is pressed
    //!         You can hook to this events in PrzykladUzycia.cs class
    //!
    //! \sa PrzykladUzycia.cs
    class AppHelper
    {

        /// OnWcisnietyKlawiszKlawiatury event.
        /// This event is invoked when any HID key is pressed
        //! \sa PrzykladUzycia.cs class
        //! \brief OnWcisnietyKlawiszKlawiatury action event
        public static event Action<int, int, int> OnWcisnietyKlawiszKlawiatury;

        /// \brief Helper method. This method is invoked to activate event
        /// \sa classA 
        //! \sa jbdnhjcb jifkb ckscbn fjnsk
        /*!
         Komentarz bardziej szczegolowy
         */
        public static void WyslijPowiadomienie_WcisnietyKlawiszKlawiatury
            (int nrUrzadzenia, int klawisz1, int klawisz2)
        {
            if (null != OnWcisnietyKlawiszKlawiatury) OnWcisnietyKlawiszKlawiatury(nrUrzadzenia, klawisz1, klawisz2);
        }

        public static event Action<int, int> OnPokretloPokrecone;//PokretloZostaloPokrecone

        public static void WyslijPowiadomienie_PokretloPokrecone(int nrUrzadzenia, int obrot)
        {
            if (null != OnPokretloPokrecone) OnPokretloPokrecone(nrUrzadzenia, obrot);
        }
    }
}
