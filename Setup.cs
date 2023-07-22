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
        public string PLsToSyncFilename;
        public List<PlaylistToSync> PLsToSync { get; }

    }
}