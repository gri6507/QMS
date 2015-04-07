using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using System.Diagnostics;

/* Since FTDI communication relies on a system DLL which is not distributed
 * with this C# implementation, it makes sense to place the implementation
 * into its own C# Project. This way, any solution that requires support
 * for FTDI communication can explicitly pull in this project and inherit
 * the system requirement for the DLL. However, if the solution does not
 * need FTDI communication, then there would not be a requirement on this
 * project, and hence no requirement on the DLL.
 */

namespace QMSTool
{
	#region FTDI enums

    [Flags]
    enum ModemStatus : byte
    {
        CTS = 0x10,
        DSR = 0x20,
        RI  = 0x40,
        DCD = 0x80,
    }

	[Flags]
	public enum FtdiFlowControl : ushort {
		NONE = 0x0000,
		RTS_CTS = 0x0100,
		DTR_DSR = 0x0200,
		XON_XOFF = 0x0400,
	}

	/// <summary>Specifies the parity bit for a FTDI com port</summary>
	/// <remarks>This matches the Serial.IO.Ports.Parity enum</remarks>
	public enum FtdiParity : byte {
		/// <summary>No parity check occurs.</summary>
		None = 0,
		/// <summary>Sets the parity bit so that the count of bits set is an odd number.</summary>
		Odd = 1,
		/// <summary> Sets the parity bit so that the count of bits set is an even number.</summary>
		Even = 2,
		/// <summary>Leaves the parity bit set to 1..</summary>
		Mark = 3,
		/// <summary>Leaves the parity bit set to 0.</summary>
		Space = 4,
	}

	/// <summary>Specifies the number of stop bits used on the FTDI com port</summary>
	/// <remarks>This DOES NOT match the Serial.IO.Ports.StopBits enum</remarks>
	public enum FtdiStopBits : byte {
		///<summary>One stop bit is used.</summary>
		One = 0,
		///<summary>1.5 stop bits are used.</summary>
		OnePointFive = 1,
		///<summary>Two stop bits are used.</summary>
		Two = 2,
	}

	public enum FtdiDeviceType : uint {
		FT232BM = 0,
		FT232AM,
		FT100AX,
		UNKNOWN,
		FT2232C,
		FT232R
	}

	#endregion FTDI enums

	#region FTDI structures
	/// <summary>
	/// FTDI device information
	/// </summary>
	//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FtdiDeviceInfoStruct {
		/// <summary>1 if port is open, 0 if port is closed</summary>
		public uint Flags;

		/// <summary>Which kind of FTDI chip we are talking to</summary>
		public FtdiDeviceType Type;

		/// <summary>USB Vendor and product ID packed together</summary>
		public uint ID;

		/// <summary>location ID for device (0 if not working, i.e. on USB 2.0 controller)</summary>
		public uint LocId;

		///// <summary>Serial Number of device (from EEPROM)</summary>
		//[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public String SerialNumber;

		///// <summary>Description of device (from EEPROM)</summary>
		//[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public String Description;

		/// <summary>handle to device (0 if not open)</summary>
		public IntPtr ftHandle;

		public override string ToString() {
			return "FTDI Loc: 0x" + LocId.ToString("x") + " / SN: '" + SerialNumber + "' / Desc: '" + Description + "' / Type: " + Type.ToString() + " / ID: 0x" + ID.ToString("x8") + " / handle: 0x" + ftHandle.ToInt64().ToString("x");
		}

