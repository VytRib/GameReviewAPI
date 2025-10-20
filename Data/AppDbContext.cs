using GameReviewsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GameReviewsAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<User> Users => Set<User>();
}