using System;

namespace yt_playlists_synchronizer
{
	class Logger
	{
		public void ErrorLine(string err)
		{
			var defaultConsoleFgColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(err);
			Console.ForegroundColor = defaultConsoleFgColor;
		}

		public void InfoLine(string err)
		{
			Console.WriteLine(err);
		}

		public void SuccessLine(string err)
		{
			var defaultConsoleFgColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(err);
			Console.ForegroundColor = defaultConsoleFgColor;
		}

		public void WarningLine(string err)
		{
			var defaultConsoleFgColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine(err);
			Console.ForegroundColor = defaultConsoleFgColor;
		}
	}
}
