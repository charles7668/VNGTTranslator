using VNGTTranslator.Models;

namespace VNGTTranslator.TTSProviders
{
    public interface ITTSProvider
    {
        public string ProviderName { get; }

        public string SelectedVoice { get; }

        public int Rate { get; set; }

        public int Volume { get; set; }

        public IEnumerable<string> GetSupportedVoiceList();

        public Result SetVoice(string voiceName);

        /// <summary>
        /// Speak text asynchronously
        /// </summary>
        /// <param name="text"></param>
        public Task SpeakAsync(string text);

        /// <summary>
        /// store current settings
        /// </summary>
        /// <returns></returns>
        public Task<Result> StoreSettingsAsync();

        public void StopSpeak();
    }
}