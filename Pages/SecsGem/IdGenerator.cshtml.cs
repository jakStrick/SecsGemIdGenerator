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

    public List<SecsGemIdEntry>? Preview  { get; private set; }
    public int FilledCount  { get; private set; }
    public int ClearedCount { get; private set; }

    // Cleared state lives in TempData (server-side cookie store) so it survives
    // every button press without relying on hidden form fields.
    // Peek() reads without consuming → the key stays alive for the next request.
    // Only SetClearedState(false) / OnGet removes it.
    private const string ClearedKey = "IdsCleared";
    public  bool IsClearedState => TempData.Peek(ClearedKey) is string;
    private void SetClearedState(bool cleared)
    {
        if (cleared) TempData[ClearedKey] = "yes";
        else         TempData.Remove(ClearedKey);
    }

    // Fresh navigation always resets to normal (ID-assigned) mode.
    public void OnGet() => SetClearedState(false);

    public IActionResult OnPostPreview()
    {
        var rows = _generator.Generate(Config);
        Preview  = IsClearedState ? _generator.ClearAllIds(rows) : [..rows];
        return Page();
    }

    public IActionResult OnPostDownload()
    {
        var rows   = _generator.Generate(Config);
        IEnumerable<SecsGemIdEntry> result = IsClearedState ? _generator.ClearAllIds(rows) : rows;
        var bytes  = _generator.ToZipCsv(result);
        return File(bytes, "application/zip", $"solstice-secsgem-ids-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
    }

    public IActionResult OnPostDownloadJson()
    {
        var rows   = _generator.Generate(Config);
        var result = IsClearedState ? (IEnumerable<SecsGemIdEntry>)_generator.ClearAllIds(rows) : rows;
        var json   = _generator.ToJson(result, Config);
        var bytes  = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"solstice-secsgem-ids-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    }

    public IActionResult OnPostFillMissing()
    {
        var rows    = _generator.Generate(Config);
        var before  = rows.Count(r => r.IdNumber == null);
        Preview     = _generator.FillMissingIds(rows);
        FilledCount = before;
        SetClearedState(false); // Exit cleared mode — IDs are now assigned.
        return Page();
    }

    public IActionResult OnPostClearIds()
    {
        var rows     = _generator.Generate(Config);
        ClearedCount = rows.Count(r => r.IdNumber.HasValue);
        Preview      = _generator.ClearAllIds(rows);
        SetClearedState(true);
        return Page();
    }
}
#endregion
