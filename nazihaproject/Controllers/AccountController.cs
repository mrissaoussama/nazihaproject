using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NazihaProject.Data;
using NazihaProject.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace NazihaProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // If user is already logged in, redirect to home page
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            
            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            
            // Find user by username and check password (in production use hashing)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View(model);
            }
            
            // Build claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Add user ID claim
            };
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
            
            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
        
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
