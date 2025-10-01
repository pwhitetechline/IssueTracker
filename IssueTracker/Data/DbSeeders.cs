using IssueTracker.Data;
using IssueTracker.Models;
using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.IssueTypes.Any())
        {
            db.IssueTypes.AddRange(
                new IssueType { Name = "Website Bug" },
                new IssueType { Name = "UI Bug" },
                new IssueType { Name = "Performance" },
                new IssueType { Name = "Security" },
                new IssueType { Name = "API" },
                new IssueType { Name = "Content" },
                new IssueType { Name = "SEO" },
                new IssueType { Name = "UX" },
                new IssueType { Name = "Infrastructure" },
                new IssueType { Name = "Feature Request" }
            );
            db.SaveChanges();
        }
    }
}
