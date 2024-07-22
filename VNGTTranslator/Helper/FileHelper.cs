using System.IO;
using VNGTTranslator.Models;

namespace VNGTTranslator.Helper
{
    internal static class FileHelper
    {
        internal static Task<Result> DeleteIfExistAsync(string fileName)
        {
            if (!File.Exists(fileName))
                return Task.FromResult(Result.Success());
            try
            {
                File.Delete(fileName);
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result.Fail(ex.Message));
            }

            return Task.FromResult(Result.Success());
        }
    }
}