using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace QMSTool
{
    public partial class QMSTool : Form
    {
        private IFTDI _uart;

        private readonly Dictionary<UInt32, String> _registers = new Dictionary<UInt32, String>
        {
            {0x000, "FPGAVersion"},
            {0x004, "ModeCtrl"},
            {0x008, "DAC1"},
            {0x00c, "DAC2"},
            {0x010, "DAC3"},
            {0x014, "DAC4"},
            {0x018, "ADC1"},
            {0x01c, "ADC2"},
            {0x020, "ADC3"},
            {0x024, "ADC4"},
        };

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

        private String SendCmdGetResponse(String cmd)
        {
            String ans = String.Empty;

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
                    WriteLine("Error reading response.");
                }
                else
                {
                    ans = line;
                }
            }

            return ans;
        }

        private void buttonVersion_Click(object sender, EventArgs e)
        {
            String version = SendCmdGetResponse("V\n");
            if (!String.IsNullOrEmpty(version))
            {
                WriteLine("Version is " + version);
            }
        }

        private void buttonReadRegister_Click(object sender, EventArgs e)
        {
            if (0 == textBoxRegAddr.Text.Length)
            {
                WriteLine("Bad Register address");
            }
            else
            {
                try
                {
                    UInt32 regAddr = UInt32.Parse(textBoxRegAddr.Text, NumberStyles.HexNumber);
                    String regValue;
                    ReadRegister(regAddr, out regValue);
                    if (String.IsNullOrEmpty(regValue))
                    {
                        WriteLine("Unable to read register at 0x" + regAddr.ToString("x3"));
                        textBoxRegValue.Text = String.Empty;
                    }
                    else
                    { 
                        WriteLine(_registers[regAddr] + " = " + regValue);
                        textBoxRegValue.Text = regValue;
                    }
                }
                catch (FormatException)
                {
                    WriteLine("Bad register address 0x" + textBoxRegAddr.Text);
                }
            }
        }

        private void buttonWriteRegister_Click(object sender, EventArgs e)
        {
            if (0 == textBoxRegAddr.Text.Length)
            {
                WriteLine("Bad Register address");
            }
            else if (0 == textBoxRegValue.Text.Length)
            {
                WriteLine("Bad Register Value");
            }
            else
            {
                try
                {
                    UInt32 regAddr = UInt32.Parse(textBoxRegAddr.Text, NumberStyles.HexNumber);
                    UInt32 regValue = UInt32.Parse(textBoxRegValue.Text, NumberStyles.HexNumber);
                    if (WriteRegister(regAddr, regValue))
                    {
                        WriteLine("Error writing register " + regAddr.ToString("x3"));
                    }
                }
                catch (FormatException)
                {
                    WriteLine("Bad register address 0x" + textBoxRegAddr.Text);
                }
            }
        }

        private void buttonReadAllRegisters_Click(object sender, EventArgs e)
        {
            foreach (var reg in _registers)
            {
                String regValue;
                ReadRegister(reg.Key, out regValue);
                if (!String.IsNullOrEmpty(regValue))
                {
                    WriteLine(reg.Value + " = " + regValue);
                }
            }
        }

        private bool WriteRegister(uint regAddr, uint regValue)
        {
            bool status = false;
            try
            {
                String answer = SendCmdGetResponse("W " + regAddr.ToString("x") + " " + regValue.ToString("x") + "\n");
                if (answer.StartsWith("Y"))
                {
                    status = true;
                }
            }
            catch
            {
                // ignored
            }
            return status;
        }

        private void ReadRegister(uint regAddr, out string regValue)
        {
            regValue = String.Empty;

            try
            {
                String answer = SendCmdGetResponse("R " + regAddr.ToString("x") + "\n");
                String[] tokens = answer.Split(' ');
                if (tokens[0].Equals("Y"))
                {
                    regValue = tokens[1];
                }
            }
            catch
            {
                // ignored
            }
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


        public void WriteLine(String s)
        {
            richTextBoxInfo.AppendText(s);
            richTextBoxInfo.ScrollToCaret();
        }
    }
}
