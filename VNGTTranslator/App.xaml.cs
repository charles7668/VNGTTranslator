using HandyControl.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Windows;
using VNGTTranslator.Hooker;
using VNGTTranslator.LunaHook;

namespace VNGTTranslator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private IHooker _hooker = null!;

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _hooker.Detach(Program.PID);
            }
            catch (Exception)
            {
                // ignored
            }

            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Program.InitServices();

            ConfigHelper.Instance.SetLang("en");

            var culture = new CultureInfo("zh-tw");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            base.OnStartup(e);

            Program.PID = 0;
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length >= 2)
            {
                if (uint.TryParse(commandLineArgs[1], out uint pid))
                    Program.PID = pid;
                else
                {
                    MessageBox.Show("Invalid PID arg", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(-1);
                    return;
                }
            }

            _hooker = Program.ServiceProvider.GetRequiredService<IHooker>();
            _hooker.Start();

            if (Program.PID == 0)
                return;
            try
            {
                _hooker.Inject(Program.PID, LunaDll.LunaHookDllPath);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Inject failed : {exc.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }
    }
}