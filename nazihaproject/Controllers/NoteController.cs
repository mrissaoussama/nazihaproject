using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nazihaproject.Models;
using System.Linq;
using System.Threading.Tasks;
using NazihaProject.Data;

namespace nazihaproject.Controllers
{
    public class NoteController : Controller
    {
        private readonly AppDbContext _context;

        public NoteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Note/Index - View all notes
        public async Task<IActionResult> Index()
        {
            var notes = await _context.Notes.ToListAsync();
            return View(notes);
        }

        // GET: Note/Create - Show create form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Note/Create - Add new note
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,LastName,Description")] Note note)
        {
            if (ModelState.IsValid)
            {
                _context.Add(note);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(note);
        }
    }
}