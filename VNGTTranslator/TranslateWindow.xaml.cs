using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Drawing;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using VNGTTranslator.Configs;
using VNGTTranslator.Enums;
using VNGTTranslator.Helper;
using VNGTTranslator.Hooker;
using VNGTTranslator.LunaHook;
using VNGTTranslator.Models;
using VNGTTranslator.OCRProviders;
using VNGTTranslator.TranslateProviders;
using VNGTTranslator.TTSProviders;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Localization = VNGTTranslator.Properties.Localization;
using MessageBox = System.Windows.MessageBox;
using TextBox = HandyControl.Controls.TextBox;

namespace VNGTTranslator
{
    /// <summary>
    /// Interaction logic for TranslateWindow.xaml
    /// </summary>
    public partial class TranslateWindow : INotifyPropertyChanged
    {
        public TranslateWindow()
        {
            InitializeComponent();
            DataContext = this;
            _hooker = Program.ServiceProvider.GetRequiredService<IHooker>();
            _hooker.OnHookTextReceived += OnHookerTextReceived;
            _appConfig = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>().GetAppConfig();
            _ttsProviderFactory = Program.ServiceProvider.GetRequiredService<TTSProviderFactory>();
            _ocrProviderFactory = Program.ServiceProvider.GetRequiredService<OCRProviderFactory>();
            UpdateTTSProvider();
            _translateTimeoutTimer = new Timer(TranslateTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            RefreshDisplayUI();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            // listen to process start and stop
            if (Program.BaseExecutionPath != null)
                StartUpProcessMonitor();
        }

        private readonly AppConfig _appConfig;

        private readonly DropShadowEffect _dropShadowEffect = new()
        {
            Opacity = 1,
            ShadowDepth = 0,
            BlurRadius = 6
        };

        private readonly StringBuilder _history = new();

        private readonly IHooker _hooker;

        private readonly OCRProviderFactory _ocrProviderFactory;

        private readonly Timer _translateTimeoutTimer;

        private readonly TTSProviderFactory _ttsProviderFactory;

        private bool _isShowSourceText = true;

        private bool _isTransparent;

        private Timer? _ocrRefreshTimer;
        private bool _pauseState;

        private DateTime _previousTranslateTime = new(2000, 01, 01);
        private CancellationTokenSource _processMonitorCancellationTokenSource = new();

        private Thread _processMonitorThread = null!;

        private FontFamily _sourceFontFamily = new("Arial");

        private uint _sourceFontSize = 15;
        private string _sourceText = Localization.TranslateWindow_Text_PleaseSelectHookItem;
        private Color _sourceTextColor = Colors.White;

        private Effect? _sourceTextEffect;

        private ITTSProvider? _ttsProvider;

        private List<TranslateProviderDataContext> _useTranslateProviderDataContexts = [];

        public bool IsHookMode => Program.Mode == Mode.HOOK_MODE;

        public Effect? SourceTextEffect
        {
            get => _sourceTextEffect;
            set => SetField(ref _sourceTextEffect, value);
        }

        public Brush SourceTextColor
        {
            get => new SolidColorBrush(_sourceTextColor);
            set
            {
                _sourceTextColor = ((SolidColorBrush)value).Color;
                OnPropertyChanged();
            }
        }

        public bool IsShowSourceText
        {
            get => _isShowSourceText;
            set => SetField(ref _isShowSourceText, value);
        }

        public FontFamily SourceFontFamily
        {
            get => _sourceFontFamily;
            set => SetField(ref _sourceFontFamily, value);
        }

        public uint SourceFontSize
        {
            get => _sourceFontSize;
            set => SetField(ref _sourceFontSize, value);
        }

        public bool PauseState
        {
            get => _pauseState;
            set => SetField(ref _pauseState, value);
        }

        public bool IsTransparent
        {
            get => _isTransparent;
            set => SetField(ref _isTransparent, value);
        }

        public string SourceText
        {
            get => _sourceText;
            private set => SetField(ref _sourceText, value);
        }

        public List<TranslateProviderDataContext> UseTranslateProviderDataContexts
        {
            get => _useTranslateProviderDataContexts;
            private set
            {
                _useTranslateProviderDataContexts = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void BtnAllowSizeChange_OnClick(object sender, RoutedEventArgs e)
        {
            SetAllowChangeSize(((ToggleButton)sender).IsChecked ?? false);
        }

        private void BtnExit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnHistory_OnClick(object sender, RoutedEventArgs e)
        {
            var textbox = new TextBox
            {
                Text = _history.ToString(),
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Left,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Visible
            };
            var window = new PopupWindow
            {
                PopupElement = textbox,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                BorderThickness = new Thickness(0, 0, 0, 0),
                MaxWidth = 600,
                MaxHeight = 300,
                MinWidth = 600,
                MinHeight = 300,
                Owner = this,
                Title = Localization.HistoryWindow_Text_Title
            };
            _hooker.OnHookTextReceived -= OnHookerTextReceived;
            window.Topmost = true;
            window.ShowDialog();
            _hooker.OnHookTextReceived += OnHookerTextReceived;
        }

        private void BtnLock_OnClick(object sender, RoutedEventArgs e)
        {
            SetWindowColor();
        }

        private void BtnMinimizeWindow_OnClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnPause_OnClick(object sender, RoutedEventArgs e)
        {
            if (!PauseState)
            {
                _hooker.OnHookTextReceived -= OnHookerTextReceived;
                _ocrRefreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                PauseState = true;
            }
            else
            {
                _hooker.OnHookTextReceived += OnHookerTextReceived;
                _ocrRefreshTimer?.Change(0,
                    Math.Max(TimeSpan.FromMilliseconds(100).Milliseconds, _appConfig.TranslateInterval));
                PauseState = false;
            }
        }

        private void BtnReadAloud_OnClick(object sender, RoutedEventArgs e)
        {
            if (_ttsProvider == null)
            {
                MessageBox.Show(Localization.Error_NoTTSCanUse, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _ttsProvider.StopSpeak();
            _ttsProvider.SpeakAsync(SourceText);
        }

        private async void BtnReTranslate_OnClick(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll(UseTranslateProviderDataContexts.Select(context => context.Translate(SourceText))
                .ToArray());
        }

        private void BtnSelectHookCode_OnClick(object sender, RoutedEventArgs e)
        {
            HookSelectWindow hookSelectWindow = new();
            _hooker.OnHookTextReceived -= OnHookerTextReceived;
            hookSelectWindow.ShowDialog();
            if (Program.SelectedHookItem != null)
                SourceText = Localization.TranslateWindow_Text_WaitSourceText;
            _hooker.OnHookTextReceived += OnHookerTextReceived;
        }

        private void BtnSelectProcess_OnClick(object sender, RoutedEventArgs e)
        {
            ProcessSelectWindow processSelectWindow = new()
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            _hooker.OnHookTextReceived -= OnHookerTextReceived;
            Mode recordMode = Program.Mode;
            processSelectWindow.ShowDialog();
            _hooker.OnHookTextReceived += OnHookerTextReceived;
            if (recordMode != Program.Mode)
            {
                OnPropertyChanged(nameof(IsHookMode));
            }

            if (!IsHookMode)
            {
                _ocrRefreshTimer ??= new Timer(OCRRefreshTimerCallback, null, 0,
                    Math.Max(TimeSpan.FromMilliseconds(100).Milliseconds, _appConfig.TranslateInterval));
            }
            else
            {
                _ocrRefreshTimer?.Dispose();
                _ocrRefreshTimer = null;
            }
        }

        private void BtnSetting_OnClick(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new()
            {
                Owner = this
            };
            _hooker.OnHookTextReceived -= OnHookerTextReceived;
            settingWindow.ShowDialog();
            _hooker.OnHookTextReceived += OnHookerTextReceived;
            RefreshDisplayUI();
            UpdateTTSProvider();
        }

        private void BtnShowSourceText_OnClick(object sender, RoutedEventArgs e)
        {
            IsShowSourceText = !IsShowSourceText;
        }

        private void BtnToggleOnTop_OnClick(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }

        private async void OCRRefreshTimerCallback(object? state)
        {
            OCRSetting ocrSetting = Program.OCRSetting;
            var rectangle = new Rectangle(0, 0, 0, 0);
            if (ocrSetting.OCRArea != null)
                rectangle = ocrSetting.OCRArea.Value;

            Bitmap? image = ImageHelper.GetWindowRectCapture(ocrSetting.WinHandle, rectangle, ocrSetting.IsUseScreen);
            if (image == null)
            {
                SourceText = Localization.Error_CanNotCaptureImage;
                return;
            }

            IOCRProvider? ocrProvider = _ocrProviderFactory.GetProvider(_appConfig.UseOCRProvider);
            if (ocrProvider == null)
            {
                SourceText = Localization.Error_NoOCRCanUse;
                return;
            }

            Result setLanguageResult = ocrProvider.SetOcrLanguage(Program.OCRSetting.OCRLang);
            if (!setLanguageResult)
            {
                SourceText = setLanguageResult.ErrorMessage;
                return;
            }

            Result<string> recognizeTextResult =
                await ocrProvider.RecognizeTextAsync(image, Program.OCRSetting.PreProcessFunc);
            if (!recognizeTextResult)
            {
                SourceText = recognizeTextResult.ErrorMessage;
                return;
            }

            SourceText = recognizeTextResult.Value ?? "";
            await TranslateAsync();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!PauseState)
                _hooker.OnHookTextReceived -= OnHookerTextReceived;
            _processMonitorCancellationTokenSource.Cancel();
            while (_processMonitorThread.IsAlive)
            {
                Thread.Sleep(10);
            }

            base.OnClosing(e);
        }

        private async Task OnHookerTextReceived(HookTextReceivedEventArgs e)
        {
            if (!IsHookMode)
                return;
            var data = new ReceivedHookData
            {
                Ctx = e.Ctx,
                Ctx2 = e.Ctx2,
                HookFunc = e.HookFunc,
                Data = e.Text,
                HookCode = e.HookCode
            };
            if (Program.SelectedHookItem == null || data.DisplayHookCode != Program.SelectedHookItem.DisplayHookCode)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SourceText = e.Text;
                // clear text
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (TranslateProviderDataContext context in UseTranslateProviderDataContexts)
                    {
                        context.TranslatedText = string.Empty;
                    }
                });

                TimeSpan diffTime = DateTime.Now - _previousTranslateTime;
                if (diffTime < TimeSpan.FromMilliseconds(_appConfig.TranslateInterval))
                {
                    return;
                }

                _translateTimeoutTimer.Change(diffTime.Milliseconds, Timeout.Infinite);
            });
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject?)e.NewEvent?.Properties["TargetInstance"]?.Value;
            string? exePath = (string?)instance?.Properties["ExecutablePath"]?.Value;
            if (Program.PID != 0 || exePath == null || !exePath.StartsWith(Program.BaseExecutionPath!))
                return;
            int handle = int.Parse((string?)instance?.Properties["Handle"]?.Value ?? "0");
            if (handle == 0)
                return;
            _hooker.Inject(Program.PID, LunaDll.LunaHookDllPath);
            Program.PID = (uint)handle;
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject?)e.NewEvent?.Properties["TargetInstance"]?.Value;
            int handle = int.Parse((string?)instance?.Properties["Handle"]?.Value ?? "0");
            if (handle == 0 || handle != Program.PID)
                return;
            _hooker.Detach(Program.PID);
            Program.PID = 0;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RefreshDisplayUI()
        {
            SetTranslateProviderDataContext();
            SetWindowColor();
            SetFontStyle();
        }

