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

namespace Soway传感器配置工具
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> SysPortList = [];
        List<string> BaudList = ["4800","9600","38400","115200"];
        SerialPort? CommPort = null;

        string MBData_Head = ":";
        string[] MBData_Addrs = ["41", "42", "43", "44"];
        string[] MBData_Functions = ["03", "10", "05", "0F", "04", "2B"];

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

            if (btn_PortSwitch.Tag as bool? == false)
            {
                btn_PortSwitch.Tag = true;

                CommPort = new SerialPort();
                CommPort.PortName = cb_port!.SelectedValue.ToString();
                CommPort.BaudRate = int.Parse(cb_baud.SelectedValue.ToString()!);
                CommPort.Open();
            }
            else
            {
                btn_PortSwitch.Tag = false;
                if(CommPort!.IsOpen)
                {
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

        private void btn_FindDevice_Click(object sender, RoutedEventArgs e)
        {
            if (CommPort == null) {
                MessageBox.Show("请先打开端口");
                return;
            }
            if (CommPort?.IsOpen == true) 
            {
                string FindDeviceCmd = MBData_Addrs[0];
                FindDeviceCmd += MBData_Functions[0];
                FindDeviceCmd += MBData_Addrs[0];
                FindDeviceCmd += "30";
                FindDeviceCmd += "0001";
                FindDeviceCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(FindDeviceCmd);
                Trace.WriteLine(FindDeviceCmd);
               


            }

        }
    }
}