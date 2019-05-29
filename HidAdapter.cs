using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;




namespace Uzycie_Manipulatora
{

    // /////////////////////////////////////////////////////////////////////////////
    // / \file    HidAdapter.cs 
    // /	\brief   This file provides method and structures which wrap WinApi component
    // / \author  Krzysztof Brzostowski
    // /	\date    17.04.2012
    // /	\version 0.1
    // / \remarks \n
    // / \remarks \n
    // /////////////////////////////////////////////////////////////////////////////



    // /////////////////////////////////////////////////////////////////////////////
    // Structures
    // /////////////////////////////////////////////////////////////////////////////

    // / Serial number and device number structure
    // / A structure that tracks the number of serial numbers
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    public struct strTrackSerialNumbers
    {
        [FieldOffset(0)]
        public int deviceNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        [FieldOffset(4)]
        public char[] serialNum;
    }


    /// \brief  Device information structure.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct strHidDevice
    {
        IntPtr hndHidDevice;
        bool bDeviceOpen;
        UInt32 uGetReportTimeout;
        UInt32 uSetReportTimeout;
        NativeOverlapped oRead;
        NativeOverlapped oWrite;
        UInt16 wInReportBufferLength;
        UInt16 wOutReportBufferLength;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8192)]
        byte[] inBuffer;
        ushort inBufferUsed;
    };


    ///Wrapper class for HID device WinApi calls
    public  class HidAdapter
    {
        /// \brief Wrapper class for HID device WinApi calls - wpis 02
        /// \file HidAdapter.h
        /// \author KB
        /// \version     0.1


        
        /// ----Device Create---
        [DllImport("HidDevice01.dll")]
        public static extern IntPtr HID_DeviceCreate();

        [DllImport("HidDevice01.dll")]
        public static extern void HID_Init(ref strHidDevice deviceHandler);

        [DllImport("HidDevice01.dll")]
        public static extern uint HID_GetSerNums(UInt16 vid, UInt16 pid,
            [In, Out] strTrackSerialNumbers[] serialNumList);

        [DllImport("HidDevice01.dll")]
        public static extern bool HID_IsDeviceAffected(ref strHidDevice deviceHandler);

        [DllImport("HidDevice01.dll", CharSet = CharSet.Ansi)]
        public static extern byte HID_Open(ref strHidDevice deviceHandler,
            UInt16 vid, UInt16 pid, uint deviceIndex,
            char[] serialNumber,
            uint totalDevNum,
            uint totalSerNum);

        [DllImport("HidDevice01.dll", CharSet = CharSet.Ansi)]
        public static extern uint HID_GetNumOfInterfaces(ushort vid, ushort pid,
            uint numSerNums);

        [DllImport("HidDevice01.dll")]
        public static extern byte HID_WriteFile(ref strHidDevice deviceHandler,
                  [In, Out] byte[] buffer,
                  uint ile);

        /// <summary>
        /// HID_ReadFile - Reading Method
        /// </summary>
        /// <param name="deviceHandler"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <param name="bytesReturned"></param>
        /// <returns></returns>
        [DllImport("HidDevice01.dll")]
        public static extern byte HID_ReadFile(ref strHidDevice deviceHandler,
                  [In, Out] byte[] buffer,
                  uint bufferSize,
                  ref uint bytesReturned);


        //! \brief  Close a HID Device.
        //!
        //!         This function will close a HID device based on the HID structure
        //!
        //! \param  pstrHidDevice Structure which contains important data of an HID 
        //!                       device
        //!
        //! \return Returns the error status, as one of
        //!         \n \b HID_DEVICE_SUCCESS 
        //!         \n \b HID_DEVICE_NOT_OPENED
        //!         \n \b HID_DEVICE_HANDLE_ERROR
        [DllImport("HidDevice01.dll")]
        public static extern byte HID_Close(ref strHidDevice deviceHandler);

       /// <summary>
       /// HID_RegisterForDeviceNotification - Notification Method
       /// </summary>
       /// <param name="hWnd"> wiecej info: </param>
       /// <param name="diNotifyHandle"></param>
       /// <returns></returns>
        [DllImport("HidDevice01.dll")]
        public static extern byte HID_RegisterForDeviceNotification(IntPtr hWnd,
                                                                    IntPtr diNotifyHandle); 
    }





}
