using Medo.IO.Hashing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
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
    /// ValuePage.xaml 的交互逻辑
    /// </summary>
    public partial class ValuePage : Page
    {
        MainWindow? mainWindow = null;
        string LogString = "";
        Task GetValueTask;
        byte DeviceUnit = 0;
        byte DeviceDecimalplaces = 0;
        UInt16 rawValue = 0;
        string rawValueHex = "";

        string[] UnitStrs = new string[]
        {
            "Mpa(℃)",
            "Kpa",
            "Pa",
            "Bar",
            "mbar",
            "kg/cm^2",
            "Psi",
            "mh2o",
            "mmh2o"
        };

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

            Crc16 crc = Crc16.GetModbus();

            List<byte> DeviceCmd = new List<byte>();

            DeviceCmd.Add(byte.Parse(mainWindow.currentDeviceAddr));

            DeviceCmd.Add(0x03);

            DeviceCmd.Add(0x00);
            DeviceCmd.Add(0x04);

            DeviceCmd.Add(0x00);
            DeviceCmd.Add(0x01);

            crc.Reset();
            crc.Append(DeviceCmd.ToArray());
            byte[] crcValue = crc.GetCurrentHash();

            DeviceCmd.Add(crcValue[0]);
            DeviceCmd.Add(crcValue[1]);

            string DeviceCmdString = "";
            foreach (byte b in DeviceCmd)
            {
                DeviceCmdString += b.ToString("X2");
                DeviceCmdString += " ";
            }
            while (GetValueTaskRunning)
            {

                try
                {
                    await Dispatcher.BeginInvoke(() =>
                    {
                        AddLog("----------------------------------------------------------------------------");
                        AddLog("发送指令:" + DeviceCmdString);
                    });

                    mainWindow.CommPort.Write(DeviceCmd.ToArray(), 0, DeviceCmd.Count);
                    mainWindow.waitPortRecvSignal.Reset();
                    mainWindow.RecvPortData.Clear();
                    await Task.Run(() =>
                    {
                        mainWindow.waitPortRecvSignal.Wait(1000);

                    });
                    string RecvDataStr = "";
                    mainWindow.RecvPortData.ForEach(b =>
                    {
                        RecvDataStr += b.ToString("X2");
                        RecvDataStr += " ";
                    });
                    AddLog("接收到:" + RecvDataStr);
                    if(mainWindow.RecvPortData.Count >= 7 && mainWindow.RecvPortData[0] == byte.Parse(mainWindow.currentDeviceAddr) && mainWindow.RecvPortData[1] == 0x03)
                    {
                        rawValueHex = mainWindow.RecvPortData[3].ToString("X2") + mainWindow.RecvPortData[4].ToString("X2"); 

                        UInt16 rawValue = (UInt16)(mainWindow.RecvPortData[3] << 8 | mainWindow.RecvPortData[4]);

                        float displayValue = rawValue;

                        

                        await Dispatcher.BeginInvoke(() =>
                        {
                            tb_ADValueHex.Text = rawValueHex;
    
                            for (int i = 0; i < DeviceDecimalplaces; i++)
                            {
                                displayValue = displayValue / 10.0f;
                            }
                            tb_ADValue.Text = displayValue.ToString("F3") + " " + UnitStrs[DeviceUnit];
                        });

                        mainWindow.CurrentDeviceADValue = displayValue;

                    }

                }
                catch (Exception ex)
                {
                    AddLog(ex.Message);
                }
                await Task.Delay(400);
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

            #region 查询单位类型
            try
            {
                Crc16 crc = Crc16.GetModbus();

                List<byte> unitDeviceCmd = new List<byte>();

                unitDeviceCmd.Add(byte.Parse(mainWindow.currentDeviceAddr));

                unitDeviceCmd.Add(0x03);

                unitDeviceCmd.Add(0x00);
                unitDeviceCmd.Add(0x02);

                unitDeviceCmd.Add(0x00);
                unitDeviceCmd.Add(0x01);

                crc.Reset();
                crc.Append(unitDeviceCmd.ToArray());
                byte[] crcValue = crc.GetCurrentHash();

                unitDeviceCmd.Add(crcValue[0]);
                unitDeviceCmd.Add(crcValue[1]);

                string unitDeviceCmdString = "";
                foreach (byte b in unitDeviceCmd)
                {
                    unitDeviceCmdString += b.ToString("X2");
                    unitDeviceCmdString += " ";
                }
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送单位查询指令:" + unitDeviceCmdString);
                mainWindow.CommPort.Write(unitDeviceCmd.ToArray(), 0, unitDeviceCmd.Count);
                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });

                string RecvDataStr = "";
                mainWindow.RecvPortData.ForEach(b =>
                {
                    RecvDataStr += b.ToString("X2");
                    RecvDataStr += " ";
                });
                AddLog("接收到:" + RecvDataStr);
                if (mainWindow.RecvPortData[0] == byte.Parse(mainWindow.currentDeviceAddr) && mainWindow.RecvPortData[1] == 0x03)
                {
                    DeviceUnit = mainWindow.RecvPortData[4];
                    AddLog("设备单位类型:" + DeviceUnit.ToString());
                }
                else
                {
                    AddLog("设备响应错误！");
                }
            }
            catch (Exception ex) 
            {
                AddLog(ex.Message);
                return;
            }
            #endregion

            #region 查询小数位数
            try
            {
                Crc16 crc = Crc16.GetModbus();

                List<byte> unitDeviceCmd = new List<byte>();

                unitDeviceCmd.Add(byte.Parse(mainWindow.currentDeviceAddr));

                unitDeviceCmd.Add(0x03);

                unitDeviceCmd.Add(0x00);
                unitDeviceCmd.Add(0x03);

                unitDeviceCmd.Add(0x00);
                unitDeviceCmd.Add(0x01);

                crc.Reset();
                crc.Append(unitDeviceCmd.ToArray());
                byte[] crcValue = crc.GetCurrentHash();

                unitDeviceCmd.Add(crcValue[0]);
                unitDeviceCmd.Add(crcValue[1]);

                string unitDeviceCmdString = "";
                foreach (byte b in unitDeviceCmd)
                {
                    unitDeviceCmdString += b.ToString("X2");
                    unitDeviceCmdString += " ";
                }
                AddLog("----------------------------------------------------------------------------");
                AddLog("发送单位查询指令:" + unitDeviceCmdString);
                mainWindow.CommPort.Write(unitDeviceCmd.ToArray(), 0, unitDeviceCmd.Count);
                mainWindow.waitPortRecvSignal.Reset();
                mainWindow.RecvPortData.Clear();
                await Task.Run(() =>
                {
                    mainWindow.waitPortRecvSignal.Wait(5000);

                });

                string RecvDataStr = "";
                mainWindow.RecvPortData.ForEach(b =>
                {
                    RecvDataStr += b.ToString("X2");
                    RecvDataStr += " ";
                });
                AddLog("接收到:" + RecvDataStr);
                if (mainWindow.RecvPortData[0] == byte.Parse(mainWindow.currentDeviceAddr) && mainWindow.RecvPortData[1] == 0x03)
                {
                    DeviceDecimalplaces = mainWindow.RecvPortData[4];
                    AddLog("设备单位类型:" + DeviceDecimalplaces.ToString());
                }
                else
                {
                    AddLog("设备响应错误！");
                }

            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
                return;
            }
            #endregion



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
