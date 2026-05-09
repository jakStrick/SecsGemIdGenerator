#region File: Services/SecsGem/IdRangeConfig.cs
namespace Solstice.SecsGem.Services;

/// <summary>
/// Numbering plan. Default ranges are non-overlapping and leave room to grow.
/// Override from appsettings.json or the page form.
/// </summary>
public sealed class IdRangeConfig
{
    // Status variables — sliced by subsystem for human readability
    public uint SvidSystemStart    { get; set; } = 1000;
    public uint SvidEfemStart      { get; set; } = 2000;
    public uint SvidLoadPortStart  { get; set; } = 3000;   // +1000 per port
    public uint SvidProcessStart   { get; set; } = 5000;   // +1000 per chamber
    public uint SvidUtilitiesStart { get; set; } = 9000;

    public uint EcidStart   { get; set; } = 10001;
    public uint CeidStart   { get; set; } = 20001;
    public uint AlidStart   { get; set; } = 30001;
    public uint DvidStart   { get; set; } = 40001;
    public uint RptidStart  { get; set; } = 50001;

    // Tool topology
    public int LoadPortCount     { get; set; } = 2;        // Solstice typical: 2 FOUPs
    public int ProcessChamberCount { get; set; } = 4;      // plating + rinse + dry mix
    public string[] ChamberNames { get; set; } =
        new[] { "PM1_Plate", "PM2_Plate", "PM3_Rinse", "PM4_SRD" };
}
#endregion