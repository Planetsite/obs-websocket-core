using System;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("test");

            var newsett = new OBSWebsocketDotNet.Types.StreamingService();
            newsett.Type = "rtmp_custom";
            newsett.Settings = new OBSWebsocketDotNet.Types.StreamingServiceSettings
            {
                UseAuth = true,
                Server = "rtmp://192.168.150.130/live/test3",
                Key = "key",
                Password = "pass",
                Username = "user"
            };

            var so = new OBSWebsocketDotNet.OBSWebsocket();
            so.WSTimeout = TimeSpan.FromSeconds(3);
            so.Connect("ws://127.0.0.1:4444", null);

            try
            {
                var xxx = so.StartStreaming();
                so.SetStreamingSettings(newsett, true);
                var saved = so.GetStreamSettings();
            }
            catch(Exception testErr)
            {
                ;
            }

            so.Disconnect();
        }
    }
}
