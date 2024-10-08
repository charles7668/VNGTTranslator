﻿using System.Windows.Media;
using VNGTTranslator.Enums;
using VNGTTranslator.Models;
using VNGTTranslator.TranslateProviders;

namespace VNGTTranslator.Configs
{
    public class AppConfig
    {
        public Language AppDisplayLanguage { get; set; } = Language.CHINESE_TRADITIONAL;

        public uint MaxTranslateWordCount { get; set; } = 1000;

        public Color TranslateWindowColor { get; set; } = Color.FromArgb(128, 0, 0, 0);

        public DisplayTextStyle SourceTextStyle { get; set; } = new();

        public Dictionary<string, DisplayTextStyle> TranslateTextStyles { get; set; } = new();

        public HashSet<string> UsedTranslateProviderSet { get; set; } = new();

        public LanguageConstant.Language SourceLanguage { get; set; } = LanguageConstant.Language.JAPANESE;

        public LanguageConstant.Language TargetLanguage { get; set; } = LanguageConstant.Language.CHINESE;

        public uint TranslateInterval { get; set; } = 1000;

        public bool IsUseProxy { get; set; } = true;
        public bool IsUseSystemProxy { get; set; } = true;
        public string ProxyAddress { get; set; } = "";
        public string ProxyPort { get; set; } = "";
        public string? UseTTSProvider { get; set; }
        public bool AutoPlayTTS { get; set; }
        public string UseOCRProvider { get; set; } = "WindowOCR";

        public AppConfig Clone()
        {
            return new AppConfig
            {
                TranslateWindowColor = TranslateWindowColor,
                SourceTextStyle = SourceTextStyle,
                TranslateTextStyles = new Dictionary<string, DisplayTextStyle>(TranslateTextStyles),
                TranslateInterval = TranslateInterval,
                TargetLanguage = TargetLanguage,
                SourceLanguage = SourceLanguage,
                UsedTranslateProviderSet = [..UsedTranslateProviderSet],
                MaxTranslateWordCount = MaxTranslateWordCount,
                IsUseProxy = IsUseProxy,
                IsUseSystemProxy = IsUseSystemProxy,
                ProxyAddress = ProxyAddress,
                ProxyPort = ProxyPort,
                UseTTSProvider = UseTTSProvider,
                AutoPlayTTS = AutoPlayTTS,
                UseOCRProvider = UseOCRProvider,
                AppDisplayLanguage = AppDisplayLanguage
            };
        }

        public void Update(AppConfig appConfig)
        {
            if (appConfig == this) return;
            TranslateWindowColor = appConfig.TranslateWindowColor;
            SourceTextStyle = appConfig.SourceTextStyle;
            TranslateTextStyles = new Dictionary<string, DisplayTextStyle>(appConfig.TranslateTextStyles);
            TranslateInterval = appConfig.TranslateInterval;
            TargetLanguage = appConfig.TargetLanguage;
            SourceLanguage = appConfig.SourceLanguage;
            UsedTranslateProviderSet = [..appConfig.UsedTranslateProviderSet];
            MaxTranslateWordCount = appConfig.MaxTranslateWordCount;
            IsUseProxy = appConfig.IsUseProxy;
            IsUseSystemProxy = appConfig.IsUseSystemProxy;
            ProxyAddress = appConfig.ProxyAddress;
            ProxyPort = appConfig.ProxyPort;
            UseTTSProvider = appConfig.UseTTSProvider;
            AutoPlayTTS = appConfig.AutoPlayTTS;
            UseOCRProvider = appConfig.UseOCRProvider;
            AppDisplayLanguage = appConfig.AppDisplayLanguage;
        }
    }
}