using System;
using System.IO;
using System.Collections.Generic;

using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace yt_playlists_synchronizer
{
	struct SyncedVideo
	{
		//public string FoundingDate;
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
		private List<PlaylistItem> VideosToSync;
		private List<SyncedVideo> SyncedVideos;
		private int BDVidAvailablePos;

		public PlSync(PlaylistToSync pl)
		{
			Playlist = pl;
			VideosToSync = new List<PlaylistItem>();
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

			if(!FirstSync)
			{
				using(var sr = new StreamReader(SyncDataPath))
				{
					try
					{
						BDVidAvailablePos = int.Parse(sr.ReadLine().Split(",;")[0]) + 1;
					}
					catch
					{
						Program.Log.ErrorLine($"Playlist sync data file is corrupted: '{SyncDataPath}'. This playlist won't be synchronized. Interrupting...");
						return;
					}
				}
			}
			else
				BDVidAvailablePos = 1;

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
						int nextVidPosByCsv;
						using(var sr = new StreamReader(CsvPath))
						{
							try
							{
								nextVidPosByCsv = int.Parse(sr.ReadLine().Split(",;")[0]) + 1;
							}
							catch
							{
								Program.Log.ErrorLine("Playlist csv file is corrupted. This playlist won't be synchronized. Interrupting...");
								return;
							}
						}
						int BDVidPosWithNO = BDVidAvailablePos + Playlist.NumberingOffset;
						if(nextVidPosByCsv > BDVidPosWithNO)
						{
							Program.Log.ErrorLine($"Numbering offset is too low, which can cause overwritting some previously synced files. Change offset or (backup synced data and delete content of '{TargetDir}') and start sync again. This playlist won't be synchronized. Interrupting...");
							return;
						}
						if(nextVidPosByCsv < BDVidPosWithNO)
							Program.Log.WarningLine("Numbering offset is higher than expected, which will cause gap in csv file and videos numbering. Check this after sync");
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

			if(FirstSync && Playlist.NumberingOffset != 0)
				Program.Log.WarningLine("Numbering offset does not equal to 0 even though this is the first sync. Because of this, synced videos won't start at number 1. Check this after sync");
			
			//Sync
			if(!FirstSync)
			{
				SyncedVideos = ReadSyncedVids();
				if(SyncedVideos == null)
				{
					Program.Log.ErrorLine($"Playlist sync data file is corrupted: '{SyncDataPath}'. This playlist won't be synchronized. Interrupting...");
					return;
				}
			}
			else
				SyncedVideos = new List<SyncedVideo>();

			var ytReq = Program.Context.YtService.PlaylistItems.List("snippet");
			ytReq.MaxResults = 50;
			ytReq.PlaylistId = Playlist.PlaylistID;
			ytReq.PageToken = "";
			while(ytReq.PageToken != null)
			{
				PlaylistItemListResponse ytRes;
				try
				{
					ytRes = ytReq.Execute();
				}
				catch
				{
					Program.Log.ErrorLine("Something went wrong while connecting to YouTube. Check your internet connection and Playlist Id. This playlist won't be synchronized. Interrupting...");
					return;
				}
				ytReq.PageToken = ytRes.NextPageToken;
				foreach(var video in ytRes.Items)
				{
					int pos = SyncedVidPos(video.Snippet.ResourceId.VideoId);
					if(pos == -1)
						VideosToSync.Add(video);
					else
						SyncedVideos.RemoveAt(pos);
				}
			}

			Program.Log.InfoLine($"Synchronization End: {Playlist.DesiredPlaylistName}");
		}

		private List<SyncedVideo> ReadSyncedVids()
		{
			var videos = new List<SyncedVideo>();
			string[] lines = File.ReadAllLines(SyncDataPath);
			foreach(string line in lines)
			{
				SyncedVideo vid;
				string[] fields = line.Split(",;");
				try
				{
					vid.Pos = int.Parse(fields[0]);
					vid.Id = fields[1];
					//vid.FoundingDate = int.Parse(fields[2]);
				}
				catch
				{
					return null;
				}
				videos.Add(vid);
			}
			return videos;
		}

		private int SyncedVidPos(string id)
		{
			for(int i = 0; i < SyncedVideos.Count; i++)
				if(SyncedVideos[i].Id == id)
					return i;
			return -1;
		}

		private void PerformSync()
		{
		}
	}
}
