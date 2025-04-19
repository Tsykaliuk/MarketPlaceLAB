using ClosedXML.Excel;
using MarketPlaceDomain.Model;
using MarketPlaceInfrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarketPlaceInfrastructure.Services
{
    public class ProductsExportService : IExportService<Product>
    {
        private readonly OlxContext _context;

        private const string ColId = "Id";
        private const string ColTitle = "Title";
        private const string ColDescription = "Description";
        private const string ColPrice = "Price";
        private const string ColStatus = "Status";
        private const string ColStock = "Stock";
        private const string ColCategoryId = "CategoryId";
        private const string ColCategoryName = "CategoryName";

        public ProductsExportService(OlxContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<byte[]> ExportToByteArrayAsync(CancellationToken cancellationToken)
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .AsNoTracking()
                                         .ToListAsync(cancellationToken);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Products");

                worksheet.Cell(1, 1).Value = ColId;
                worksheet.Cell(1, 2).Value = ColTitle;
                worksheet.Cell(1, 3).Value = ColDescription;
                worksheet.Cell(1, 4).Value = ColPrice;
                worksheet.Cell(1, 5).Value = ColStatus;
                worksheet.Cell(1, 6).Value = ColStock;
                worksheet.Cell(1, 7).Value = ColCategoryId;
                worksheet.Cell(1, 8).Value = ColCategoryName;

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                int currentRow = 2;
                foreach (var product in products)
                {
                    worksheet.Cell(currentRow, 1).Value = product.Id;
                    worksheet.Cell(currentRow, 2).Value = product.Title;
                    worksheet.Cell(currentRow, 3).Value = product.Description;
                    worksheet.Cell(currentRow, 4).Value = product.Price;
                    worksheet.Cell(currentRow, 5).Value = product.Status.ToString();
                    worksheet.Cell(currentRow, 6).Value = product.Stock;
                    worksheet.Cell(currentRow, 7).Value = product.CategoryId;
                    worksheet.Cell(currentRow, 8).Value = product.Category?.Name;

                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}