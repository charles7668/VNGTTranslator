using HandyControl.Controls;
using VNGTTranslator.Models;
using Window = System.Windows.Window;

namespace VNGTTranslator.TranslateProviders
{
    public interface ITranslateProvider
    {
        string ProviderName { get; }

        public Task<string> TranslateAsync(string text, LanguageConstant.Language sourceLanguage,
            LanguageConstant.Language targetLanguage);

        public PopupWindow GetSettingWindow(Window parent);

        public Task<Result> StoreSettingsAsync();
    }
}