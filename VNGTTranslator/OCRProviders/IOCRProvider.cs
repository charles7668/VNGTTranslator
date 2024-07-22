using HandyControl.Controls;
using System.Drawing;
using VNGTTranslator.Enums;
using VNGTTranslator.Models;

namespace VNGTTranslator.OCRProviders
{
    public interface IOCRProvider
    {
        public string ProviderName { get; }

        public bool SupportSetting { get; }

        public Task<Result<string>> RecognizeTextAsync(Bitmap originalImage, ImagePreProcessFunction preProcessFunc);

        public Result SetOcrLanguage(string lang);

        public PopupWindow GetSettingWindow(Window parent);
    }
}