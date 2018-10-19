using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Net;
using NodeProxy.Util;

namespace NodeProxy.Socks5
{
    public class Socks5Server
    {
        protected volatile TcpListener tcpS;
        protected volatile UdpClient udpS;
        protected byte[] udpBuffer = new byte[4096];
        protected IPEndPoint updREP = new IPEndPoint(IPAddress.Any, 0);
        public int port = 1080;
        public string host = "127.0.0.1";
        public IPEndPoint ipep { get; protected set; }

        public int parentPort;
        public string parentHost;

        public bool isNode { get { return !string.IsNullOrEmpty(parentHost) && parentPort > 0; } }

        /// <summary>
        /// 是否开启用户密码验证
        /// </summary>
        public bool useUPAuth = false;
        public bool inMask = false;
        public bool outMask = false;
        public int enMaskNum = 5;
        public int deMaskNum { get { return -enMaskNum; } }
        public Socks5NodeType type = Socks5NodeType.Client;
        private ConcurrentDictionary<int, Socks5Worker> tcpWorkers = new ConcurrentDictionary<int, Socks5Worker>();
        private ConcurrentDictionary<string, Socks5Worker> udpWorkers = new ConcurrentDictionary<string, Socks5Worker>();

        /// <summary>
        /// 开始服务
        /// </summary>
        public void Start()
        {
            if (tcpS != null || udpS != null) Stop();
            ipep = new IPEndPoint((string.IsNullOrEmpty(host) ? IPAddress.Any : IPAddress.Parse(host)), port);
            tcpS = new TcpListener(ipep);
            tcpS.Start();
            udpS = new UdpClient(ipep);
            TCPAcceptLoop();
            UDPRecvLoop();
            Console.WriteLine(string.Format("Socks5Server Start! {0}:{1} node:{2}", host, port, isNode));
        }

        private void TCPAcceptLoop()
        {
            tcpS.BeginAcceptTcpClient(new AsyncCallback(TCPAcceptClent), tcpS);
        }

        private void TCPAcceptClent(IAsyncResult result)
        {
            try
            {
                var client = tcpS.EndAcceptTcpClient(result);
                var worker = new Socks5Worker(this, client);
                tcpWorkers.TryAdd(worker.id, worker);
                worker.Start();
                TCPAcceptLoop();
            }catch{

            }
        }
            
        private void UDPRecvLoop()
        {
            udpS.BeginReceive(new AsyncCallback(UDPRecvFrom), udpS);
        }

        private void UDPRecvFrom(IAsyncResult result)
        {
            try
            {
                var udpBuffer = udpS.EndReceive(result, ref updREP);
                DebugUtil.LogFormat("UDPRecvFrom {0} size:{1}", updREP, udpBuffer.Length);
                Socks5Worker worker;
                if (udpWorkers.TryGetValue(updREP.ToString(), out worker) && udpBuffer.Length > 3)
                {
                    worker.SendToTar(udpBuffer, udpBuffer.Length);
                }
                UDPRecvLoop();
            }catch{

            }
        }

        public void UDPJoin(Socks5Worker worker)
        {
            udpWorkers.TryAdd(worker.udpID, worker);
        }

        public void UDPSendToSrc(byte[] data, IPEndPoint ep)
        {
            udpS.Send(data, data.Length, ep);
        }

        public void Stop(Socks5Worker worker)
        {
            var udpID = worker.udpID;
            tcpWorkers.TryRemove(worker.id, out worker);
            if (!string.IsNullOrEmpty(udpID)) udpWorkers.TryRemove(udpID, out worker);
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (tcpS == null) return;
            var ws = tcpWorkers.ToArray();
            foreach (var kv in ws)
            {
                kv.Value.Stop();
            }
            tcpWorkers.Clear();
            udpWorkers.Clear();
            if (tcpS != null)
                tcpS.Stop();
            tcpS = null;
            if (udpS != null)
                udpS.Close();
            udpS = null;
            DebugUtil.Log("Socks5Server Stop");
        }
    }
}