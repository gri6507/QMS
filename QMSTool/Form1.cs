using System;
using System.IO;
using System.Windows.Forms;

namespace QMSTool
{
    public partial class QMSTool : Form
    {
        private IFTDI _uart;

        public QMSTool()
        {
            InitializeComponent();
            Text = @"QMSTool - " + RetrieveLinkerTimestamp();

            ScanForFtdiDevices();
        }

        private void ScanForFtdiDevices()
        {
            FtdiDeviceInfoStruct[] devices = FTDI.GetDeviceInfoList();
            comboBoxFtdiDevice.Items.Clear();
            foreach (var device in devices)
                comboBoxFtdiDevice.Items.Add(device);
            if (0 == devices.Length)
            {
                const MessageBoxButtons button = MessageBoxButtons.OK;
                const MessageBoxIcon icon = MessageBoxIcon.Error;
                MessageBox.Show(@"No FTDI devices found", @"Error", button, icon);
            }
            else
                comboBoxFtdiDevice.SelectedIndex = 0;
        }

        /// <summary>
        /// Function to extract the build date/time stamp
        /// </summary>
        /// <returns>the build datetime</returns>
        private DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, cPeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        private void buttonReadRegister_Click(object sender, EventArgs e)
        {

        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            FtdiDeviceInfoStruct device = (FtdiDeviceInfoStruct)comboBoxFtdiDevice.SelectedItem; 
            _uart = new FTDI(
                device.SerialNumber,
                921600,
                FtdiParity.None,
                8,
                FtdiStopBits.One,
                FtdiFlowControl.NONE);
            _uart.Open();

            buttonConnect.Enabled = false;
            comboBoxFtdiDevice.Enabled = false;
            buttonDisconnect.Enabled = true;
            groupBoxCommunication.Enabled = true;
            richTextBoxInfo.Enabled = true;
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            _uart.Close();
            buttonConnect.Enabled = true;
            comboBoxFtdiDevice.Enabled = true;
            buttonDisconnect.Enabled = false;
            groupBoxCommunication.Enabled = false;
            richTextBoxInfo.Enabled = false;
        }

        private void buttonVersion_Click(object sender, EventArgs e)
        {
            // Ask for the version
            _uart.WriteLine("V\n");

            // Get the echo 
            String line = _uart.ReadLineTimeout(1000);
            if (String.IsNullOrEmpty(line))
            {
                WriteLine("Error sending the command.");
            }
            else
            {
                line = _uart.ReadLineTimeout(1000);
                if (String.IsNullOrEmpty(line))
                {
                    WriteLine("Error reading version.");
                }
                else
                {
                    WriteLine("Version is " + line);
                }
            }
        }


        public void WriteLine(String s)
        {
            richTextBoxInfo.AppendText(s);
            richTextBoxInfo.ScrollToCaret();
        }
    }
}
