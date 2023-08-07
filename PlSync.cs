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
		private readonly string CapsDir;
		private readonly string ThumbDir;
		private readonly string DescDir;
		private readonly string VideosDir;
		private readonly string SyncDataPath;
		private List<PlaylistsResource> VideosToSync;
		private int AvailablePos;

		public PlSync(PlaylistToSync pl)
		{
			Playlist = pl;
			VideosToSync = new List<PlaylistsResource>();
			TargetDir = Program.Context.SyncedPlsDir
				+ Playlist.DesiredPlaylistName
				+ '/';
			CapsDir = TargetDir + "captions/";
			ThumbDir = TargetDir + "thumbs/";
			DescDir = TargetDir + "descriptions/";
			VideosDir = TargetDir + "videos/";
			SyncDataPath = Program.Context.SyncDataDir
				+ Playlist.DesiredPlaylistName
				+ ".csv";
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
