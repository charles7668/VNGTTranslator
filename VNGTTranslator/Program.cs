using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VNGTTranslator.Configs;
using VNGTTranslator.Enums;
using VNGTTranslator.Hooker;
using VNGTTranslator.Models;
using VNGTTranslator.Network;
using VNGTTranslator.OCRProviders;
using VNGTTranslator.TranslateProviders;
using VNGTTranslator.TTSProviders;

namespace VNGTTranslator
{
    public static class Program
    {
        public static uint PID { get; set; }

        /// <summary>
        /// This path is for using PID arguments on startup.
        /// When the process with the matching PID closes, it will then listen to a new process in the same directory.
        /// </summary>
        public static string? BaseExecutionPath { get; set; }

        public static Mode Mode { get; set; } = Mode.HOOK_MODE;

        public static OCRSetting OCRSetting { get; set; } = new();

        public static ReceivedHookData? SelectedHookItem { get; set; }

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static void InitServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IHooker, LunaHooker>();
            services.AddSingleton<IAppConfigProvider, AppConfigProvider>(_ =>
            {
#if DEBUG
                // if debug mode , use configs folder in project directory
                // you could also use string.Empty to use memory storage
                return new AppConfigProvider(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs",
                    "appconf.json"));
#else
                return new AppConfigProvider(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VNGTTranslator" , "configs" , "appconf.json"));
#endif
            });
            services.AddSingleton<TranslateProviderFactory, TranslateProviderFactory>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<TTSProviderFactory, TTSProviderFactory>();
            services.AddSingleton<OCRProviderFactory, OCRProviderFactory>();
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}