using Microsoft.Extensions.DependencyInjection;
using System.IO;
using VNGTTranslator.Configs;
using VNGTTranslator.Hooker;
using VNGTTranslator.Network;
using VNGTTranslator.TranslateProviders;
using VNGTTranslator.TTSProviders;

namespace VNGTTranslator
{
    public static class Program
    {
        public static uint PID { get; set; }

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
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}