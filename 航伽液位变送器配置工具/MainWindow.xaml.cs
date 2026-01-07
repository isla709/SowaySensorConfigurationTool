using Medo.IO.Hashing;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 航伽液位变送器配置工具
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal List<string> SysPortList = [];
        internal List<string> BaudList = ["2400", "4800", "9600", "19200", "38400", "57600", "115200"];
        internal SerialPort? CommPort = null;


        internal string currentDeviceAddr = "N/D";

        internal List<ushort> DeviceADValue = new List<ushort>();

        internal float CurrentDeviceADValue = 0;

        public MainWindow()
        {
            InitializeComponent();

            SysPortList = SerialPort.GetPortNames().ToList();

        }

        private void GetSysPortToComboBox(ComboBox? comboBox)
        {
            SysPortList = SerialPort.GetPortNames().ToList();
            if (comboBox != null)
            {
                comboBox.Items.Clear();
                SysPortList.ForEach(port =>
                {
                    comboBox.Items.Add(port);
                });
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox? cb_port = cc_Port.Template.FindName("LC_Combobox", cc_Port) as ComboBox;
            ComboBox? cb_baud = cc_Baud.Template.FindName("LC_Combobox", cc_Baud) as ComboBox;

            GetSysPortToComboBox(cb_port);

            if (cb_baud != null)
            {
                BaudList.ForEach(baud => {
                    cb_baud.Items.Add(baud);
                });
            }

            btn_PortSwitch.Tag = false;

            Task.Run(async () => {
                TextBox? textBox = cc_Deviceaddr.Template.FindName("LT_Textbox", cc_Deviceaddr) as TextBox;
                if (textBox == null)
                {
                    MessageBox.Show("控件cc_Deviceaddr为Null", "未知错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                while (true)
                {
                    await Task.Delay(1000);
                    this.Dispatcher.Invoke(() =>
                    {
                        textBox.Text = currentDeviceAddr;
                    });
                }
            });

        }

        private void btn_reloadports_Click(object sender, RoutedEventArgs e)
        {
            ComboBox? cb_port = cc_Port.Template.FindName("LC_Combobox", cc_Port) as ComboBox;
            GetSysPortToComboBox(cb_port);
        }



        private void btn_PortSwitch_Click(object sender, RoutedEventArgs e)
        {

            ComboBox? cb_port = cc_Port.Template.FindName("LC_Combobox", cc_Port) as ComboBox;
            ComboBox? cb_baud = cc_Baud.Template.FindName("LC_Combobox", cc_Baud) as ComboBox;
            try
            {

                if (cb_port == null)
                {
                    throw new Exception("控件cb_port为空");
                }
                if (cb_baud == null)
                {
                    throw new Exception("控件cb_baud为空");

                }
                if (cb_port.SelectedIndex == -1 || cb_baud.SelectedIndex == -1)
                {
                    MessageBox.Show("请选择串口和波特率");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开串口失败:" + ex.Message);
                return;
            }




            if (btn_PortSwitch.Tag as bool? == false)
            {
                btn_PortSwitch.Tag = true;
                btn_reloadports.IsEnabled = false;
                cb_port!.IsEnabled = false;
                cb_baud!.IsEnabled = false;

                CommPort = new SerialPort();
                CommPort.PortName = cb_port!.SelectedValue.ToString();
                CommPort.BaudRate = int.Parse(cb_baud!.SelectedValue.ToString()!);
                CommPort.ReadTimeout = 2000;
                CommPort.DataReceived += CommPort_DataReceived;
                try
                {
                    CommPort.Open();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开串口失败:" + ex.Message);
                    btn_PortSwitch.Tag = false;
                    btn_reloadports.IsEnabled = true;
                    cb_port!.IsEnabled = true;
                    cb_baud!.IsEnabled = true;
                    return;
                }
            }
            else
            {
                btn_PortSwitch.Tag = false;
                btn_reloadports.IsEnabled = true;
                cb_port!.IsEnabled = true;
                cb_baud!.IsEnabled = true;

                if (CommPort!.IsOpen)
                {
                    CommPort.DataReceived -= CommPort_DataReceived;
                    CommPort.Close();
                    CommPort.Dispose();
                }
                CommPort = null;
            }

            if (btn_PortSwitch.Tag as bool? == false)
            {

                btn_PortSwitch.Content = "打开";
            }
            else
            {
                btn_PortSwitch.Content = "关闭";

            }

        }

        internal List<byte> RecvPortData = new List<byte>();
        private void CommPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int DataSize = CommPort.BytesToRead;
            byte[] RecvData = new byte[DataSize];
            CommPort.Read(RecvData, 0, DataSize);
            RecvPortData = RecvData.ToList();
            waitPortRecvSignal.Set();
        }

        internal ManualResetEventSlim waitPortRecvSignal = new ManualResetEventSlim(false);
        private async void btn_FindDevice_Click(object sender, RoutedEventArgs e)
        {

            TextBox? StartAddrText = cc_Startaddr.Template.FindName("LT_Textbox", cc_Startaddr) as TextBox;
            TextBox? EndAddrText = cc_Endaddr.Template.FindName("LT_Textbox", cc_Endaddr) as TextBox;

            int StartAddr = 1;
            int EndAddr = 10;

            try
            {
                StartAddr = int.Parse(StartAddrText!.Text);
                EndAddr = int.Parse(EndAddrText!.Text);
            }
            catch 
            {
                MessageBox.Show("输入起始或结束地址无效，默认以1-10查询", "错误", MessageBoxButton.OK);
            }

            if(StartAddr >= EndAddr)
            {
                MessageBox.Show("结束地址必须大于起始地址", "错误", MessageBoxButton.OK);
                return;
            }

            if (CommPort == null)
            {
                MessageBox.Show("请先打开端口");
                return;
            }
            if (CommPort?.IsOpen == true)
            {
                btn_FindDevice.IsEnabled = false;

                Crc16 crc = Crc16.GetModbus();
                
                for (int i = StartAddr; i <= EndAddr; i++)
                {
                    List<byte> FindDeviceCmd = new List<byte>();

                    FindDeviceCmd.Add((byte)i);
                    
                    FindDeviceCmd.Add(0x03);

                    FindDeviceCmd.Add(0x00);
                    FindDeviceCmd.Add(0x00);

                    FindDeviceCmd.Add(0x00);
                    FindDeviceCmd.Add(0x01);

                    crc.Reset();
                    crc.Append(FindDeviceCmd.ToArray());
                    byte[] crcValue = crc.GetCurrentHash();

                    FindDeviceCmd.Add(crcValue[0]);
                    FindDeviceCmd.Add(crcValue[1]);


                    Trace.WriteLine("----------------------------------------------------------------------------");
                    string FindDeviceCmdString = "";
                    foreach (byte b in FindDeviceCmd)
                    {
                        FindDeviceCmdString += b.ToString("X2");
                        FindDeviceCmdString += " ";
                    }
                    Trace.WriteLine("尝试发送指令:" + FindDeviceCmdString);
                    CommPort.Write(FindDeviceCmd.ToArray(),0,FindDeviceCmd.Count);

                    waitPortRecvSignal.Reset();
                    RecvPortData.Clear();
                    currentDeviceAddr = "查询中...";
                    await Task.Run(() =>
                    {
                        waitPortRecvSignal.Wait(500);

                    });
                    string RecvDataStr = "";
                    RecvPortData.ForEach(b =>
                    {
                        RecvDataStr += b.ToString("X2");
                        RecvDataStr += " ";
                    });
                    Trace.WriteLine("接收到:" + RecvDataStr);
                    if (RecvPortData.Count >= 7)
                    {
                        byte recvAddr = RecvPortData[0];
                        byte recvFunc = RecvPortData[1];
                        byte recvData = RecvPortData[4];
                        if (recvAddr == (byte)i && recvFunc == 0x03 && recvAddr == recvData)
                        {
                            currentDeviceAddr = i.ToString();
                            Trace.WriteLine("找到设备，地址:" + currentDeviceAddr);
                            btn_FindDevice.IsEnabled = true;
                            return;
                        }
                    }
                }
                currentDeviceAddr = "N/D";
                btn_FindDevice.IsEnabled = true;
            }

        }
    }
}