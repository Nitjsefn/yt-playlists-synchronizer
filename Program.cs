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
		public static string ConfigFileName = "./config.txt";
		public static Logger Log;
		public static Setup Context;
		private static void Main(string[] args)
		{
			Log = new Logger();
			Context = new Setup(ConfigFileName);
		}
	}
}
