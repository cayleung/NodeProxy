using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using NodeProxy.Util;

namespace NodeProxy.Socks5
{
    /// <summary>
    /// 
    /// </summary>
    public class Socks5Worker
    {
        private Socks5Server server;
        public int id{get{ return GetHashCode();}}
        public string udpID;
        public const int RECV_TIMEOUT = 15000;
        public const int SEND_TIMEOUT = 5000;
        public const int RECV_BUFF_SIZE = 1048576;
        public const int SEND_BUFF_SIZE = 1048576;
        protected volatile TcpClient srcTCP;
        protected volatile TcpClient tarTCP;
        protected volatile Socket tarUDP;
        protected IPEndPoint srcUDPREP;
        protected IPEndPoint tarUDPREP;
        private EndPoint tarUDPREP2;
        protected byte[] srcTCPBody = new byte[10240];
        protected int srcTCPSize;
        protected byte[] tarTCPBody = new byte[10240];
        protected int tarTCPSize;
        protected byte[] tarUDPBody;
        protected int tarUDPSize;
        protected MemoryStream srcRS = new MemoryStream();
        protected MemoryStream srcSS = new MemoryStream();
        protected string tarHost = "";
        protected int tarPort = 0;
        protected IPAddress tarIPA;
        protected string srcHost = "";
        protected int srcPort = 0;
        private bool srcMask = false;
        private bool tarMask = false;
        private int version;
        private int[] methods;

        public Socks5Worker(Socks5Server server, TcpClient client)
        {
            this.server = server;
            srcMask = server.inMask;
            tarMask = server.outMask;
            srcTCP = client;
        }

        private void InitTCPClient(TcpClient tcp)
        {
            tcp.ReceiveTimeout = RECV_TIMEOUT;
            tcp.SendTimeout = SEND_TIMEOUT;
            tcp.ReceiveBufferSize = RECV_BUFF_SIZE;
            tcp.SendBufferSize = SEND_BUFF_SIZE;
            tcp.NoDelay = false;
        }

        public void Start()
        {
            InitTCPClient(srcTCP);
            srcHost = ((IPEndPoint)srcTCP.Client.RemoteEndPoint).Address.ToString();
            srcPort = ((IPEndPoint)srcTCP.Client.RemoteEndPoint).Port;
            if (server.isNode){
                // 节点模式
                Task.Factory.StartNew(ReceiveSorketLink);
            }else{
                Task.Factory.StartNew(ReceiveSorketAuth);
            }
        }

        public void Stop()
        {
            if (srcTCP == null) return;
            if (srcTCP != null) srcTCP.Close();
            if (tarTCP != null) tarTCP.Close();
            if (tarUDP != null) tarUDP.Close();
            if (srcRS != null) srcRS.Close();
            if (srcSS != null) srcSS.Close();
            srcTCP = null;
            tarTCP = null;
            tarUDP = null;
            srcRS = null;
            srcSS = null;
            server.Stop(this);
        }

        private void ReceiveSorketLink()
        {
            try
            {
                // 节点模式
                tarTCP = new TcpClient();
                tarTCP.Connect(server.parentHost, server.parentPort);
                ReceiveSorketSrcTCPLoop();
                ReceiveSorketTarTCPLoop();
                DebugUtil.LogFormat("Link {0} => {1}", srcTCP.Client.RemoteEndPoint, tarTCP.Client.RemoteEndPoint);
            }catch{
                DebugUtil.LogFormat("Failed to connect parent:{0}:{1}", server.parentHost, server.parentPort);
                Stop();
            }
        }

