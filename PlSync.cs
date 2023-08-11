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
		private readonly string CsvPath;
		private readonly string SyncDataPath;
		private readonly bool FirstSync;
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
			CsvPath = TargetDir + Playlist.DesiredPlaylistName
				+ ".csv";
			FirstSync = false;
			if(!File.Exists(SyncDataPath) || new FileInfo(SyncDataPath).Length == 0)
				FirstSync = true;
		}

		public void Synchronize()
		{
			Program.Log.InfoLine($"Synchronization Begin: {Playlist.DesiredPlaylistName}");

			if(Directory.Exists(TargetDir))
			{
				int filesCount = Directory.GetFiles(TargetDir, "*.*", SearchOption.AllDirectories).Length;
				if(filesCount > 0)
				{
					if(FirstSync)
					{
						Program.Log.ErrorLine("The sync directory is not empty even though this is the first sync. This playlist won't be synchronized. Interrupting...");
						return;
					}
					if(!File.Exists(CsvPath))
					{
						Program.Log.ErrorLine("Playlist directory is not empty even though csv file does not exist. This playlist won't be synchronized. Interrupting...");
						return;
					}
					if(filesCount > 1 && new FileInfo(CsvPath).Length == 0)
					{
						Program.Log.ErrorLine("Playlist directory is not empty even though csv file is. This playlist won't be synchronized. Interrupting...");
						return;
					}
					if(filesCount == 1 && new FileInfo(CsvPath).Length > 0)
					{
						Program.Log.ErrorLine("Playlist directory doesn't have files even though csv file is not empty. This playlist won't be synchronized. Interrupting...");
						return;
					}
					if(filesCount != 1)
					{
						Program.Log.WarningLine("Playlist directory is not empty. You may have forgotten to backup data after last sync. Old data will not be intentionally deleted");
					}
				}
			}
			else Directory.CreateDirectory(TargetDir);

			if(!Directory.Exists(CapsDir))
				Directory.CreateDirectory(CapsDir);
			if(!Directory.Exists(ThumbDir))
				Directory.CreateDirectory(ThumbDir);
			if(!Directory.Exists(DescDir))
				Directory.CreateDirectory(DescDir);
			if(!Directory.Exists(VideosDir))
				Directory.CreateDirectory(VideosDir);

			
			
			Program.Log.InfoLine($"Synchronization End: {Playlist.DesiredPlaylistName}");
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
