using System;
using System.Text;

namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// 收包载荷的顺序读取器(BIG_ENDIAN),格式字符与 yu_client UserMsgAdapter 完全一致:
    ///   c=u8  C=i8  h=u16  H=i16  i=u32  I=i32  l=u64(高32+低32)  L=i64  s=u16长度+UTF8
    /// 协议处理器拿到它之后按 yu_client 同样的 ReadFmt 顺序读即可。
    /// </summary>
    public sealed class NetReader
    {
        private readonly byte[] _buf;
        private int _pos;
        private readonly int _end;

        public NetReader(byte[] buf, int offset, int count)
        {
            _buf = buf;
            _pos = offset;
            _end = offset + count;
        }

        public int Remaining => _end - _pos;

        public byte ReadU8() { Require(1); return _buf[_pos++]; }
        public sbyte ReadI8() { return (sbyte)ReadU8(); }

        public ushort ReadU16()
        {
            Require(2);
            ushort v = (ushort)((_buf[_pos] << 8) | _buf[_pos + 1]);
            _pos += 2;
            return v;
        }

        public short ReadI16() { return (short)ReadU16(); }

        public uint ReadU32()
        {
            Require(4);
            uint v = ((uint)_buf[_pos] << 24) | ((uint)_buf[_pos + 1] << 16) | ((uint)_buf[_pos + 2] << 8) | _buf[_pos + 3];
            _pos += 4;
            return v;
        }

        public int ReadI32() { return (int)ReadU32(); }

        /// <summary>'l':两个 u32 拼 64 位(与 Laya hi*2^32+lo 一致)。</summary>
        public long ReadU64()
        {
            long hi = ReadU32();
            long lo = ReadU32();
            return (hi << 32) | lo;
        }

        /// <summary>'L':高位按 i32 解释。</summary>
        public long ReadI64()
        {
            long hi = ReadI32();
            long lo = ReadU32();
            return (hi << 32) | lo;
        }

        public string ReadString()
        {
            int len = ReadU16();
            Require(len);
            string s = Encoding.UTF8.GetString(_buf, _pos, len);
            _pos += len;
            return s;
        }

        /// <summary>按格式串顺序读取,对标 Laya 的 ReadFmt("clihi")。</summary>
        public object[] ReadFmt(string format)
        {
            if (string.IsNullOrEmpty(format)) return Array.Empty<object>();
            object[] result = new object[format.Length];
            for (int i = 0; i < format.Length; i++)
            {
                result[i] = ReadOne(format[i]);
            }
            return result;
        }

        private object ReadOne(char fmt)
        {
            switch (fmt)
            {
                case 'c': return ReadU8();
                case 'C': return ReadI8();
                case 'h': return ReadU16();
                case 'H': return ReadI16();
                case 'i': return ReadU32();
                case 'I': return ReadI32();
                case 'l': return ReadU64();
                case 'L': return ReadI64();
                case 's': return ReadString();
                default: throw new ArgumentException("未知格式字符: " + fmt);
            }
        }

        private void Require(int n)
        {
            if (_pos + n > _end)
            {
                throw new InvalidOperationException($"NetReader 越界: 需要 {n} 字节,剩余 {Remaining}");
            }
        }
    }
}
