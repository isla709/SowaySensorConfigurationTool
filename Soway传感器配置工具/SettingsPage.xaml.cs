using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        MainWindow? mainWindow = null;
        string LogString = "";

        public SettingsPage()
        {
            InitializeComponent();

            mainWindow = Application.Current.MainWindow as MainWindow;
            if(mainWindow == null ) {
                throw new Exception("Get MainWindow Error");
            }
        }

        private void AddLog(string msg)
        {
            LogString += msg + "\n";
            Trace.WriteLine(msg);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                while (true) {
                    await Task.Delay(200);
                    await Dispatcher.BeginInvoke(() =>
                    {
                        logdisplay.Text = LogString;
                    });
                }
                
            });
        }

        private async void SetAddressButton_Click(object sender, RoutedEventArgs e)
        {
      
            if(mainWindow == null) 
            {
                return;
            }
            if(mainWindow.CommPort == null)
            {
                return;
            }
            if(mainWindow.currentDeviceAddr == "请重新搜索" || mainWindow.currentDeviceAddr == "N/D")
            {
                MessageBox.Show("请先搜索设备获取当前地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindow.CommPort!.IsOpen == true)
            {
                string TargetAddr = (AddressComboBox.SelectedValue as ComboBoxItem)!.Content.ToString()!;
                string DeviceCmd = mainWindow.currentDeviceAddr;
                DeviceCmd += mainWindow.MBData_Functions[1];
                DeviceCmd += mainWindow.currentDeviceAddr;
                DeviceCmd += "30";
                DeviceCmd += "0001";
                DeviceCmd += "02";
                DeviceCmd += "00";
                DeviceCmd += TargetAddr;
                string CompleteCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(DeviceCmd);
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送指令:" + CompleteCmd);
                mainWindow.CommPort.Write(CompleteCmd);
                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });
                string RecvDataStr = Encoding.ASCII.GetString(mainWindow.RecvPortData.ToArray());
                AddLog("接收到:" + RecvDataStr);
                if(RecvDataStr.Length >= 13)
                {
                    string RecvLrcStr = RecvDataStr.Substring(RecvDataStr.Length - 4, 2);
                    string CalcLrcStr = ModbusAsciiLrcHelper.CalculateLrcToString(ModbusAsciiLrcHelper.StringToHexBytes(RecvDataStr.Substring(1, RecvDataStr.Length - 5)));
                    if (RecvLrcStr.Equals(CalcLrcStr, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        MessageBox.Show("设置成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        mainWindow.currentDeviceAddr = TargetAddr;
                    }
                    else
                    {
                        MessageBox.Show("设置失败，校验错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        AddLog("接收数据校验错误!");
                        mainWindow.currentDeviceAddr = "请重新搜索";
                    }
                }
                else
                {
                    MessageBox.Show("设置失败，未收到完整数据！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddLog("接收数据不完整!");
                    mainWindow.currentDeviceAddr = "请重新搜索";
                }

            }

        }

        private async void SetBaudButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }
            if (mainWindow.CommPort == null)
            {
                return;
            }
            if (mainWindow.currentDeviceAddr == "请重新搜索" || mainWindow.currentDeviceAddr == "N/D")
            {
                MessageBox.Show("请先搜索设备获取当前地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindow.CommPort!.IsOpen == true)
            {
                string Targetbaud = (BaudRateCombobox.SelectedValue as ComboBoxItem)!.Tag.ToString()!;
                string DeviceCmd = mainWindow.currentDeviceAddr;
                DeviceCmd += mainWindow.MBData_Functions[1];
                DeviceCmd += mainWindow.currentDeviceAddr;
                DeviceCmd += "31";
                DeviceCmd += "0001";
                DeviceCmd += "02";
                DeviceCmd += "00";
                DeviceCmd += Targetbaud;
                string CompleteCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(DeviceCmd);
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送指令:" + CompleteCmd);
                mainWindow.CommPort.Write(CompleteCmd);
                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });
                string RecvDataStr = Encoding.ASCII.GetString(mainWindow.RecvPortData.ToArray());
                AddLog("接收到:" + RecvDataStr);

            }


        }

        private async void SetFilteringButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null)
            {
                return;
            }
            if (mainWindow.CommPort == null)
            {
                return;
            }
            if (mainWindow.currentDeviceAddr == "请重新搜索" || mainWindow.currentDeviceAddr == "N/D")
            {
                MessageBox.Show("请先搜索设备获取当前地址！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (mainWindow.CommPort!.IsOpen == true)
            {
                string TargetFiltering = (FilteringCombobox.SelectedValue as ComboBoxItem)!.Tag.ToString()!;
                string DeviceCmd = mainWindow.currentDeviceAddr;
                DeviceCmd += mainWindow.MBData_Functions[1];
                DeviceCmd += mainWindow.currentDeviceAddr;
                DeviceCmd += "35";
                DeviceCmd += "0001";
                DeviceCmd += "02";
                DeviceCmd += "00";
                DeviceCmd += TargetFiltering;
                string CompleteCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(DeviceCmd);
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送指令:" + CompleteCmd);
                mainWindow.CommPort.Write(CompleteCmd);
                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });
                string RecvDataStr = Encoding.ASCII.GetString(mainWindow.RecvPortData.ToArray());
                AddLog("接收到:" + RecvDataStr);
                if(RecvDataStr.StartsWith(':')) 
                {
                    string RecvAddr = RecvDataStr.Substring(1, 2);
                    string RecvFunction = RecvDataStr.Substring(3, 2);
                    string RecvMsgID = RecvDataStr.Substring(7, 2);
                    Trace.WriteLine($"接收到设备响应:{RecvAddr},功能码:{RecvFunction},消息ID:{RecvMsgID}");
                    if(RecvAddr == mainWindow.currentDeviceAddr && RecvFunction == "10" && RecvMsgID == "35") 
                    {
                        MessageBox.Show("设置成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("设置失败，设备响应错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }


                }
            }
        }
    }
}
