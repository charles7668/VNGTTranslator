using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Text.Json;
using VNGTTranslator.Helper;
using VNGTTranslator.Models;

namespace VNGTTranslator.Configs
{
    internal class AppConfigProvider : IAppConfigProvider
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="appConfigFilePath">config file path , if empty then store in memory</param>
        /// <exception cref="JsonException"></exception>
        public AppConfigProvider(string appConfigFilePath)
        {
            if (string.IsNullOrEmpty(appConfigFilePath))
                throw new ArgumentException(@"path is empty", nameof(appConfigFilePath));

            if (!PathHelper.IsValidPath(appConfigFilePath))
                throw new ArgumentException(@"invalid path", nameof(appConfigFilePath));

            string? directoryName = Path.GetDirectoryName(appConfigFilePath);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);

            if (File.Exists(appConfigFilePath))
            {
                string jsonString = File.ReadAllText(appConfigFilePath);
                AppConfig? config = JsonSerializer.Deserialize<AppConfig>(jsonString);
                _appConfig = config ?? throw new JsonException("Invalid app config file");
            }
            else
            {
                _appConfig = new AppConfig();
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 20
            });

            _appConfigFilePath = Path.GetFullPath(appConfigFilePath);
            _appConfigDirectory = Path.GetDirectoryName(_appConfigFilePath)!;
        }

        private readonly AppConfig _appConfig;

        private readonly string _appConfigDirectory;

        private readonly string _appConfigFilePath;

        private readonly IMemoryCache _memoryCache;

        public AppConfig GetAppConfig()
        {
            return _appConfig;
        }

        public async Task<Dictionary<string, object>> GetTranslatorProviderConfigAsync(string providerName)
        {
            string cacheKey = $"translator:{providerName}";
            if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, object>? config)
                && config != null)
            {
                return config;
            }

            string configFolder = Path.Combine(_appConfigDirectory, "translator");
            string configFile = Path.Combine(configFolder, $"{providerName}.json");
            if (!File.Exists(configFile))
                return [];
            string jsonString = await File.ReadAllTextAsync(configFile);
            config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            config ??= new Dictionary<string, object>();
            _memoryCache.Set(cacheKey, config, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return config;
        }

        public async Task<Result> SaveTranslatorProviderConfigAsync(string providerName,
            Dictionary<string, object> config)
        {
            string cacheKey = $"translator:{providerName}";
            try
            {
                string configFolder = Path.Combine(_appConfigDirectory, "translator");
                Directory.CreateDirectory(configFolder);
                string configFile = Path.Combine(configFolder, $"{providerName}.json");
                string writeString = JsonSerializer.Serialize(config);
                await File.WriteAllTextAsync(configFile, writeString);
                _memoryCache.Set(cacheKey, config, new MemoryCacheEntryOptions
                {
                    Size = 1
                });
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }

            return Result.Success();
        }

        public async Task<Dictionary<string, object>> GetTTSProviderConfigAsync(string providerName)
        {
            string cacheKey = $"tts:{providerName}";
            if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, object>? config)
                && config != null)
            {
                return config;
            }

            string configFile = Path.Combine(_appConfigDirectory, "tts", $"{providerName}.json");
            if (!File.Exists(configFile))
                return [];
            string jsonString = await File.ReadAllTextAsync(configFile);
            Dictionary<string, object>?
                deserialize = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            deserialize ??= [];
            _memoryCache.Set(cacheKey, deserialize, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return deserialize;
        }

        public async Task<Result> SaveTTSProviderConfigAsync(string providerName,
            Dictionary<string, object>? config = null)
        {
            string cacheKey = $"tts:{providerName}";
            try
            {
                string configFolder = Path.Combine(_appConfigDirectory, "tts");
                Directory.CreateDirectory(configFolder);
                string configFile = Path.Combine(configFolder, $"{providerName}.json");
                string writeString = JsonSerializer.Serialize(config);
                await File.WriteAllTextAsync(configFile, writeString);
                _memoryCache.Set(cacheKey, config, new MemoryCacheEntryOptions
                {
                    Size = 1
                });
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }

            return Result.Success();
        }

        public async Task<Dictionary<string, object>> GetOCRProviderConfigAsync(string providerName)
        {
            string cacheKey = $"ocr:{providerName}";
            if (_memoryCache.TryGetValue(cacheKey,
                    out Dictionary<string, object>? config) && config != null)
            {
                return config;
            }

            string configFile = Path.Combine(_appConfigDirectory, "ocr", $"{providerName}.json");
            if (!File.Exists(configFile))
                return [];
            string jsonString = await File.ReadAllTextAsync(configFile);
            Dictionary<string, object>?
                deserialize = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            deserialize ??= [];
            _memoryCache.Set(cacheKey, deserialize, new MemoryCacheEntryOptions
            {
                Size = 1
            });
            return deserialize;
        }

        public async Task<Result> SaveOCRProviderConfigAsync(string providerName, Dictionary<string, object> config)
        {
            string cacheKey = $"ocr:{providerName}";
            try
            {
                string configFolder = Path.Combine(_appConfigDirectory, "ocr");
                Directory.CreateDirectory(configFolder);
                string configFile = Path.Combine(configFolder, $"{providerName}.json");
                string writeString = JsonSerializer.Serialize(config);
                await File.WriteAllTextAsync(configFile, writeString);
                _memoryCache.Set(cacheKey, config, new MemoryCacheEntryOptions
                {
                    Size = 1
                });
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }

            return Result.Success();
        }

        public Result TrySaveAppConfig()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(_appConfig);
                File.WriteAllText(_appConfigFilePath, jsonString);
                return Result.Success();
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }
        }
    }
}