        /// <summary>
        /// 登录流程
        /// </summary>
        /// <param name="o"></param>
        private void ReceiveSorketAuth()
        {
            try
            {
                if (srcTCP == null || !srcTCP.Connected)
                {
                    Stop();
                }
                // 接收登录信息
                SrcRecvData();
                if (srcTCPSize == 0)
                {
                    throw (new Exception("Src Link Shutdown!"));
                }
                ByteUtil.FlushBytes(srcRS, srcTCPBody, 0, srcTCPSize);
                version = ByteUtil.ReadUInt8(srcRS);
                methods = ByteUtil.Read1BUInt8s(srcRS);
                if (version != 5){
                    throw (new Exception("Version no supported!"));
                }
                // 回复登录信息
                if (server.useUPAuth)
                {
                    var methodList = new List<int>(methods);
                    if (methodList.Contains((int)Socks5AuthType.USERNAME_PASSWORD)){
                        SrcSendData(new byte[] { 0x05, (int)Socks5AuthType.USERNAME_PASSWORD });
                        SrcRecvData();
                        if (srcTCPSize == 0)
                        {
                            throw (new Exception("Src Link Shutdown!"));
                        }
                        ByteUtil.FlushBytes(srcRS, srcTCPBody, 0, srcTCPSize);
                        version = ByteUtil.ReadUInt8(srcRS);
                        if (version != 1)
                        {
                            throw (new Exception("Auth Version no supported!"));
                        }
                        var username = ByteUtil.Read1BString(srcRS);
                        var password = ByteUtil.Read1BString(srcRS);
                        // 暂时不校验
                        SrcSendData(new byte[] { 0x01, 0x00});
                        //srcClient.Client.Send(new byte[] { 0x01, 0x01}); 校验失败返回非0
                    }else{
                        SrcSendData(new byte[] { 0x05, (int)Socks5AuthType.NO_ACCEPTABLE_METHODS });
                        throw (new Exception("Auth failed!"));
                    }
                }else{
                    SrcSendData(new byte[] { 0x05, (int)Socks5AuthType.NO_AUTHENTICATION_REQUIRED });
                }
                // 接收代理请求
                SrcRecvData();
                if (srcTCPSize == 0)
                {
                    throw (new Exception("Src Link Shutdown!"));
                }
                ByteUtil.FlushBytes(srcRS, srcTCPBody, 0, srcTCPSize);
                version = ByteUtil.ReadUInt8(srcRS);
                var cmd = (Socks5CMD)ByteUtil.ReadUInt8(srcRS);
                ByteUtil.ReadUInt8(srcRS);
                Socks5ATYP atyp;
                Socks5Util.ReadAddress(srcRS, out atyp, out tarIPA, out tarHost, out tarPort);
                if (version != 5)
                {
                    throw (new Exception("Version no supported!"));
                }
                if (cmd == Socks5CMD.CONNECT)
                {
                    tarTCP = new TcpClient();
                    InitTCPClient(tarTCP);
                    tarTCP.Connect(tarHost, tarPort);
                    if (!tarTCP.Connected)
                    {
                        throw (new Exception("Not connect!"));
                    }
                    // 回复连接结果
                    ByteUtil.FlushBytes(srcSS);
                    // Connect不关心返回ip和端口，这里都发0
                    // ByteUtil.WriteBytes(srcSS, new byte[]{
                    //     0x05, (int)Socks5Reply.Succeeded, 0x00, (int)Socks5ATYP.IPV4,
                    //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    // });
                    ByteUtil.WriteBytes(srcSS, new byte[]{0x05, (int)Socks5Reply.Succeeded, 0x00});
                    Socks5Util.WriteAddress(srcSS, server.ipep);
                    SrcSendData(srcSS.ToArray());
                    DebugUtil.LogFormat("TCP {0} => {1}", srcTCP.Client.RemoteEndPoint, tarTCP.Client.RemoteEndPoint);
                    ReceiveSorketSrcTCPLoop();
                    ReceiveSorketTarTCPLoop();
                }
                else if(cmd == Socks5CMD.BIND){
                    DebugUtil.LogFormat("ReceiveSorketAuth size:{0}  version:{1} cmd:{2} atyp:{3} tarHost:{4} tarPort:{5}", srcTCPSize, version, cmd, atyp, tarHost, tarPort);
                    throw (new Exception("No suppered BIND!"));
                }else if(cmd == Socks5CMD.UDP_ASSOCIATE){
                    // ByteUtil.FlushBytes(srcSS);
                    // ByteUtil.WriteBytes(srcSS, new byte[]{
                    //     0x05, (int)Socks5Reply.CommandNotSupported, 0x00, (int)Socks5ATYP.IPV4,
                    //     0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                    // });
                    // SrcSendData(srcSS.ToArray());
                    // Stop();
                    // 使用UDP 第一次提交的地址和端口是，客户端的接收端口
                    srcUDPREP = new IPEndPoint(((IPEndPoint)srcTCP.Client.RemoteEndPoint).Address, tarPort);
                    udpID = srcUDPREP.ToString();
                    DebugUtil.LogFormat("UDP {0} => {1} {2}", srcUDPREP, server.ipep, srcTCP.Client.RemoteEndPoint);
                    ByteUtil.FlushBytes(srcSS);
                    ByteUtil.WriteBytes(srcSS, new byte[]{0x05, (int)Socks5Reply.Succeeded, 0x00});
                    Socks5Util.WriteAddress(srcSS, server.ipep);
                    SrcSendData(srcSS.ToArray());
                    server.UDPJoin(this);
                    ReceiveSorketSrcTCPLoop2(); // 空读取
                }
            }
            catch (Exception e)
            {
                DebugUtil.Error("Failed to ReceiveSorketAuth error." + e);
                Stop();
            }
        }

        private void SrcRecvData()
        {
            srcTCPSize = srcTCP.Client.Receive(srcTCPBody);
            if (srcMask) Socks5Util.Mask(server.deMaskNum, srcTCPBody, srcTCPSize);
        }

        private void SrcSendData(byte[] bytes)
        {
            if (srcMask) Socks5Util.Mask(server.enMaskNum, bytes, bytes.Length);
            srcTCP.Client.Send(bytes);
        }

        private void ReceiveSorketSrcTCPLoop()
        {
            srcTCP.Client.BeginReceive(srcTCPBody, 0, srcTCPBody.Length, SocketFlags.None, ReceiveSorketSrcTCP, srcTCP);
        }

