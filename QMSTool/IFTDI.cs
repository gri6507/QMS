using System;

namespace QMSTool
{	
    /// <summary>
    /// Common, stripped down interface functions for a UART
    /// </summary>
    public interface IFTDI
    {
        void Open();
        void Close();
        bool IsOpen { get; }
        void SetBaudRate(int baudRate);

        void DiscardInBuffer();
        void DiscardOutBuffer();

        void WriteLine(string line);
        void Write(byte byteVal);
        void Write(byte[] b);
        string ReadLineTimeout(Int32 timeout);
        bool ReadBytesTimeout(UInt32 numBytesToRead, Int32 timeout, out byte[] data);
    }
}
