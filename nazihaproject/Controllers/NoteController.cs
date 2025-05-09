using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nazihaproject.Models;
using System.Linq;
using System.Threading.Tasks;
using NazihaProject.Data;
using Microsoft.AspNetCore.Authorization;
using NazihaProject.Models;

namespace nazihaproject.Controllers
{
    [Authorize] 
    public class NoteController : Controller
    {
        private readonly AppDbContext _context;

        public NoteController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = nameof(RoleType.Responsable))]
        public async Task<IActionResult> Index()
        {
            var notes = await _context.Notes.ToListAsync();
            return View(notes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,LastName,Description")] Note note)
        {
            if (ModelState.IsValid)
            {
                _context.Add(note);
                await _context.SaveChangesAsync();
                
                if (User.IsInRole(nameof(RoleType.Responsable)))
                    return RedirectToAction(nameof(Index));
                else
                    return RedirectToAction("Index", "Home");
            }
            return View(note);
        }
    }
}