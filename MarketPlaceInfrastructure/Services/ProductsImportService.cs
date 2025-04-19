using ClosedXML.Excel;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace MarketPlaceInfrastructure.Services
{
    public class ProductsImportService : IImportService<Product>
    {
        private readonly OlxContext _context;
        private readonly ILogger<ProductsImportService> _logger;

        private const string ColTitle = "Title";
        private const string ColDescription = "Description";
        private const string ColPrice = "Price";
        private const string ColStatus = "Status";
        private const string ColStock = "Stock";
        private const string ColCategoryId = "CategoryId";

        public ProductsImportService(OlxContext context, ILogger<ProductsImportService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ImportFromStreamAsync(Stream stream, Dictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("Input stream cannot be null or empty.", nameof(stream));
            }

            if (!parameters.TryGetValue("UserId", out var userIdObj) || !(userIdObj is string currentUserId) || string.IsNullOrWhiteSpace(currentUserId))
            {
                _logger.LogError("UserId was not provided or is invalid for product import.");
                throw new ArgumentException("Required parameter 'UserId' is missing or invalid.", nameof(parameters));
            }
            _logger.LogInformation("Import process started for UserId: {UserId}", currentUserId);


            var productsToAdd = new List<Product>();
            var categoriesInDb = await _context.Categories
                                     .AsNoTracking()
                                     .ToDictionaryAsync(c => c.Id, c => c.Name, StringComparer.OrdinalIgnoreCase, cancellationToken); // Додано StringComparer для регістронезалежності ID (про всяк випадок)


            try
            {
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null) { return; }

                    var headerRow = worksheet.Row(1);
                    if (headerRow.IsEmpty()) { return; }

                    int titleCol = FindColumnIndex(headerRow, ColTitle);
                    int descCol = FindColumnIndex(headerRow, ColDescription);
                    int priceCol = FindColumnIndex(headerRow, ColPrice);
                    int statusCol = FindColumnIndex(headerRow, ColStatus);
                    int stockCol = FindColumnIndex(headerRow, ColStock);
                    int categoryIdCol = FindColumnIndex(headerRow, ColCategoryId);

                    if (titleCol == -1 || priceCol == -1 || statusCol == -1 || stockCol == -1 || categoryIdCol == -1)
                    {
                        throw new InvalidDataException($"Required columns missing...");
                    }

                    _logger.LogInformation("Found {RowCount} data rows in the Excel file.", worksheet.RowsUsed().Count() - 1);

                    foreach (var row in worksheet.RowsUsed().Skip(1))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            string title = GetCellValueAsString(row, titleCol);
                            string description = GetCellValueAsString(row, descCol);
                            string priceStr = GetCellValueAsString(row, priceCol);
                            string statusStr = GetCellValueAsString(row, statusCol);
                            string stockStr = GetCellValueAsString(row, stockCol);
                            string categoryId = GetCellValueAsString(row, categoryIdCol);

                            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(priceStr) ||
                               string.IsNullOrWhiteSpace(statusStr) || string.IsNullOrWhiteSpace(stockStr) ||
                               string.IsNullOrWhiteSpace(categoryId))
                            {
                                _logger.LogWarning("Skipping row {RowNum} due to missing required data (Title, Price, Status, Stock, or CategoryId).", row.RowNumber());
                                continue;
                            }

                            if (!decimal.TryParse(priceStr, out decimal price) || price < 0) {  continue; }
                            if (!int.TryParse(stockStr, out int stock) || stock < 0) {  continue; }
                            if (!Enum.TryParse<ProductStatus>(statusStr, true, out ProductStatus status)) {  continue; }

                            if (!categoriesInDb.ContainsKey(categoryId))
                            {
                                _logger.LogWarning("Skipping row {RowNum}: CategoryId '{CatId}' not found in the database.", row.RowNumber(), categoryId);
                                continue;
                            }

                            var newProduct = new Product
                            {
                                Title = title.Trim(),
                                Description = description?.Trim(),
                                Price = price,
                                Status = status,
                                Stock = stock,
                                CategoryId = categoryId,
                                UserId = currentUserId,
                                CreatedAt = DateTime.UtcNow
                            };

                            _logger.LogDebug("Row {RowNum}: Prepared product '{ProductTitle}' for addition.", row.RowNumber(), newProduct.Title);
                            productsToAdd.Add(newProduct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing row {RowNum} during Excel import.", row.RowNumber());
                        }
                    }

                    _logger.LogInformation("Finished processing rows. Found {Count} potential products to add.", productsToAdd.Count);

                }

                if (productsToAdd.Any())
                {
                    await _context.Products.AddRangeAsync(productsToAdd, cancellationToken);
                    _logger.LogInformation("Attempting to save {Count} new products to the database...", productsToAdd.Count);
                    int savedCount = await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("SaveChangesAsync result: {SavedCount} state entries written to the database.", savedCount);

                    if (savedCount < productsToAdd.Count && savedCount == 0 && productsToAdd.Any())
                    {
                        _logger.LogWarning("SaveChangesAsync reported 0 saved entries despite having products to add. Potential transaction, validation errors, or other DB issue?");
                    }
                }
                else
                {
                    _logger.LogInformation("No valid products were found in the file to import.");
                }

            }
            catch (DbUpdateException dbEx) // Ловимо специфічну помилку EF Core
            {
                _logger.LogError(dbEx, "Database update error during import. Check inner exception and entity entries.");
                var errorDetails = new List<string>();
                try
                {
                    foreach (var entry in dbEx.Entries)
                    {
                        _logger.LogError("Entity error: Type '{entityType}', State '{entityState}'.", entry.Entity.GetType().Name, entry.State);
                    }
                    var innerException = dbEx.InnerException;
                    while (innerException != null)
                    {
                        errorDetails.Add(innerException.Message);
                        innerException = innerException.InnerException;
                    }
                }
                catch (Exception loggingEx)
                {
                    _logger.LogError(loggingEx, "Error occurred while trying to log DbUpdateException details.");
                }

                string combinedDetails = string.Join("; ", errorDetails);
                if (string.IsNullOrWhiteSpace(combinedDetails)) combinedDetails = dbEx.Message;

                throw new InvalidOperationException($"An error occurred saving data to the database: {combinedDetails}", dbEx);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogError(argEx, "Argument error during import setup.");
                throw new InvalidOperationException($"Import setup failed: {argEx.Message}", argEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during the import process.");
                throw new InvalidOperationException("An error occurred during the import process. Please check logs or contact support.", ex);
            }
        }


        private int FindColumnIndex(IXLRow headerRow, string columnName)
        {
            var cell = headerRow.CellsUsed(c => c.Value.ToString().Equals(columnName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return cell?.Address.ColumnNumber ?? -1;
        }

        private string GetCellValueAsString(IXLRow row, int colIndex)
        {
            if (colIndex <= 0) return null;
            var cell = row.Cell(colIndex);
            if (cell == null || cell.IsEmpty()) return null;
            return cell.GetString()?.Trim(); // Використовуємо GetString()
        }
    }
}