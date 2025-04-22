using Microsoft.EntityFrameworkCore;
using NazihaProject.Models;

namespace NazihaProject.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<AnalysisRecord> AnalysisRecords { get; set; }
    public DbSet<AnalysisData> AnalysisData { get; set; }


}