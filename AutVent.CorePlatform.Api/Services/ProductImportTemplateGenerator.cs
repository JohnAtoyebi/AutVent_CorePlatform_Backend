using ClosedXML.Excel;
using AutVent.CorePlatform.Infrastructure.Persistence;
using AutVent.CorePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutVent.CorePlatform.Api.Services;

public static class ProductImportTemplateGenerator
{
    public static async Task<MemoryStream> GenerateTemplateAsync(IUnitOfWork unitOfWork)
    {
        using var workbook = new XLWorkbook();
        
        // Products template sheet
        var productsSheet = workbook.Worksheets.Add("Products");

        // Set up headers
        productsSheet.Cell(1, 1).Value = "Product Name";
        productsSheet.Cell(1, 2).Value = "Price";
        productsSheet.Cell(1, 3).Value = "Quantity";

        // Format header row
        var headerRow = productsSheet.Range(1, 1, 1, 3);
        headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Add example row
        productsSheet.Cell(2, 1).Value = "Example Product";
        productsSheet.Cell(2, 2).Value = "99.99";
        productsSheet.Cell(2, 3).Value = "100";

        // Format example row
        var exampleRow = productsSheet.Range(2, 1, 2, 3);
        exampleRow.Style.Fill.BackgroundColor = XLColor.LightGray;
        exampleRow.Style.Font.Italic = true;

        // Set column widths
        productsSheet.Column(1).Width = 25;
        productsSheet.Column(2).Width = 12;
        productsSheet.Column(3).Width = 12;

        // Add instructions
        productsSheet.Cell(3, 1).Value = "Instructions:";
        productsSheet.Cell(3, 1).Style.Font.Bold = true;
        productsSheet.Cell(4, 1).Value = "1. Enter product name (max 200 characters)";
        productsSheet.Cell(5, 1).Value = "2. Enter price (e.g., 99.99)";
        productsSheet.Cell(6, 1).Value = "3. Enter quantity (must be > 0)";
        productsSheet.Cell(7, 1).Value = "4. Products will be automatically assigned a category";
        productsSheet.Cell(8, 1).Value = "5. Do not modify the header row";
        productsSheet.Cell(9, 1).Value = "6. Delete the example row before importing";

        // Format instruction text
        for (int i = 4; i <= 9; i++)
        {
            productsSheet.Cell(i, 1).Style.Font.Italic = true;
            productsSheet.Cell(i, 1).Style.Font.FontColor = XLColor.Gray;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
