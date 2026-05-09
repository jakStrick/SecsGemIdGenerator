#region File: Services/SecsGem/SecsGemIdGeneratorService.cs
using Solstice.SecsGem.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solstice.SecsGem.Services;

public sealed class SecsGemIdGeneratorService
{
    public IReadOnlyList<SecsGemIdEntry> Generate(IdRangeConfig cfg)
    {
        var list = new List<SecsGemIdEntry>(512);

        AddGemStandardSvids(list, cfg);     // E30 standard SVIDs
        AddSystemSvids(list, cfg);
        AddEfemSvids(list, cfg);
        AddLoadPortSvids(list, cfg);
        AddProcessChamberSvids(list, cfg);
        AddUtilitiesSvids(list, cfg);

        AddEcids(list, cfg);
        AddCeids(list, cfg);                // E30 + tool-specific
        AddAlids(list, cfg);                // E30 alarm framework
        AddDvids(list, cfg);                // Reportable data variables
        AddRptids(list, cfg);               // Default report skeletons

        AddE40Examples(list);               // PRJobID (process job)
        AddE94Examples(list);               // ControlJobID
        AddE87Examples(list, cfg);          // CarrierID, PortID, AccessMode
        AddE90Examples(list);               // SubstrateID
        AddE116States(list);                // EPT state mapping (informational rows)

        return list;
    }

    // -------- E30 standard SVIDs (mandatory minimum set) --------------------
    // Ref: SEMI E30 "Standard Status Variables".
    private static void AddGemStandardSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.SvidSystemStart;
        void Add(string name, string desc, string type, string units = "")
            => list.Add(new SecsGemIdEntry {
                IdType="SVID", IdNumber=id++, Name=name, Description=desc,
                DataType=type, Units=units, SemiStandard="E30", Subsystem="System"
            });

