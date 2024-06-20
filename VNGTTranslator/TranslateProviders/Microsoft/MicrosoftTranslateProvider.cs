﻿using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using VNGTTranslator.Configs;
using VNGTTranslator.Models;
using VNGTTranslator.Network;
using VNGTTranslator.TranslateProviders.SettingPages;
using Window = System.Windows.Window;

namespace VNGTTranslator.TranslateProviders.Microsoft
{
    [Export(typeof(ITranslateProvider))]
    internal class MicrosoftTranslateProvider : ITranslateProvider
    {
        public MicrosoftTranslateProvider()
        {
            _setting = _appConfigProvider.GetTranslatorProviderConfig(ProviderName);
        }

        private const string IS_USE_PROXY_SETTING_STRING = "IsProxyUse";

        private readonly IAppConfigProvider _appConfigProvider =
            Program.ServiceProvider.GetRequiredService<IAppConfigProvider>();

        private readonly INetworkService
            _networkService = Program.ServiceProvider.GetRequiredService<INetworkService>();

        private readonly Dictionary<string, object> _setting;

        public string ProviderName { get; } = "Microsoft";

        public async Task<string> TranslateAsync(string text, LanguageConstant.Language sourceLanguage,
            LanguageConstant.Language targetLanguage)
        {
            bool isProxyUse = GetIsUseProxyFromSetting();
            HttpClient? httpClient = isProxyUse ? _networkService.ProxyHttpClient : _networkService.DefaultHttpClient;
            if (httpClient == null)
            {
                throw new Exception("HttpClient is null");
            }

            string apiEndpoint = "api.cognitive.microsofttranslator.com";
            string apiVersion = "3.0";
            string apiUrl =
                $"{apiEndpoint}/translate?api-version={apiVersion}"
                + $"&to={LanguageConstant.GetLanguageCode(targetLanguage)}"
                + $"&from={LanguageConstant.GetLanguageCode(sourceLanguage)}";
            byte[] privateKey =
            [
                0xA2, 0x29, 0x3A, 0x3D, 0xD0, 0xDD, 0x32, 0x73, 0x97, 0x7A, 0x64, 0xDB, 0xC2, 0xF3, 0x27, 0xF5, 0xD7,
                0xBF, 0x87, 0xD9, 0x45, 0x9D, 0xF0, 0x5A, 0x09, 0x66, 0xC6, 0x30, 0xC6, 0x6A, 0xAA, 0x84, 0x9A, 0x41,
                0xAA, 0x94, 0x3A, 0xA8, 0xD5, 0x1A, 0x6E, 0x4D, 0xAA, 0xC9, 0xA3, 0x70, 0x12, 0x35, 0xC7, 0xEB, 0x12,
                0xF6, 0xE8, 0x23, 0x07, 0x9E, 0x47, 0x10, 0x95, 0x91, 0x88, 0x55, 0xD8, 0x17
            ];
            string fullUrl = $"https://{apiUrl}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            requestMessage.Headers.Add("X-MT-Signature", GetSignature(apiUrl, privateKey));
            var data = new List<Dictionary<string, string>>
            {
                new()
                {
                    { "Text", text }
                }
            };
            string jsonString = JsonSerializer.Serialize(data);
            var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            requestMessage.Content = jsonContent;
            HttpResponseMessage response =
                await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"{response.Content}";
            }

            JsonArray? responseJson = await response.Content.ReadFromJsonAsync<JsonArray>();
            string? result = responseJson?[0]?["translations"]?[0]?["text"]?.ToString();
            if (result == null)
                return "";
            return result;
        }

        public PopupWindow GetSettingWindow(Window parent)
        {
            bool isProxyUse = GetIsUseProxyFromSetting();
            BaseSettingPage.SettingParams settingParams = new()
            {
                IsUseProxy = isProxyUse
            };
            Page settingPage = new BaseSettingPage(ref settingParams);
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
                if (settingParams.IsUseProxy != isProxyUse)
                {
                    _setting["IsProxyUse"] = settingParams.IsUseProxy;
                    hasChange = true;
                }

                if (hasChange)
                    await StoreSettingsAsync();
            };
            return window;
        }

        public Task<Result> StoreSettingsAsync()
        {
            return _appConfigProvider.SaveTranslatorProviderConfigAsync(ProviderName, _setting);
        }

        private bool GetIsUseProxyFromSetting()
        {
            bool isProxyUse = true;
            if (_setting.TryGetValue(IS_USE_PROXY_SETTING_STRING, out object? obj))
            {
                if (bool.TryParse(obj.ToString(), out bool parsed))
                {
                    isProxyUse = parsed;
                }
            }

            return isProxyUse;
        }

        private string GetSignature(string requestUrl, byte[] privateKey)
        {
            string guid = Guid.NewGuid().ToString();
            string escapedUrl = Uri.EscapeDataString(requestUrl);
            DateTime utcNow = DateTime.UtcNow;
            string formattedDateTime = utcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", new CultureInfo("en"));
            string baseStr = $"MSTranslatorAndroidApp{escapedUrl}{formattedDateTime}{guid}".ToLower();

            using var hmac = new HMACSHA256(privateKey);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseStr));
            string base64Hash = Convert.ToBase64String(hashBytes);

            string signature = $"MSTranslatorAndroidApp::{base64Hash}::{formattedDateTime}::{guid}";
            return signature;
        }
    }
}