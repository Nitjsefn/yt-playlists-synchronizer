using System;
using System.IO;
using System.Collections.Generic;

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
        private string Token;
        public string PLsToSyncPath;
		public YouTubeService ytService;
        public List<PlaylistToSync> PLsToSync { get; }
        public Setup(string configFilename)
        {
            Token = "";
            PLsToSyncPath = "";
			PLsToSync = new List<PlaylistToSync>();
			//New method ReadCOnfigFile
            if(!File.Exists(configFilename))
				throw new FileNotFoundException("Could not find configuration file.");
            string cfgFile = File.ReadAllText(configFilename);
            int lineStartIndx = cfgFile.IndexOf("Token:");
            if(lineStartIndx == -1)
				throw new Exception("Configuration file is invalid. Could not find Token.");
            for(int i = 7 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                Token += cfgFile[i];
            }
            if(Token.Length == 0)
				throw new Exception("Configuration file is invalid. Could not find Token.");
            lineStartIndx = cfgFile.IndexOf("PLsToSyncPath:");
            if(lineStartIndx == -1)
				throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");
            for(int i = 15 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                PLsToSyncPath += cfgFile[i];
            }
            if(PLsToSyncPath.Length == 0)
				throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");

            if(!File.Exists(PLsToSyncPath))
				throw new FileNotFoundException("Could not find playlists configuration file.");
            string[] playlistsFileLines = File.ReadAllLines(PLsToSyncPath);
			//new method ReadPlToSyncFile
			foreach(var line in playlistsFileLines)
			{
				//For each line call method GetPlaylistSyncInfo
				PlaylistToSync playlist = new PlaylistToSync();
				string[] splitted;
				splitted = line.Split(' ');
				//Add checking if name and id fields are present
				playlist.DesiredPlaylistName = splitted[0];
				playlist.PlaylistID = splitted[1];
				int numberingOffset;
				if(splitted.Length == 3)
					if(int.TryParse(splitted[2], out numberingOffset))
						playlist.NumberingOffset = numberingOffset;
				PLsToSync.Add(playlist);
			}
			//new method ConnecToApi
			try
			{
				ytService = new YouTubeService(new BaseClientService.Initializer()
				{
					ApiKey = Token 
				});
			}
			catch(Exception e)
			{
				throw new Exception("Could not connect or authorize with YouTube API");
			}
        }
    }
}