		public bool Equals(FtdiDeviceInfoStruct other) {
			return ID == other.ID && SerialNumber.Equals(other.SerialNumber);
		}
	};


	/// <summary>
	/// FTDI EEPROM data structure
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FtdiConfig {
		public uint Signature1;			// Header - must be 0x00000000 
		public uint Signature2;			// Header - must be 0xffffffff
		public uint Version;			// Header - FT_PROGRAM_DATA version
		//          0 = original
		//          1 = FT2232C extensions
		//			2 = FT232R extensions

		public ushort VendorId;				// 0x0403
		public ushort ProductId;			// 0x6001

		public String Manufacturer;		// "FTDI"
		public String ManufacturerId;	// "FT"
		public String Description;		// "USB HS Serial Converter"
		public String SerialNumber;		// "FT000001" if fixed, or NULL

		public ushort MaxPower;				// 0 < MaxPower <= 500
		public ushort PnP;					// 0 = disabled, 1 = enabled
		public ushort SelfPowered;			// 0 = bus powered, 1 = self powered
		public ushort RemoteWakeup;			// 0 = not capable, 1 = capable
		//
		// Rev4 extensions
		//
		public byte Rev4;					// non-zero if Rev4 chip, zero otherwise
		public byte IsoIn;					// non-zero if in endpoint is isochronous
		public byte IsoOut;				// non-zero if out endpoint is isochronous
		public byte PullDownEnable;		// non-zero if pull down enabled
		public byte SerNumEnable;			// non-zero if serial number to be used
		public byte USBVersionEnable;		// non-zero if chip uses USBVersion
		public ushort USBVersion;			// BCD (0x0200 => USB2)
		//
		// FT2232C extensions
		//
		public byte Rev5;					// non-zero if Rev5 chip, zero otherwise
		public byte IsoInA;				// non-zero if in endpoint is isochronous
		public byte IsoInB;				// non-zero if in endpoint is isochronous
		public byte IsoOutA;				// non-zero if out endpoint is isochronous
		public byte IsoOutB;				// non-zero if out endpoint is isochronous
		public byte PullDownEnable5;		// non-zero if pull down enabled
		public byte SerNumEnable5;			// non-zero if serial number to be used
		public byte USBVersionEnable5;		// non-zero if chip uses USBVersion
		public ushort USBVersion5;			// BCD (0x0200 => USB2)
		public byte AIsHighCurrent;		// non-zero if interface is high current
		public byte BIsHighCurrent;		// non-zero if interface is high current
		public byte IFAIsFifo;				// non-zero if interface is 245 FIFO
		public byte IFAIsFifoTar;			// non-zero if interface is 245 FIFO CPU target
		public byte IFAIsFastSer;			// non-zero if interface is Fast serial
		public byte AIsVCP;				// non-zero if interface is to use VCP drivers
		public byte IFBIsFifo;				// non-zero if interface is 245 FIFO
		public byte IFBIsFifoTar;			// non-zero if interface is 245 FIFO CPU target
		public byte IFBIsFastSer;			// non-zero if interface is Fast serial
		public byte BIsVCP;				// non-zero if interface is to use VCP drivers
		//
		// FT232R extensions
		//
		public byte UseExtOsc;				// Use External Oscillator
		public byte HighDriveIOs;			// High Drive I/Os
		public byte EndpointSize;			// Endpoint size

		public byte PullDownEnableR;		// non-zero if pull down enabled
		public byte SerNumEnableR;			// non-zero if serial number to be used

		public byte InvertTXD;				// non-zero if invert TXD
		public byte InvertRXD;				// non-zero if invert RXD
		public byte InvertRTS;				// non-zero if invert RTS
		public byte InvertCTS;				// non-zero if invert CTS
		public byte InvertDTR;				// non-zero if invert DTR
		public byte InvertDSR;				// non-zero if invert DSR
		public byte InvertDCD;				// non-zero if invert DCD
		public byte InvertRI;				// non-zero if invert RI

		public byte Cbus0;					// Cbus Mux control
		public byte Cbus1;					// Cbus Mux control
		public byte Cbus2;					// Cbus Mux control
		public byte Cbus3;					// Cbus Mux control
		public byte Cbus4;					// Cbus Mux control

		public byte RIsD2XX;				// non-zero if using D2XX driver
	};

	#endregion FTDI structures

	/// <summary>
	/// FTDI USB to serial chip wrapper (build on FTD2XX.dll)
	/// </summary>
	public class FTDI : IFTDI
    {
        
		#region Class Enums

		protected enum ErrorCode : int {
			OK = 0,
			INVALID_HANDLE,
			DEVICE_NOT_FOUND,
			DEVICE_NOT_OPENED,
			IO_ERROR,
			INSUFFICIENT_RESOURCES,
			INVALID_PARAMETER,
			INVALID_BAUD_RATE,
			DEVICE_NOT_OPENED_FOR_ERASE,
			DEVICE_NOT_OPENED_FOR_WRITE,
			FAILED_TO_WRITE_DEVICE,
			EEPROM_READ_FAILED,
			EEPROM_WRITE_FAILED,
			EEPROM_ERASE_FAILED,
			EEPROM_NOT_PRESENT,
			EEPROM_NOT_PROGRAMMED,
			INVALID_ARGS,
			NOT_SUPPORTED,
			OTHER_ERROR
		}

		[Flags]
		protected enum OpenExFlag : uint {
			BY_SERIAL_NUMBER = 1,
			BY_DESCRIPTION = 2,
			BY_LOCATION = 4,
		}

		[Flags]
		protected enum ListDevicesFlag : uint {
			NUMBER_ONLY = 0x80000000,
			BY_INDEX = 0x40000000,
			ALL = 0x20000000,
		}

		[Flags]
		protected enum PurgeFlag : byte {
			RX = 1,
			TX = 2,
		}

        public static string[] ErrorString = new string[] {
			"OK",
			"INVALID HANDLE",
			"DEVICE NOT FOUND",
			"DEVICE NOT OPENED",
			"IO ERROR",
			"INSUFFICIENT RESOURCES",
			"INVALID PARAMETER",
			"INVALID BAUD RATE",
			"DEVICE NOT OPENED FOR ERASE",
			"DEVICE NOT OPENED FOR WRITE",
			"FAILED TO WRITE DEVICE",
			"EEPROM READ FAILED",
			"EEPROM WRITE FAILED",
			"EEPROM ERASE FAILED",
			"EEPROM NOT PRESENT",
			"EEPROM NOT PROGRAMMED",
			"INVALID ARGS",
			"NOT SUPPORTED",
			"OTHER ERROR",
		};

		#endregion Class Enums

		#region P/Invoke to FTD2XX.DLL stuff

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ListDevices(out int arg1, int arg2, ListDevicesFlag flags);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_Open(int deviceNum, out IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_OpenEx(string serial, OpenExFlag flags, out IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_OpenEx(int location, OpenExFlag flags, out IntPtr handle);

		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_Read(IntPtr handle, byte[] data, int length, out int read);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_Read(IntPtr handle, ref byte data, int length, out int read);

		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_Write(IntPtr handle, byte[] data, int length, ref int written);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_Write(IntPtr handle, ref byte data, int length, ref int written);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_Close(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ResetDevice(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetBaudRate(IntPtr handle, int bps);

		// unused: use SetBaudRate instead
		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_SetDivisor(IntPtr handle, ushort divisor);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetDataCharacteristics(IntPtr handle, byte wordLength, FtdiStopBits stopBits, FtdiParity parity);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetFlowControl(IntPtr handle, FtdiFlowControl flowControl, byte xOnChar, byte xOffChar);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetRts(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ClrRts(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetModemStatus(IntPtr handle, out int modemStatus);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetDtr(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ClrDtr(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetChars(IntPtr handle, byte eventChar, byte eventCharEnable, byte errorChar, byte errorCharEnable);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_Purge(IntPtr handle, PurgeFlag mask);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetTimeouts(IntPtr handle, int readTimeout, int writeTimeout);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetQueueStatus(IntPtr handle, out int amountInRxQueue);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetBreakOn(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetBreakOff(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetStatus(IntPtr handle, out int rxCount, out int txCount, out int eventStatus);

		// not sure how to implement event handling from an unmanaged DLL
		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_SetEventNotification(IntPtr handle, int eventMask, System.EventHandler e);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetDeviceInfo(IntPtr handle, out int Type, out int Id, StringBuilder SerialNumber, StringBuilder Description, out int junk);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetResetPipeRetryCount(IntPtr handle, int count);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_StopInTask(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_RestartInTask(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ResetPort(IntPtr handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_CyclePort(IntPtr handle);

		/// <summary>
		/// This function builds a device information list and returns the number of D2XX devices connected to
		/// the system. The list contains information about both unopen and open devices.
		/// </summary>
		/// <param name="numDevices">pointer to where to store the # of devices</param>
		/// <returns>0 if successful, negative if error (FT error code)</returns>
		[DllImport("ftd2xx.dll")]
		protected static extern int FT_CreateDeviceInfoList(out int numDevices);

		//// couldn't get this damn thing working right!
		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_GetDeviceInfoList(out FtdiDeviceInfo devInfos, ref int numDevices);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetDeviceInfoDetail(int Index, out uint Flags, out FtdiDeviceType Type, out uint Id, out uint LocId, StringBuilder SerialNumber, StringBuilder Description, out IntPtr Handle);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetDriverVersion(IntPtr handle, out int version);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetLibraryVersion(out int DllVersion);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_ReadEE(IntPtr handle, int address, out ushort value);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_WriteEE(IntPtr handle, int address, ushort value);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EraseEE(IntPtr handle);

		// couldn't get this working right, using Ex version instead
		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_EE_Read(IntPtr handle, ref ProgramData data);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_ReadEx(IntPtr handle, ref FtdiConfig data, StringBuilder Manufacturer, StringBuilder ManufacturerID, StringBuilder Description, StringBuilder SerialNumber);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_ReadEx(IntPtr handle, ref FtdiConfig data, byte[] Manufacturer, byte[] ManufacturerID, byte[] Description, byte[] SerialNumber);

		// didn't even try to get this working, using Ex version instead
		//[DllImport("ftd2xx.dll")]
		//protected static extern int FT_EE_Program(IntPtr handle, ref ProgramData data);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_ProgramEx(IntPtr handle, ref FtdiConfig data, string Manufacturer, string ManufacturerID, string Description, string SerialNumber);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_ProgramEx(IntPtr handle, ref FtdiConfig data, byte[] Manufacturer, byte[] ManufacturerID, byte[] Description, byte[] SerialNumber);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_UASize(IntPtr handle, out int size);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_UARead(IntPtr handle, byte[] data, int dataLen, out int bytesRead);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_EE_UAWrite(IntPtr handle, byte[] data, int dataLen);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetLatencyTimer(IntPtr handle, out byte timer);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetLatencyTimer(IntPtr handle, byte timer);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetUSBParameters(IntPtr handle, int inTransferSize, int outTransferSize);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_GetBitMode(IntPtr handle, out byte mode);

		[DllImport("ftd2xx.dll")]
		protected static extern int FT_SetBitMode(IntPtr handle, byte mask, byte mode);

		#endregion P/Invoke to FTD2XX.DLL stuff

        private IntPtr          _handle = IntPtr.Zero;
        private object          _curObject;
        private int             _baudRate;
        private FtdiParity      _parity;
        private byte            _dataBits;
        private FtdiStopBits    _stopBits;
        private FtdiFlowControl _flowControl;
        private int             _readTimeout = -1;  // Note: 300ms is default in driver
        private int             _writeTimeout = -1; // Note: 300ms is default in driver
        private bool            _dtr = true;
        private bool            _rts = true;
        private bool            _breakState = false;
		private String          _newLine = Environment.NewLine;

		#region Constructors

		public FTDI(string serial, int baudRate, FtdiParity parity, byte dataBits, FtdiStopBits stopBits, FtdiFlowControl flowControl)
        {
            SetPort(serial);
			_baudRate = baudRate;
			_parity = parity;
			_dataBits = dataBits;
			_stopBits = stopBits;
            _flowControl = flowControl;
		}

        public FTDI(int locationId, int baudRate, FtdiParity parity, byte dataBits, FtdiStopBits stopBits, FtdiFlowControl flowControl)
        {
            _curObject = locationId;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _flowControl = flowControl;
		}

        public FTDI(String description)
        {
            HandleResult(FT_OpenEx(description, OpenExFlag.BY_DESCRIPTION, out _handle));
        }

        /// <summary>
        /// Support the constructors in a genetic way to set the object
        /// that is the port.
        /// </summary>
        /// <param name="port">The string to convert into a number that makes sense to FTDI</param>
        private void SetPort(string port)
        {
            _curObject = null;

            // A string of 8 characters is an FTDI identifier
            if (port.Length == 8)
            {
                _curObject = port;
                return;
            }

            try
            {
                int location;
                if (port.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                    location = int.Parse(port.Substring(2), NumberStyles.HexNumber);
                else
                    location = int.Parse(port);
                _curObject = location;
            }
            catch (FormatException)
            {
                // not a number, so try serial number
                _curObject = port;
            }

            if (null == _curObject)
            {
                throw new Exception("Cannot have a NULL object");
            }
        }

        #endregion Constructors

        #region Get and Set FTDI configurations

        /// <summary>
        /// Get the number of bytes available to read
        /// </summary>
        private UInt32 BytesToRead
        {
            get
            {
                int readBytes, writeBytes, eventState;
                GetStatus(out readBytes, out writeBytes, out eventState);
                return (UInt32)readBytes;
            }
        }

        /// <summary>
        /// Get the number of bytes available to write
        /// </summary>
        private int BytesToWrite
        {
            get
            {
                int readBytes, writeBytes, eventState;
                GetStatus(out readBytes, out writeBytes, out eventState);
                return writeBytes;
            }
        }

        /// <summary>
        /// A generic function to handle FTDI DLL reports
        /// </summary>
        /// <param name="result"></param>
        internal static void HandleResult(int result)
        {
            if (result != (int)ErrorCode.OK)
            {
                if (result < 0 || result > ErrorString.Length - 1)
                    result = ErrorString.Length - 1;

                string msg = ErrorString[result] + Environment.NewLine;
                Debug.WriteLine(msg);
                throw new IOException(msg);
            }
        }

        /// <summary>
        /// A getter/setter for the read timeout
        /// </summary>
        public int ReadTimeout
        {
            get
            {
                return _readTimeout;
            }
            set
            {
                _readTimeout = value;
                if (IsOpen)
                    HandleResult(FT_SetTimeouts(_handle, _readTimeout, _writeTimeout));
            }
        }

        /// <summary>
        /// A getter/setter for the write timeout
        /// </summary>
        public int WriteTimeout
        {
            get
            {
                return _writeTimeout;
            }
            set
            {
                _writeTimeout = value;
                if (IsOpen)
                    HandleResult(FT_SetTimeouts(_handle, _readTimeout, _writeTimeout));
            }
        }

        /// <summary>
        /// A getter/setter for DTR
        /// </summary>
        public bool DTR
        {
            get
            {
                return _dtr;
            }
            set
            {
                _dtr = value;
                if (IsOpen)
                {
                    if (_dtr)
                        HandleResult(FT_SetDtr(_handle));
                    else
                        HandleResult(FT_ClrDtr(_handle));
                }
            }
        }

        /// <summary>
        /// A getter/setter for RTS
        /// </summary>
        public bool RTS
        {
            get
            {
                return _rts;
            }
            set
            {
                _rts = value;
                if (IsOpen)
                {
                    if (_rts)
                        HandleResult(FT_SetRts(_handle));
                    else
                        HandleResult(FT_ClrRts(_handle));
                }
            }
        }

        /// <summary>
        /// A getter/setter for CTS
        /// </summary>
        public bool CTS
        {
            get
            {
                return (GetModemStatus() & (int)ModemStatus.CTS) != 0;
            }
        }

        /// <summary>
        /// A getter/setter for DSR
        /// </summary>
        public bool DSR
        {
            get
            {
                return (GetModemStatus() & (int)ModemStatus.DSR) != 0;
            }
        }

        /// <summary>
        /// A getter/setter for RI
        /// </summary>
        public bool RI
        {
            get
            {
                return (GetModemStatus() & (int)ModemStatus.RI) != 0;
            }
        }

        /// <summary>
        /// A getter/setter for DCD
        /// </summary>
        public bool DCD
        {
            get
            {
                return (GetModemStatus() & (int)ModemStatus.DCD) != 0;
            }
        }

        /// <summary>
        /// A getter/setter for Break State
        /// </summary>
        public bool BreakState
        {
            set
            {
                CheckOpen();
                _breakState = value;
                if (_breakState)
                    HandleResult(FT_SetBreakOn(_handle));
                else
                    HandleResult(FT_SetBreakOff(_handle));
            }
            get
            {
                return _breakState;
            }
        }

        /// <summary>
        /// A getter/setter for Latency Timer
        /// </summary>
        public byte LatencyTimer
        {
            get
            {
                CheckOpen();
                byte ms;
                HandleResult(FT_GetLatencyTimer(_handle, out ms));
                return ms;
            }
            set
            {
                CheckOpen();
                HandleResult(FT_SetLatencyTimer(_handle, value));
            }
        }

	    public void SetChars(byte eventChar, byte eventCharEnable, byte errorChar, byte errorCharEnable)
	    {
            HandleResult(FT_SetChars(_handle, eventChar, eventCharEnable, errorChar, errorCharEnable));
	    }


        /// <summary>
        /// Helper function to get the state of the FTDI modem
        /// </summary>
        /// <returns>The bitfield of the modem status</returns>
        private int GetModemStatus()
        {
            int status;

            CheckOpen();
            HandleResult(FT_GetModemStatus(_handle, out status));

            return status;
        }

        /// <summary>
        /// Helper function to get status of FTDI IO buffers
        /// </summary>
        /// <param name="readBytes"></param>
        /// <param name="writeBytes"></param>
        /// <param name="eventState"></param>
        private void GetStatus(out int readBytes, out int writeBytes, out int eventState)
        {
            CheckOpen();
            HandleResult(FT_GetStatus(_handle, out readBytes, out writeBytes, out eventState));
        }

        public void SetUSBParameters(int inTransferSize, int outTransferSize)
        {
			CheckOpen();
			HandleResult(FT_SetUSBParameters(_handle, inTransferSize, outTransferSize));
		}

        public void SetBitMode(byte mask, byte mode)
        {
            CheckOpen();
            HandleResult(FT_SetBitMode(_handle, mask, mode));
        }

        public void SetResetPipeRetryCount(int count)
        {
			CheckOpen();
			HandleResult(FT_SetResetPipeRetryCount(_handle, count));
		}
        
		public String NewLine 
        {
			get { return _newLine; }
			set { _newLine = value; }
		}

        public void SetBaudRate(int baudRate)
        {
            _baudRate = baudRate;
            HandleResult(FT_SetBaudRate(_handle, _baudRate));
        }

        #endregion Get and Set FTDI configurations

        /// <summary>
        /// Public interface to check whether the FTDI port is open
        /// </summary>
		public bool IsOpen 
        {
            get
            {
                return (_handle != IntPtr.Zero);
            }
		}

        /// <summary>
        /// Private interface to ensure that the FTDI port is open, and throw
        /// an exception it it's not
        /// </summary>
        private void CheckOpen()
        {
			if (!IsOpen)
				throw new InvalidOperationException("FTDI port is not open");
		}

        /// <summary>
        /// Open an FTDI port and configure it for communication in the mode
        /// specified in the constructor
        /// </summary>
		public void Open() 
        {
			// close any prior handle
			Close();

			// open the port
            if (_curObject is string)
            {
                HandleResult(FT_OpenEx((string)_curObject, OpenExFlag.BY_SERIAL_NUMBER, out _handle));
            }
            else if (_curObject is int)
            {
                int value = (int)_curObject;
                int result = 0;
                if (value == 0)
                {
                    result = FT_Open(value, out _handle);
                }
                else
                {
                    result = FT_OpenEx(value, OpenExFlag.BY_LOCATION, out _handle);
                    if (result == (int)ErrorCode.DEVICE_NOT_FOUND)
                    {
                        result = FT_Open((int)_curObject, out _handle);
                    }
                }

                // check our latest result
                HandleResult(result);
            }
            else
                throw new InvalidOperationException("FTDI port not specified (" + _curObject + ")");

			//
			// setup port parameters immediately after open (we can't do it beforehand!)
			//

			// setup the baud rate
            SetBaudRate(_baudRate);

			// setup the read/write timeouts
			HandleResult(FT_SetTimeouts(_handle, _readTimeout, _writeTimeout));

			// setup data bits, stop bits and parity
			HandleResult(FT_SetDataCharacteristics(_handle, _dataBits, _stopBits, _parity));

			// setup flow control
			HandleResult(FT_SetFlowControl(_handle, _flowControl, 17, 19));

			// setup RTS and DTR (depending on flow control mode)
			switch (_flowControl) 
            {
			    case FtdiFlowControl.RTS_CTS:
				    if (_dtr)
					    HandleResult(FT_SetDtr(_handle));
				    else
					    HandleResult(FT_ClrDtr(_handle));
				    break;
			    case FtdiFlowControl.DTR_DSR:
				    if (_rts)
					    HandleResult(FT_SetRts(_handle));
				    else
					    HandleResult(FT_ClrRts(_handle));
				    break;
			    case FtdiFlowControl.XON_XOFF:
			    case FtdiFlowControl.NONE:
			    default:
				    if (_dtr)
					    HandleResult(FT_SetDtr(_handle));
				    else
					    HandleResult(FT_ClrDtr(_handle));

				    if (_rts)
					    HandleResult(FT_SetRts(_handle));
				    else
					    HandleResult(FT_ClrRts(_handle));
				    break;
			}

			// disable event chars (not supported in this class)
			HandleResult(FT_SetChars(_handle, 0, 0, 0, 0));

			// set latency to something better than default
			HandleResult(FT_SetLatencyTimer(_handle, 4));

			// set USB xfer size to 1088 (1024 byte X-modem packet + overhead in x-modem and driver)
			HandleResult(FT_SetUSBParameters(_handle, 1024 + 128, 1024 + 128));
		}
        
        /// <summary>
        /// If an FTDI port is open, close it
        /// </summary>
        public void Close()
        {
			if (IsOpen) 
            {
				FT_Close(_handle);
				_handle = IntPtr.Zero;
			}
		}

        #region Read interface

		/// <summary>
		/// Read 1 byte .
		/// </summary>
		/// <returns>the byte that was read</returns>
        public byte ReadByte()
        {
			byte[] b = new byte[1];
            if (1 != Read(b, 0, 1))
                throw new Exception("Unable to read one byte");

			return b[0];
		}

        /// <summary>
        /// Read a byte of data, waiting at most the specified timeout milliseconds
        /// before calling it a timeout.
        /// </summary>
        /// <param name="numBytesToRead">The number of bytes to read</param>
        /// <param name="timeout">Timeout in ms</param>
        /// <param name="value">The byte value that was read in, or zero on failure</param>
        /// <returns>True on success, false otherwise</returns>
        public bool ReadBytesTimeout(UInt32 numBytesToRead, Int32 timeout, out byte[] data)
        {
            ReadTimeout = timeout;
            data = new byte[0];

            try
            {
                data = new byte[numBytesToRead];
                if (numBytesToRead != Read(data, 0, numBytesToRead))
                    throw new TimeoutException("Unable to read " + numBytesToRead.ToString() + " byte");
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Read the requested number of bytes from the FTDI com port and put 
        /// them into the requested offset within the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to store the data</param>
        /// <param name="offset">The offset within the buffer at which to store the data</param>
        /// <param name="count">The number of bytes to read</param>
        /// <returns></returns>
        public UInt32 Read(byte[] buffer, UInt32 offset, UInt32 count)
        {
			CheckOpen();

			int bytesRead;
			HandleResult(FT_Read(_handle, ref buffer[offset], (int)count, out bytesRead));
			if (bytesRead <= 0)
                throw new TimeoutException(MethodBase.GetCurrentMethod().Name);

			return (UInt32)bytesRead;
		}
        
		/// <summary>
		/// discard all incoming data but the last byte, then return that last byte
		/// </summary>
		/// <returns>last byte in the current receive buffer</returns>
		public byte ReadLastByte() 
        {
			UInt32 n = BytesToRead;
			if (n > 1) 
            {
				byte[] junk = new byte[n - 1];
				Read(junk, 0, n - 1);
			}
			return ReadByte();
		}

        /// <summary>
        /// discard all incoming data but the last byte, then return that last byte
        /// </summary>
        /// <param name="timeout">timeout int mS for ReadLastByte</param>
        /// <returns></returns>
        public byte ReadLastByte(int timeout)
        {
            ReadTimeout = timeout;
            return ReadLastByte();
        }

		/// <summary>
		/// read from the serial port into b[] until it's full or there is ReadTimeout milliseconds of inactivity
		/// </summary>
		/// <remarks>
		/// This differs from the behaviour of Read() in that Read() will return after ReadTimeout milliseconds
		/// with the data it has and NOT throw a timeout exception.  You may want this behaviour if your
		/// timeout interval starts at the beginning of the read--then you can check the size to tell if you've
		/// gotten all the data you wanted in the time allotted.
		/// </remarks>
		/// <param name="b">byte array to read data into</param>
		/// <returns># of bytes actually ready (should be b.Length)</returns>
		public UInt32 ReadFully(byte[] b) 
        {
			UInt32 offset = 0;
			UInt32 len = (UInt32)b.Length;

			while (len > 0) 
            {
				UInt32 count = Read(b, offset, len);
				if (count == 0) return offset;
				offset += count;
				len -= count;
			}

			return offset;
		}

		/// <summary>
		/// byte array read method--shortcut for Read (b, 0, b.length)
		/// </summary>
		/// <param name="b">byte array to send out</param>
		public UInt32 Read(byte[] b) {
			return Read(b, 0, (UInt32)b.Length);
		}
        		/// <summary>
		/// wait for input to be idle for a given amount of time
		/// </summary>
		/// <param name="ms">idle time required before function returns</param>
        public void IdleInput(int ms)
        {
			ReadTimeout = ms;
			IdleInput();
		}

		/// <summary>
		/// wait for input to be idle for the current receive timeout period
		/// </summary>
		public void IdleInput() 
        {
			try {
				//DiscardInBuffer ();
				do {
					// now we want a timeout!
					ReadByte();
					//int b = ReadByte();
					//Debug.WriteLine("byte = " + b + " / " + (char) b);
				} while (true);
			} catch (TimeoutException) { }
		}

		/// <summary>
		/// read a particular number of characters from the serial port into a string
		/// </summary>
		/// <param name="numChars">the # of characters to read</param>
		/// <returns>string containing the characters received</returns>
		public String ReadCount(int count) 
        {
			byte[] b = new byte[count];
			ReadFully(b);
			return Encoding.ASCII.GetString(b);
		}

		/// <summary>
		/// read a line from the device
		/// </summary>
		/// <remarks>
		/// will throw a TimeoutException if the serial device doesn't terminate a line in time
		/// </remarks>
		/// <returns>a string containing the data form the serial device</returns>
		public String ReadLine() 
        {
			StringBuilder sb = new StringBuilder();

			while (true) 
            {
				int b = ReadByte();
				//Debug.WriteLine("b = " + b);
				if (b == '\n')
					return sb.ToString();
				if (b == '\r')
					continue;
				sb.Append((char) b);
			}
		}

		/// <summary>
		/// read a line from the device with specified timeout
		/// </summary>
		/// <param name="timeout">timeout int mS for ReadLineTimeout</param>
		/// <returns>a string containing the data form the serial device</returns>
        public string ReadLine(int timeout)
        {
            //AddHistoryLog(MethodBase.GetCurrentMethod().Name, "");
			ReadTimeout = timeout;
			return ReadLine();
		}

		/// <summary>
		/// read a line from the device (timeout handled version)
		/// </summary>
		/// <remarks>
		/// similar to ReadLine() but will return the received string upon serial port timeout instead of throwing the TimeoutException
		/// </remarks>
		/// <returns>a string containing the data form the serial device</returns>
		public string ReadLineTimeout() 
        {
			StringBuilder sb = new StringBuilder();

			try {
				while (true) {
					int b = ReadByte();
					//System.Console.WriteLine ("b = " + b);
					// new: terminate on either line end, but
					// only if we've got some data for the line
					if (b == '\n' || b == '\r') {
						if (sb.Length > 0)
							return sb.ToString();
						continue;
					}
					sb.Append((char) b);
				}
			} catch (TimeoutException) {
				// return what we have so far (due to timeout)
				return sb.ToString();
			}
		}

		/// <summary>
		/// read a line from the device (timeout handled version)
		/// </summary>
		/// <param name="timeout">timeout int mS for ReadLineTimeout</param>
		/// <returns>a string containing the data form the serial device</returns>
        public string ReadLineTimeout(int timeout)
        {
			ReadTimeout = timeout;
			return ReadLineTimeout();
		}

        #endregion Read interface

        #region Write interface

        /// <summary>
        /// Write 1 byte to the FTDI com port
        /// </summary>
        /// <param name="value"></param>
        public void Write(byte value)
        {
			byte[] b = new byte[1] {value};
            Write(b, 0, 1);
		}

        /// <summary>
        /// Write the requested number of bytes from the provided buffer
        /// at the provided offset into the FTDI com port
        /// </summary>
        /// <param name="buffer">The buffer containing the data</param>
        /// <param name="offset">The offset intothe buffer at which to start taking the data</param>
        /// <param name="count">The number of bytes to write</param>
        public void Write(byte[] buffer, int offset, int count)
        {
			CheckOpen();

			// if we write parts, retry until it's all written (should we sleep during this?)
			while (count > 0) 
            {
				int bytesWritten = 0;
				HandleResult(FT_Write(_handle, ref buffer[offset], count, ref bytesWritten));
				offset += bytesWritten;
				count -= bytesWritten;
			}
		}

		/// <summary>
		/// byte array writing method--shortcut for Write (b, 0, b.length)
		/// </summary>
		/// <param name="b">byte array to send out</param>
		public void Write(byte[] b) 
        {
			Write(b, 0, b.Length);
		}
        
        public void Write(string s)
        {
			// get ASCII byte array from string and send it
			Write(Encoding.ASCII.GetBytes(s));
		}

        public void WriteLine(string s)
        {
			// get ASCII byte array from string and send it
			Write(Encoding.ASCII.GetBytes(s + NewLine));
		}

        #endregion Write interface

        #region Low level driver interface

	    public void GetQueueStatus(out int amountInRxQueue)
	    {
            HandleResult(FT_GetQueueStatus(_handle, out amountInRxQueue));
	    }

        /// <summary>
        /// Purge the FTDI IN buffer
        /// </summary>
        public void DiscardInBuffer()
        {
            CheckOpen();

            int ftStatus;
            DateTime start = new DateTime();
            start = DateTime.Now;
            do
            {
                ftStatus = FT_StopInTask(_handle);

                // Break out of the loop in 3 seconds
                if (DateTime.Now >= start.AddSeconds(3))
                {
                    // Do not throw exceptions while running inside a debugger
                    if (!Debugger.IsAttached)
                    {
                        throw new TimeoutException("Could not FT_StopInTask()");
                    }
                }
            } while (ftStatus != 0 /*FT_OK*/);

            HandleResult(FT_Purge(_handle, PurgeFlag.RX));

            start = DateTime.Now;
            do
            {
                ftStatus = FT_RestartInTask(_handle);

                // Break out of the loop in 3 seconds
                if (DateTime.Now >= start.AddSeconds(3))
                {
                    throw new TimeoutException("Could not FT_RestartInTask()");
                }
            } while (ftStatus != 0 /*FT_OK*/);
        }

        /// <summary>
        /// Purge the FTDI OUT buffer
        /// </summary>
        public void DiscardOutBuffer()
        {
			CheckOpen();
			HandleResult(FT_Purge(_handle, PurgeFlag.TX));

		}

        private void StopInTask()
        {
			CheckOpen();
			HandleResult(FT_StopInTask(_handle));
		}

        private void RestartInTask()
        {
			CheckOpen();
			HandleResult(FT_RestartInTask(_handle));
		}

        public void ResetDevice()
        {
			CheckOpen();
			HandleResult(FT_ResetDevice(_handle));
		}

        public void ResetPort()
        {
			CheckOpen();
			HandleResult(FT_ResetPort(_handle));
		}

        public void CyclePort()
        {
			CheckOpen();
			HandleResult(FT_CyclePort(_handle));
			Close();
		}

        public string GetFtdiSN()
        {
			CheckOpen();

			StringBuilder sn = new StringBuilder(16);
			StringBuilder desc = new StringBuilder(64);

			int type;
			int id;
			int junk;

			HandleResult(FT_GetDeviceInfo (_handle, out type, out id, sn, desc, out junk));

			return sn.ToString();
		}

        public static FtdiDeviceInfoStruct[] GetDeviceInfoList()
        {
			int count = 0;

            try
            {
                HandleResult(FT_CreateDeviceInfoList(out count));
            }
            catch
            {
                return null;
            }

			FtdiDeviceInfoStruct[] nodes = new FtdiDeviceInfoStruct[count];

			// I gave up on getting the list via the single function due to marshalling issues... this works well enough!
			for (int i = 0; i < count; i++) 
            {
				StringBuilder sn = new StringBuilder(16);
				StringBuilder desc = new StringBuilder(64);
				//Debug.WriteLine("fetching info for FTDI device #" + i);
				HandleResult(FT_GetDeviceInfoDetail(i, out nodes[i].Flags, out nodes[i].Type, out nodes[i].ID, out nodes[i].LocId, sn, desc, out nodes[i].ftHandle));
				nodes[i].SerialNumber = sn.ToString();
				nodes[i].Description = desc.ToString();
				//Debug.WriteLine("info: " + nodes[i].ToString());
			}

			return nodes;
		}
        
        public int NumDevices()
        {
			int count;
			HandleResult(FT_ListDevices(out count, 0, ListDevicesFlag.NUMBER_ONLY));
			return count;
		}

		public int DriverVersion 
        {
            get
            {
				CheckOpen();
				int version;
				HandleResult(FT_GetDriverVersion(_handle, out version));
				return version;
			}
		}

		public int DllVersion 
        {
            get
            {
				int version;
				HandleResult(FT_GetLibraryVersion(out version));
				return version;
			}
        }
        
        #endregion Low level driver interface

        #region EEPROM access

        public FtdiConfig ReadEEConfig()
        {
			byte[] SerialNumber = new byte[16];
			byte[] Description = new byte[64];
			byte[] Manufacturer = new byte[32];
			byte[] ManufacturerId = new byte[16];

			FtdiConfig data = new FtdiConfig();

			data.Signature1 = 0x00000000;
			data.Signature2 = 0xffffffff;
			data.Version = 0x00000002;

			HandleResult(FT_EE_ReadEx(_handle, ref data, Manufacturer, ManufacturerId, Description, SerialNumber));

			data.SerialNumber = this.GetStringFromBytes(SerialNumber);
			data.Description = this.GetStringFromBytes(Description);
			data.Manufacturer = this.GetStringFromBytes(Manufacturer);
			data.ManufacturerId = this.GetStringFromBytes(ManufacturerId);

			return data;
		}

		private string GetStringFromBytes(byte[] bytes)
        {
			int len = 0;
			while (bytes[len] != 0 && len < bytes.Length)
			{
				len++;
			}

			return Encoding.ASCII.GetString(bytes, 0, len);
		}

		/// <summary>
		/// program the FTDI chips EEPROM with the contents of the FtdiConfig structure
		/// </summary>
		/// <param name="data">sturcture holding the FTDI chip configuration</param>
		public void ProgramEEConfig(ref FtdiConfig data)
        {
			// programmers manual says this is a restriction
			if (data.Manufacturer.Length + data.Description.Length > 40)
			{
				throw new InvalidDataException("Manufacturer length + Description length is too big (> 40)");
			}

			HandleResult(
				FT_EE_ProgramEx(
					_handle,
					ref data, 
					ASCIIEncoding.ASCII.GetBytes(data.Manufacturer),
					ASCIIEncoding.ASCII.GetBytes(data.ManufacturerId),
					ASCIIEncoding.ASCII.GetBytes(data.Description),
					ASCIIEncoding.ASCII.GetBytes(data.SerialNumber)
				)
			);
		}

        public int GetEEUASize()
        {
			int size;
			HandleResult(FT_EE_UASize(_handle, out size));
			return size;
		}

        public byte[] GetEEUAData()
        {
			int size = GetEEUASize();
			byte[] data = new byte[size];

			int bytesRead;
			FT_EE_UARead(_handle, data, size, out bytesRead);

			//Debug.WriteLine("size = " + size);
			//Debug.WriteLine("bytesRead = " + bytesRead);

			return data;
		}

        public void WriteEEUAData(byte[] data)
        {
			int maxSize = GetEEUASize();

			if (data.Length > maxSize)
				throw new InvalidDataException("array is bigger than available space in EE");

			FT_EE_UAWrite(_handle, data, data.Length);
		}

        public ushort EEReadUInt16(int address)
        {
			CheckOpen();
			ushort word;
			HandleResult(FT_ReadEE(_handle, address, out word));
			return word;
		}

        public void EEWriteUInt16(int address, ushort value)
        {
			CheckOpen();
			HandleResult(FT_WriteEE(_handle, address, value));
		}

        public void EEEraseAll()
        {
			CheckOpen();
			HandleResult(FT_EraseEE(_handle));
		}

        #endregion EEPROM access
	}
}
