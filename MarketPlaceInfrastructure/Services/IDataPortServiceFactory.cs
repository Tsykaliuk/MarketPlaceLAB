namespace MarketPlaceInfrastructure.Services
{
    /// <summary>
    /// Defines the contract for a factory that provides import and export services for specific entity types.
    /// </summary>
    public interface IDataPortServiceFactory
    {
        /// <summary>
        /// Gets the appropriate import service for the specified entity type T.
        /// </summary>
        /// <typeparam name="T">The type of entity to import.</typeparam>
        /// <returns>An instance of IImportService<T>.</returns>
        IImportService<T> GetImportService<T>() where T : class;

        /// <summary>
        /// Gets the appropriate export service for the specified entity type T.
        /// </summary>
        /// <typeparam name="T">The type of entity to export.</typeparam>
        /// <returns>An instance of IExportService<T>.</returns>
        IExportService<T> GetExportService<T>() where T : class;
    }
}