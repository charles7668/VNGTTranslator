using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using VNGTTranslator.Configs;
using VNGTTranslator.Models;
using VNGTTranslator.OCRProviders;
using Window = System.Windows.Window;

namespace VNGTTranslator.SettingPages
{
    /// <summary>
    /// OCRSetting.xaml 的互動邏輯
    /// </summary>
    public partial class OCRSetting : INotifyPropertyChanged, ISaveable
    {
        public OCRSetting()
        {
            InitializeComponent();
            DataContext = this;
            _serviceProvider = Program.ServiceProvider;
            _appConfig = _serviceProvider.GetRequiredService<IAppConfigProvider>().GetAppConfig();
            OCRProviderList =
                _serviceProvider.GetRequiredService<OCRProviderFactory>().OCRProviders!.Select(x =>
                    new OCRProviderContext(x, _appConfig)).ToList();
            _previousSelected = OCRProviderList.FirstOrDefault(x => x.IsChecked)!;
        }

        private readonly AppConfig _appConfig;


        private readonly IServiceProvider _serviceProvider;

        private OCRProviderContext _previousSelected;

        public List<OCRProviderContext> OCRProviderList { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Save()
        {
            string? selectedProvider = OCRProviderList.FirstOrDefault(x => x.IsChecked)?.ProviderName;
            if (selectedProvider == null)
                return;
            _appConfig.UseOCRProvider = selectedProvider;
            _serviceProvider.GetRequiredService<IAppConfigProvider>().TrySaveAppConfig();
        }

        private async void BtnOCRProviderSetting_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;
            if (button.DataContext is not OCRProviderContext context)
                return;
            if (!context.IsSettingSupport)
                return;
            PopupWindow popUpWindow = await context.GetSettingWindow(Window.GetWindow(this)!);
            popUpWindow.ShowDialog();
        }

        private void BtnProviderCheck_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;
            if (button.DataContext is not OCRProviderContext context)
                return;
            _previousSelected.IsChecked = false;
            context.IsChecked = true;
            _previousSelected = context;
        }

        private void OCRSetting_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Save();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        public class OCRProviderContext : INotifyPropertyChanged
        {
            public OCRProviderContext(IOCRProvider provider, AppConfig appConfig)
            {
                _provider = provider;
                _isChecked = appConfig.UseOCRProvider == provider.ProviderName;
            }

            private readonly IOCRProvider _provider;

            private bool _isChecked;

            public bool IsChecked
            {
                get => _isChecked;
                set => SetField(ref _isChecked, value);
            }

            public string ProviderName => _provider.ProviderName;

            public bool IsSettingSupport => _provider.SupportSetting;

            public event PropertyChangedEventHandler? PropertyChanged;

            public Task<PopupWindow> GetSettingWindow(Window parent)
            {
                return _provider.GetSettingWindowAsync(parent);
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
        }
    }
}