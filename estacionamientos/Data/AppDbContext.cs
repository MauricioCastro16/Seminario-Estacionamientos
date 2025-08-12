using Microsoft.EntityFrameworkCore;
using estacionamientos.Models;

namespace estacionamientos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayaEstacionamiento> PlayasEstacionamiento => Set<PlayaEstacionamiento>();
}
