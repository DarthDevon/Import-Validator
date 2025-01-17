using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Importer.Controllers
{
    public class FileComparisonService
    {
        private readonly ILogger<FileComparisonService> _logger;

        public FileComparisonService(ILogger<FileComparisonService> logger)
        {
            _logger = logger;
        }

        // Compare files for duplicates
        public List<string> CompareFiles(List<dynamic> importRecords, List<dynamic> libraryRecords)
        {
            var duplicateRowNumbers = new List<int>();
            var librarySignatures = new HashSet<string>();

            _logger.LogInformation($"Starting comparison: {importRecords.Count} import rows vs {libraryRecords.Count} library rows");

            int libraryRowNumber = 2;
            foreach (var record in libraryRecords)
            {
                var rowDict = (IDictionary<string, object>)record;
                var rowSignature = CreateSignature(rowDict, ignoreFirstColumn: true); // Ignore first column in the library

                librarySignatures.Add(rowSignature);
                libraryRowNumber++;
            }

            int importRowNumber = 2;
            foreach (var record in importRecords)
            {
                var rowDict = (IDictionary<string, object>)record;
                var rowSignature = CreateSignature(rowDict, ignoreFirstColumn: false);

                if (librarySignatures.Contains(rowSignature))
                {
                    duplicateRowNumbers.Add(importRowNumber);
                }

                importRowNumber++;
            }

            string formattedRanges = GetRowRanges(duplicateRowNumbers);
            var duplicateWarnings = new List<string>();

            if (duplicateRowNumbers.Any())
            {
                duplicateWarnings.Add($"The items in {formattedRanges} from the import spreadsheet are already contained in your Library Spreadsheet. Uploading will duplicate these items.");
            }

            _logger.LogInformation($"Comparison completed. {duplicateRowNumbers.Count} duplicates found.");
            return duplicateWarnings;
        }

        // Signature for duplicates in the same file
        public List<string> CheckDuplicatesInFile(List<dynamic> records)
        {
            var rowSignatures = new Dictionary<string, List<int>>();
            int rowNumber = 2;

            foreach (var record in records)
            {
                var rowDict = (IDictionary<string, object>)record;
                var rowSignature = CreateSignature(rowDict, ignoreFirstColumn: false);

                if (!rowSignatures.ContainsKey(rowSignature))
                {
                    rowSignatures[rowSignature] = new List<int>();
                }
                rowSignatures[rowSignature].Add(rowNumber);
                rowNumber++;
            }

            return FormatDuplicateWarnings(rowSignatures);
        }

        private string CreateSignature(IDictionary<string, object> rowDict, bool ignoreFirstColumn)
        {
            var folderKeys = new HashSet<string> { "FolderLevel1", "FolderLevel2", "FolderLevel3", "FolderLevel4" };

            return string.Join("|", rowDict
                .Where((kv, index) => !(ignoreFirstColumn && index == 0))
                .Where(kv => !folderKeys.Contains(kv.Key))
                .OrderBy(kv => kv.Key)
                .Select(kv =>
                {
                    var value = kv.Value?.ToString()?.Trim() ?? "";
                    value = value.Replace("\r", "").Replace("\n", "").Replace("\u200B", "").Replace("\u00A0", "").Trim();

                    if (decimal.TryParse(value, out var numericValue))
                    {
                        value = numericValue.ToString("0.00");
                    }

                    return $"{kv.Key}:{value}";
                }));
        }

        private List<string> FormatDuplicateWarnings(Dictionary<string, List<int>> rowSignatures)
        {
            var warnings = new List<string>();

            foreach (var entry in rowSignatures)
            {
                var rows = entry.Value;
                if (rows.Count > 1)
                {
                    string rangeText = GetRowRanges(rows);
                    warnings.Add($"The data in {rangeText} represent the same item. Are you sure you want to import duplicates of this item?");
                }
            }

            return warnings;
        }

        private string GetRowRanges(List<int> rows)
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
}
