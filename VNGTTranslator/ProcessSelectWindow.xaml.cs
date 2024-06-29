using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VNGTTranslator.Configs;
using VNGTTranslator.Enums;
using VNGTTranslator.Helper;
using VNGTTranslator.Hooker;
using VNGTTranslator.LunaHook;
using VNGTTranslator.Models;
using VNGTTranslator.OCRProviders;
using Win32ApiLibrary;
using Windows.System;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;

namespace VNGTTranslator
{
    public partial class ProcessSelectWindow : INotifyPropertyChanged
    {
        public ProcessSelectWindow()
        {
            ProcessList = GetProcessList();
            InitializeComponent();
            Topmost = true;
            SelectedProcess = ProcessList.FirstOrDefault(x => x.EndsWith(Program.PID.ToString()));
            _selectedMode = Program.Mode;
            _isUseScreen = Program.OCRSetting.IsUseScreen;
            _ocrArea = Program.OCRSetting.OCRArea;
            _selectedHwnd = Program.OCRSetting.WinHandle;
            _selectedOCRLanguage = Program.OCRSetting.OCRLang;
            _selectedPreProcessFunc = Program.OCRSetting.PreProcessFunc;
            DataContext = this;
        }

        private string _currentOCRTargetWindow = "";

        private bool _isUseScreen;

        private Rectangle? _ocrArea;

        private BitmapImage? _previewImage;

        private Mode _selectedMode;

        private string? _selectedOCRLanguage;

        private ImagePreProcessFunction _selectedPreProcessFunc;

        private string? _selectedProcess;

        public string? SelectedOCRLanguage
        {
            get => _selectedOCRLanguage;
            set => SetField(ref _selectedOCRLanguage, value);
        }

        public List<string> OCRLangList { get; set; } =
        [
            "en",
            "ja",
            "zh-tw",
            "zh-cn"
        ];

        public ImagePreProcessFunction SelectedPreProcessFunc
        {
            get => _selectedPreProcessFunc;
            set => SetField(ref _selectedPreProcessFunc, value);
        }

        public List<ImagePreProcessFunction> PreProcessFuncList { get; set; } =
        [
            ImagePreProcessFunction.NONE,
            ImagePreProcessFunction.OTSU_ALGORITHM
        ];

        public bool IsUseScreen
        {
            get => _isUseScreen;
            set => SetField(ref _isUseScreen, value);
        }

        public string DisplayCurrentOcrTarget
        {
            get => "Target Window: " + _currentOCRTargetWindow;
            private set => SetField(ref _currentOCRTargetWindow, value);
        }

        public List<string> ProcessList { get; }

        public string? SelectedProcess
        {
            get => _selectedProcess;
            set => SetField(ref _selectedProcess, value);
        }

        public int SelectedMode
        {
            get => (int)_selectedMode;
            set => SetField(ref _selectedMode, (Mode)value);
        }

        public BitmapImage? PreviewImage
        {
            get => _previewImage;
            private set => SetField(ref _previewImage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            RefreshCapture();
        }

        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            Program.Mode = _selectedMode;
            IHooker hooker = Program.ServiceProvider.GetRequiredService<IHooker>();
            try
            {
                hooker.Detach(Program.PID);
            }
            catch (Exception)
            {
                // ignore
            }

            if (_selectedMode == Mode.HOOK_MODE)
            {
                string? pidString = SelectedProcess?.Split("—").Last();
                if (!uint.TryParse(pidString, out uint pid))
                {
                    MessageBox.Show("please select a process");
                    return;
                }

                hooker.Inject(pid, LunaDll.LunaHookDllPath);
                Program.PID = pid;
            }
            else
            {
                Program.OCRSetting.IsUseScreen = _isUseScreen;
                Program.OCRSetting.OCRArea = _ocrArea;
                Program.OCRSetting.WinHandle = _selectedHwnd;
                Program.OCRSetting.OCRLang = _selectedOCRLanguage ?? "en";
                Program.OCRSetting.PreProcessFunc = _selectedPreProcessFunc;
            }

            Close();
        }

        #region BtnSelectRegionEvents

        private void BtnSelectRegion_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsUseScreen && _selectedHwnd == IntPtr.Zero)
                return;
            BitmapImage? img = ImageHelper.ImageToBitmapImage(IsUseScreen
                ? ImageHelper.CaptureFullWindow()
                : ImageHelper.CaptureWindowByHandle(_selectedHwnd));
            if (img == null) return;
            var captureWindow = new ScreenCaptureWindow(img)
            {
                Width = img.Width,
                Height = img.Height,
                Topmost = true,
                Top = 0,
                Left = 0
            };
            captureWindow.ShowDialog();
            _ocrArea = captureWindow.OCRArea;
            RefreshCapture();
        }

        #endregion

        private async void BtnTestOCR_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Please select a process", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_ocrArea == null)
            {
                MessageBox.Show("Please select a region", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OCRProviderFactory ocrProviderFactor = Program.ServiceProvider.GetRequiredService<OCRProviderFactory>();
            string useProviderName = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>().GetAppConfig()
                .UseOCRProvider;
            IOCRProvider? ocrProvider = ocrProviderFactor.GetProvider(useProviderName);
            if (ocrProvider == null)
            {
                throw new Exception("OCR provider not found");
            }

            Bitmap? image = ImageHelper.GetWindowRectCapture(_selectedHwnd, (Rectangle)_ocrArea, _isUseScreen);
            if (image == null)
            {
                MessageBox.Show("Cannot capture image", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Result setLanguageResult = ocrProvider.SetOcrLanguage(SelectedOCRLanguage ?? "en");
            if (!setLanguageResult)
            {
                MessageBox.Show(setLanguageResult.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (ocrProvider is WindowOCRProvider)
                    Launcher.LaunchUriAsync(new Uri("ms-settings:regionlanguage-adddisplaylanguage"));
                return;
            }

            Result<string> recognizeTextResult = await ocrProvider.RecognizeTextAsync(image, SelectedPreProcessFunc);
            if (!recognizeTextResult)
            {
                MessageBox.Show(recognizeTextResult.ErrorMessage, "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(recognizeTextResult.Value);
        }

        private static List<string> GetProcessList()
        {
            var processList = new List<string>();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    string info = p.ProcessName + "—" + p.Id;
                    processList.Add(info);
                }

                p.Dispose();
            }

            return processList;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RefreshCapture()
        {
            if (_ocrArea == null) return;
            PreviewImage = ImageHelper.ImageToBitmapImage(
                ImageHelper.GetWindowRectCapture(_selectedHwnd, (Rectangle)_ocrArea, _isUseScreen));
            GC.Collect();
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }


        #region BtnDragToTargetEvents

        private bool _isStartDrag;

        private IntPtr _selectedHwnd;

        private void BtnDragToTarget_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                _isStartDrag = true;
        }

        private void BtnDragToTarget_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isStartDrag) return;
            _isStartDrag = false;
            Point tempPos = e.GetPosition(this);
            Point pos = PointToScreen(tempPos);
            _selectedHwnd =
                Win32Wrapper.GetWindowHwnd(new System.Drawing.Point((int)pos.X, (int)pos.Y));
            string gameName = Win32Wrapper.GetWindowName(_selectedHwnd);
            uint pid = Win32Wrapper.GetProcessIdByHwnd(_selectedHwnd);
            string className = Win32Wrapper.GetWindowClassName(_selectedHwnd);

            if (Process.GetCurrentProcess().Id != pid)
            {
                DisplayCurrentOcrTarget = $"{gameName} - {pid} - {className}";
            }
        }

        #endregion
    }
}