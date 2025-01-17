﻿using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Speech.Synthesis;
using System.Text.Json;
using VNGTTranslator.Configs;
using VNGTTranslator.Models;

namespace VNGTTranslator.TTSProviders.Microsoft
{
    [Export(typeof(ITTSProvider))]
    public class WindowsTTSProvider : ITTSProvider
    {
        public WindowsTTSProvider()
        {
            _speechSynthesizer = new SpeechSynthesizer();
            // Install OneCore voices
            // Windows has two sets of installed voices:
            // 1. HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices
            // 2. HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices
            // 
            // By default, SpeechSynthesizer only accesses the first registry.
            _speechSynthesizer.InjectOneCoreVoices();

            _appConfigProvider = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>();
            ReadOnlyCollection<InstalledVoice>? installedVoices = _speechSynthesizer.GetInstalledVoices();
            Dictionary<string, object> configs = null!;
            Task.Run(async () =>
            {
                configs = await _appConfigProvider.GetTTSProviderConfigAsync(ProviderName).ConfigureAwait(false);
            }).Wait();
            configs ??= [];
            configs.TryGetValue(COMMON_SETTING_KEY, out object? commonSetting);
            if (commonSetting is JsonElement commonSettingJson)
                _commonSetting = commonSettingJson.Deserialize<TTSCommonSetting>() ?? new TTSCommonSetting();
            else
                _commonSetting = (TTSCommonSetting?)commonSetting ?? new TTSCommonSetting();
            configs.TryGetValue(OTHER_SETTING_KEY, out object? otherSettings);
            if (otherSettings is JsonElement otherSettingJson)
                _otherSettings = otherSettingJson.Deserialize<Dictionary<string, object>>() ?? [];
            else
                _otherSettings = (Dictionary<string, object>?)otherSettings ?? [];

            foreach (InstalledVoice installedVoice in installedVoices)
            {
                _installedVoices.Add(installedVoice.VoiceInfo.Description, installedVoice);
            }

            // set default voice if not exist
            SetVoice(string.IsNullOrEmpty(_commonSetting.SelectedVoice)
                ? installedVoices[0].VoiceInfo.Description
                : _commonSetting.SelectedVoice);
            Rate = _commonSetting.Rate;
            Volume = _commonSetting.Volume;
        }

        private const string COMMON_SETTING_KEY = "commonSetting";
        private const string OTHER_SETTING_KEY = "otherSettings";
        private readonly IAppConfigProvider _appConfigProvider;

        private readonly TTSCommonSetting _commonSetting;

        private readonly Dictionary<string, InstalledVoice> _installedVoices = [];

        private readonly Dictionary<string, object> _otherSettings;

        private readonly SpeechSynthesizer _speechSynthesizer;

        public string ProviderName { get; } = "WindowsTTS";

        public string SelectedVoice => _commonSetting.SelectedVoice;

        public int Rate
        {
            get => _commonSetting.Rate;
            set
            {
                if (value is < -10 or > 10)
                    return;
                _speechSynthesizer.Rate = value;
                _commonSetting.Rate = value;
            }
        }

        public int Volume
        {
            get => _commonSetting.Volume;
            set
            {
                if (value is < 0 or > 100)
                    return;
                _speechSynthesizer.Volume = value;
                _commonSetting.Volume = value;
            }
        }

        public IEnumerable<string> GetSupportedVoiceList()
        {
            ReadOnlyCollection<InstalledVoice>? installedVoices = _speechSynthesizer.GetInstalledVoices();
            return installedVoices.Select(voice => voice.VoiceInfo.Description);
        }

        public Result SetVoice(string voiceName)
        {
            if (string.IsNullOrWhiteSpace(voiceName))
                return Result.Success();
            if (!_installedVoices.TryGetValue(voiceName, out InstalledVoice? installedVoice))
                return Result.Fail($"{voiceName} not exist");
            _speechSynthesizer.SelectVoice(installedVoice.VoiceInfo.Name);
            _commonSetting.SelectedVoice = voiceName;
            return Result.Success();
        }

        public Task SpeakAsync(string text)
        {
            _speechSynthesizer.SpeakAsync(text);
            return Task.CompletedTask;
        }

        public Task<Result> StoreSettingsAsync()
        {
            Dictionary<string, object> configs = new()
            {
                [COMMON_SETTING_KEY] = _commonSetting,
                [OTHER_SETTING_KEY] = _otherSettings
            };
            return _appConfigProvider.SaveTTSProviderConfigAsync(ProviderName, configs);
        }

        public void StopSpeak()
        {
            _speechSynthesizer.SpeakAsyncCancelAll();
        }
    }
}