        Add("Clock",                "Equipment clock, format A[16]: YYYYMMDDhhmmsscc", "A:16");
        Add("ControlState",         "GEM control state per E30 fig.7-2 (1..5)",        "U1");
        Add("EventsEnabled",        "List of CEIDs currently enabled (S2F37 result)",  "L");
        Add("AlarmsEnabled",        "List of ALIDs currently enabled (S5F3 result)",   "L");
        Add("AlarmsSet",            "List of currently set ALIDs",                     "L");
        Add("PPExecName",           "Name of the process program currently executing", "A:80");
        Add("PreviousProcessState", "Previous E116 EPT processing state",              "U1");
        Add("CommunicationState",   "E37 HSMS communication state",                    "U1");
        Add("SpoolCountActual",     "Current number of spooled messages",              "U4");
        Add("SpoolCountTotal",      "Max spool capacity",                              "U4");
        Add("SpoolFullTime",        "Time when spool became full",                     "A:16");
        Add("SpoolStartTime",       "Time spooling started",                           "A:16");
    }

    private static void AddSystemSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.SvidSystemStart + 100;
        SV("EquipmentModel",     "Solstice model identifier",     "A:32", "", "System");
        SV("EquipmentSerialNum", "Solstice serial number",        "A:32", "", "System");
        SV("SoftwareRevision",   "Equipment SW revision string",  "A:32", "", "System");
        SV("ProcessProgramDir",  "Active recipe directory path",  "A:120","", "System");
        SV("EmoStatus",          "Emergency Off button state",    "BOOLEAN","",  "System");
        SV("DoorInterlocks",     "Bitmap of door interlock states","U4",  "",   "System");
        SV("UpsOnBattery",       "UPS running on battery",        "BOOLEAN","",  "System");

        void SV(string n, string d, string t, string u, string s)
            => list.Add(new SecsGemIdEntry {
                IdType="SVID", IdNumber=id++, Name=n, Description=d,
                DataType=t, Units=u, SemiStandard="E5", Subsystem=s
            });
    }

    private static void AddEfemSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.SvidEfemStart;
        void SV(string n, string d, string t, string u = "")
            => list.Add(new SecsGemIdEntry {
                IdType="SVID", IdNumber=id++, Name=n, Description=d,
                DataType=t, Units=u, SemiStandard="E5", Subsystem="EFEM"
            });

        SV("EfemRobotState",       "Idle/Moving/Error",        "U1");
        SV("EfemRobotPosition",    "Encoded R/Theta/Z position","A:32");
        SV("EfemArmHasWafer_A",    "Arm A occupancy",           "BOOLEAN");
        SV("EfemArmHasWafer_B",    "Arm B occupancy",           "BOOLEAN");
        SV("EfemAlignerState",     "Aligner state",             "U1");
        SV("EfemFfuRpm",           "Fan filter unit RPM",       "U2", "RPM");
        SV("EfemDifferentialPress","Cleanroom dP",              "F4", "Pa");
        SV("EfemHumidity",         "EFEM humidity",             "F4", "%RH");
        SV("EfemN2PurgeFlow",      "N2 purge flow",             "F4", "L/min");
    }

    private static void AddLoadPortSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        for (int p = 1; p <= cfg.LoadPortCount; p++)
        {
            uint id = (uint)(cfg.SvidLoadPortStart + (p - 1) * 1000);
            string ss = $"LP{p}";
            void SV(string n, string d, string t, string u = "")
                => list.Add(new SecsGemIdEntry {
                    IdType="SVID", IdNumber=id++, Name=$"LP{p}_{n}", Description=d,
                    DataType=t, Units=u, SemiStandard="E87", Subsystem=ss
                });

            SV("PortAssociationState", "E87 port association state",                    "U1");
            SV("PortTransferState",    "E87 port transfer state",                       "U1");
            SV("PortReservationState", "E87 port reservation state",                    "U1");
            SV("AccessMode",           "Manual/Auto access mode",                       "U1");
            SV("CarrierIdAtPort",      "CarrierID currently at port (A:n)",             "A:32");
            SV("LoadPortPresent",      "Carrier present sensor",                        "BOOLEAN");
            SV("LoadPortPlaced",       "Carrier placed sensor",                         "BOOLEAN");
            SV("LoadPortClamped",      "Clamp state",                                   "BOOLEAN");
            SV("LoadPortDocked",       "Dock state",                                    "BOOLEAN");
            SV("LoadPortDoorOpen",     "FOUP door open",                                "BOOLEAN");
            SV("E84_HoAvbl",           "E84 HO_AVBL signal",                            "BOOLEAN");
            SV("E84_Cs0",              "E84 CS_0 signal",                               "BOOLEAN");
            SV("E84_Valid",            "E84 VALID signal",                              "BOOLEAN");
            SV("E84_TrReq",            "E84 TR_REQ signal",                             "BOOLEAN");
            SV("E84_Busy",             "E84 BUSY signal",                               "BOOLEAN");
            SV("E84_Compt",             "E84 COMPT signal",                             "BOOLEAN");
            SV("E84_Continue",         "E84 CONT signal",                               "BOOLEAN");
        }
    }

    private static void AddProcessChamberSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        for (int c = 0; c < cfg.ProcessChamberCount; c++)
        {
            string name = c < cfg.ChamberNames.Length ? cfg.ChamberNames[c] : $"PM{c+1}";
            uint id = (uint)(cfg.SvidProcessStart + c * 1000);
            void SV(string n, string d, string t, string u = "")
                => list.Add(new SecsGemIdEntry {
                    IdType="SVID", IdNumber=id++, Name=$"{name}_{n}", Description=d,
                    DataType=t, Units=u, SemiStandard="E5", Subsystem=name
                });

            // Common to every wet PM
            SV("ChamberState",      "E116 EPT processing state",        "U1");
            SV("WaferPresent",      "Wafer present in chamber",         "BOOLEAN");
            SV("CurrentRecipe",     "Active recipe step name",          "A:80");
            SV("RecipeStep",        "Active recipe step number",        "U2");
            SV("ProcessTimeElapsed","Elapsed seconds in current step",  "F4", "s");

            if (name.Contains("Plate", StringComparison.OrdinalIgnoreCase))
            {
                SV("BathTemp",          "Plating bath temperature",     "F4", "degC");
                SV("BathLevel",         "Plating bath level",           "F4", "%");
                SV("BathConductivity",  "Bath conductivity",            "F4", "mS/cm");
                SV("BathPh",            "Bath pH (if monitored)",       "F4", "pH");
                SV("AnodeCurrent",      "Anode total current",          "F4", "A");
                SV("AnodeVoltage",      "Anode voltage",                "F4", "V");
                SV("CathodeCurrent",    "Cathode current",              "F4", "A");
                SV("PumpFlowMain",      "Main recirc pump flow",        "F4", "L/min");
                SV("PumpPressureMain",  "Main recirc pump pressure",    "F4", "kPa");
                SV("FilterDp",          "Filter differential pressure", "F4", "kPa");
                SV("Acceleratorppm",    "Accelerator concentration",    "F4", "ppm");
                SV("Suppressorppm",     "Suppressor concentration",     "F4", "ppm");
                SV("Levelerppm",        "Leveler concentration",        "F4", "ppm");
                SV("CuConcentration",   "Cu metal concentration",       "F4", "g/L");
                SV("ChlorideConc",      "Chloride concentration",       "F4", "ppm");
                SV("WaferRpm",          "Wafer rotation",               "F4", "RPM");
                SV("PaddleRpm",         "Paddle/agitator rotation",     "F4", "RPM");
            }
            else if (name.Contains("Rinse", StringComparison.OrdinalIgnoreCase))
            {
                SV("DiwFlow",          "DI water flow",                 "F4", "L/min");
                SV("DiwResistivity",   "DI water resistivity",          "F4", "MOhm-cm");
                SV("DiwTemp",          "DI water temperature",          "F4", "degC");
                SV("RinseTime",        "Rinse time setpoint",           "F4", "s");
            }
            else if (name.Contains("SRD", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Dry", StringComparison.OrdinalIgnoreCase))
            {
                SV("SpinRpm",          "Spin RPM",                      "F4", "RPM");
                SV("N2Flow",           "N2 dry flow",                   "F4", "L/min");
                SV("ChamberHumidity",  "Chamber humidity",              "F4", "%RH");
                SV("SpinTime",         "Spin time elapsed",             "F4", "s");
            }
        }
    }

    private static void AddUtilitiesSvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.SvidUtilitiesStart;
        void SV(string n, string d, string t, string u = "")
            => list.Add(new SecsGemIdEntry {
                IdType="SVID", IdNumber=id++, Name=n, Description=d,
                DataType=t, Units=u, SemiStandard="E5", Subsystem="Utilities"
            });

        SV("DiwSupplyPressure",  "DI water supply pressure",        "F4", "kPa");
        SV("CdaPressure",        "Clean dry air pressure",          "F4", "kPa");
        SV("N2SupplyPressure",   "N2 supply pressure",              "F4", "kPa");
        SV("ExhaustPressure",    "Exhaust pressure",                "F4", "Pa");
        SV("DrainLeak",          "Drain leak sensor",               "BOOLEAN");
        SV("FacilitiesOk",       "All facilities within spec",      "BOOLEAN");
        SV("ChillerTemp",        "Chiller supply temperature",      "F4", "degC");
        SV("ChillerFlow",        "Chiller flow rate",               "F4", "L/min");
    }

    // -------- ECIDs ---------------------------------------------------------
    private static void AddEcids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.EcidStart;
        void EC(string n, string d, string t, string min, string max, string def, string u = "", string ss = "System")
            => list.Add(new SecsGemIdEntry {
                IdType="ECID", IdNumber=id++, Name=n, Description=d, DataType=t,
                MinValue=min, MaxValue=max, DefaultValue=def, Units=u,
                SemiStandard="E30", Subsystem=ss
            });

        // GEM standard ECs
        EC("EstablishCommunicationsTimeout", "T-time before retry on S1F13",          "U2", "1",   "120",  "10",  "s");
        EC("TimeFormat",                     "0=12-byte 1=16-byte 2=ISO-8601",        "U1", "0",   "2",    "1");
        EC("MaxSpoolTransmit",               "Max spooled msgs per S6F23 transmit",   "U4", "0",   "65535","100");
        EC("OverWriteSpool",                 "0=Discard newest 1=Discard oldest",     "U1", "0",   "1",    "0");
        EC("ConfigSpool",                    "Spooling enabled (1) or disabled (0)",  "BOOLEAN","","","1");
        EC("DefaultControlState",            "Power-on control state",                "U1", "1",   "5",    "4");
        EC("DefaultCommState",               "Power-on comm state",                   "U1", "0",   "1",    "1");

        // Tool-specific
        EC("MaxRecipeSize",          "Max recipe file size",            "U4","1024","10485760","1048576","bytes","System");
        EC("AlarmCsvLogPath",        "Alarm log path",                  "A:120","","","C:\\Logs\\Alarms\\","","System");
        EC("DefaultBathTempSetpoint","Default plating bath setpoint",   "F4","15","45","25","degC","Plating");
        EC("PlatingCurrentLimit",    "Maximum plating current allowed", "F4","0","200","100","A","Plating");
        EC("MaxSpinRpm",             "SRD spin RPM ceiling",            "U2","0","3000","2000","RPM","SRD");
        EC("DiwResistivityMin",      "Minimum acceptable DIW resistivity","F4","0","18","17","MOhm-cm","Utilities");
    }

    // -------- CEIDs ---------------------------------------------------------
    private static void AddCeids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.CeidStart;
        void CE(string n, string d, string ss = "System")
            => list.Add(new SecsGemIdEntry {
                IdType="CEID", IdNumber=id++, Name=n, Description=d,
                SemiStandard="E30", Subsystem=ss, DataType="—"
            });

        // E30 lifecycle
        CE("OperatorEquipmentConstantChange", "Operator changed an EC",       "System");
        CE("OperatorCommandIssued",           "Operator issued a remote cmd", "System");
        CE("ControlStateLocal",               "Transition to LOCAL",          "System");
        CE("ControlStateRemote",              "Transition to REMOTE",         "System");
        CE("ControlStateOffline",             "Transition to OFFLINE",        "System");
        CE("CommEstablished",                 "S1F13/F14 succeeded",          "System");
        CE("MaterialReceived",                "Carrier or wafer received",    "EFEM");
        CE("MaterialRemoved",                 "Material removed",             "EFEM");
        CE("PpChangeEvent",                   "Process program changed",      "System");
        CE("AlarmDetected",                   "Any alarm set",                "System");
        CE("AlarmCleared",                    "Any alarm cleared",            "System");

        // E40 process job
        CE("PrJobCreated",   "Process job created (S16F11)", "ProcessJob");
        CE("PrJobSetup",     "Process job entered SETUP",    "ProcessJob");
        CE("PrJobProcessing","Process job PROCESSING",       "ProcessJob");
        CE("PrJobPaused",    "Process job PAUSED",           "ProcessJob");
        CE("PrJobCompleted", "Process job COMPLETED",        "ProcessJob");
        CE("PrJobAborted",   "Process job ABORTED",          "ProcessJob");

        // E94 control job
        CE("CtrlJobCreated",  "Control job created", "ControlJob");
        CE("CtrlJobExecuting","Control job EXECUTING","ControlJob");
        CE("CtrlJobCompleted","Control job COMPLETED","ControlJob");

        // E87 carrier
        CE("CarrierArrived",         "Carrier placed at port",          "Carrier");
        CE("CarrierIdRead",          "Carrier ID read OK",              "Carrier");
        CE("CarrierIdReadFail",      "Carrier ID read failed",          "Carrier");
        CE("SlotMapAcquired",        "Slot map complete",               "Carrier");
        CE("CarrierClosed",          "FOUP door closed",                "Carrier");
        CE("CarrierDeparted",        "Carrier removed from port",       "Carrier");

        // E90 substrate
        CE("SubstrateAcquired",  "Wafer acquired by EFEM robot",   "Substrate");
        CE("SubstrateAtSource",  "Wafer at source location",       "Substrate");
        CE("SubstrateAtDest",    "Wafer at destination location",  "Substrate");
        CE("SubstrateProcessed", "Wafer processing complete",      "Substrate");

        // Tool-specific (per chamber)
        for (int c = 0; c < cfg.ProcessChamberCount; c++)
        {
            string n = c < cfg.ChamberNames.Length ? cfg.ChamberNames[c] : $"PM{c+1}";
            CE($"{n}_ProcessStarted",  $"{n} processing started",  n);
            CE($"{n}_ProcessComplete", $"{n} processing complete", n);
            CE($"{n}_StepChanged",     $"{n} recipe step changed", n);
            CE($"{n}_RecipeChanged",   $"{n} recipe changed",      n);
        }
    }

    // -------- ALIDs ---------------------------------------------------------
    private static void AddAlids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.AlidStart;
        void AL(string n, string d, string ss)
            => list.Add(new SecsGemIdEntry {
                IdType="ALID", IdNumber=id++, Name=n, Description=d,
                SemiStandard="E30", Subsystem=ss, DataType="A:120"
            });

        // System
        AL("EmoPressed",                "Emergency Off pressed",                 "System");
        AL("DoorInterlockOpen",         "A door interlock opened during process","System");
        AL("UpsOnBattery",              "UPS switched to battery power",         "System");
        AL("CommunicationLost",         "HSMS link lost",                        "System");
        AL("SpoolFull",                 "Message spool full",                    "System");
        AL("SpoolOverflow",             "Spool overflow — messages discarded",   "System");

        // Utilities
        AL("DiwLowPressure",            "DI water supply low pressure",          "Utilities");
        AL("DiwLowResistivity",         "DI water resistivity below limit",      "Utilities");
        AL("CdaLowPressure",            "Clean dry air low pressure",            "Utilities");
        AL("N2LowPressure",             "N2 low pressure",                       "Utilities");
        AL("ExhaustLowFlow",            "Exhaust flow below limit",              "Utilities");
        AL("DrainLeakDetected",         "Liquid leak in drain pan",              "Utilities");
        AL("ChillerFault",              "Process chiller fault",                 "Utilities");

        // EFEM
        AL("EfemRobotFault",            "EFEM robot fault",                      "EFEM");
        AL("EfemAlignerFault",          "Aligner fault",                         "EFEM");
        AL("EfemFfuLow",                "FFU below minimum RPM",                 "EFEM");

        // Per chamber
        for (int c = 0; c < cfg.ProcessChamberCount; c++)
        {
            string n = c < cfg.ChamberNames.Length ? cfg.ChamberNames[c] : $"PM{c+1}";
            AL($"{n}_TempOutOfRange",       $"{n} bath temperature out of range",   n);
            AL($"{n}_LevelLow",             $"{n} bath/tank level low",             n);
            AL($"{n}_PumpFault",            $"{n} recirc pump fault",               n);
            AL($"{n}_FilterDpHigh",         $"{n} filter DP exceeds limit",         n);
            AL($"{n}_RecipeAbort",          $"{n} recipe aborted",                  n);
            if (n.Contains("Plate", StringComparison.OrdinalIgnoreCase))
            {
                AL($"{n}_AnodeOverCurrent", $"{n} anode current exceeded limit",    n);
                AL($"{n}_RectifierFault",   $"{n} plating rectifier fault",         n);
            }
        }
    }

    // -------- DVIDs ---------------------------------------------------------
    private static void AddDvids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.DvidStart;
        void DV(string n, string d, string t, string ss = "System", string u = "")
            => list.Add(new SecsGemIdEntry {
                IdType="DVID", IdNumber=id++, Name=n, Description=d,
                DataType=t, Units=u, SemiStandard="E5", Subsystem=ss,
                ReportableInDvvalList=true
            });

        DV("AlarmId",            "ALID for alarm-related events",      "U4");
        DV("AlarmText",          "Alarm text snapshot",                "A:120");
        DV("PpChangeName",       "Recipe name in PP change event",     "A:80");
        DV("PpChangeStatus",     "Recipe change status code",          "U1");
        DV("CarrierId",          "CarrierID for E87 events",           "A:32",  "Carrier");
        DV("PortId",             "Port number for E87 events",         "U1",    "Carrier");
        DV("SubstrateId",        "SubstrateID for E90 events",         "A:32",  "Substrate");
        DV("PrJobId",            "ProcessJob ID for E40 events",       "A:32",  "ProcessJob");
        DV("CtrlJobId",          "ControlJob ID for E94 events",       "A:32",  "ControlJob");
        DV("ProcessResultCode",  "0=Pass, 1..n = fail reason",         "U2");
        DV("RecipeStepNumber",   "Step at which event occurred",       "U2");
        DV("WaferProcessTime",   "Total seconds spent processing",     "F4", "System", "s");
    }

    // -------- RPTIDs --------------------------------------------------------
    // Skeleton reports the host can S2F33-define against.
    private static void AddRptids(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        uint id = cfg.RptidStart;
        void RP(string n, string d)
            => list.Add(new SecsGemIdEntry {
                IdType="RPTID", IdNumber=id++, Name=n, Description=d,
                SemiStandard="E30", Subsystem="Reporting", DataType="L"
            });

        RP("AlarmReport",     "ALID + AlarmText + Clock");
        RP("CarrierReport",   "CarrierId + PortId + Clock");
        RP("SubstrateReport", "SubstrateId + LocationID + Clock");
        RP("PrJobReport",     "PrJobId + state + Clock");
        RP("CtrlJobReport",   "CtrlJobId + state + Clock");
        RP("ProcessReport",   "Recipe + step + result code + Clock");
        RP("StateChangeReport","ControlState + PreviousProcessState + Clock");
    }

    // -------- E40 Process Job placeholders ---------------------------------
    private static void AddE40Examples(List<SecsGemIdEntry> list)
    {
        list.Add(new SecsGemIdEntry {
            IdType="PRJobID", IdNumber=null, Name="PRJ-{yyyyMMdd}-{seq:0000}",
            Description="Process job identifier — formatted A:n string per E40 §6",
            DataType="A:32", SemiStandard="E40", Subsystem="ProcessJob",
            Notes="Generated at job creation. Globally unique within the equipment."
        });
    }

    // -------- E94 Control Job placeholders ---------------------------------
    private static void AddE94Examples(List<SecsGemIdEntry> list)
    {
        list.Add(new SecsGemIdEntry {
            IdType="ControlJobID", IdNumber=null, Name="CJ-{yyyyMMdd}-{seq:0000}",
            Description="Control job identifier per E94 §6",
            DataType="A:32", SemiStandard="E94", Subsystem="ControlJob",
            Notes="Created by host or operator. Owns 1..n PRJobIDs."
        });
    }

    // -------- E87 Carrier placeholders -------------------------------------
    private static void AddE87Examples(List<SecsGemIdEntry> list, IdRangeConfig cfg)
    {
        list.Add(new SecsGemIdEntry {
            IdType="CarrierID", IdNumber=null, Name="<read-from-RFID>",
            Description="A:n carrier identifier read by load port reader",
            DataType="A:32", SemiStandard="E87", Subsystem="Carrier"
        });
        for (int p = 1; p <= cfg.LoadPortCount; p++)
            list.Add(new SecsGemIdEntry {
                IdType="PortID", IdNumber=(uint)p, Name=$"LoadPort{p}",
                Description="Physical port index per E87",
                DataType="U1", SemiStandard="E87", Subsystem=$"LP{p}"
            });
    }

    // -------- E90 Substrate placeholders -----------------------------------
    private static void AddE90Examples(List<SecsGemIdEntry> list)
    {
        list.Add(new SecsGemIdEntry {
            IdType="SubstrateID", IdNumber=null, Name="<host-supplied or auto>",
            Description="Substrate identifier per E90 §7. Must be unique while substrate is on the equipment.",
            DataType="A:32", SemiStandard="E90", Subsystem="Substrate"
        });
    }

    // -------- E116 EPT informational mapping --------------------------------
    private static void AddE116States(List<SecsGemIdEntry> list)
    {
        var states = new (uint id, string name, string desc)[] {
            (1, "NotScheduled",      "Equipment not scheduled to operate"),
            (2, "ScheduledDowntime", "Planned maintenance"),
            (3, "UnscheduledDowntime","Unplanned downtime"),
            (4, "Engineering",       "Engineering use"),
            (5, "Productive",        "Productive — running material"),
            (6, "Standby",           "Standby — ready, no material"),
        };
        foreach (var (id, name, desc) in states)
            list.Add(new SecsGemIdEntry {
                IdType="E116State", IdNumber=id, Name=name, Description=desc,
                DataType="U1", SemiStandard="E116", Subsystem="EPT"
            });
    }

    // -------- CSV serialization --------------------------------------------
    public string ToCsv(IEnumerable<SecsGemIdEntry> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IdType,IdNumber,Name,Description,DataType,Units,DefaultValue,MinValue,MaxValue,SemiStandard,Subsystem,ReportableInDvvalList,Notes");
        foreach (var r in rows)
        {
            sb.Append(Esc(r.IdType)).Append(',')
              .Append(r.IdNumber?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',')
              .Append(Esc(r.Name)).Append(',')
              .Append(Esc(r.Description)).Append(',')
              .Append(Esc(r.DataType)).Append(',')
              .Append(Esc(r.Units)).Append(',')
              .Append(Esc(r.DefaultValue)).Append(',')
              .Append(Esc(r.MinValue)).Append(',')
              .Append(Esc(r.MaxValue)).Append(',')
              .Append(Esc(r.SemiStandard)).Append(',')
              .Append(Esc(r.Subsystem)).Append(',')
              .Append(r.ReportableInDvvalList ? "true" : "false").Append(',')
              .AppendLine(Esc(r.Notes));
        }
        return sb.ToString();

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool needsQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            var v = s.Replace("\"", "\"\"");
            return needsQuote ? $"\"{v}\"" : v;
        }
    }

    // -------- JSON serialization --------------------------------------------
    // Grouped by IdType so the runtime framework can deserialize directly into
    // typed dictionaries (Dictionary<uint, SvidDef>, etc.) at startup.
    public string ToJson(IEnumerable<SecsGemIdEntry> rows, IdRangeConfig cfg)
    {
        var all = rows.ToList();
        var doc = new
        {
            generatedUtc = DateTime.UtcNow,
            schemaVersion = "1.0",
            config       = cfg,
            counts = new
            {
                total = all.Count,
                svid  = all.Count(r => r.IdType == "SVID"),
                ecid  = all.Count(r => r.IdType == "ECID"),
                ceid  = all.Count(r => r.IdType == "CEID"),
                alid  = all.Count(r => r.IdType == "ALID"),
                dvid  = all.Count(r => r.IdType == "DVID"),
                rptid = all.Count(r => r.IdType == "RPTID"),
            },
            svids         = all.Where(r => r.IdType == "SVID"),
            ecids         = all.Where(r => r.IdType == "ECID"),
            ceids         = all.Where(r => r.IdType == "CEID"),
            alids         = all.Where(r => r.IdType == "ALID"),
            dvids         = all.Where(r => r.IdType == "DVID"),
            rptids        = all.Where(r => r.IdType == "RPTID"),
            prJobIds      = all.Where(r => r.IdType == "PRJobID"),
            controlJobIds = all.Where(r => r.IdType == "ControlJobID"),
            carrierIds    = all.Where(r => r.IdType == "CarrierID"),
            portIds       = all.Where(r => r.IdType == "PortID"),
            substrateIds  = all.Where(r => r.IdType == "SubstrateID"),
            e116States    = all.Where(r => r.IdType == "E116State"),
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented          = true,
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder                = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }
}
#endregion