using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Soway传感器配置工具
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal List<string> SysPortList = [];
        internal List<string> BaudList = ["2400","4800","9600", "19200", "38400", "57600", "115200"];
        internal SerialPort? CommPort = null;

        internal string MBData_Head = ":";
        internal string[] MBData_Addrs = ["41", "42", "43", "44"];
        internal string[] MBData_Functions = ["03", "10", "05", "0F", "04", "2B"];

        internal string currentDeviceAddr = "N/D";

        internal List<ushort> DeviceADValue = new List<ushort>();

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
                if (textBox == null) {
                    MessageBox.Show("控件cc_Deviceaddr为Null","未知错误",MessageBoxButton.OK,MessageBoxImage.Error);
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
            if (CommPort == null) {
                MessageBox.Show("请先打开端口");
                return;
            }
            if (CommPort?.IsOpen == true) 
            {
                btn_FindDevice.IsEnabled = false;

                for (int i = 0; i < MBData_Addrs.Length; i++)
                {
                    string FindDeviceCmd = MBData_Addrs[i];
                    FindDeviceCmd += MBData_Functions[0];
                    FindDeviceCmd += MBData_Addrs[i];
                    FindDeviceCmd += "30";
                    FindDeviceCmd += "0001";
                    FindDeviceCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(FindDeviceCmd);
                    Trace.WriteLine("----------------------------------------------------------------------------");
                    Trace.WriteLine("尝试发送指令:" + FindDeviceCmd);
                    CommPort.Write(FindDeviceCmd);
                    waitPortRecvSignal.Reset();
                    RecvPortData.Clear();
                    currentDeviceAddr = "查询中...";
                    await Task.Run(() => 
                    {
                        waitPortRecvSignal.Wait(1000);

                    });
                    string RecvDataStr = Encoding.ASCII.GetString(RecvPortData.ToArray());
                    Trace.WriteLine("接收到:" + RecvDataStr);

                    if (RecvDataStr.StartsWith(MBData_Head) && RecvDataStr.Length > 6)
                    {
                        string RecvAddr = RecvDataStr.Substring(1, 2);
                        string RecvFunction = RecvDataStr.Substring(3, 2);
                        string RecvDataLen = RecvDataStr.Substring(5, 2);
                        string RecvDataContent = RecvDataStr.Substring(7, int.Parse(RecvDataLen) * 2);
                        Trace.WriteLine($"接收到设备响应:{RecvAddr},功能码:{RecvFunction},数据长度:{RecvDataLen},数据内容:{RecvDataContent}");
                        if (RecvAddr == FindDeviceCmd.Substring(1, 2))
                        {
                            btn_FindDevice.IsEnabled = true;
                            currentDeviceAddr = RecvAddr;
                            return;
                        }
                    }

                }
                btn_FindDevice.IsEnabled = true;
            }

        }
    }
}