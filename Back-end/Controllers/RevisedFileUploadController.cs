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

[ApiController]
[Route("api/revisedfile")]
public class RevisedFileUploadController : ControllerBase
{
    private readonly RevisedFileComparisonService _comparisonService;

    public RevisedFileUploadController(RevisedFileComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    [HttpPost("validate")]
    public IActionResult ValidateRevisedFile([FromForm] IFormFile revisedFile)
    {
        var validationResults = _comparisonService.ValidateRevisedFile(revisedFile);
        return Ok(validationResults);
    }

    [HttpPost("compare")]
    public IActionResult CompareRevisedFile([FromForm] IFormFile revisedFile)
    {
        var comparisonResults = _comparisonService.CompareRevisedFile(revisedFile);
        return Ok(comparisonResults);
    }
}
