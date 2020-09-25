using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Tests();
            tests.TestMultiple().Wait();
        }
    }

    class Tests
    {
        public async Task Test()
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
            await so.ConnectAsync("ws://127.0.0.1:4444");
            await so.StartAsync();

            //bool clsmute = await so.GetMuteAsync("Audio del desktop");
            //await so.SetMuteAsync("Audio del desktop", true);
            //await so.SetMuteAsync("asdfdaskbdskj", true);
            //string p = so.GetCurrentProfile();
            //var pp = so.ListProfiles();
            var prop = await so.GetSceneItemPropertiesAsync("Fonte di colore");

            try
            {
                await so.SetCurrentProfileAsync("prof1");
                await so.SetCurrentProfileAsync("PROF1");
                var gcp = await so.GetCurrentProfileAsync();
            }
            catch(Exception e)
            {
                ;
            }

            //var ss = so.GetSceneList();
            //var s = so.GetCurrentScene();
            try
            {
                await so.SetCurrentSceneAsync("Scena");
                await so.SetCurrentSceneAsync("SCENA 2");
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
                var saved = await so.GetStreamSettingsAsync();
                await so.SetStreamingSettingsAsync(newsett, true);
                var xxx = await so.StartStreamingAsync();
            }
            catch(Exception testErr)
            {
                ;
            }

            await so.DisconnectAsync();
        }

        public async Task TestMultiple()
        {
            var so = new OBSWebsocketDotNet.OBSWebsocket();
            so.WSTimeout = TimeSpan.FromSeconds(3);
            await so.ConnectAsync("ws://127.0.0.1:4444");
            await so.StartAsync();

            so.StreamingStateChanged += (ws, state) =>
            {
                Console.WriteLine("streaming state changed: " + state);
            };

            so.StreamStatus += (ws, status) =>
            {
                Console.WriteLine("stream status:\n" + Newtonsoft.Json.JsonConvert.SerializeObject(status));
            };

            var stdin = Console.OpenStandardInput();
            var consoleIn = new StreamReader(stdin);

            string cmd;

            do
            {
                cmd = await consoleIn.ReadLineAsync();
                switch (cmd)
                {
                    case "start": await so.StartStreamingAsync(); break;
                    case "stop" : await so.StopStreamingAsync(); break;
                }
            } while (cmd != "exit");

            await so.DisconnectAsync();
        }
    }
}
