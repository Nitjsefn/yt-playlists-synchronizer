using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using FileNameCheckerNs;

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
		private readonly string RevCsvPath;
		private readonly string NewCsvPath;
		private readonly string SyncDataPath;
		private readonly string RevSyncDataPath;
		private readonly string NewSyncDataPath;
		private readonly string DoubledFilesDir;
		private readonly string NotFinishedDir;
		private readonly bool FirstSync;
		private readonly string Delim;
		private readonly Ping YtPing;
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
			DoubledFilesDir = TargetDir + "doubled filenames/";
			NotFinishedDir = TargetDir + "not finished/";
			SyncDataPath = Program.Context.SyncDataDir
				+ Playlist.DesiredPlaylistName
				+ ".csv";
			RevSyncDataPath = Program.Context.SyncDataDir
				+ Playlist.DesiredPlaylistName
				+ ".rcsv";
			NewSyncDataPath = Program.Context.SyncDataDir
				+ Playlist.DesiredPlaylistName
				+ ".ncsv";
			CsvPath = TargetDir + Playlist.DesiredPlaylistName
				+ ".csv";
			RevCsvPath = TargetDir + Playlist.DesiredPlaylistName
				+ ".rcsv";
			NewCsvPath = TargetDir + Playlist.DesiredPlaylistName
				+ ".ncsv";
			FirstSync = false;
			if(!File.Exists(SyncDataPath) || new FileInfo(SyncDataPath).Length == 0)
				FirstSync = true;
			Delim = ",;";
			YtPing = new Ping();
		}

		public void Synchronize()
		{
			Program.Log.InfoLine($"Synchronization Begin: {Playlist.DesiredPlaylistName}");

			string testFilePath = Program.Context.TestDir + "test-file.test";
			try
			{
				File.WriteAllText(testFilePath, "Lorem ipsum dolor sit amet");
				File.Delete(testFilePath);
			}
			catch
			{
				Program.Log.ErrorLine("Something went wrong while saving or deleting test file. Check directory access privileges and available space on drive. This playlist won't be synchronized. Interrupting...");
				return;
			}

			if(Directory.Exists(DoubledFilesDir) && Directory.GetFiles(DoubledFilesDir).Length > 0)
			{
				Program.Log.ErrorLine($"The \"{DoubledFilesDir}\" dir exists and has files, which is not acceptable to proceed. Resolve conflicted filenames and remove them from that dir. This playlist won't be synchronized. Interrupting...");
				return;
			}

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

			bool gotAllVids = false;
			var ytReq = Program.Context.YtService.PlaylistItems.List("snippet");
			ytReq.MaxResults = 50;
			ytReq.PlaylistId = Playlist.PlaylistID;
			while(!gotAllVids)
			{
				ytReq.PageToken = "";
				var syncedVids = new List<SyncedVideo>(SyncedVideos);
				var addedToSync = new List<PlaylistItem>(VideosToSync);
				gotAllVids = true;
				bool firstPage = true;
				int totalResults = 0;
				while(ytReq.PageToken != null)
				{
					PlaylistItemListResponse ytRes;
					try
					{
						ytRes = ytReq.Execute();
						if(ytRes == null || ytRes.ETag == null || ytRes.ETag == "")
							throw new Exception("Something went wrong while downloading playlist info. Check your internet connection and available quota points");
					}
					catch
					{
						Program.Log.ErrorLine($"Something went wrong while connecting to YouTube. Check your internet connection and Playlist Id. Also you may have exceeded your quota units. Already received data will be stored in \"{NotFinishedDir}\" in case of some video loss before next sync. Program won't use this data for future syncs. This playlist won't be synchronized. Interrupting...");
						if(!Directory.Exists(NotFinishedDir))
							Directory.CreateDirectory(NotFinishedDir);
						foreach(var video in VideosToSync)
						{
							var filePath = $"{NotFinishedDir}{FileNameChecker.FormatFileName(video.Snippet.Title)}.{video.Snippet.ResourceId.VideoId}.{DateTime.Now.ToString("yyyyMMddHHmmss")}.json";
							try
							{
								File.AppendAllText(filePath, JsonSerializer.Serialize(video, new JsonSerializerOptions{ WriteIndented = true }));
							}
							catch
							{
								Program.Log.ErrorLine($"Something went wrong while saving files. Check directory access permissions and available space on drive");
								break;
							}
						}
						return;
					}
					ytReq.PageToken = ytRes.NextPageToken;
					foreach(var video in ytRes.Items)
					{
						//int pos = SyncedVidPos(video.Snippet.ResourceId.VideoId);
						int pos = syncedVids.FindIndex((e) => { return e.Id == video.Snippet.ResourceId.VideoId; });
						if(pos == -1)
						{
							pos = addedToSync.FindIndex((e) => { return e.Snippet.ResourceId.VideoId == video.Snippet.ResourceId.VideoId; });
							if(pos == -1)
								VideosToSync.Add(video);
							else
								addedToSync.RemoveAt(pos);
						}
						else
							syncedVids.RemoveAt(pos);
					}
					if(firstPage)
					{
						firstPage = false;
						totalResults = (int)ytRes.PageInfo.TotalResults;
					}
					else if(ytRes.PageInfo.TotalResults != totalResults)
						gotAllVids = false;
				}
			}

			PerformSync();

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
			int csvAvailablePos = BDVidAvailablePos + Playlist.NumberingOffset;
			string dateTimeFormat = "dd.MM.yyyy HH:mm:ss";
			string csvFile = "";
			string syncdataFile = "";
			var ytdlp = new Process();
			ytdlp.StartInfo = new ProcessStartInfo("yt-dlp");
			ytdlp.StartInfo.RedirectStandardError = true;
			ytdlp.StartInfo.RedirectStandardOutput = true;
// Move this to end, just before saving
			if(File.Exists(CsvPath))
				csvFile = File.ReadAllText(CsvPath);
			if(File.Exists(SyncDataPath))
				syncdataFile = File.ReadAllText(SyncDataPath);

			while(VideosToSync.Count > 0)
			{
        		int oldestVidIndx = 0;
                for(int i = 0; i < VideosToSync.Count; i++)
                	if(VideosToSync[i].Snippet.PublishedAtDateTimeOffset < VideosToSync[oldestVidIndx].Snippet.PublishedAtDateTimeOffset)
                    	oldestVidIndx = i;
                var video = VideosToSync[oldestVidIndx];
                int videoNumber = BDVidAvailablePos + Playlist.NumberingOffset;
                var backupDate = DateTime.Now.ToString(dateTimeFormat);
                var videoName = "";
                var videoLink = "";
                var videoCreator = "";
                var videoDescription = "";
                var thumbnailUrl = "";
                var videoCreatorLink = "";
                var descriptionFileName = "";
                var descriptionFilePath = "";
                var thumbnailFileName = "";
                var thumbnailFilePath = "";
                var thumbnailExtension = "";
				var videoFileName = "";
				var videoFileNameFormat = "";
				var capsFileName = "";
				var capsFileNameFormat = "";
				var dateOfFindingVideo = "";
                if(video.Snippet.PublishedAt != null) dateOfFindingVideo = video.Snippet.PublishedAt.Value.ToString(dateTimeFormat);
                if(video.Snippet.ResourceId.VideoId != null) videoLink = "https://youtu.be/" + video.Snippet.ResourceId.VideoId;
                if(video.Snippet.Thumbnails.Maxres != null) thumbnailUrl = video.Snippet.Thumbnails.Maxres.Url;
                else if(video.Snippet.Thumbnails.Standard != null) thumbnailUrl = video.Snippet.Thumbnails.Standard.Url;
                else if(video.Snippet.Thumbnails.High != null) thumbnailUrl = video.Snippet.Thumbnails.High.Url;
                else if(video.Snippet.Thumbnails.Medium != null) thumbnailUrl = video.Snippet.Thumbnails.Medium.Url;
                else if(video.Snippet.Thumbnails.Default__ != null) thumbnailUrl = video.Snippet.Thumbnails.Default__.Url;
                if(video.Snippet.Title != null) videoName = video.Snippet.Title;
                if(video.Snippet.VideoOwnerChannelTitle != null) videoCreator = video.Snippet.VideoOwnerChannelTitle;
                if(video.Snippet.VideoOwnerChannelId != null) videoCreatorLink = "http://youtube.com/channel/" + video.Snippet.VideoOwnerChannelId;
                if( (videoCreator != "") || (videoCreatorLink != "") || (videoLink != "") || (video.Snippet.Description != null) || (dateOfFindingVideo != "") ) videoDescription = $"(by {videoCreator} --> {videoCreatorLink})\nOriginal video link: {videoLink}\nFound:\t{dateOfFindingVideo}\nBackup:\t{backupDate}\n\n" + video.Snippet.Description;

				if(CheckYtConn() == false)
				{
					Program.Log.ErrorLine($"Cannot connect with YouTube. Check your internet connection. Check if the last video data is synced correctly. Already received data will be stored in \"{NotFinishedDir}\" in case of some video loss before next sync. Program won't use this data for future syncs. This playlist won't be fully synchronized. Interrupting...");
				   	try
				   	{
				   		SaveVideosToSync();
				   	    File.WriteAllText(NewSyncDataPath, syncdataFile);
				   	    File.WriteAllText(NewCsvPath, csvFile);
				   	    File.Move(NewSyncDataPath, SyncDataPath, true);
				   	    File.Move(NewCsvPath, CsvPath, true);
				   	}
				   	catch
				   	{
				   	    Program.Log.ErrorLine($"Something went wrong while saving files. Check directory access permissions and available space on drive. Progress of sync won't be saved, but already downloaded files will remain untouched until the next sync, which can move those files into '{DoubledFilesDir}'");
				   	}
				   	ytdlp.Close();
				   	return;
				}

               	csvFile = $"{videoNumber}{Delim}{videoName}{Delim}{thumbnailUrl != ""}{Delim}{videoDescription != ""}{Delim}{dateOfFindingVideo}{Delim}{backupDate}\n" + csvFile;
               	syncdataFile = $"{BDVidAvailablePos}{Delim}{video.Snippet.ResourceId.VideoId}\n" + syncdataFile;

               	BDVidAvailablePos++;
               	VideosToSync.RemoveAt(oldestVidIndx);
			}
			ytdlp.Close();
		}
	}
}
