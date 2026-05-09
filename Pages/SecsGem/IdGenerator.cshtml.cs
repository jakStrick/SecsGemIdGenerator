#region File: Pages/SecsGem/IdGenerator.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Solstice.SecsGem.Models;
using Solstice.SecsGem.Services;
using System.Text;

namespace Solstice.Pages.SecsGem;

public sealed class IdGeneratorModel : PageModel
{
    private readonly SecsGemIdGeneratorService _generator;
    public IdGeneratorModel(SecsGemIdGeneratorService generator) => _generator = generator;

    [BindProperty] public IdRangeConfig Config { get; set; } = new();
    public List<SecsGemIdEntry>? Preview { get; private set; }

    public void OnGet() { /* Show form with defaults */ }

    public IActionResult OnPostPreview()
    {
        Preview = _generator.Generate(Config).ToList();
        return Page();
    }

    public IActionResult OnPostDownload()
    {
        var rows = _generator.Generate(Config);
        var csv  = _generator.ToCsv(rows);
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"solstice-secsgem-ids-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    public IActionResult OnPostDownloadJson()
    {
        var rows = _generator.Generate(Config);
        var json = _generator.ToJson(rows, Config);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"solstice-secsgem-ids-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        return File(bytes, "application/json", fileName);
    }
}
#endregion
