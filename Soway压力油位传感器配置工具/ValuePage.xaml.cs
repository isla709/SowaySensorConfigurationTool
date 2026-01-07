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
    /// ValuePage.xaml 的交互逻辑
    /// </summary>
    public partial class ValuePage : Page
    {
        MainWindow? mainWindow = null;
        string LogString = "";
        Task GetValueTask;

        public ValuePage()
        {
            InitializeComponent();

            mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
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
                while (true)
                {
                    await Task.Delay(200);
                    await Dispatcher.BeginInvoke(() =>
                    {
                        logdisplay.Text = LogString;
                        if(LogString.Length > 10000)
                        {
                            LogString = "";
                        }
                        
                    });
                }

            });
        }

        private bool GetValueTaskRunning = false;
        private async void GetValueTaskFunc()
        {
            string DeviceCmd = mainWindow.currentDeviceAddr;
            DeviceCmd += mainWindow.MBData_Functions[4];
            DeviceCmd += mainWindow.currentDeviceAddr;
            DeviceCmd += "00";
            DeviceCmd += "0002";
            string CompleteCmd = ModbusAsciiLrcHelper.GenerateCompleteAsciiCmdWithLrc(DeviceCmd);
            while (GetValueTaskRunning)
            {

                try
                {
                    await Dispatcher.BeginInvoke(() =>
                    {
                        AddLog("----------------------------------------------------------------------------");
                        AddLog("发送指令:" + CompleteCmd);
                    });

                    mainWindow.CommPort!.Write(CompleteCmd);
                    mainWindow.waitPortRecvSignal.Reset();
                    mainWindow.RecvPortData.Clear();
                    await Task.Run(() =>
                    {
                        mainWindow.waitPortRecvSignal.Wait(1000);

                    });
                    string RecvDataStr = Encoding.ASCII.GetString(mainWindow.RecvPortData.ToArray());
                    AddLog("接收到:" + RecvDataStr);
                    if (RecvDataStr.Length > 15)
                    {
                        RecvDataStr = RecvDataStr.Trim('\r', '\n');
                        string Value_HEX = RecvDataStr.Substring(11, 4);
                        ushort Value_U16 = Convert.ToUInt16(Value_HEX, 16);

                        mainWindow.CurrentDeviceADValue = Value_U16;
                        mainWindow.DeviceADValue.Add(Value_U16);

                        await Dispatcher.BeginInvoke(() =>
                        {
                            ValueList.Items.Add(Value_U16);
                            tb_ADValueHex.Text = Value_HEX;
                            tb_ADValue.Text = Value_U16.ToString();
                        });

                    }

                }
                catch(Exception ex)
                {
                    AddLog(ex.Message);
                }
                await Task.Delay(500);
            }

        }

        private async void btn_StartGetValue_Click(object sender, RoutedEventArgs e)
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
                if(btn_StartGetValue.Content.ToString() == "启动采集")
                {
                    btn_StartGetValue.Content = "停止采集";
                    GetValueTaskRunning = true;
                    GetValueTask = new Task(GetValueTaskFunc);
                    GetValueTask.Start();
                }
                else
                {
                    btn_StartGetValue.Content = "启动采集";
                    GetValueTaskRunning = false;
                    GetValueTask.Dispose();

                }
                


            }
        }
    }
}
