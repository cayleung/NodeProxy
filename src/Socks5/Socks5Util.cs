using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using NodeProxy.Util;

namespace NodeProxy.Socks5
{
    public sealed class Socks5Util
    {
        /// <summary>
        /// 读取地址
        /// </summary>
        /// <param name="s"></param>
        /// <param name="atyp"></param>
        /// <param name="address"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public static void ReadAddress(Stream s, out Socks5ATYP atyp, out IPAddress address, out string host, out int port)
        {
            atyp = (Socks5ATYP)ByteUtil.ReadUInt8(s);
            if (atyp == Socks5ATYP.IPV4)
            {
                address = new IPAddress(ByteUtil.ReadNBytes(s, 4));
                host = address.ToString();
                port = ByteUtil.ReadUInt16(s);
            }
            else if (atyp == Socks5ATYP.IPV6)
            {
                address = new IPAddress(ByteUtil.ReadNBytes(s, 6));
                host = address.ToString();
                port = ByteUtil.ReadUInt16(s);
            }
            else if (atyp == Socks5ATYP.DomainName)
            {
                host = ByteUtil.Read1BString(s);
                if (!IPAddress.TryParse(host, out address)){
                    IPHostEntry hostInfo = Dns.GetHostEntry(host);
                    address = hostInfo.AddressList[0];
                }
                port = ByteUtil.ReadUInt16(s);
            }
            else
            {
                atyp = Socks5ATYP.IPV4;
                address = IPAddress.Any;
                host = "0.0.0.0";
                port = 0;
            }
        }

        // 写入地址
        public static void WriteAddress(Stream s, IPEndPoint ipep){
            ByteUtil.WriteUInt8(s, (int)(ipep.AddressFamily == AddressFamily.InterNetwork?Socks5ATYP.IPV4:Socks5ATYP.IPV6));
            ByteUtil.WriteBytes(s, ipep.Address.GetAddressBytes());
            ByteUtil.WriteUInt16(s, ipep.Port);
        }

        public static void Mask(int offset, byte[] bytes, int length)
        {
            int newByte = 0;
            for(int i=0; i < length; i++)
            {
                newByte = bytes[i] + offset;
                if (newByte > 255) newByte -= 256;
                else if (newByte < 0) newByte += 256;
                bytes[i] = (byte)newByte;
            }
        }
    }
}
