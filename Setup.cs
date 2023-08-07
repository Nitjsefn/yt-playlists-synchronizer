using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using FileNameCheckerNs;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;

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
        public string PlsCfgPath;
		public string BasicCfgPath;
        public string SyncedPlsDir;
        public string SyncDataDir;
		public YouTubeService YtService;
        public List<PlaylistToSync> PLsToSync { get; }

        public Setup(string configFilename)
        {
            Token = "";
            PlsCfgPath = "";
			BasicCfgPath = configFilename;
			PLsToSync = new List<PlaylistToSync>();
			ReadConfigFile();
			SyncedPlsDir = GetParentDirectory(PlsCfgPath);
			SyncDataDir = SyncedPlsDir + "sync-data/";
			ReadPlaylistsFromConfigFile();
			CreateSyncDataDir();
			ConnecToYtApi();
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
		}

		private void ReadPlaylistsFromConfigFile()
		{
            if(!File.Exists(PlsCfgPath))
				throw new FileNotFoundException("Could not find playlists configuration file.");
            string[] playlistsFileLines = File.ReadAllLines(PlsCfgPath);
			List<string> PlNamesInUse = new List<string>();
			PlNamesInUse.Add("sync-data");
			int counter = 0;
			foreach(var line in playlistsFileLines)
			{
				counter++;
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

		private string GetParentDirectory(string path)
		{
			int i = path.Length - 1;
			if(path.EndsWith('/') || path.EndsWith('\\'))
				i--;
			while(i >= 0 && path[i] != '/' && path[i] != '\\')
				i--;
			if(i < 0) return "./";
			return path.Substring(0, i+1);
		}

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
