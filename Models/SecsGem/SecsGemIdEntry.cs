// =============================================================================
//  SECS/GEM ID Generator — ClassOne Solstice
//  Target: ASP.NET Core 8 (Razor Pages). Split this single file into the
//  paths shown in each region header for production use.
//
//  SEMI standards covered: E5, E30, E37, E40, E87, E90, E94, E116
//  (E84 has no user-defined IDs — signal-name based; E58/ARAMS recipe IDs
//   are emitted as A:n placeholder examples.)
// =============================================================================

#region File: Models/SecsGem/SecsGemIdEntry.cs
namespace Solstice.SecsGem.Models;

/// <summary>
/// One row in the master ID dictionary. Maps 1:1 to a CSV row.
/// </summary>
public sealed class SecsGemIdEntry
{
    public string IdType        { get; set; } = "";   // SVID, ECID, CEID, ALID, DVID, RPTID, PRJobID, ControlJobID, CarrierID, SubstrateID
    public uint?  IdNumber      { get; set; }         // null for A:n keyed IDs (PRJobID, CarrierID, etc.)
    public string Name          { get; set; } = "";   // Mnemonic (VID name for VIDs, alarm text for ALID)
    public string Description   { get; set; } = "";
    public string DataType      { get; set; } = "";   // SEMI E5 format codes: U1/U2/U4/U8, I1..I8, F4/F8, A:n, B:n, BOOLEAN, L
    public string Units         { get; set; } = "";   // SI preferred (degC, mL/min, A, V, RPM, kPa, ...)
    public string DefaultValue  { get; set; } = "";
    public string MinValue      { get; set; } = "";
    public string MaxValue      { get; set; } = "";
    public string SemiStandard  { get; set; } = "";   // "E30", "E87", etc.
    public string Subsystem     { get; set; } = "";   // System, EFEM, LP1, PM1, ChemDelivery, ...
    public string Notes         { get; set; } = "";
    public bool   ReportableInDvvalList { get; set; } // true if it can appear in S6F11 DVVAL list of a CEID
    public string AlarmCode     { get; set; } = "";   // E30 alarm category (e.g. EquipmentStatusWarning)
}
#endregion