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

[Route("api/[controller]")]
[ApiController]
public class FileUploadController : ControllerBase
{
    private readonly ILogger<FileUploadController> _logger;
    private readonly FileComparisonService _comparisonService;

    public FileUploadController(ILogger<FileUploadController> logger, FileComparisonService comparisonService)
    {
        _logger = logger;
        _comparisonService = comparisonService;
    }

[HttpPost("upload")]
[EnableCors("AllowAll")]
[Consumes("multipart/form-data")]
[ApiExplorerSettings(IgnoreApi = true)]
public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
{
    _logger.LogInformation("UploadFile endpoint hit.");

    if (file == null || file.Length == 0)
    {
        _logger.LogWarning("No file was uploaded or file length is zero.");
        return BadRequest(new { message = "No file uploaded." });
    }

    _logger.LogInformation($"Received file: {file.FileName}, Size: {file.Length} bytes");

    var errorMessages = new Dictionary<string, List<int>>();
    var warningMessages = new Dictionary<string, List<int>>();
    var duplicateGroups = new Dictionary<string, List<int>>();

    using (var reader = new StreamReader(file.OpenReadStream()))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        try
        {
            csv.Read();
            csv.ReadHeader();
            var records = csv.GetRecords<dynamic>().ToList();
            var rowSignatures = new Dictionary<string, (string Content, List<int> Rows)>();

            int rowNumber = 2;
            foreach (var record in records)
            {
                var rowDict = (IDictionary<string, object>)record;

                // Generate the signature based on the ItemName only
                var signature = GenerateRowSignature(rowDict);
                var itemName = rowDict.ContainsKey("ItemName") ? rowDict["ItemName"]?.ToString()?.Trim() ?? "Unknown" : "Unknown";

                if (rowSignatures.ContainsKey(signature))
                {
                    rowSignatures[signature].Rows.Add(rowNumber);
                }
                else
                {
                    rowSignatures[signature] = (Content: itemName, Rows: new List<int> { rowNumber });
                }

                ValidateRowFormat(rowDict, rowNumber, errorMessages);

                foreach (var colValue in rowDict.Values)
                {
                    if (colValue != null && ContainsReplacementSymbols(colValue.ToString()))
                    {
                        AddToMessages(warningMessages, $"Unrecognized characters detected in row {rowNumber}. These may be due to encoding issues (e.g., �, □, etc.).", new List<int> { rowNumber });
                    }
                }

                rowNumber++;
            }

            // Update duplicate warnings to display only the Item Name and location
            foreach (var signature in rowSignatures.Values.Where(s => s.Rows.Count > 1))
            {
                warningMessages[$"Duplicate item detected: \"{signature.Content}\""] = signature.Rows;
            }

            var formattedErrors = FormatMessages(errorMessages, "Error");
            var formattedWarnings = FormatMessages(warningMessages, "Warning");

            if (!formattedWarnings.Any() && !formattedErrors.Any())
            {
                _logger.LogInformation("File validation succeeded. No issues found.");
                return Ok(new
                {
                    message = "<span style='color:green;'>File uploaded and validated successfully!</span>",
                    errors = new string[0],
                    warnings = formattedWarnings
                });
            }

            return BadRequest(new
            {
                message = "<span style='color:red; font-weight:bold;'>Validation Errors:</span>",
                warningsHeader = "<span style='color:yellow; font-weight:bold;'>Warnings:</span>",
                warnings = formattedWarnings,
                errors = formattedErrors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing the file.");
            return BadRequest(new { message = "Error processing file.", details = ex.Message });
        }
    }
}

    [HttpPost("compare")]
    [Consumes("multipart/form-data")]
    public IActionResult CompareFiles([FromForm] IFormFile importFile, [FromForm] IFormFile libraryFile)
    {
        if (importFile == null || importFile.Length == 0)
        {
            return BadRequest(new { message = "The import file is required." });
        }

        if (libraryFile == null || libraryFile.Length == 0)
        {
            return BadRequest(new { message = "The library file is required." });
        }

        try
        {
            var importRecords = ParseCsv(importFile);
            var libraryRecords = ParseCsv(libraryFile);

            var warnings = _comparisonService.CompareFiles(importRecords, libraryRecords);

            return Ok(new { warnings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during file comparison.");
            return StatusCode(500, new { message = "An error occurred during file comparison.", details = ex.Message });
        }
    }

    private List<dynamic> ParseCsv(IFormFile file)
    {
        using (var reader = new StreamReader(file.OpenReadStream()))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            return csv.GetRecords<dynamic>().ToList();
        }
    }

private string GenerateRowSignature(IDictionary<string, object> rowDict)
{
    // Only use the ItemName for generating the duplicate signature
    if (rowDict.ContainsKey("ItemName"))
    {
        return rowDict["ItemName"]?.ToString()?.Trim() ?? string.Empty;
    }
    return string.Empty; // Default to empty if ItemName is missing
}


    private void ValidateRowFormat(IDictionary<string, object> rowDict, int rowNumber, Dictionary<string, List<int>> errorMessages)
    {
        CheckColumn(rowDict, "ItemName", 120, "text", rowNumber, errorMessages, true);
        CheckColumn(rowDict, "PurchaseUnit", 255, "text", rowNumber, errorMessages, false);
        CheckColumn(rowDict, "UnitOfMeasure", 0, "enum", rowNumber, errorMessages, true);
        CheckColumn(rowDict, "CoverageRatePurchase", 0, "numeric", rowNumber, errorMessages, true);
        CheckColumn(rowDict, "CostType1", 50, "text", rowNumber, errorMessages, true);
        CheckColumn(rowDict, "UnitCost1", 0, "numeric", rowNumber, errorMessages, true);
    }

    private void CheckColumn(IDictionary<string, object> rowDict, string colName, int maxLength, string type, int rowNumber, Dictionary<string, List<int>> errorMessages, bool isRequired)
    {
        if (rowDict.ContainsKey(colName))
        {
            string value = rowDict[colName]?.ToString()?.Trim() ?? string.Empty;

            if (isRequired && string.IsNullOrWhiteSpace(value))
            {
                AddToMessages(errorMessages, $"{colName} is required but missing", new List<int> { rowNumber });
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (type == "text" && value.Length > maxLength)
                {
                    AddToMessages(errorMessages, $"{colName} exceeds max length of {maxLength} characters", new List<int> { rowNumber });
                }

                if (type == "numeric" && !decimal.TryParse(value, out _))
                {
                    AddToMessages(errorMessages, $"{colName} must be a numeric value", new List<int> { rowNumber });
                }

                if (type == "enum" && !new[] { "sq ft", "lin ft", "cu yd", "m", "sq m", "cu m", "each" }.Contains(value.ToLower()))
                {
                    AddToMessages(errorMessages, $"{colName} contains an invalid value. Must be one of: sq ft, lin ft, cu yd, m, sq m, cu m, each", new List<int> { rowNumber });
                }
            }
        }
    }

    private bool ContainsReplacementSymbols(string value)
    {
        var replacementChars = new[] { "\uFFFD", "�", "□", "\u25A1", "\u0001", "\u00A0" };
        return replacementChars.Any(c => value.Contains(c));
    }

    private void AddToMessages(Dictionary<string, List<int>> messages, string message, List<int> rows)
    {
        if (!messages.ContainsKey(message))
        {
            messages[message] = new List<int>();
        }
        messages[message].AddRange(rows.Except(messages[message]));
    }

    private List<string> FormatMessages(Dictionary<string, List<int>> messages, string type)
    {
        var formattedMessages = new List<string>();
        foreach (var message in messages)
        {
            var ranges = GetRanges(message.Value);
            formattedMessages.Add($"{type}: {message.Key}. Location: {ranges}");
        }
        return formattedMessages;
    }

    private string GetRanges(List<int> rows)
    {
        rows.Sort();
        var ranges = new List<string>();
        int start = rows[0], end = rows[0];

        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i] == end + 1)
            {
                end = rows[i];
            }
            else
            {
                ranges.Add(start == end ? $"row {start}" : $"rows {start}-{end}");
                start = end = rows[i];
            }
        }
        ranges.Add(start == end ? $"row {start}" : $"rows {start}-{end}");
        return string.Join(", ", ranges);
    }
}