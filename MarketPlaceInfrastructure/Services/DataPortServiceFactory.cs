namespace MarketPlaceInfrastructure.Services
{
    public class DataPortServiceFactory : IDataPortServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DataPortServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IImportService<T> GetImportService<T>() where T : class
        {
            var service = _serviceProvider.GetService<IImportService<T>>();
            if (service == null)
            {
                throw new InvalidOperationException($"No import service registered for type {typeof(T).Name}");
            }
            return service;
        }

        public IExportService<T> GetExportService<T>() where T : class
        {
            var service = _serviceProvider.GetService<IExportService<T>>();
            if (service == null)
            {
                throw new InvalidOperationException($"No export service registered for type {typeof(T).Name}");
            }
            return service;
        }
    }
}