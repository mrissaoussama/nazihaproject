using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NazihaProject.Models;

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
public enum AnalysisType
{
    EauDeRetour,
    ChaudiereFME,
    LiquersSucree,
    ColorationEgouts,
    ColorationRefonte,
    SucreBlancCristalisee ,
    SucreEnSachoge,
    ColorationJetC,
    Sirops,
    PureteEgouts,
    PureteJetC,
    JetRaffine1,
    JetRaffine2
}
public class AnalysisRecord
{
    public int Id { get; set; }
    public AnalysisType AnalysisType { get; set; }
    public DateTime AnalysisDate { get; set; }
    public string Shift { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int? ApprovedById { get; set; }

    [ForeignKey("ApprovedById")]
    public User? ApprovedBy { get; set; }

    [JsonIgnore]
    public List<AnalysisData> AnalysisData { get; set; } = new List<AnalysisData>();
}

public class AnalysisData
{
    public int Id { get; set; }
    public int AnalysisRecordId { get; set; }

    [JsonIgnore]
    public AnalysisRecord AnalysisRecord { get; set; }

    public int Hour { get; set; }
    public string Category { get; set; }
    public string ParametersJson { get; set; }

    [NotMapped] // This property won't be mapped to database
    public Dictionary<string, AnalysisParameter> Parameters =>
        JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(ParametersJson)
        ?? new Dictionary<string, AnalysisParameter>();
}

public class AnalysisParameter
{
    public decimal Value { get; set; }
    public string Unit { get; set; }
}