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
			try
			{
				Context = new Setup(ConfigFileName);
			}
			catch(Exception e)
			{
				Log.ErrorLine(e.Message);
				return;
			}

			foreach(var pl in Context.PLsToSync)
			{
				try
				{
					new PlSync(pl).Synchronize();
				}
				catch(Exception e)
				{
					Log.ErrorLine($"Sync error: {e.Message}");
					return;
				}
			}
		}

		public static bool IsPartVidExtension(string ext)
		{
			// Valid ext: .*.part
			if(ext.Length < 6)
				return false;
			if(ext[0] != '.')
				return false;
			if(!ext.EndsWith(".part"))
				return false;
			return true;
		}

		public static bool IsOneFormatVidExtension(string ext)
		{
			// Valid ext: .f*.*
			if(ext.Length < 3)
				return false;
			if(ext.EndsWith(".part"))
				return false;
			if(!ext.StartsWith(".f"))
				return false;
			int i = ext.Length - 1;
			while(ext[i] != '.') i--;
			if(i == 0)
				return false;
			return true;
		}
	}
}
