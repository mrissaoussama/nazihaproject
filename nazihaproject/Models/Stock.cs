using System.ComponentModel.DataAnnotations;

namespace NazihaProject.Models;

public class Stock
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nom { get; set; }

    [Required]
    public long Quantity { get; set; }
}