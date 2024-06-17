﻿using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IAppConfigProvider, AppConfigProvider>(_ => new AppConfigProvider(string.Empty));
            services.AddSingleton<TranslateProviderFactory, TranslateProviderFactory>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<TTSProviderFactory, TTSProviderFactory>();
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}