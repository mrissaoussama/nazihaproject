using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using NazihaProject.Models;

namespace NazihaProject.ViewModels;
public class UserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public RoleType[] Roles { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Matricule { get; set; }
    public string Password { get; set; }

    public static IEnumerable<SelectListItem> GetRoles()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = RoleType.Responsable.ToString(), Text = "Responsable" },
            new SelectListItem { Value = RoleType. TechnicienPosteEau.ToString(), Text = "Technicien Poste Eau" },
            new SelectListItem { Value = RoleType.TechnicienPosteColoration.ToString(), Text = "Technicien Poste Coloration" },
            new SelectListItem { Value = RoleType.TechnicienPostePurete.ToString(), Text = "Technicien Poste Purette " }
        };
    }
}

public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
    public string SearchTerm { get; set; }
    public string SortBy { get; set; }
    public bool SortAscending { get; set; } = true;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
}


