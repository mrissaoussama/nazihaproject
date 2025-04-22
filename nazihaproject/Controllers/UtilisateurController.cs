using Microsoft.AspNetCore.Mvc;
using NazihaProject.ViewModels;
using NazihaProject.Models;
using NazihaProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace NazihaProject.Controllers;

public class UtilisateurController : Controller
{
    private readonly AppDbContext _context;
    public UtilisateurController(AppDbContext context)
    {
        _context = context;
    }
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Register()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Register(UserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if the email already exists
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email already exists.");
            return View(model);
        }

        // Hash password before storing
     

        var utilisateur = new User
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Matricule = model.Matricule,
            Email = model.Email,
            Password = model.Password,
             
        };
        utilisateur.Roles.AddRange(model.Roles);

        _context.Users.Add(utilisateur);
        await _context.SaveChangesAsync();

        return RedirectToAction("List_Utilisateur"); // Redirect to login page after successful registration
    }

        
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> UserList()
    {
        var users = await _context.Users
            .Select(u => new UserViewModel {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Roles = u.Roles.ToArray(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToListAsync();

        return View(new UserListViewModel { Users = users });
    }





    public async Task<IActionResult> Edit_Utilisateur(int id)
    {
        var utilisateur = await _context.Users.FindAsync(id);
        if (utilisateur == null)
        {
            return NotFound();
        }

        var model = new UserViewModel
        {
            Id = utilisateur.Id,
            FirstName = utilisateur.FirstName,
            LastName = utilisateur.LastName,
            Matricule = utilisateur.Matricule,
            Email = utilisateur.Email,

        };
        utilisateur.Roles.AddRange(model.Roles);


        return View(model);
    }

    // POST: Update User
    [HttpPost]
    public async Task<IActionResult> Edit_Utilisateur(UserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Erreur : {modelError.ErrorMessage}");
            }
            return View(model); // Retourne la vue avec les erreurs affichées
        }


        var utilisateur = await _context.Users.FindAsync(model.Id);
        if (utilisateur == null)
        {
            return NotFound();
        }

        // Mise à jour des champs
        utilisateur.FirstName = model.FirstName;
        utilisateur.LastName = model.LastName;
        utilisateur.Matricule = model.Matricule;
        utilisateur.Email = model.Email;

    utilisateur.Roles.AddRange(model.Roles);

        try
        {
            _context.Users.Update(utilisateur);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Une erreur s'est produite lors de la mise à jour.");
            return View(model);
        }

        return RedirectToAction("List_Utilisateur");
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {

        var utilisaateur = await _context.Users.FindAsync(id);
        if (utilisaateur == null)
        {
            return NotFound();
        }

        _context.Users.Remove(utilisaateur);

        await _context.SaveChangesAsync();

        return RedirectToAction("List_Utilisateur");
    }
    public IActionResult Login()
    {
        return View();
    }
}