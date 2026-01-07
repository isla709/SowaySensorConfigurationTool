using Soway传感器配置工具;
using System;
using System.Collections.Generic;
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

namespace Soway压力油位传感器配置工具
{
    /// <summary>
    /// CalibrationPage.xaml 的交互逻辑
    /// </summary>
    public partial class CalibrationPage : Page
    {
        MainWindow? mainWindow = null;

        List<KeyValuePair<string, string>> CalibrationData = new List<KeyValuePair<string, string>>();



        public CalibrationPage()
        {
            InitializeComponent();

             mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                throw new Exception("Get MainWindow Error");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => {
                if (mainWindow == null)
                {
                    return;
                }

                while (true)
                {

                    Dispatcher.Invoke(() => 
                    {
                        tb_DeviceValue.Text = mainWindow.CurrentDeviceADValue.ToString();
                    });

                    await Task.Delay(500);
                }
            } );
        }

        private void btn_SaveData_Click(object sender, RoutedEventArgs e)
        {
            CalibrationData.Add(new KeyValuePair<string,string>(tb_inputData.Text, tb_DeviceValue.Text));
            lv_CalibrationData.ItemsSource = null;
            lv_CalibrationData.ItemsSource = CalibrationData;
            lv_CalibrationData.Items.Refresh();

        }

        private void btn_ClearList_Click(object sender, RoutedEventArgs e)
        {
            CalibrationData.Clear();
            lv_CalibrationData.ItemsSource = null;
            lv_CalibrationData.ItemsSource = CalibrationData;
            lv_CalibrationData.Items.Refresh();
        }

        private void btn_outList_Click(object sender, RoutedEventArgs e)
        {
            CsvTools.ExportToCsv("容积值,传感器读数",CalibrationData);
        }
    }
}
