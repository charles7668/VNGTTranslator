using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using VNGTTranslator.Configs;
using VNGTTranslator.Enums;
using VNGTTranslator.Helper;
using VNGTTranslator.Models;
using VNGTTranslator.OCRProviders.SettingPages;
using Localization = VNGTTranslator.Properties.Localization;
using Window = System.Windows.Window;

namespace VNGTTranslator.OCRProviders
{
    [Export(typeof(IOCRProvider))]
    internal class TesseractOCRProvider : IOCRProvider
    {
        internal TesseractOCRProvider()
        {
            _appConfigProvider = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>();
        }

        private const string TESSERACT_PATH_KEY = "TesseractPath";

        private static readonly Dictionary<string, string> _LangMapping = new()
        {
            { "en", "eng" },
            { "zh-CN", "chi_sim" },
            { "zh-TW", "chi_tra" },
            { "ja", "jpn" }
        };

        private readonly IAppConfigProvider _appConfigProvider;

        private string _ocrLanguage = "eng";
        private Dictionary<string, object>? _setting;
        public string ProviderName { get; } = "Tesseract";

        public bool SupportSetting { get; } = true;

        public async Task<Result<string>> RecognizeTextAsync(Bitmap originalImage,
            ImagePreProcessFunction preProcessFunc)
        {
            string? tesseractPath = await GetSettingItemAsync<string>(TESSERACT_PATH_KEY);
            tesseractPath ??= "";
            string executionFilePath = Path.Join(tesseractPath, "tesseract.exe");
            if (!File.Exists(executionFilePath))
            {
                return Result<string>.Fail("Tesseract : " + Localization.Error_ExecutionFileNotFound);
            }

            // generate hash value
            string tempPath = Path.GetTempPath();
            string imageName = Guid.NewGuid().ToString();
            string tempFile = Path.Join(tempPath, $"{imageName}.png");
            originalImage.Save(tempFile);
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Join(tesseractPath, "tesseract.exe"),
                Arguments = $"\"{tempFile}\" stdout -l {_ocrLanguage}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            if (process == null)
            {
                await FileHelper.DeleteIfExistAsync(tempFile);
                return Result<string>.Fail(Localization.Error_FailedToStartProcess + " : Tesseract");
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            await FileHelper.DeleteIfExistAsync(tempFile);
            return Result<string>.Success(output);
        }

        public Result SetOcrLanguage(string lang)
        {
            bool ok = _LangMapping.TryGetValue(lang, out string? useLang);
            if (!ok)
                return Result.Fail("Language not supported");
            _ocrLanguage = useLang!;
            return Result.Success();
        }

        public async Task<PopupWindow> GetSettingWindowAsync(Window parent)
        {
            string? tesseractPath = await GetSettingItemAsync<string>(TESSERACT_PATH_KEY);
            tesseractPath ??= "";


            TesseractOCRSetting.SettingParams settingParams = new()
            {
                TesseractPath = tesseractPath
            };
            Page settingPage = new TesseractOCRSetting(ref settingParams);
            var window = new PopupWindow
            {
                PopupElement = settingPage,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AllowsTransparency = true,
                WindowStyle = WindowStyle.None,
                MinWidth = 0,
                MinHeight = 0,
                Title = ProviderName,
                Owner = parent
            };
            window.Closing += async (_, _) =>
            {
                bool hasChange = false;
                if (settingParams.TesseractPath != tesseractPath)
                {
                    await SetSettingItemAsync(TESSERACT_PATH_KEY, settingParams.TesseractPath);
                    hasChange = true;
                }

                if (hasChange)
                    await StoreSettingsAsync();
            };
            return window;
        }

        private async Task<T?> GetSettingItemAsync<T>(string key)
        {
            _setting ??= await _appConfigProvider.GetOCRProviderConfigAsync(ProviderName);
            if (_setting == null)
            {
                return default;
            }

            object? obj = _setting!.GetValueOrDefault(key, null);
            if (obj == null)
            {
                return default;
            }

            try
            {
                if (obj is JsonElement element)
                    return element.Deserialize<T>();
                return (T)obj;
            }
            catch (Exception)
            {
                return default;
            }
        }

        private async Task SetSettingItemAsync(string key, object value)
        {
            _setting ??= await _appConfigProvider.GetOCRProviderConfigAsync(ProviderName);
            if (_setting == null)
                return;
            _setting[key] = value;
        }

        private Task StoreSettingsAsync()
        {
            return _appConfigProvider.SaveOCRProviderConfigAsync(ProviderName,
                _setting ?? new Dictionary<string, object>());
        }
    }
}