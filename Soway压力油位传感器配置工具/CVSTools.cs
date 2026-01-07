using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace Soway压力油位传感器配置工具
{
    public class CsvTools
    {
        /// <summary>
        /// 导出为CSV文件
        /// </summary>
        public static bool ExportToCsv(string TableHead,List<KeyValuePair<string, string>> data, string filePath = null)
        {
            try
            {
                // 如果没有提供路径，弹出保存对话框
                if (string.IsNullOrEmpty(filePath))
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                        DefaultExt = ".csv",
                        FileName = $"校准数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    };

                    if (saveDialog.ShowDialog() != true)
                        return false;

                    filePath = saveDialog.FileName;
                }

                // 构建CSV内容
                var csvContent = new StringBuilder();

                // 添加表头
                csvContent.AppendLine(TableHead);

                // 添加数据行
                foreach (var item in data)
                {
                    // 处理特殊字符：包含逗号、引号、换行符时需要用引号包裹
                    string key = FormatCsvField(item.Key);
                    string value = FormatCsvField(item.Value);

                    csvContent.AppendLine($"{key},{value}");
                }

                // 写入文件（使用UTF-8编码，带BOM以兼容Excel）
                File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出CSV失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 格式化CSV字段（处理特殊字符）
        /// </summary>
        private static string FormatCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // 如果字段包含逗号、引号、换行符，需要用双引号包裹
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                // 转义内部的双引号（替换为两个双引号）
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        /// <summary>
        /// 带进度显示的批量导出
        /// </summary>
        public static bool ExportToCsvWithProgress(List<KeyValuePair<string, string>> data,
            IProgress<int> progress = null)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"校准数据_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() != true)
                    return false;

                using (var writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    // 写入表头
                    writer.WriteLine("参数名称,参数值");

                    int total = data.Count;
                    for (int i = 0; i < total; i++)
                    {
                        var item = data[i];
                        string key = FormatCsvField(item.Key);
                        string value = FormatCsvField(item.Value);

                        writer.WriteLine($"{key},{value}");

                        // 报告进度
                        progress?.Report((i + 1) * 100 / total);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
