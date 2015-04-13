using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            {0x028, "GPIO32_1"},
            {0x02C, "GPIO64_33"},
            {0x030, "GPIO_H10_1_and_GPIO80_65"},
            {0x034, "ConfigGPIO32_1"},
            {0x038, "ConfigGPIO64_33"},
            {0x03C, "ConfigGPIO_H10_1_and_GPIO80_65"},
        };

        private readonly CheckBox[] _ioConfig;
        private readonly CheckBox[] _ioState;

        public QMSTool()
        {
            InitializeComponent();
            Text = @"QMSTool - " + RetrieveLinkerTimestamp();

            ScanForFtdiDevices();

            _ioConfig = new CheckBox[90];
            _ioState = new CheckBox[_ioConfig.Length];
            const int topOffset = 20;
            const int lHeight = 25;

            CreateRowOfIo( 1, 32,               topOffset + lHeight * 0, topOffset + lHeight * 1, topOffset + lHeight * 2);
            CreateRowOfIo(33, 64,               topOffset + lHeight * 3, topOffset + lHeight * 4, topOffset + lHeight * 5);
            CreateRowOfIo(65, _ioConfig.Length, topOffset + lHeight * 6, topOffset + lHeight * 7, topOffset + lHeight * 8);

            // Default every IO to input
            UInt32[] regAddrs = { 0x028, 0x02c, 0x030 };
            foreach (var regAddr in regAddrs)
            {
                if (!WriteRegister(regAddr, 0))
                {
                    WriteLine("Error setting IO config");
                }
            }

            Button buttonUpdateInputs = new Button
            {
                Text = @"Update Inputs",
                Left = 750,
                Top = topOffset + lHeight * 8 - 15,
                AutoSize = true,
            };
            buttonUpdateInputs.Click += UpdateAllInputs;
            groupBoxIo.Controls.Add(buttonUpdateInputs);
            buttonUpdateInputs.PerformClick();
        }

        private void CreateRowOfIo(int startNum, int stopNum, int top1, int top2, int top3)
        {
            const int leftOffset = 30;
            const int cbHeight = 20;
            const int cbWidth = 25;

            Label labelConfig = new Label
            {
                Text = @"Config",
                Left = 5,
                Top = top2,
                AutoSize = true,
            };
            Label labelState = new Label
            {
                Text = @"State",
                Left = 5,
                Top = top3,
                AutoSize = true,
            };
            groupBoxIo.Controls.Add(labelConfig);
            groupBoxIo.Controls.Add(labelState);

            for (int i = startNum; i <= stopNum; i++)
            {
                Label labelIoNumber = new Label
                {
                    Text = i.ToString(),
                    Left = leftOffset + (i - startNum + 1) * (cbWidth),
                    Top = top1,
                    Width = cbWidth,
                    Height = cbHeight,
                };
                groupBoxIo.Controls.Add(labelIoNumber);

                _ioConfig[i - 1] = new CheckBox
                {
                    Left = leftOffset + (i - startNum + 1) * (cbWidth),
                    Top = top2,
                    Width = cbWidth,
                    Height = cbHeight,
                    Name = "Config" + i,
                    Checked = false,
                };
                _ioConfig[i - 1].CheckedChanged += HandleIoConfigChange;
                groupBoxIo.Controls.Add(_ioConfig[i - 1]);

                _ioState[i - 1] = new CheckBox
                {
                    Left = leftOffset + (i - startNum + 1) * (cbWidth),
                    Top = top3,
                    Width = cbWidth,
                    Height = cbHeight,
                    Name = "State" + i,
                    Enabled = false,
                };
                _ioState[i - 1].CheckedChanged += HandleIoStateChange;
                groupBoxIo.Controls.Add(_ioState[i - 1]);
            }
        }

        private void UpdateAllInputs(object sender, EventArgs e)
        {
            UInt32[] regAddrs = {0x028, 0x02C, 0x030};
            int ioNumZeroBased = 0;

            foreach (UInt32 regAddr in regAddrs)
            {
                String regValue;
                ReadRegister(regAddr, out regValue);
                if (String.IsNullOrEmpty(regValue))
                {
                    WriteLine("Error reading IO state");
                }
                else
                {
                    WriteLine(_registers[regAddr] + " = " + regValue);
                    UInt32 regVal = UInt32.Parse(regValue, NumberStyles.HexNumber);
                    for (int i = 0; i < 32; i++)
                    {
                        if (!_ioConfig[ioNumZeroBased].Checked)
                        {
                            bool bitVal = (regVal & (1 << i)) == 1;
                            _ioState[ioNumZeroBased].Enabled = bitVal;
                        }
                        ioNumZeroBased++;
                        if (ioNumZeroBased >= _ioState.Length)
                            return;
                    }
                }
            }
        }

        private void HandleIoStateChange(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null && cb.Name.StartsWith("State"))
            {
                int ioNum = int.Parse(new String(cb.Name.ToCharArray().Where(Char.IsDigit).ToArray()));
                bool newVal = cb.Checked; 
                UInt32 regAddr;
                int bit;
                if ((ioNum >= 1) && (ioNum <= 32))
                {
                    regAddr = 0x028;
                    bit = ioNum - 1;
                }
                else if ((ioNum >= 33) && (ioNum <= 64))
                {
                    regAddr = 0x02c;
                    bit = ioNum - 33;
                }
                else
                {
                    regAddr = 0x030;
                    bit = ioNum - 65;
                }

                ReadModifyWriteReg(regAddr, newVal, bit, ioNum);
            }
        }

        private void HandleIoConfigChange(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null && cb.Name.StartsWith("Config"))
            {
                int ioNum = int.Parse(new String(cb.Name.ToCharArray().Where(Char.IsDigit).ToArray()));
                bool isOutput = cb.Checked;

                UInt32 regAddr;
                int bit;
                if ((ioNum >= 1) && (ioNum <= 32))
                {
                    regAddr = 0x034;
                    bit = ioNum - 1;
                }
                else if ((ioNum >= 33) && (ioNum <= 64))
                {
                    regAddr = 0x038;
                    bit = ioNum - 33;
                }
                else
                {
                    regAddr = 0x03C;
                    bit = ioNum - 65;
                }

                ReadModifyWriteReg(regAddr, isOutput, bit, ioNum);
            }
        }

        private bool ReadModifyWriteReg(UInt32 regAddr, bool isChecked, int bit, int ioNum)
        {
            bool status = false;
            String regValue;
            ReadRegister(regAddr, out regValue);
            if (String.IsNullOrEmpty(regValue))
            {
                WriteLine("Error reading IO config");
            }
            else
            {
                WriteLine(_registers[regAddr] + " = " + regValue);
                UInt32 regVal = UInt32.Parse(regValue, NumberStyles.HexNumber);
                if (isChecked)
                {
                    regVal &= (UInt32)(1 << bit);
                    _ioState[ioNum - 1].Enabled = true;
                }
                else
                {
                    regVal |= (UInt32)(1 << bit);
                    _ioState[ioNum - 1].Enabled = false;
                }
                if (!WriteRegister(regAddr, regVal))
                {
                    WriteLine("Error setting IO config");
                }
                else
                {
                    status = true;
                }
            }
            return status;
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
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

            // Send the command
            _uart.DiscardInBuffer();
            _uart.DiscardOutBuffer();
            _uart.WriteLine(cmd);

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
                    if (!WriteRegister(regAddr, regValue))
                    {
                        WriteLine("Error writing register " + regAddr.ToString("x3"));
                    }
                    else
                    {
                        WriteLine(_registers[regAddr] + " = " + regValue.ToString("x8"));
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

        private void buttonUpdateFirmware_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = @"Firmware (*.BIN)|*.BIN|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                WriteLine("Firmware update aborted by user");
                return;
            }

            bool success = false;
            try
            {
                Cursor = Cursors.WaitCursor;

                String filename = ofd.FileName;
                byte[] data = File.ReadAllBytes(filename);

                const int chunkSize = 4*1024;
                int dataIndex = 0;
                bool haveFailure = false;
                while (dataIndex < data.Length)
                {
                    int numBytesInChunk = 0;
                    UInt32 chunkChecksum = 0;
                    while (((dataIndex + numBytesInChunk) < data.Length) && (numBytesInChunk < chunkSize))
                    {
                        chunkChecksum += data[dataIndex + numBytesInChunk];
                        numBytesInChunk++;
                    }

                    // Request to send the chunk 
                    String cmd = String.Format("F {0:x} {1:x} {2:x}\n", dataIndex, numBytesInChunk, chunkChecksum);
                    String answer = SendCmdGetResponse(cmd);
                    if (!answer.StartsWith("Y"))
                    {
                        haveFailure = true;
                        break;
                    }

                    // We can now send the chunk
                    for (int i = 0; i < numBytesInChunk; i++)
                    {
                        _uart.Write(data[dataIndex++]);
                    }

                    // Verify the response
                    answer = _uart.ReadLineTimeout(1000);
                    if (!answer.StartsWith("Y"))
                    {
                        haveFailure = true;
                        break;
                    }
                }

                if (false == haveFailure)
                    success = true;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            WriteLine(success ? "Firmware update complete. Restart device!" : "Firmware update failed!");
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
            groupBoxIo.Enabled = true;
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            _uart.Close();
            buttonConnect.Enabled = true;
            comboBoxFtdiDevice.Enabled = true;
            buttonDisconnect.Enabled = false;
            groupBoxCommunication.Enabled = false;
            richTextBoxInfo.Enabled = false;
            groupBoxIo.Enabled = false;
        }


        public void WriteLine(String s)
        {
            richTextBoxInfo.AppendText(s + Environment.NewLine);
            richTextBoxInfo.ScrollToCaret();
        }
    }
}
