using IssueTracker.Data;
using IssueTracker.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    // KPIs
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int BlockedCount { get; set; }
    public int ResolvedToday { get; set; }

    // Recent Open issues
    public List<Issue> RecentOpen { get; set; } = new();

    public async Task OnGetAsync()
    {
        ViewData["Title"] = "Dashboard";
        OpenCount       = await _db.Issues.CountAsync(i => i.Status == "Open");
        InProgressCount = await _db.Issues.CountAsync(i => i.Status == "In Progress");
        BlockedCount    = await _db.Issues.CountAsync(i => i.Status == "Blocked");
        ResolvedToday   = await _db.Issues.CountAsync(i => i.Status == "Resolved" && i.DateResolved >= DateTime.UtcNow.Date);

        RecentOpen = await _db.Issues
            .Where(i => i.Status == "Open")
            .OrderByDescending(i => i.IssuePriority)   // Critical/High first
            .ThenByDescending(i => i.DateReported)
            .Take(10)
            .ToListAsync();
    }
}