        /// <summary>
        /// Set allow change size or not
        /// </summary>
        /// <param name="isAllow"></param>
        private void SetAllowChangeSize(bool isAllow)
        {
            if (isAllow)
            {
                BorderThickness = new Thickness(3);
                ResizeMode = ResizeMode.CanResizeWithGrip;
            }
            else
            {
                BorderThickness = new Thickness(0);
                ResizeMode = ResizeMode.NoResize;
            }
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        private void SetFontStyle()
        {
            SourceFontFamily = new FontFamily(_appConfig.SourceTextStyle.FontFamily);
            SourceFontSize = _appConfig.SourceTextStyle.FontSize;
            SourceTextColor = new SolidColorBrush(_appConfig.SourceTextStyle.TextColor);
            SourceTextEffect = _appConfig.SourceTextStyle.IsTextShadowEnabled ? _dropShadowEffect : null;
        }

        private void SetTranslateProviderDataContext()
        {
            TranslateProviderFactory translateProviderFactory =
                Program.ServiceProvider.GetRequiredService<TranslateProviderFactory>();
            var tempList = _appConfig.UsedTranslateProviderSet.Select(s =>
                new TranslateProviderDataContext(translateProviderFactory.GetProvider(s), _appConfig)).ToList();
            UseTranslateProviderDataContexts = tempList;
        }

        /// <summary>
        /// set background transparent or not
        /// </summary>
        private void SetWindowColor()
        {
            Background = IsTransparent
                ? new SolidColorBrush(Colors.Transparent)
                : new SolidColorBrush(_appConfig.TranslateWindowColor);
        }

        private void StartUpProcessMonitor()
        {
            _processMonitorCancellationTokenSource = new CancellationTokenSource();
            _processMonitorThread = new Thread(() =>
            {
                string queryString =
                    "SELECT * FROM __InstanceCreationEvent WITHIN .025 WHERE TargetInstance ISA 'Win32_Process'";
                var startWatch = new ManagementEventWatcher(@"\\.\root\CIMV2", queryString);
                startWatch.EventArrived += OnProcessStarted;
                startWatch.Start();
                queryString =
                    "SELECT * FROM __InstanceDeletionEvent WITHIN .025 WHERE TargetInstance ISA 'Win32_Process'";
                var stopWatch = new ManagementEventWatcher(@"\\.\root\CIMV2", queryString);
                stopWatch.EventArrived += OnProcessStopped;
                stopWatch.Start();
                while (!_processMonitorCancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }

                startWatch.Stop();
                stopWatch.Stop();
            });
            _processMonitorThread.Start();
        }

        private async Task TranslateAsync()
        {
            _history.AppendLine(SourceText);
            await Task.WhenAll(UseTranslateProviderDataContexts.Select(context =>
                context.Translate(SourceText)).ToArray());
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _previousTranslateTime = DateTime.Now;
                foreach (TranslateProviderDataContext context in UseTranslateProviderDataContexts)
                {
                    _history.AppendLine(context.TranslatedText);
                }

                _history.AppendLine();
            });
            _history.AppendLine("----------------------"); // line separator
        }

        private async void TranslateTimerCallback(object? state)
        {
            await TranslateAsync();
        }

        private void UpdateTTSProvider()
        {
            ITTSProvider? provider = _ttsProviderFactory.GetProvider(_appConfig.UseTTSProvider ?? "")
                                     ?? _ttsProviderFactory.GetProvider("WindowsTTS");
            _ttsProvider = provider;
        }

        public class TranslateProviderDataContext : INotifyPropertyChanged
        {
            public TranslateProviderDataContext(ITranslateProvider provider, AppConfig appConfig)
            {
                Provider = provider;
                _appConfig = appConfig;
                _providerStyle = appConfig.TranslateTextStyles.TryGetValue(provider.ProviderName,
                    out DisplayTextStyle displayTextStyle)
                    ? displayTextStyle
                    : new DisplayTextStyle();
            }

            private readonly AppConfig _appConfig;

            private readonly DisplayTextStyle _providerStyle;

            private string _translatedText = string.Empty;

            private ITranslateProvider Provider { get; }

            public Brush ProviderTextColor => new SolidColorBrush(_providerStyle.TextColor);

            public uint ProviderTextSize => _providerStyle.FontSize;

            public FontFamily ProviderFontFamily => new(_providerStyle.FontFamily);

            public Effect? ProviderTextEffect =>
                _providerStyle.IsTextShadowEnabled
                    ? new DropShadowEffect
                    {
                        Opacity = 1,
                        ShadowDepth = 0,
                        BlurRadius = 6
                    }
                    : null;

            public string TranslatedText
            {
                get => _translatedText;
                set
                {
                    _translatedText = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public async Task Translate(string text)
            {
                string translated = await Provider.TranslateAsync(text
                    , _appConfig.SourceLanguage
                    , _appConfig.TargetLanguage);
                TranslatedText = translated;
            }
        }
    }
}