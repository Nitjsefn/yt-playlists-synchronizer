using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using FileNameCheckerNs;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace yt_playlists_synchronizer
{
    struct PlaylistToSync
    {
        public string DesiredPlaylistName;
        public string PlaylistID;
        public int NumberingOffset;
    }

    class Setup
    {
		// SyncedPlsPath + PlsCfgFilename = PlsCfgPath
        private string Token;
        private string RegionCode;
        public string PlsCfgPath;
		public string BasicCfgPath;
        public string SyncedPlsDir;
        public string SyncDataDir;
        public string TestDir;
		public YouTubeService YtService;
        public List<PlaylistToSync> PLsToSync { get; }

        public Setup(string configFilename)
        {
            Token = "";
            PlsCfgPath = "";
			BasicCfgPath = configFilename;
			PLsToSync = new List<PlaylistToSync>();
			ReadConfigFile();
			SyncedPlsDir = Path.GetDirectoryName(PlsCfgPath) + '/';
			SyncDataDir = SyncedPlsDir + ".sync-data/";
			TestDir = SyncedPlsDir + ".sync-testing-temp/";
			ReadPlaylistsFromConfigFile();
			CreateSyncDataDir();
			ConnecToYtApi();
			CheckYtdlpCorrectness();
        }

		private void ReadConfigFile()
		{
            if(!File.Exists(BasicCfgPath))
				throw new FileNotFoundException("Could not find configuration file.");
            string cfgFile = File.ReadAllText(BasicCfgPath);
            int lineStartIndx = cfgFile.IndexOf("Token:");
            if(lineStartIndx == -1)
				throw new Exception("Configuration file is invalid. Could not find Token.");
            for(int i = 7 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                Token += cfgFile[i];
            }
            if(Token.Length == 0)
				throw new Exception("Configuration file is invalid. Could not find Token.");
            lineStartIndx = cfgFile.IndexOf("PlsCfgPath:");
            if(lineStartIndx == -1)
				throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");
            for(int i = 12 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                PlsCfgPath += cfgFile[i];
            }
            if(PlsCfgPath.Length == 0)
				throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");
            lineStartIndx = cfgFile.IndexOf("RegionCode:");
            if(lineStartIndx == -1)
				RegionCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
			else
			{
				for(int i = 12 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
				{
					RegionCode += cfgFile[i];
				}
			}
		}

		private void ReadPlaylistsFromConfigFile()
		{
            if(!File.Exists(PlsCfgPath))
				throw new FileNotFoundException("Could not find playlists configuration file.");
            string[] playlistsFileLines = File.ReadAllLines(PlsCfgPath);
			List<string> PlNamesInUse = new List<string>();
			PlNamesInUse.Add(".sync-data");
			PlNamesInUse.Add(".sync-testing-temp");
			AddUsedFilenames(PlNamesInUse, SyncedPlsDir);
			int counter = 0;
			foreach(var line in playlistsFileLines)
			{
				counter++;
				if(line.Length == 0)
					continue;
				PlaylistToSync playlist = LineToPlaylistToSync(line); 
				if(playlist.PlaylistID.Length == 0)
				{
					Program.Log.ErrorLine($"Problem with Playlist ID in {PlsCfgPath} file in line: {counter}. This playlist won't be synchronized");
					continue;
				}
				if(playlist.DesiredPlaylistName.Length == 0)
				{
					Program.Log.ErrorLine($"Problem with Playlist Name in {PlsCfgPath} file in line: {counter}. This playlist won't be synchronized");
					continue;
				}
				if(PlNamesInUse.Contains(playlist.DesiredPlaylistName))
				{
					Program.Log.ErrorLine($"Playlist Name: {playlist.DesiredPlaylistName} is currently in use and cannot be used again. File {PlsCfgPath} Line: {counter}. This playlist won't be synchronized");
					continue;
				}
				PLsToSync.Add(playlist);
				PlNamesInUse.Add(playlist.DesiredPlaylistName);
			}
		}

		private void AddUsedFilenames(List<string> list, string path)
		{
			foreach(var name in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly))
				list.Add(Path.GetFileName(name));
		}

		//private string GetParentDirectory(string path)
		//{
			//int i = path.Length - 1;
			//if(path.EndsWith('/') || path.EndsWith('\\'))
				//i--;
			//while(i >= 0 && path[i] != '/' && path[i] != '\\')
				//i--;
			//if(i < 0) return "./";
			//return path.Substring(0, i+1);
		//}

		//public string GetNameFromPath(string path)
		//{
			//int nameOffset = 0;
			//if(path.EndsWith('/') || path.EndsWith('\\'))
				//nameOffset--;
			//int i = path.Length - 1;
			//while(i > 0 && path[i-1] != '/' && path[i-1] != '\\')
				//i--;
			////if(i == 0) return path.Substring(0, path.Length - Convert.ToInt32(Convert.ToBoolean(path[path.Length - 1]  - '\\') != Convert.ToBoolean(path[path.Length - 1] - '/')));
			////return path.Substring(i, path.Length - i - Convert.ToInt32(Convert.ToBoolean(path[path.Length - 1]  - '\\') != Convert.ToBoolean(path[path.Length - 1] - '/')));
			//return path.Substring(i, path.Length + nameOffset - i);
		//}

		private PlaylistToSync LineToPlaylistToSync(string line)
		{
			var playlist = new PlaylistToSync();
			string[] splitted;
			splitted = line.Split(';');
			if(splitted.Length < 2)
				return playlist;
			playlist.DesiredPlaylistName = FileNameChecker.FormatFileName(splitted[0]);
			playlist.PlaylistID = splitted[1];
			int numberingOffset;
			if(splitted.Length == 3)
				if(int.TryParse(splitted[2], out numberingOffset))
					playlist.NumberingOffset = numberingOffset;
			return playlist;
		}

		private void CreateSyncDataDir()
		{
			if(!Directory.Exists(SyncDataDir))
				Directory.CreateDirectory(SyncDataDir);
		}

		private void ConnecToYtApi()
		{
			try
			{
				YtService = new YouTubeService(new BaseClientService.Initializer()
				{
					ApiKey = Token 
				});
			}
			catch(Exception e)
			{
				throw new Exception("Could not connect or authorize with YouTube API");
			}
			if(!CheckApiConnection())
				throw new Exception("Could not connect or authorize with YouTube API");
			//Task<bool> connectionSuccessTask = CheckApiConnectionAsync();
			//connectionSuccessTask.Wait();
			//if(!connectionSuccessTask.Result)
			//	throw new Exception("Could not connect or authorize with YouTube API");
		}

		public async Task<bool> CheckApiConnectionAsync()
		{
			var req = YtService.Videos.List("snippet");
			req.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
			try
			{
				await req.ExecuteAsync();
			}
			catch(Exception e)
			{
				return false;
			}
			return true;
		}

		public bool CheckApiConnection()
		{
			var req = YtService.Videos.List("snippet");
			req.Chart = VideosResource.ListRequest.ChartEnum.MostPopular;
			try
			{
				req.ExecuteAsync().Wait();
			}
			catch(Exception e)
			{
				return false;
			}
			return true;
		}
    }
}
