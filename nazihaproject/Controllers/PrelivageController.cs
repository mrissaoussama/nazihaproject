using Microsoft.AspNetCore.Mvc;
using NazihaProject.Data;
using NazihaProject.ViewModels;
using NazihaProject.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NazihaProject.Controllers;

public class PrelivageController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<PrelivageController> _logger;
    private int _count;

    public PrelivageController(AppDbContext context, ILogger<PrelivageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SaveAnalysis(AnalysisRequest request, string returnUrl = null)
    {
        _logger.LogInformation("SaveAnalysis called with: Type={Type}, Date={Date}, Shift={Shift}, HourlyDataCount={Count}",
            request.AnalysisType, request.Date, request.Shift, request.HourlyData?.Count ?? 0);

        try
        {
            if (!DateTime.TryParse(request.Date, out var analysisDate))
            {
                _logger.LogError("Invalid date format: {Date}", request.Date);
                TempData["ErrorMessage"] = "Format de date invalide";
                return RedirectToAction(GetActionNameForAnalysisType(request.AnalysisType));
            }

            foreach (var hourlyData in request.HourlyData)
            {
                var existingRecord = await _context.AnalysisRecords
                    .Include(r => r.AnalysisData)
                    .Where(r => r.AnalysisType == request.AnalysisType &&
                               r.AnalysisDate.Date == analysisDate.Date)
                    .Where(r => r.AnalysisData.Any(d => d.Hour == hourlyData.Hour &&
                                                      d.Category == hourlyData.Category))
                    .FirstOrDefaultAsync();

                if (existingRecord != null)
                {
                    _logger.LogWarning("Duplicate entry detected: Type={Type}, Date={Date}, Hour={Hour}, Category={Category}",
                        request.AnalysisType, request.Date, hourlyData.Hour, hourlyData.Category);

                    TempData["ErrorMessage"] = $"Un enregistrement existe déjà pour la catégorie '{hourlyData.Category}' à {hourlyData.Hour}:00 le {analysisDate.ToShortDateString()}";
                    return RedirectToAction(GetActionNameForAnalysisType(request.AnalysisType));
                }
            }

            var record = new AnalysisRecord
            {
                AnalysisType = request.AnalysisType,
                AnalysisDate = analysisDate,
                Shift = request.Shift,
                ApprovalStatus = ApprovalStatus.Pending
            };

            foreach (var h in request.HourlyData)
            {
                _logger.LogDebug("Processing hourly data: Hour={Hour}, Category={Category}, ParameterCount={Count}",
                    h.Hour, h.Category, h.Parameters?.Count ?? 0);

                record.AnalysisData.Add(new AnalysisData
                {
                    Hour = h.Hour,
                    Category = h.Category,
                    ParametersJson = JsonSerializer.Serialize(h.Parameters)
                });
            }

            _context.AnalysisRecords.Add(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Analysis record saved with ID: {Id}", record.Id);
            TempData["SuccessMessage"] = "Analyse enregistrée avec succès et en attente d'approbation";

            return RedirectToAction("AnalysisResults", new { type = request.AnalysisType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving analysis data");
            TempData["ErrorMessage"] = $"Erreur: {ex.Message}";
            return RedirectToAction(GetActionNameForAnalysisType(request.AnalysisType));
        }
    }
    private string GetActionNameForAnalysisType(AnalysisType type)
    {
        return type switch
        {
            AnalysisType.EauDeRetour => "EauDeRetour_Page",
            AnalysisType.ChaudiereFME => "Eauchaudure_Page",
            AnalysisType.LiquersSucree => "Liqueursucree_Page",
            AnalysisType.ColorationEgouts => "Poste_Decoloration_Egouts_Page",
            AnalysisType.ColorationRefonte => "Poste_Decoloration_Refonte_Page",
            AnalysisType.SucreBlancCristalisee => "SucreBlancCristalisee_Page",
            AnalysisType.SucreEnSachoge => "SucreEnSachoge_Page",
            AnalysisType.ColorationJetC => "Poste_Decoloration_JetC_Page",
            AnalysisType.Sirops => "Poste_Purete_Sirops_Page",
            AnalysisType.PureteEgouts => "Poste_Purete_Egouts_Page",
            AnalysisType.PureteJetC => "Poste_Purete_JetC_Page",
            AnalysisType.JetRaffine1 => "JetRaffine1_Page",
            AnalysisType.JetRaffine2 => "JetRaffine2_Page",
            _ => "Index"
        };
    }
    [HttpGet]
    public async Task<IActionResult> GetAnalysisDetails(int id)
    {
        try
        {
            var record = await _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .Include(r => r.ApprovedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
                return NotFound();

            var viewModel = new AnalysisDetailsViewModel
            {
                Id = record.Id,
                AnalysisType = record.AnalysisType,
                AnalysisDate = record.AnalysisDate,
                Shift = record.Shift,
                ApprovalStatus = record.ApprovalStatus,
                ApprovalDate = record.ApprovalDate,
                ApprovedByName = record.ApprovedBy != null ? $"{record.ApprovedBy.FirstName} {record.ApprovedBy.LastName}" : null,
                RejectionReason = record.RejectionReason,
                Data = record.AnalysisData.GroupBy(d => d.Hour)
                    .Select(g => new AnalysisDataGroupViewModel
                    {
                        Hour = g.Key,
                        Categories = g.Select(d => new AnalysisDataViewModel
                        {
                            Id = d.Id,
                            Category = d.Category,
                            Parameters = JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(d.ParametersJson)
                        }).ToList()
                    }).OrderBy(d => d.Hour).ToList()
            };

            return PartialView("_AnalysisDetails", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis details for ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving analysis details.");
        }
    }
    [HttpGet]
    public IActionResult AnalysisResults(AnalysisType? type, DateTime? date, int? hour, ApprovalStatus? status = null)
    {
        // Log the request with nullable type
        _logger.LogInformation("AnalysisResults called with: Type={Type}, Date={Date}, Hour={Hour}, Status={Status}",
            type, date, hour, status);

        try
        {
            var query = _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .Include(r => r.ApprovedBy)
                .AsQueryable();
            if (type.HasValue)
            {
                query = query.Where(r => r.AnalysisType == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.ApprovalStatus == status.Value);
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(r => r.AnalysisDate >= startDate && r.AnalysisDate < endDate);
            }

            if (hour.HasValue)
            {
                query = query.Where(r => r.AnalysisData.Any(d => d.Hour == hour.Value));
            }

            var baseRecords = query.OrderByDescending(r => r.AnalysisDate).ToList();

            var results = baseRecords
                .Select(r => new AnalysisResultViewModel
                {
                    Id = r.Id,
                    AnalysisType = r.AnalysisType,
                    AnalysisDate = r.AnalysisDate,
                    Shift = r.Shift,
                    ApprovalStatus = r.ApprovalStatus,
                    RejectionReason = r.RejectionReason,
                    ApprovalDate = r.ApprovalDate,
                    ApprovedByName = r.ApprovedBy != null ? $"{r.ApprovedBy.FirstName} {r.ApprovedBy.LastName}" : null,
                    Data = r.AnalysisData.GroupBy(d => d.Hour)
                        .Select(g => new AnalysisDataGroupViewModel
                        {
                            Hour = g.Key,
                            Categories = g.Select(d => new AnalysisDataViewModel
                            {
                                Id = d.Id,
                                Category = d.Category,
                                Parameters = JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(d.ParametersJson)
                            }).ToList()
                        }).ToList()
                })
                .Take(50)
                .ToList();

            ViewBag.AnalysisType = type;
            ViewBag.StatusFilter = status;
            ViewBag.DateFilter = date?.ToString("yyyy-MM-dd");
            ViewBag.HourFilter = hour;
            ViewBag.IsAdmin = User.IsInRole(RoleType.Responsable.ToString());

            return View(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis results");
            ViewBag.AnalysisType = type;
            ViewBag.StatusFilter = status;
            ViewBag.DateFilter = date?.ToString("yyyy-MM-dd");
            ViewBag.HourFilter = hour;
            ViewBag.IsAdmin = User.IsInRole(RoleType.Responsable.ToString());
            ViewBag.ErrorMessage = "An error occurred while retrieving analysis results: " + ex.Message;

            return View(new List<AnalysisResultViewModel>());
        }
    }

    [HttpGet]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> EditAnalysis(int id)
    {
        var record = await _context.AnalysisRecords
            .Include(r => r.AnalysisData)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null) return NotFound();

        var viewModel = new AnalysisResponse
        {
            Id = record.Id,
            AnalysisType = record.AnalysisType,
            AnalysisDate = record.AnalysisDate,
            Shift = record.Shift,
            AnalysisData = record.AnalysisData.Select(d => new AnalysisDataResponse
            {
                Hour = d.Hour,
                Category = d.Category,
                Parameters = JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(d.ParametersJson)
            }).ToList()
        };

        // Get the analysis config for this type to ensure we show the right fields
        var config = GetAnalysisConfig(record.AnalysisType);
        ViewBag.AnalysisConfig = config;

        return View(viewModel);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> UpdateAnalysis(AnalysisRequest request, int id, string returnUrl)
    {
        try
        {
            _logger.LogInformation("UpdateAnalysis called for ID: {Id}, Type: {Type}", id, request.AnalysisType);

            var record = await _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (record == null)
            {
                _logger.LogWarning("Record not found for ID: {Id}", id);
                TempData["ErrorMessage"] = "Enregistrement non trouvé";
                return RedirectToAction("AnalysisResults", new { type = request.AnalysisType });
            }

            if (!DateTime.TryParse(request.Date, out var analysisDate))
            {
                _logger.LogError("Invalid date format: {Date}", request.Date);
                TempData["ErrorMessage"] = "Format de date invalide";
                return RedirectToAction("EditAnalysis", new { id = id });
            }

            _logger.LogInformation("Original record type: {OriginalType}, New type: {NewType}",
                record.AnalysisType, request.AnalysisType);

            // Check for duplicate entries excluding this entry's ID
            foreach (var hourlyData in request.HourlyData)
            {
                var exists = await _context.AnalysisRecords
                    .AnyAsync(r => r.Id != id
                        && r.AnalysisType == request.AnalysisType
                        && r.AnalysisDate.Date == analysisDate.Date
                        && r.AnalysisData.Any(d => d.Hour == hourlyData.Hour
                                                && d.Category == hourlyData.Category));

                if (exists)
                {
                    _logger.LogWarning("Duplicate entry detected for category {Category} at hour {Hour}",
                        hourlyData.Category, hourlyData.Hour);
                    TempData["ErrorMessage"] = $"Un enregistrement existe déjà pour la catégorie '{hourlyData.Category}' à {hourlyData.Hour}:00";
                    return RedirectToAction("EditAnalysis", new { id = id });
                }
            }

            // Preserve the original type to avoid type changes
            // request.AnalysisType should match record.AnalysisType
            if (request.AnalysisType != record.AnalysisType)
            {
                _logger.LogWarning("Type mismatch detected! Original: {OriginalType}, Request: {RequestType}. Using original type.",
                    record.AnalysisType, request.AnalysisType);
                // Preserve the original type
                request.AnalysisType = record.AnalysisType;
            }

            record.AnalysisDate = analysisDate;
            record.Shift = request.Shift;
            // Reset approval status since the record has been edited
            record.ApprovalStatus = ApprovalStatus.Pending;
            record.ApprovalDate = null;
            record.ApprovedById = null;
            record.RejectionReason = null;

            // Clear and re-add the analysis data
            _context.AnalysisData.RemoveRange(record.AnalysisData);
            record.AnalysisData.Clear();

            foreach (var h in request.HourlyData)
            {
                _logger.LogDebug("Adding hourly data: Hour={Hour}, Category={Category}", h.Hour, h.Category);
                record.AnalysisData.Add(new AnalysisData
                {
                    Hour = h.Hour,
                    Category = h.Category,
                    ParametersJson = JsonSerializer.Serialize(h.Parameters)
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Analyse mise à jour avec succès";
            return RedirectToLocal(returnUrl, request.AnalysisType);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating record {Id}", id);
            TempData["ErrorMessage"] = "La donnée a été modifiée par un autre utilisateur. Veuillez réessayer.";
            return RedirectToAction("EditAnalysis", new { id = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in UpdateAnalysis");
            TempData["ErrorMessage"] = $"Erreur inattendue: {ex.Message}";
            return RedirectToAction("EditAnalysis", new { id = id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> ApproveAnalysis(int id)
    {
        try
        {
            var record = await _context.AnalysisRecords.FindAsync(id);
            if (record == null)
                return NotFound();

            record.ApprovalStatus = ApprovalStatus.Approved;
            record.ApprovalDate = DateTime.Now;

            // Get current user's ID
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                record.ApprovedById = userId;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Analyse approuvée avec succès";
            return RedirectToAction("AnalysisResults", new { type = record.AnalysisType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving analysis with ID: {Id}", id);
            TempData["ErrorMessage"] = $"Erreur: {ex.Message}";
            return RedirectToAction("AnalysisResults");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> RejectAnalysis(int id, string reason)
    {
        try
        {
            var record = await _context.AnalysisRecords.FindAsync(id);
            if (record == null)
                return NotFound();

            record.ApprovalStatus = ApprovalStatus.Rejected;
            record.ApprovalDate = DateTime.Now;
            record.RejectionReason = reason;

            // Get current user's ID
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                record.ApprovedById = userId;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Analyse rejetée";
            return RedirectToAction("AnalysisResults", new { type = record.AnalysisType });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting analysis with ID: {Id}", id);
            TempData["ErrorMessage"] = $"Erreur: {ex.Message}";
            return RedirectToAction("AnalysisResults");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> PendingAnalyses()
    {
        try
        {
            var pendingRecords = await _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .Where(r => r.ApprovalStatus == ApprovalStatus.Pending)
                .OrderByDescending(r => r.AnalysisDate)
                .ToListAsync();

            var viewModel = new PendingAnalysisViewModel
            {
                TotalPending = pendingRecords.Count,
                PendingCountByType = pendingRecords
                    .GroupBy(r => r.AnalysisType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PendingAnalyses = pendingRecords.Select(r => new AnalysisResultViewModel
                {
                    Id = r.Id,
                    AnalysisType = r.AnalysisType,
                    AnalysisDate = r.AnalysisDate,
                    Shift = r.Shift,
                    Data = r.AnalysisData.GroupBy(d => d.Hour)
                        .Select(g => new AnalysisDataGroupViewModel
                        {
                            Hour = g.Key,
                            Categories = g.Select(d => new AnalysisDataViewModel
                            {
                                Id = d.Id,
                                Category = d.Category,
                                Parameters = JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(d.ParametersJson)
                            }).ToList()
                        }).ToList()
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending analyses");
            TempData["ErrorMessage"] = $"Erreur: {ex.Message}";
            return View(new PendingAnalysisViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> BulkApproveAnalyses(List<int> recordIds)
    {
        try
        {
            if (recordIds == null || !recordIds.Any())
            {
                TempData["ErrorMessage"] = "Aucun enregistrement sélectionné";
                return RedirectToAction("PendingAnalyses");
            }

            var records = await _context.AnalysisRecords
                .Where(r => recordIds.Contains(r.Id) && r.ApprovalStatus == ApprovalStatus.Pending)
                .ToListAsync();

            if (!records.Any())
            {
                TempData["ErrorMessage"] = "Aucun enregistrement valide trouvé";
                return RedirectToAction("PendingAnalyses");
            }

            int userId = 0;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId))
            {
                foreach (var record in records)
                {
                    record.ApprovalStatus = ApprovalStatus.Approved;
                    record.ApprovalDate = DateTime.Now;
                    record.ApprovedById = userId;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{records.Count} analyses approuvées avec succès";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossible d'identifier l'utilisateur courant";
            }

            return RedirectToAction("PendingAnalyses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk approval");
            TempData["ErrorMessage"] = $"Erreur: {ex.Message}";
            return RedirectToAction("PendingAnalyses");
        }
    }
    private IActionResult RedirectToLocal(string returnUrl, AnalysisType analysisType)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("AnalysisResults", new { type = analysisType });
    }    
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult EauDeRetour_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.EauDeRetour);
        return View("AnalysisEntry", config);
    }

    public IActionResult SucreBlancCristalisee_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.SucreBlancCristalisee);
        return View("AnalysisEntry", config);
    }

    public IActionResult SucreEnSachoge_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.SucreEnSachoge);
        return View("AnalysisEntry", config);
    }

    public IActionResult JetRaffine1_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.JetRaffine1);
        return View("AnalysisEntry", config);
    }

    public IActionResult JetRaffine2_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.JetRaffine2);
        return View("AnalysisEntry", config);
    }

    // Update existing pages to use the new enum values
    public IActionResult Liqueursucree_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.LiquersSucree);
        return View("AnalysisEntry", config);
    }

    public IActionResult Eauchaudure_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.ChaudiereFME);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Decoloration_Refonte_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.ColorationRefonte);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Decoloration_Egouts_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.ColorationEgouts);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Decoloration_JetC_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.ColorationJetC);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Purete_Sirops_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.Sirops);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Purete_Egouts_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.PureteEgouts);
        return View("AnalysisEntry", config);
    }

    public IActionResult Poste_Purete_JetC_Page()
    {
        var config = GetAnalysisConfig(AnalysisType.PureteJetC);
        return View("AnalysisEntry", config);
    }

    [HttpGet]
    [Authorize(Roles = "Responsable")] // Restrict to admin role

    public IActionResult AnalysisStatistics(AnalysisType type, DateTime? startDate, DateTime? endDate)
    {
        _logger.LogInformation("AnalysisStatistics called with: Type={Type}, StartDate={StartDate}, EndDate={EndDate}",
            type, startDate, endDate);

        try
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            _logger.LogDebug($"Date range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

            // Query ALL records to get accurate statistics
            var approvedQuery = _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .Where(r => r.AnalysisType == type &&
                           r.AnalysisDate >= start &&
                           r.AnalysisDate <= end &&
                           r.ApprovalStatus == ApprovalStatus.Approved)
                .OrderByDescending(r => r.AnalysisDate);

            var pendingQuery = _context.AnalysisRecords
                .Where(r => r.AnalysisType == type &&
                           r.AnalysisDate >= start &&
                           r.AnalysisDate <= end &&
                           r.ApprovalStatus == ApprovalStatus.Pending);

            var rejectedQuery = _context.AnalysisRecords
                .Where(r => r.AnalysisType == type &&
                           r.AnalysisDate >= start &&
                           r.AnalysisDate <= end &&
                           r.ApprovalStatus == ApprovalStatus.Rejected);

            var approvedRecords = approvedQuery.ToList();
            var pendingCount = pendingQuery.Count();
            var rejectedCount = rejectedQuery.Count();

            _logger.LogDebug($"Found {approvedRecords.Count} approved records, {pendingCount} pending records, and {rejectedCount} rejected records");

            var statistics = new AnalysisStatisticsViewModel
            {
                AnalysisType = type,
                StartDate = start,
                EndDate = end
            };

            var today = DateTime.Today;
            var todaysApprovedReports = approvedRecords.Where(r => r.AnalysisDate.Date == today).ToList();
            var completedReportsCount = todaysApprovedReports.SelectMany(r => r.AnalysisData)
                .Select(d => d.Hour)
                .Distinct()
                .Count();

            // Count pending as remaining
            var pendingToday = _context.AnalysisRecords
                .Count(r => r.AnalysisType == type &&
                            r.AnalysisDate.Date == today &&
                            r.ApprovalStatus == ApprovalStatus.Pending);

            // Count rejected as missed
            var rejectedToday = _context.AnalysisRecords
                .Count(r => r.AnalysisType == type &&
                            r.AnalysisDate.Date == today &&
                            r.ApprovalStatus == ApprovalStatus.Rejected);

            var currentHour = DateTime.Now.Hour;
            var totalExpectedByNow = Math.Min(currentHour + 1, 24); // How many hours should have been completed by now
            var missedReports = Math.Max(0, totalExpectedByNow - completedReportsCount - pendingToday) + rejectedToday;
            var actualRemaining = Math.Max(0, 24 - completedReportsCount - missedReports);

            _logger.LogDebug($"Stats: Completed={completedReportsCount}, Pending={pendingToday}, Missed={missedReports}, Remaining={actualRemaining}");

            statistics.CompletedReportsCount = completedReportsCount;
            statistics.RemainingReportsCount = pendingToday;
            statistics.MissedReportsCount = missedReports;

            // Calculate statistics for the entire date range, not just today
            var categorizedData = approvedRecords
                .SelectMany(r => r.AnalysisData)
                .GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogDebug($"Found {categorizedData.Count} categories for statistics");

            foreach (var category in categorizedData)
            {
                var categoryStats = new CategoryStatistics
                {
                    Category = category.Key,
                    Parameters = []
                };

                var allParameters = new Dictionary<string, List<(decimal Value, string Unit)>>();

                foreach (var data in category.Value)
                {
                    var parameters = JsonSerializer.Deserialize<Dictionary<string, AnalysisParameter>>(data.ParametersJson);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            if (!allParameters.ContainsKey(param.Key))
                            {
                                allParameters[param.Key] = new List<(decimal, string)>();
                            }
                            allParameters[param.Key].Add((param.Value.Value, param.Value.Unit));
                        }
                    }
                }

                foreach (var param in allParameters)
                {
                    if (param.Value.Count > 0)
                    {
                        var values = param.Value.Select(p => p.Value).ToList();
                        var unit = param.Value.First().Unit;

                        categoryStats.Parameters.Add(new ParameterStatistics
                        {
                            Name = param.Key,
                            Unit = unit,
                            Average = values.Count > 0 ? values.Average() : 0,
                            Minimum = values.Count > 0 ? values.Min() : 0,
                            Maximum = values.Count > 0 ? values.Max() : 0,
                            Count = values.Count
                        });
                    }
                }

                statistics.Categories.Add(categoryStats);
                _logger.LogDebug($"Added category '{category.Key}' with {categoryStats.Parameters.Count} parameters");
            }

            ViewBag.AnalysisType = type;
            ViewBag.AllAnalysisTypes = Enum.GetValues(typeof(AnalysisType))
                                          .Cast<AnalysisType>()
                                          .ToList();
            return View(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating analysis statistics");
            ViewBag.AnalysisType = type;
            ViewBag.AllAnalysisTypes = Enum.GetValues(typeof(AnalysisType))
                                          .Cast<AnalysisType>()
                                          .ToList();
            ViewBag.ErrorMessage = "An error occurred while calculating statistics: " + ex.Message;
            return View(new AnalysisStatisticsViewModel { AnalysisType = type });
        }
    }
    private AnalysisConfigViewModel GetAnalysisConfig(AnalysisType type)
    {
        var config = new AnalysisConfigViewModel
        {
            Title = type.ToString(),
            AnalysisType = type,
            HistoricalDataUrl = Url.Action("AnalysisResults", "Prelivage", new { type = type })
        };

        switch (type)
        {
            case AnalysisType.ChaudiereFME:
                config.Title = "Chaudière FME Analysis";
                config.Tables =
                [
                    new()
                    {
                        Title = "Chaudière FME",
                        Category = "ChaudiereFME",
                        Parameters =
                        [
                            new() { Name = "TA", Unit = "°C" },
                            new() { Name = "TAC", Unit = "°F" },
                            new() { Name = "TH", Unit = "°F" },
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "PO4", Unit = "ppm" },
                            new() { Name = "Cond", Unit = "µS/cm" }
                        ]
                    },

                    new()
                    {
                        Title = "Vapeur Condensée",
                        Category = "VapeurCondensee",
                        Parameters = [new() { Name = "pH", Unit = "-" }, new() { Name = "Cond", Unit = "µS/cm" }]
                    }
                ];
                break;

            case AnalysisType.EauDeRetour:
                config.Title = "Eau de Retour Analysis";
                config.Tables =
                [
                    new()
                    {
                        Title = "Eau de Retour",
                        Category = "EauRetour",
                        Parameters =
                        [
                            new() { Name = "S%", Unit = "%" },
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Conductivité", Unit = "µS/cm" }
                        ]
                    }
                ];
                break;

            case AnalysisType.ColorationRefonte:
                config.Title = "Coloration Refonte";
                config.Tables =
                [
                    new()
                    {
                        Title = "Refonte Brute",
                        Category = "Refonte Brute",
                        Parameters =
                        [
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Refonte Epurée",
                        Category = "Refonte Epurée",
                        Parameters =
                        [
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Refonte Decoloration",
                        Category = "Refonte Decoloration",
                        Parameters =
                        [
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Refonte Concentrée",
                        Category = "Refonte Concentrée",
                        Parameters =
                        [
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Refonte Sirops",
                        Category = "Refonte Sirops",
                        Parameters =
                        [
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    }
                ];
                break;

            case AnalysisType.ColorationEgouts:
                config.Title = "Coloration Egouts";
                config.Tables =
                [
                    new()
                    {
                        Title = "Sirops+ER1",
                        Category = "Sirops+ER1",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" }
                        ]
                    },

                    new()
                    {
                        Title = "E.R1(AT)",
                        Category = "E.R1(AT)",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "col", Unit = "UI" }
                        ]
                    }

                ];
                break;

            case AnalysisType.LiquersSucree:
                config.Title = "Liqueurs Sucrées Analysis";
           config.Tables = new List<AnalysisTableConfig>
                {
                    new AnalysisTableConfig
                    {
                        Title = "Refonte Brute",
                        Category = "Refonte Brute",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "Prix", Unit = "€" },
                            new ParameterConfig { Name = "pH", Unit = "-" },
                            new ParameterConfig { Name = "TC", Unit = "°C" }
                        }
                    },
                    new AnalysisTableConfig
                    {
                        Title = "Four à Chaud",
                        Category = "Four à Chaud",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "CO2", Unit = "ppm" },
                            new ParameterConfig { Name = "O2", Unit = "ppm" },
                            new ParameterConfig { Name = "Alc", Unit = "%" }
                        }
                    },
                    new AnalysisTableConfig
                    {
                        Title = "Chauffage",
                        Category = "Chauffage",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "Alc", Unit = "%" }
                        }
                    },
                    new AnalysisTableConfig
                    {
                        Title = "1ère Corbo",
                        Category = "1ère Corbo",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "Alc", Unit = "%" },
                            new ParameterConfig { Name = "pH", Unit = "-" },
                            new ParameterConfig { Name = "TC", Unit = "°C" }
                        }
                    },
                    new AnalysisTableConfig
                    {
                        Title = "2ème Corbo",
                        Category = "2ème Corbo",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "Alc", Unit = "%" },
                            new ParameterConfig { Name = "pH", Unit = "-" },
                            new ParameterConfig { Name = "TC", Unit = "°C" }
                        }
                    },
                    new AnalysisTableConfig
                    {
                        Title = "Refonte Brut",
                        Category = "Refonte Brut",
                        Parameters = new List<ParameterConfig>
                        {
                            new ParameterConfig { Name = "Brix", Unit = "°Bx" },
                            new ParameterConfig { Name = "pH", Unit = "-" },
                            new ParameterConfig { Name = "TC", Unit = "°C" }
                        }
                    }
                };
                break;
            case AnalysisType.ColorationJetC:
                config.Title = "Coloration Jet C Analysis";
                config.Tables =
                [
                    new()
                    {
                        Title = "Masse Cuite",
                        Category = "Masse Cuite",
                        Parameters =
                        [
                            new() { Name = "N", Unit = "-" },
                            new() { Name = "H", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Color", Unit = "UI" },
                            new() { Name = "RT", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Egouts Mère",
                        Category = "Egouts Mère",
                        Parameters = [new() { Name = "Bx", Unit = "°Bx" }, new() { Name = "Color", Unit = "UI" }]
                    }
                ];
                break;
            case AnalysisType.JetRaffine1:
                config.Title = "Jet Raffiné 1";
                config.Tables =
                [
                    new()
                    {
                        Title = "Masse Cuite JR1",
                        Category = "Masse Cuite JR1",
                        Parameters =
                        [
                            new() { Name = "N", Unit = "-" },
                            new() { Name = "H", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Egouts JR1",
                        Category = "Egouts JR1",
                        Parameters = [new() { Name = "Bx", Unit = "°Bx" }, new() { Name = "Pureté", Unit = "%" }]
                    }
                ];
                break;

            // Update JetRaffine2 with the correct parameters
            case AnalysisType.JetRaffine2:
                config.Title = "Jet Raffiné 2";
                config.Tables =
                [
                    new()
                    {
                        Title = "Masse Cuite JR2",
                        Category = "Masse Cuite JR2",
                        Parameters =
                        [
                            new() { Name = "N", Unit = "-" },
                            new() { Name = "H", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" },
                            new() { Name = "pH", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Egouts JR2",
                        Category = "Egouts JR2",
                        Parameters = [new() { Name = "Bx", Unit = "°Bx" }, new() { Name = "Pureté", Unit = "%" }]
                    }
                ];
                break;
            case AnalysisType.Sirops:
                config.Title = "Sirops Analysis";
                config.Tables =
                [
                    new()
                    {
                        Title = "Sirops",
                        Category = "Sirops",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Brix", Unit = "°Bx" },
                            new() { Name = "Purete", Unit = "%" }
                        ]
                    }
                ];
                break;

            case AnalysisType.PureteEgouts:
                config.Title = "Pureté Egouts";
                config.Tables =
                [
                    new()
                    {
                        Title = "Sirops+ER1",
                        Category = "Sirops+ER1",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },

                    new()
                    {
                        Title = "E.R1(AT)",
                        Category = "E.R1(AT)",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },


                    new()
                    {
                        Title = "E.R1+E.R2",
                        Category = "E.R1+E.R2",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },

                    new()
                    {
                        Title = "E.R2",
                        Category = "E.R2",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },

                    new()
                    {
                        Title = "E.APP",
                        Category = "E.APP",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },

                    new()
                    {
                        Title = "E.A",
                        Category = "E.A",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    },

                    new()
                    {
                        Title = "E.B",
                        Category = "E.B",
                        Parameters =
                        [
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "Pureté", Unit = "%" }
                        ]
                    }
                ];

    break;

            case AnalysisType.PureteJetC:
                config.Title = "Pureté Jet C";
                config.Tables =
                [
                    new()
                    {
                        Title = "Masse Cuite",
                        Category = "Masse Cuite",
                        Parameters =
                        [
                            new() { Name = "N", Unit = "-" },
                            new() { Name = "H", Unit = "-" },
                            new() { Name = "Bx", Unit = "°Bx" },
                            new() { Name = "pH", Unit = "-" },
                            new() { Name = "Pureté", Unit = "%" },
                            new() { Name = "RT", Unit = "-" }
                        ]
                    },

                    new()
                    {
                        Title = "Egouts Mère",
                        Category = "Egouts Mère",
                        Parameters = [new() { Name = "Bx", Unit = "°Bx" }, new() { Name = "Pureté", Unit = "%" }]
                    }
                ];
                break;

            case AnalysisType.SucreBlancCristalisee:
                config.Title = "Sucre Blanc Cristalisé";
                config.Tables =
                [
                    new()
                    {
                        Title = "Sucre Blanc",
                        Category = "Sucre Blanc",
                        Parameters =
                        [
                            new() { Name = "Humidité", Unit = "%" },
                            new() { Name = "Couleur", Unit = "UI" },
                            new() { Name = "Cendres", Unit = "ppm" },
                            new() { Name = "Granulométrie", Unit = "mm" }
                        ]
                    }
                ];
                break;

            case AnalysisType.SucreEnSachoge:
                config.Title = "Sucre en Sachets";
                config.Tables =
                [
                    new()
                    {
                        Title = "Sucre en Sachets",
                        Category = "Sucre en Sachets",
                        Parameters =
                        [
                            new() { Name = "Poids", Unit = "g" },
                            new() { Name = "Humidité", Unit = "%" },
                            new() { Name = "Qualité emballage", Unit = "-" }
                        ]
                    }
                ];
                break;

 

            default:
                config.Tables =
                [
                    new()
                    {
                        Title = "Default Table",
                        Category = "Default",
                        Parameters = [new() { Name = "pH", Unit = "-" }, new() { Name = "Temperature", Unit = "°C" }]
                    }
                ];
                break;
        }

        return config;
    }
}


