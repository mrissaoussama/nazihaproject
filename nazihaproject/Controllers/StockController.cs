using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NazihaProject.Data;
using NazihaProject.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NazihaProject.Controllers;

public class StockController : Controller
{
    private readonly AppDbContext _context;

    public StockController(AppDbContext context)
    {
        _context = context;
    }

    // Afficher la liste de tous les articles dans le stock
    public async Task<IActionResult> Stock_Page()
    {
        var stocks = await _context.Stocks.ToListAsync();
        return View(stocks);
    }

    // Ajouter un article au stock
    public IActionResult AjouterArticle()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AjouterArticle(string nom, long quantity)
    {
        var article = new Stock
        {
            Nom = nom,
            Quantity = quantity
        };

        _context.Stocks.Add(article);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Stock_Page));
    }

    // Modifier la quantité d'un article
    [HttpPost]
    public async Task<IActionResult> ModifierArticle(int id, long quantity)
    {
        var article = await _context.Stocks.FindAsync(id);
        if (article == null) return NotFound();

        article.Quantity = quantity;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    // Supprimer un article du stock
    [HttpPost]
    public async Task<IActionResult> SupprimerArticle(int id)
    {
        var article = await _context.Stocks.FindAsync(id);
        if (article == null) return NotFound();

        _context.Stocks.Remove(article);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}