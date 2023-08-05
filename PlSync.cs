using System.IO;
using System.Collections.Generic;

using Google.Apis.YouTube.v3;

namespace yt_playlists_synchronizer
{
	struct SyncedVideo
	{
		public string FoundingDate;
		public string Id;
		public int Pos;
	}

	class PlSync
	{
		private readonly PlaylistToSync Playlist;
		private readonly string TargetDir;
		private List<PlaylistsResource> VideosToSync;
		private int AvailablePos;

		public PlSync(PlaylistToSync pl)
		{
			Playlist = pl;
			VideosToSync = new List<PlaylistsResource>();
			TargetDir = Program.Context.SyncedPlsDir
				+ Playlist.DesiredPlaylistName
				+ '/';
		}

		public void Synchronize()
		{
		}

		private List<SyncedVideo> ReadSyncedPls()
		{
			return new List<SyncedVideo>();
		}

		private void PerformSync()
		{
		}
	}
}
