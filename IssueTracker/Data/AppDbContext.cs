using IssueTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();
}

