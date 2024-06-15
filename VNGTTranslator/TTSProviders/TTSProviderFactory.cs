using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace VNGTTranslator.TTSProviders
{
    public class TTSProviderFactory
    {
        public TTSProviderFactory()
        {
            try
            {
                // An aggregate catalog that combines multiple catalogs.
                var catalog = new AggregateCatalog();
                // Adds all the parts found in the same assembly as the Program class.
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));

                // Create the CompositionContainer with the parts in the catalog.
                var container = new CompositionContainer(catalog);
                container.ComposeParts(this);
            }
            catch (CompositionException)
            {
            }

            _cachedProviders = TTSProviders != null
                ? TTSProviders.ToDictionary(p => p.ProviderName)
                : new Dictionary<string, ITTSProvider>();
        }

        private readonly Dictionary<string, ITTSProvider> _cachedProviders;

        [ImportMany]
        public IEnumerable<ITTSProvider>? TTSProviders { get; private set; }

        public ITTSProvider? GetProvider(string providerName)
        {
            return _cachedProviders.GetValueOrDefault(providerName);
        }
    }
}