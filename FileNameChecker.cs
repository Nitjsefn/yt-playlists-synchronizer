using System;

namespace FileNameCheckerNs
{
    class FileNameChecker
    {
        static char[] forbiddenChars = {'<', '>', ':', '\"', '/', '\\', '|', '?', '*'};
        public static string FormatFileName(string fileName)
        {
            string formattedFileName = "";
            foreach (char chr in fileName)
            {
                if (Array.IndexOf(forbiddenChars, chr) != -1) formattedFileName += '_';
                else formattedFileName += chr;
            }
            return formattedFileName;
        }
    }
}