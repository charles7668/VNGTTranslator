using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VNGTTranslator.OCRProviders.SettingPages
{
    /// <summary>
    /// TesseractOCRSetting.xaml 的互動邏輯
    /// </summary>
    public partial class TesseractOCRSetting : INotifyPropertyChanged
    {
        public TesseractOCRSetting(ref SettingParams settingParams)
        {
            _settingParams = settingParams;
            InitializeComponent();
            DataContext = this;
        }

        private readonly SettingParams _settingParams;

        public string TesseractPath
        {
            get => _settingParams.TesseractPath;
            set
            {
                _settingParams.TesseractPath = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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


        public class SettingParams
        {
            public string TesseractPath { get; set; } = "";
        }
    }
}