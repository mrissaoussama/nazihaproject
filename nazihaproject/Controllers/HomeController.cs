using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NazihaProject.Data;
using NazihaProject.Models;
using NazihaProject.ViewModels;
using System.Diagnostics;

namespace NazihaProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(RoleType.Responsable.ToString()))
        {
            // Admin dashboard
            var viewModel = new DashboardViewModel
            {
                PendingAnalysesCount = await _context.AnalysisRecords
                .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Pending),

                ApprovedAnalysesCount = await _context.AnalysisRecords
                .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Approved),

                RejectedAnalysesCount = await _context.AnalysisRecords
                    .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Rejected),

                AnalysesByType = await _context.AnalysisRecords
                    .Where(r => r.ApprovalStatus == ApprovalStatus.Pending)
                    .GroupBy(r => r.AnalysisType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Type, g => g.Count)
            };

            return View("AdminDashboard", viewModel);
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> GetAnalysisStatistics()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
    
        var result = new
        {
            todayCount = await _context.AnalysisRecords
                .CountAsync(r => r.AnalysisDate >= today && r.AnalysisDate < tomorrow),
            
            pendingCount = await _context.AnalysisRecords
                .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Pending),
            
            approvedCount = await _context.AnalysisRecords
                .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Approved),
            
            rejectedCount = await _context.AnalysisRecords
                .CountAsync(r => r.ApprovalStatus == ApprovalStatus.Rejected)
        };
    
        return Json(result);
    }
    [HttpGet]
    public async Task<IActionResult> GetRecentAnalyses()
    {
        try
        {
            var recentAnalyses = await _context.AnalysisRecords
                .Include(r => r.AnalysisData)
                .OrderByDescending(r => r.AnalysisDate)
                .Take(5)
                .ToListAsync();

            var viewModel = recentAnalyses.Select(r => new AnalysisResultViewModel
            {
                Id = r.Id,
                AnalysisType = r.AnalysisType,
                AnalysisDate = r.AnalysisDate,
                Shift = r.Shift,
                ApprovalStatus = r.ApprovalStatus
            }).ToList();

            return PartialView("_RecentAnalyses", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent analyses");
            return Content("<div class=\"text-center py-3 text-danger\">Erreur de chargement des données</div>");
        }
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}