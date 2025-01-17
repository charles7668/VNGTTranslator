﻿using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VNGTTranslator.Configs;
using VNGTTranslator.Models;
using VNGTTranslator.TTSProviders;

namespace VNGTTranslator.SettingPages
{
    /// <summary>
    /// TTSSetting.xaml 的互動邏輯
    /// </summary>
    public partial class TTSSetting : INotifyPropertyChanged, ISaveable
    {
        public TTSSetting()
        {
            _appConfigProvider = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>();
            _appConfig = _appConfigProvider.GetAppConfig();
            IEnumerable<ITTSProvider>? providers =
                Program.ServiceProvider.GetRequiredService<TTSProviderFactory>().TTSProviders;
            TTSProviderFactory factory = Program.ServiceProvider.GetRequiredService<TTSProviderFactory>();
            TTSProviders = providers != null
                ? providers.Select(p => new TTSProviderDataContext(p)).ToList()
                : [];
            TTSProviderDataContext? defaultProvider =
                TTSProviders.FirstOrDefault(x => x.ProviderName == _appConfig.UseTTSProvider) ??
                TTSProviders.FirstOrDefault(x => x.ProviderName == factory.GetProvider(x.ProviderName)?.ProviderName) ??
                TTSProviders.FirstOrDefault();
            if (defaultProvider != null)
            {
                defaultProvider.IsChecked = true;
                ProviderVoices = defaultProvider.ProviderVoices;
                SelectedVoice = defaultProvider.Provider.SelectedVoice;
                _previousSelectedProvider = defaultProvider;
            }

            InitializeComponent();

            DataContext = this;
        }

        private readonly AppConfig _appConfig;

        private readonly IAppConfigProvider _appConfigProvider;

        private TTSProviderDataContext _previousSelectedProvider = null!;

        private List<string> _providerVoices = [];

        private string _testText = "input your test text here";

        public string TestText
        {
            get => _testText;
            set => SetField(ref _testText, value);
        }

        public List<TTSProviderDataContext> TTSProviders { get; }

        public List<string> ProviderVoices
        {
            get => _providerVoices;
            private set => SetField(ref _providerVoices, value);
        }

        public int Speed
        {
            get => _previousSelectedProvider == null! ? 0 : _previousSelectedProvider.Provider.Rate;
            set
            {
                if (_previousSelectedProvider == null!) return;
                _previousSelectedProvider.Provider.Rate = value;
                OnPropertyChanged();
            }
        }

        public int Volume
        {
            get => _previousSelectedProvider == null! ? 0 : _previousSelectedProvider.Provider.Volume;
            set
            {
                if (_previousSelectedProvider == null!) return;
                _previousSelectedProvider.Provider.Volume = value;
                OnPropertyChanged();
            }
        }

        public bool AutoPlayTTS
        {
            get => _appConfig.AutoPlayTTS;
            set
            {
                _appConfig.AutoPlayTTS = value;
                OnPropertyChanged();
            }
        }

        public string SelectedVoice
        {
            get => _previousSelectedProvider == null! ? "" : _previousSelectedProvider.Provider.SelectedVoice;
            set
            {
                if (_previousSelectedProvider == null!) return;
                if (!_previousSelectedProvider.Provider.SetVoice(value))
                    return;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Save()
        {
            Task.Run(async () =>
            {
                await _previousSelectedProvider.Provider.StoreSettingsAsync().ConfigureAwait(false);
            }).Wait();
            _appConfigProvider.TrySaveAppConfig();
        }

        private async void TTSProviderCheck_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton radioButton)
                return;
            if (radioButton.DataContext is not TTSProviderDataContext providerDataContext)
                return;
            await _previousSelectedProvider.Provider.StoreSettingsAsync();
            _previousSelectedProvider = providerDataContext;
            SelectedVoice = providerDataContext.Provider.SelectedVoice;
            ProviderVoices = providerDataContext.ProviderVoices;
            _appConfig.UseTTSProvider = providerDataContext.ProviderName;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        private void TTSSetting_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void NudSpeed_OnValueChanged(object? sender, FunctionEventArgs<double> e)
        {
            if (sender is not NumericUpDown nud) return;
            if (e.Info < nud.Minimum || e.Info > nud.Maximum)
                return;
            Speed = (int)e.Info;
        }

        private void NudVolume_OnValueChanged(object? sender, FunctionEventArgs<double> e)
        {
            if (sender is not NumericUpDown nud) return;
            if (e.Info < nud.Minimum || e.Info > nud.Maximum)
                return;
            Volume = (int)e.Info;
        }

        private void BtnTestVoice_OnClick(object sender, RoutedEventArgs e)
        {
            ITTSProvider provider = _previousSelectedProvider.Provider;
            provider.StopSpeak();
            provider.SpeakAsync(TestText);
        }

        public class TTSProviderDataContext(ITTSProvider provider) : INotifyPropertyChanged
        {
            public readonly ITTSProvider Provider = provider;
            private bool _isChecked;

            public List<string> ProviderVoices { get; } = provider.GetSupportedVoiceList().ToList();

            public string ProviderName => Provider.ProviderName;

            public bool IsChecked
            {
                get => _isChecked;
                set => SetField(ref _isChecked, value);
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;
                field = value;
                OnPropertyChanged(propertyName);
            }
        }
    }
}