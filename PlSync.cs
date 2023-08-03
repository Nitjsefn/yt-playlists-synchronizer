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
		public static void Synchronize(PlaylistToSync playlist)
		{
		}

		private static List<SyncedVideo> ReadSyncedPls(string path)
		{
		}

		private static PerformSync(List<PlaylistsResource> toSync, int availablePos)
		{
		}
	}
}
