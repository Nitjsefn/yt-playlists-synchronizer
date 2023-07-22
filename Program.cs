using System;
using System.Threading.Tasks;

using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace yt_playlists_synchronizer
{
    class Program
    {
        private static void Main(string[] args) => Program.Run().Wait();
        private static async Task Run()
        {
            string configFileName = "./config.txt";
            var context = new Setup(configFileName);
        }
    }
}