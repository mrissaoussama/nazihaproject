using NazihaProject.Models;
 
namespace NazihaProject.ViewModels
{
    public class AnalysisConfigViewModel
    {
        public string Title { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public List<AnalysisTableConfig> Tables { get; set; } = new List<AnalysisTableConfig>();
        public string HistoricalDataUrl { get; set; }
    }

    public class AnalysisTableConfig
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public List<ParameterConfig> Parameters { get; set; } = new List<ParameterConfig>();
    }

    public class ParameterConfig
    {
        public string Name { get; set; }
        public string Unit { get; set; }
    }
    public class AnalysisDataViewModel
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public Dictionary<string, AnalysisParameter> Parameters { get; set; }
    }
    public class AnalysisDataResponse
    {
        public int Id { get; set; }
        public int Hour { get; set; }
        public string Category { get; set; }
        public Dictionary<string, AnalysisParameter> Parameters { get; set; }
    }
    public class AnalysisDataGroupViewModel
    {
        public int Hour { get; set; }
        public List<AnalysisDataViewModel> Categories { get; set; }
    }

    public class AnalysisRequest
    {
        public AnalysisType AnalysisType { get; set; }
        public string Date { get; set; }
        public string Shift { get; set; }
        public List<HourlyDataRequest> HourlyData { get; set; }
    }
    public class AnalysisResponse
    {
        public int Id { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Shift { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AnalysisDataResponse> AnalysisData { get; set; }
    }
    public class AnalysisResultViewModel
    {
        public int Id { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Shift { get; set; }
        public List<AnalysisDataGroupViewModel> Data { get; set; } = new List<AnalysisDataGroupViewModel>();
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? ApprovedByName { get; set; }
    }

    public class ArticleViewModel
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public long Quantity { get; set; }
        public string StockId { get; set; }
        public string StockNom { get; set; }
    }
    public class HourlyDataRequest
    {
        public int Hour { get; set; }
        public string Category { get; set; }
        public Dictionary<string, AnalysisParameter> Parameters { get; set; }
    }
    public class AnalysisStatisticsViewModel
    {
        public AnalysisType AnalysisType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CategoryStatistics> Categories { get; set; } = new List<CategoryStatistics>();
        public int CompletedReportsCount { get; set; }
        public int RemainingReportsCount { get; set; }
        public int MissedReportsCount { get; set; }
    }
    public class CategoryStatistics
    {
        public string Category { get; set; }
        public List<ParameterStatistics> Parameters { get; set; } = new List<ParameterStatistics>();
    }
    public class ParameterStatistics
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Average { get; set; }
        public decimal Minimum { get; set; }
        public decimal Maximum { get; set; }
        public int Count { get; set; }
    }

    public class PendingAnalysisViewModel
    {
        public List<AnalysisResultViewModel> PendingAnalyses { get; set; } = new List<AnalysisResultViewModel>();
        public Dictionary<AnalysisType, int> PendingCountByType { get; set; } = new Dictionary<AnalysisType, int>();
        public int TotalPending { get; set; }
    }

    public class ApproveRejectViewModel
    {
        public int Id { get; set; }
        public ApprovalStatus Status { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class DashboardViewModel
    {
        public int PendingAnalysesCount { get; set; }
        public int ApprovedAnalysesCount { get; set; }
        public int RejectedAnalysesCount { get; set; }
        public Dictionary<AnalysisType, int> AnalysesByType { get; set; } = new Dictionary<AnalysisType, int>();
    }
    public class AnalysisDetailsViewModel
    {
        public int Id { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Shift { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? ApprovedByName { get; set; }
        public string? RejectionReason { get; set; }
        public List<AnalysisDataGroupViewModel> Data { get; set; } = new List<AnalysisDataGroupViewModel>();
    }
}
