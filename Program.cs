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
		public static string configFileName = "./config.txt";
		public static Setup context = new Setup(configFileName);
        private static void Main(string[] args) => Program.Run().Wait();
        private static async Task Run()
        {
			
        }
    }
}
