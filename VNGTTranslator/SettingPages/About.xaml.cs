using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Navigation;
using VNGTTranslator.Properties;

namespace VNGTTranslator.SettingPages
{
    /// <summary>
    /// About.xaml 的互動邏輯
    /// </summary>
    public partial class About : INotifyPropertyChanged
    {
        public About()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static string VersionText
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version is null
                    ? $"{Localization.Version}: 0.0.0.0"
                    : $"{Localization.Version}: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}