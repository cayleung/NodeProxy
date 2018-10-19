using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NodeProxy.Util
{
    public sealed class ByteUtil
    {
        public const int StringMaxLen = 4096;
        public const int BytesMaxLen = 52400; // 50KB
        public const int LongBytesMaxLen = 1048576; // 1MB
        private static byte[] m_byte72 = new byte[9];
        private static byte[] m_byte64 = new byte[8];
        private static byte[] m_byte32 = new byte[4];
        private static byte[] m_byte16 = new byte[2];
        private static byte[] m_bytes = new byte[StringMaxLen];
        public static int ReadUInt8(Stream s)
        {
            return s.ReadByte();
        }

        public static int ReadInt8(Stream s)
        {
            return (sbyte)s.ReadByte();
        }

        public static int ReadUInt16(Stream s)
        {
            return (s.ReadByte() << 8) + s.ReadByte();
        }

        /// <summary>
        /// 读取1个字节长度的字符串
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Read1BString(Stream s)
        {
            var len = ReadUInt8(s);
            if (len == 0) return "";
            s.Read(m_bytes, 0, len);
            return Encoding.UTF8.GetString(m_bytes, 0, len);
        }

        /// <summary>
        /// 读取1个字节长度的整型8
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int[] Read1BUInt8s(Stream s)
        {
            var len = ReadUInt8(s);
            var ints = new int[len];
            for (int i = 0; i < len; i++)
                ints[i] = ReadUInt8(s);
            return ints;
        }

        public static byte[] ReadNBytes(Stream s, int i)
        {
            var bytes = new byte[i];
            s.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// 读取所有剩余字节
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ReadRestBytes(Stream s)
        {
            var bytes = new byte[s.Length - s.Position];
            s.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public static void WriteByte(Stream s, byte b){
            s.WriteByte(b);
        }

        public static void WriteUInt8(Stream s, int i)
        {
            s.WriteByte((byte)i);
        }

        public static void WriteInt8(Stream s, int i)
        {
            s.WriteByte((byte)((sbyte)i));
        }

        public static void WriteUInt16(Stream s, int i)
        {
            m_byte16 = BitConverter.GetBytes((ushort)i);
            s.Write(m_byte16, 0, m_byte16.Length);
        }

        public static void WriteBytes(Stream s, byte[] bytes)
        {
            s.Write(bytes, 0, bytes.Length);
        }

        public static void WriteBytes(Stream s, byte[] bytes, int offset, int length)
        {
            s.Write(bytes, offset, length);
        }

        /// <summary>
        /// 刷新流里面的数据（并复位位置到0
        /// </summary>
        /// <param name="s"></param>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public static void FlushBytes(Stream s, byte[] bytes,int offset, int size)
        {
            s.SetLength(size);
            s.Position = 0;
            s.Write(bytes, offset, size);
            s.Position = 0;
        }

        public static void FlushBytes(Stream s, byte[] bytes)
        {
            s.SetLength(bytes.Length);
            s.Position = 0;
            s.Write(bytes, 0, bytes.Length);
            s.Position = 0;
        }

        public static void FlushBytes(Stream s)
        {
            s.SetLength(0);
            s.Position = 0;
        }
    }
}
