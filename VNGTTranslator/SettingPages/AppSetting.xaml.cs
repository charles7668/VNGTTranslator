using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using VNGTTranslator.Configs;
using VNGTTranslator.Enums;
using VNGTTranslator.Models;

namespace VNGTTranslator.SettingPages
{
    /// <summary>
    /// AppSetting.xaml 的互動邏輯
    /// </summary>
    public partial class AppSetting : INotifyPropertyChanged, ISaveable
    {
        public AppSetting()
        {
            InitializeComponent();
            IServiceProvider serviceProvider = Program.ServiceProvider;
            _appConfigProvider = serviceProvider.GetRequiredService<IAppConfigProvider>();
            AppConfig appConfig = _appConfigProvider.GetAppConfig();
            _selectedLanguage = appConfig.AppDisplayLanguage;
            DataContext = this;
        }

        private readonly IAppConfigProvider _appConfigProvider;

        private Language _selectedLanguage;

        public List<Language> DisplayLanguages { get; set; } =
        [
            Enums.Language.CHINESE_TRADITIONAL,
            Enums.Language.CHINESE_SIMPLIFIED,
            Enums.Language.ENGLISH
        ];

        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetField(ref _selectedLanguage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Save()
        {
            AppConfig appConfig = _appConfigProvider.GetAppConfig();
            appConfig.AppDisplayLanguage = SelectedLanguage;
            _appConfigProvider.TrySaveAppConfig();
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

        private void AppSetting_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}