        private void ReceiveSorketSrcTCP(IAsyncResult result)
        {
            try
            {
                var tcp = (TcpClient)result.AsyncState;
                srcTCPSize = tcp.Client.EndReceive(result);
                if (srcTCPSize > 0)
                {
                    if (srcMask) Socks5Util.Mask(server.deMaskNum, srcTCPBody, srcTCPSize);
                    if (tarMask) Socks5Util.Mask(server.enMaskNum, srcTCPBody, srcTCPSize);
                    tarTCP.Client.Send(srcTCPBody, srcTCPSize, SocketFlags.None);
                    ReceiveSorketSrcTCPLoop();
                }else{
                    Stop();
                }
            }catch (Exception e){
                //DebugUtil.Error("Failed to ReceiveSorketSrc error." + e);
                Stop();
            }
        }

        private void ReceiveSorketTarTCPLoop()
        {
            tarTCP.Client.BeginReceive(tarTCPBody, 0, tarTCPBody.Length, SocketFlags.None, ReceiveSorketTarTCP, tarTCP);
        }

        private void ReceiveSorketTarTCP(IAsyncResult result)
        {
            try
            {
                var tcp = (TcpClient)result.AsyncState;
                tarTCPSize = tcp.Client.EndReceive(result);
                if (tarTCPSize > 0)
                {
                    if (tarMask) Socks5Util.Mask(server.deMaskNum, tarTCPBody, tarTCPSize);
                    if (srcMask) Socks5Util.Mask(server.enMaskNum, tarTCPBody, tarTCPSize);
                    srcTCP.Client.Send(tarTCPBody, tarTCPSize, SocketFlags.None);
                    ReceiveSorketTarTCPLoop();
                }
                else
                {
                    Stop();
                }  
            }
            catch (Exception e)
            {
                //DebugUtil.Error("Failed to ReceiveSorketSrc error." + e);
                Stop();
            }
        }

        private void ReceiveSorketSrcTCPLoop2()
        {
            srcTCP.Client.BeginReceive(srcTCPBody, 0, srcTCPBody.Length, SocketFlags.None, ReceiveSorketSrcTCP2, srcTCP);
        }

        private void ReceiveSorketSrcTCP2(IAsyncResult result)
        {
            try
            {
                var tcp = (TcpClient)result.AsyncState;
                var size = tcp.Client.EndReceive(result);
                DebugUtil.LogFormat("ReceiveSorketSrcTCP2:{0}", size);
                if (size == 0){
                    Stop();
                }else{
                    ReceiveSorketSrcTCPLoop2();
                }
            }
            catch (Exception e)
            {
                //DebugUtil.Error("Failed to ReceiveSorketSrcTCP2 error." + e);
                Stop();
            }
        }

        private void ReceiveSorketTarUDPLoop()
        {
            tarUDP.BeginReceiveFrom(tarUDPBody, 0, tarUDPBody.Length, SocketFlags.None, ref tarUDPREP2, ReceiveSorketTarUDP, tarUDP);
        }

        private void ReceiveSorketTarUDP(IAsyncResult result)
        {
            try
            {
                var udp = (Socket)result.AsyncState;
                tarUDPSize = udp.EndReceiveFrom(result, ref tarUDPREP2);
                
                ByteUtil.FlushBytes(srcSS);
                ByteUtil.WriteBytes(srcSS, new byte[]{0,0,0});
                Socks5Util.WriteAddress(srcSS, tarUDPREP);
                ByteUtil.WriteBytes(srcSS, tarUDPBody, 0, tarUDPSize);
                server.UDPSendToSrc(srcSS.ToArray(), srcUDPREP);
                ReceiveSorketTarUDPLoop();
            }
            catch (Exception e)
            {
                //DebugUtil.Error("Failed to ReceiveSorketSrc error." + e);
                Stop();
            }
        }

        public void SendToTar(byte[] bytes, int length)
        {
            try {
                // 踢掉RSV  FRAG 
                if (srcTCP != null)
                {
                    ByteUtil.FlushBytes(srcRS, bytes, 3, length - 3);
                    Socks5ATYP atyp;
                    Socks5Util.ReadAddress(srcRS, out atyp, out tarIPA, out tarHost, out tarPort);
                    tarUDPREP = new IPEndPoint(tarIPA, tarPort);
                    tarUDPREP2 = tarUDPREP;
                    if (tarUDP == null)
                    {
                        tarUDP = new Socket(server.ipep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                        ReceiveSorketTarUDPLoop();
                    }
                    var data = ByteUtil.ReadRestBytes(srcRS);
                    tarUDP.SendTo(data, SocketFlags.None, tarUDPREP2);
                    DebugUtil.LogFormat("SendToTar {0} size:{1}", tarUDPREP, data.Length);
                }
            }
            catch (Exception e)
            {
                DebugUtil.Error("Failed to SendToTar error." + e);
                Stop();
            }
        }
    }
}
