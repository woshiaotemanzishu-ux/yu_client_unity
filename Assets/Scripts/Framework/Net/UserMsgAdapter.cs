using System;
using System.Collections.Generic;
using System.Text;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 发包编码器,逐字节对标 yu_client UserMsgAdapter(BIG_ENDIAN 二进制):
    ///
    /// 帧结构(见 yu_client WriteBegin/SendToGame):
    ///   [i16 总长(含自身)] [i16 1000] [i16 协议号] [按格式串写的字段...]
    ///   (Laya 先 writeUint32(1000) 再回到 pos=0 写 i16 长度,前 4 字节净效果
    ///    = [i16 len][i16 1000])
    ///
    /// 格式字符:c=u8 C=i8 h=u16 H=i16 i=u32 I=i32 l=u64(高32+低32) L=i64
    ///          s=u16字节长+UTF8。null 值:s 写空串,其余写 0(同 Laya writeStream)。
    /// </summary>
    public static class UserMsgAdapter
    {
        public static byte[] Encode(int protoId, string format, params object[] args)
        {
            var buf = new List<byte>(64);
            WriteU16(buf, 0);              // 总长占位
            WriteU16(buf, 1000);
            WriteU16(buf, (ushort)protoId);

            if (!string.IsNullOrEmpty(format))
            {
                for (int i = 0; i < format.Length; i++)
                {
                    object v = (args != null && i < args.Length) ? args[i] : null;
                    WriteOne(buf, format[i], v);
                }
            }

            int total = buf.Count;
            buf[0] = (byte)(total >> 8);
            buf[1] = (byte)total;
            return buf.ToArray();
        }

        private static void WriteOne(List<byte> buf, char fmt, object value)
        {
            switch (fmt)
            {
                case 'c': buf.Add((byte)ToLong(value)); break;
                case 'C': buf.Add(unchecked((byte)(sbyte)ToLong(value))); break;
                case 'h': WriteU16(buf, (ushort)ToLong(value)); break;
                case 'H': WriteU16(buf, unchecked((ushort)(short)ToLong(value))); break;
                case 'i': WriteU32(buf, (uint)ToLong(value)); break;
                case 'I': WriteU32(buf, unchecked((uint)(int)ToLong(value))); break;
                case 'l':
                case 'L':
                {
                    long v = ToLong(value);
                    WriteU32(buf, (uint)(v >> 32));
                    WriteU32(buf, (uint)v);
                    break;
                }
                case 's':
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty);
                    WriteU16(buf, (ushort)bytes.Length);
                    buf.AddRange(bytes);
                    break;
                }
                default:
                    throw new ArgumentException("未知格式字符: " + fmt);
            }
        }

        private static long ToLong(object value)
        {
            if (value == null) return 0;
            return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void WriteU16(List<byte> buf, ushort v)
        {
            buf.Add((byte)(v >> 8));
            buf.Add((byte)v);
        }

        private static void WriteU32(List<byte> buf, uint v)
        {
            buf.Add((byte)(v >> 24));
            buf.Add((byte)(v >> 16));
            buf.Add((byte)(v >> 8));
            buf.Add((byte)v);
        }
    }
}
