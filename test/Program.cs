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

            var clsmute = so.GetMute("Audio del desktop");
            so.SetMute("Audio del desktop", true);
            so.SetMute("asdfdaskbdskj", true);
            //string p = so.GetCurrentProfile();
            //var pp = so.ListProfiles();

            try
            {
                so.SetCurrentProfile("prof1");
                so.SetCurrentProfile("PROF1");
                var gcp = so.GetCurrentProfile();
            }
            catch(Exception e)
            {
                ;
            }

            //var ss = so.GetSceneList();
            //var s = so.GetCurrentScene();
            try
            {
                so.SetCurrentScene("Scena");
                so.SetCurrentScene("SCENA 2");
            }
            catch(Exception e)
            {
                ;
            }

            //var ll = so.GetSourcesList();

            //so.SourceCreated += (x, y) => Console.WriteLine("source created: " + y.sourceName);
            //so.SourceDestroyed += (x, a, b, c) => Console.WriteLine("source destroyed: " + a);
            //so.SourceMuteStateChanged += (x, y, z) => Console.WriteLine("source mute: " + y);
            //so.SourceAudioMixersChanged += (x, y) => Console.WriteLine("SourceAudioMixersChanged: " + y);
            //so.SourceAudioSyncOffsetChanged += (x, y, z) => Console.WriteLine("SourceAudioSyncOffsetChanged: " + y);
            //so.SourceFilterAdded += (x, y, a, b, c) => Console.WriteLine("SourceFilterAdded: " + y);
            //so.SourceFilterRemoved += (x, y, z) => Console.WriteLine("SourceFilterRemoved: " + y);
            //so.SourceFiltersReordered += (x, y, z) => Console.WriteLine("SourceFiltersReordered: " + y);
            //so.SourceFilterVisibilityChanged += (x, y, a, b) => Console.WriteLine("SourceFilterVisibilityChanged: " + y);
            //so.SourceOrderChanged += (x, y) => Console.WriteLine("SourceOrderChanged: " + y);
            //so.SourceRenamed += (x, y, z) => Console.WriteLine("SourceRenamed: " + y);
            //so.SourceVolumeChanged += (x, y, z) => Console.WriteLine("SourceVolumeChanged: " + y);

            // quando viene dis/attivata una fonte
            //so.SceneItemVisibilityChanged

            Console.ReadLine();

            try
            {
                var saved = so.GetStreamSettings();
                so.SetStreamingSettings(newsett, true);
                var xxx = so.StartStreaming();
            }
            catch(Exception testErr)
            {
                ;
            }

            so.Disconnect();
        }
    }
}
