using HandyControl.Controls;
using System.Drawing;
using VNGTTranslator.Enums;
using VNGTTranslator.Models;
using Window = System.Windows.Window;

namespace VNGTTranslator.OCRProviders
{
    public interface IOCRProvider
    {
        public string ProviderName { get; }

        public bool SupportSetting { get; }

        public Task<PopupWindow> GetSettingWindowAsync(Window parent);

        public Task<Result<string>> RecognizeTextAsync(Bitmap originalImage, ImagePreProcessFunction preProcessFunc);

        public Result SetOcrLanguage(string lang);
    }
}