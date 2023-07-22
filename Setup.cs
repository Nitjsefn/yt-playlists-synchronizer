using System;
using System.IO;
using System.Collections.Generic;

namespace yt_playlists_synchronizer
{
    struct PlaylistToSync
    {
        string DesiredPlaylistName;
        string PlaylistID;
        int NumberingOffset;
    }
    class Setup
    {
        private string Token;
        public string PLsToSyncPath;
        public List<PlaylistToSync> PLsToSync { get; }
        public Setup(string configFilename)
        {
            Token = "";
            PLsToSyncPath = "";
            if(!File.Exists(configFilename)) throw new FileNotFoundException("Could not find configuration file.");
            string cfgFile = File.ReadAllText(configFilename);
            int lineStartIndx = cfgFile.IndexOf("Token:");
            if(lineStartIndx == -1) throw new Exception("Configuration file is invalid. Could not find Token.");
            for(int i = 7 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                Token += cfgFile[i];
            }
            if(Token.Length == 0) throw new Exception("Configuration file is invalid. Could not find Token.");
            lineStartIndx = cfgFile.IndexOf("PLsToSyncPath:");
            if(lineStartIndx == -1) throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");
            for(int i = 15 + lineStartIndx; i < cfgFile.Length && cfgFile[i] != ';'; i++)
            {
                PLsToSyncPath += cfgFile[i];
            }
            if(PLsToSyncPath.Length == 0) throw new Exception("Configuration file is invalid. Could not find path to file with playlists to sync.");
            Console.WriteLine(PLsToSyncPath);
        }
    }
}