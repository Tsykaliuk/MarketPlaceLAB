using System.Threading;
using System.Threading.Tasks;

namespace MarketPlaceInfrastructure.Services
{
    /// <summary>
    /// Defines the contract for exporting data of type T.
    /// </summary>
    /// <typeparam name="T">The type of entity to export.</typeparam>
    public interface IExportService<T> where T : class
    {
        /// <summary>
        /// Exports data to a byte array asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing the byte array of the exported file.</returns>
        Task<byte[]> ExportToByteArrayAsync(CancellationToken cancellationToken);
    }
}