using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Importer.Controllers;

public class RevisedFileComparisonService
{
    private static readonly List<string> ExpectedHeaders = new List<string>
    {
        "ExportId", "ItemName", "ItemDescription", "PurchaseUnit", "UnitOfMeasure", 
        "CoverageRatePurchase", "CoverageRateMeasured", "FolderLevel1", "FolderLevel2", 
        "FolderLevel3", "FolderLevel4", "FolderLevel5", "CostType1", "UnitCost1", 
        "AccountingCode1", "CostType2", "UnitCost2", "AccountingCode2", "CostType3", 
        "UnitCost3", "AccountingCode3", "CostType4", "UnitCost4", "AccountingCode4", 
        "CostType5", "UnitCost5", "AccountingCode5", "ExternalId"
    };

    public ValidationResult ValidateRevisedFile(IFormFile revisedFile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        using (var reader = new StreamReader(revisedFile.OpenReadStream()))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<dynamic>().ToList();

            // Validate headers
            var headers = ((IDictionary<string, object>)records.First()).Keys.ToList();
            if (!headers.SequenceEqual(ExpectedHeaders))
            {
                errors.Add("The column headers in the first row do not match the expected format.");
                return new ValidationResult { Errors = errors, Warnings = warnings };
            }

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                var row = (IDictionary<string, object>)records[rowIndex];

                foreach (var (key, value) in row)
                {
                    string cellValue = value?.ToString()?.Trim() ?? "";

                    // Validation logic for specific columns
                    switch (key)
                    {
                        case "ExportId":
                            if (string.IsNullOrEmpty(cellValue))
                                errors.Add($"Error in row {rowIndex + 1}, ExportId: Required field is empty.");
                            break;

                        case "ItemName":
                            if (string.IsNullOrEmpty(cellValue))
                                errors.Add($"Error in row {rowIndex + 1}, ItemName: Required field is empty.");
                            break;

                        case "UnitOfMeasure":
                            if (!new[] { "sq ft", "lin ft", "cu yd", "m", "sq m", "cu m", "each" }.Contains(cellValue))
                                errors.Add($"Error in row {rowIndex + 1}, UnitOfMeasure: Invalid value.");
                            break;

                        case "CoverageRatePurchase":
                        case "CoverageRateMeasured":
                            if (!decimal.TryParse(cellValue, out _))
                                errors.Add($"Error in row {rowIndex + 1}, {key}: Must be a numeric value.");
                            break;

                        default:
                            // Optional field validation
                            break;
                    }

                    // Check for carriage returns
                    if (cellValue.Contains("\r") || cellValue.Contains("\n"))
                        errors.Add($"Error in row {rowIndex + 1}, {key}: Contains carriage returns.");

                    // Check for replacement symbols
                    if (cellValue.Any(c => c == '�'))
                        errors.Add($"Error in row {rowIndex + 1}, {key}: Contains replacement symbols.");
                }
            }
        }

        return new ValidationResult
        {
            Errors = errors,
            Warnings = warnings
        };
    }

    public ComparisonResult CompareRevisedFile(IFormFile revisedFile)
    {
        var compareWarnings = new List<string>();

        using (var reader = new StreamReader(revisedFile.OpenReadStream()))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<dynamic>().ToList();

            // Check for duplicate rows excluding folder columns
            var rowSignatures = new Dictionary<string, List<int>>();

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                var rowDict = (IDictionary<string, object>)records[rowIndex];
                var rowSignature = CreateSignature(rowDict);

                if (!rowSignatures.ContainsKey(rowSignature))
                    rowSignatures[rowSignature] = new List<int>();

                rowSignatures[rowSignature].Add(rowIndex + 1);
            }

            foreach (var entry in rowSignatures.Where(entry => entry.Value.Count > 1))
            {
                compareWarnings.Add($"Duplicate rows: {string.Join(", ", entry.Value)}.");
            }
        }

        return new ComparisonResult
        {
            CompareWarnings = compareWarnings
        };
    }

    private string CreateSignature(IDictionary<string, object> rowDict)
    {
        return string.Join("|", rowDict
            .Where(kv => !kv.Key.StartsWith("Folder"))
            .OrderBy(kv => kv.Key)
            .Select(kv => kv.Value?.ToString()?.Trim() ?? ""));
    }
}

public class ValidationResult
{
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}

public class ComparisonResult
{
    public List<string> CompareWarnings { get; set; }
}
