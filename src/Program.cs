using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;

namespace NodeProxy
{
    class Program
    {
        private const string CONFIG_FILE = "config.json";
        private static Socks5.Socks5Server server;
        static void Main(string[] args)
        {
            Util.DebugUtil.ClearLogFile();
            //Util.DebugUtil.outtype = Util.DebugUtil.INFO | Util.DebugUtil.ERROR | Util.DebugUtil.FILE;
            //Util.DebugUtil.outtype = Util.DebugUtil.INFO | Util.DebugUtil.FILE;
            Util.DebugUtil.outtype = Util.DebugUtil.INFO;
            server = new Socks5.Socks5Server();
            Dictionary<string, object> json = null;
            if (args.Length > 0 && File.Exists(args[0])){
                json = ReadConfig(args[0]);
            }if (File.Exists(CONFIG_FILE)){
                json = ReadConfig(CONFIG_FILE);
            }else{
                MakeDefaultConfig();
            }
            if (json!=null && json.Count > 0){
                server.host = (string)json["host"];
                server.port = (int)json["port"];
                if (json.ContainsKey("parent_host")) server.parentHost = (string)json["parent_host"];
                if (json.ContainsKey("parent_port")) server.parentPort = (int)json["parent_port"];
                if (json.ContainsKey("mask_number")) server.enMaskNum = (int)json["mask_number"];
                if (json.ContainsKey("in_mask")) server.inMask = (bool)json["in_mask"];
                if (json.ContainsKey("out_mask")) server.outMask = (bool)json["out_mask"];
            }
            server.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }

        static private void MakeDefaultConfig(){
            var jsonStr = @"{
""host"":""127.0.0.1"",
""port"":1080,
""parent_host"":"""",
""parent_port"":0,
""mask_number"":5,
""in_mask"":false,
""out_mask"":false
}";
            File.WriteAllText(CONFIG_FILE, jsonStr);
        }

        static private Dictionary<string,object> ReadConfig(string file){
            var json = new Dictionary<string, object>();
            if(File.Exists(file)){
                var jsonStr = File.ReadAllText(file);
                var ms = Regex.Matches(jsonStr, @"""(\w+)"":([^\r\n,\s]+)");
                var arr = jsonStr.Split(new char[]{':',','});
                foreach(Match m in ms){
                    var key = m.Groups[1].Value.ToString();
                    var val = m.Groups[2].Value.ToString();
                    val = val.Trim(new char[]{' ', '\t'});
                    if(val.Contains(@"""") || val.Contains(@"'")){
                        val = val.Trim(new char[]{'"', '\''});
                        json.Add(key, val);
                    }else if(val.Contains(@"true") || val.Contains(@"false")){
                        json.Add(key, Boolean.Parse(val));
                    }else{
                        json.Add(key, Int32.Parse(val));
                    }
                }
            }
            return json;
        }
    }
}
