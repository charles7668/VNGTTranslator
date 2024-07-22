using System.IO;

namespace VNGTTranslator.Helper
{
    internal static class PathHelper
    {
        internal static bool IsValidPath(string path)
        {
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (path.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}