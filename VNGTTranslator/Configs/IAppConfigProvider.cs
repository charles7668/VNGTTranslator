using VNGTTranslator.Models;
using VNGTTranslator.TTSProviders;

namespace VNGTTranslator.Configs
{
    public interface IAppConfigProvider
    {
        /// <summary>
        /// Retrieves the current application configuration instance.
        /// </summary>
        /// <returns>The AppConfig instance containing the application settings.</returns>
        AppConfig GetAppConfig();

        /// <summary>
        /// Tries to save the application configuration to file.
        /// </summary>
        /// <returns>True if the save operation is successful, otherwise false.</returns>
        Result TrySaveAppConfig();

        /// <summary>
        /// Retrieves the configuration for a specific translator provider, stored as key-value pairs in a dictionary.
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns>
        /// if no configuration is found, an empty dictionary is returned.
        /// </returns>
        public Dictionary<string, object> GetTranslatorProviderConfig(string providerName);

        /// <summary>
        /// save the configuration for a specific translator provider.
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="config"></param>
        public Task<Result> SaveTranslatorProviderConfigAsync(string providerName, Dictionary<string, object> config);

        public (TTSCommonSetting comonSetting, Dictionary<string, object> otherSettings) GetTTSProviderConfig(
            string providerName);

        public Task<Result> SaveTTSProviderConfigAsync(string providerName, TTSCommonSetting commonSetting,
            Dictionary<string, object>? otherSettings = null);

        public Task<Dictionary<string, object>> GetOCRProviderConfigAsync(string providerName);

        public Task<Result> SaveOCRProviderConfigAsync(string providerName, Dictionary<string, object> config);
    }
}