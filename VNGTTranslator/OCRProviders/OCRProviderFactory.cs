using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace VNGTTranslator.OCRProviders
{
    internal class OCRProviderFactory
    {
        public OCRProviderFactory()
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

            _cachedProviders = OCRProviders != null
                ? OCRProviders.ToDictionary(p => p.ProviderName)
                : [];
        }

        private readonly Dictionary<string, IOCRProvider> _cachedProviders;

        [ImportMany]
        public List<IOCRProvider>? OCRProviders { get; private set; }

        public IOCRProvider? GetProvider(string providerName)
        {
            return _cachedProviders.GetValueOrDefault(providerName);
        }
    }
}