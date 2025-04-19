// MarketPlaceInfrastructure/Services/IImportService.cs
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // Додайте цей using
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarketPlaceInfrastructure.Services
{
    public interface IImportService<T> where T : class
    {
        /// <summary>
        /// Imports data from the provided stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream containing the data to import.</param>
        /// <param name="parameters">Optional parameters (e.g., UserId).</param> // ЗМІНЕНО
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task ImportFromStreamAsync(Stream stream, Dictionary<string, object> parameters, CancellationToken cancellationToken); // ЗМІНЕНО
    }
}