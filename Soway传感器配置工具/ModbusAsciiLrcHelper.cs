using System;
using System.Collections.Generic;
using System.Text;

namespace Soway传感器配置工具
{
    public static class ModbusAsciiLrcHelper
    {
        /// <summary>
        /// 核心方法：计算字节数组的LRC校验码（返回单字节，最通用）
        /// </summary>
        /// <param name="dataBytes">待校验的字节数组（地址码→数据段）</param>
        /// <returns>1字节LRC校验码（0x00 ~ 0xFF）</returns>
        public static byte CalculateLrc(byte[] dataBytes)
        {
            // 空值校验，避免异常
            if (dataBytes == null || dataBytes.Length == 0)
                return 0x00;

            // 步骤1：逐字节累加求和（byte为8位，自动溢出截断，符合Modbus规范）
            byte sum = 0;
            foreach (byte b in dataBytes)
            {
                sum += b;
            }

            // 步骤2：按字节取反 → 步骤3：加1  得到最终LRC校验码
            byte lrc = (byte)(~sum + 1);
            return lrc;
        }

        /// <summary>
        /// 重载1：计算字节数组的LRC校验码 → 返回【两位十六进制字符串】（直接拼接指令用）
        /// </summary>
        /// <param name="dataBytes">待校验字节数组</param>
        /// <param name="upperCase">是否大写，默认true（Modbus推荐大写）</param>
        /// <returns>两位十六进制字符串（如：05、F8）</returns>
        public static string CalculateLrcToString(byte[] dataBytes, bool upperCase = true)
        {
            byte lrc = CalculateLrc(dataBytes);
            return upperCase ? lrc.ToString("X2") : lrc.ToString("x2");
        }

        /// <summary>
        /// 重载2：传入Modbus-ASCII指令段（不含:和CRLF）→ 生成【带LRC的完整指令帧】
        /// </summary>
        /// <param name="asciiCmdSection">指令段（如：010300000001，地址+功能+数据）</param>
        /// <returns>完整Modbus-ASCII帧（带:、LRC、CRLF，可直接下发）</returns>
        public static string GenerateCompleteAsciiCmdWithLrc(string asciiCmdSection)
        {
            try
            {
                // 1. 指令段转字节数组（十六进制ASCII→字节）
                byte[] cmdBytes = StringToHexBytes(asciiCmdSection);
                // 2. 计算LRC校验码（两位十六进制）
                string lrcStr = CalculateLrcToString(cmdBytes);
                // 3. 拼接完整帧：: + 指令段 + LRC + CR(0x0D) + LF(0x0A)
                return $":{asciiCmdSection}{lrcStr}\r\n";
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"生成Modbus-ASCII指令失败：{ex.Message}", nameof(asciiCmdSection));
            }
        }

        /// <summary>
        /// 辅助方法：十六进制字符串 → 字节数组（如"01030001" → [0x01,0x03,0x00,0x01]）
        /// </summary>
        public static byte[] StringToHexBytes(string hexStr)
        {
            hexStr = hexStr.Replace(" ", "").Trim();
            if (hexStr.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数");

            return Enumerable.Range(0, hexStr.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexStr.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
