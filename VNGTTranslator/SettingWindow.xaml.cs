using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace VNGTTranslator
{
    /// <summary>
    /// SettingWindow.xaml 的互動邏輯
    /// </summary>
    public sealed partial class SettingWindow : INotifyPropertyChanged
    {
        public SettingWindow()
        {
            InitializeComponent();
            DataContext = this;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
        }

        private Uri _navigateTo = new("SettingPages/About.xaml", UriKind.Relative);

        public Uri NavigateTo
        {
            get => _navigateTo;
            private set => SetField(ref _navigateTo, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SideMenuItemTranslateWindowSetting_OnSelected(object sender, RoutedEventArgs e)
        {
            NavigateTo = new Uri("SettingPages/TranslateWindowSetting.xaml", UriKind.Relative);
        }

        private void SideMenuItemAbout_OnSelected(object sender, RoutedEventArgs e)
        {
            NavigateTo = new Uri("SettingPages/About.xaml", UriKind.Relative);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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

        private void SideMenuItemTranslateSetting_OnSelected(object sender, RoutedEventArgs e)
        {
            NavigateTo = new Uri("SettingPages/TranslateSetting.xaml", UriKind.Relative);
        }

        private void SideMenuItemProxySetting_OnSelected(object sender, RoutedEventArgs e)
        {
            NavigateTo = new Uri("SettingPages/ProxySetting.xaml", UriKind.Relative);
        }

        private void SideMenuItemTTSSetting_OnSelected(object sender, RoutedEventArgs e)
        {
            NavigateTo = new Uri("SettingPages/TTSSetting.xaml", UriKind.Relative);
        }
    }
}