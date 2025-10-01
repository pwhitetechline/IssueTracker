using IssueTracker.Data;
using IssueTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Pages.Issues;

public class IndexModel(AppDbContext db, NotificationService notifier) : PageModel
{
    public List<Issue> Issues { get; set; } = new();

    public readonly string[] Statuses = ["Open","In Progress","Blocked","Resolved","Closed"];
    public readonly string[] Priorities = ["Low","Medium","High","Critical"];
    public readonly (string value, string label)[] SortOptions = [
        ("date_desc","Newest"),
        ("date_asc","Oldest"),
        ("priority_desc","Priority ↓"),
        ("priority_asc","Priority ↑"),
        ("status_asc","Status A→Z"),
        ("status_desc","Status Z→A"),
    ];

    [BindProperty(SupportsGet = true)]
    public FilterState Filters { get; set; } = new();

    public async Task OnGetAsync()
    {
        var q = db.Issues.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Filters.Status))
            q = q.Where(i => i.Status == Filters.Status);
        if (!string.IsNullOrWhiteSpace(Filters.Priority))
            q = q.Where(i => i.IssuePriority == Filters.Priority);
        if (!string.IsNullOrWhiteSpace(Filters.Assignee))
            q = q.Where(i => i.AssignedTo != null && EF.Functions.Like(i.AssignedTo!, $"%{Filters.Assignee}%"));
        if (!string.IsNullOrWhiteSpace(Filters.Search))
        {
            var s = $"%{Filters.Search}%";
            q = q.Where(i =>
                EF.Functions.Like(i.IssueDescription, s) ||
                EF.Functions.Like(i.IssueType, s) ||
                EF.Functions.Like(i.WebsiteUrl, s) ||
                EF.Functions.Like(i.ReporterName, s) ||
                (i.AssignedTo != null && EF.Functions.Like(i.AssignedTo!, s))
            );
        }

        q = Filters.Sort switch
        {
            "date_asc" => q.OrderBy(i => i.DateReported),
            "priority_desc" => q.OrderByDescending(i => i.IssuePriority),
            "priority_asc" => q.OrderBy(i => i.IssuePriority),
            "status_asc" => q.OrderBy(i => i.Status),
            "status_desc" => q.OrderByDescending(i => i.Status),
            _ => q.OrderByDescending(i => i.DateReported)
        };

        Issues = await q.Include(i => i.IssueTypeRef).ToListAsync();
    }

    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        var issue = await db.Issues.FindAsync(id);
        if (issue is null) return NotFound();
        var before = new Issue { Id = issue.Id, Status = issue.Status, WebsiteUrl = issue.WebsiteUrl, IssuePriority = issue.IssuePriority, IssueType = issue.IssueType, AssignedTo = issue.AssignedTo, Screenshot = issue.Screenshot, IssueDescription = issue.IssueDescription };
        issue.Status = "Resolved";
        issue.DateResolved = DateTime.UtcNow.Date;
        await db.SaveChangesAsync();
        await notifier.NotifyStatusChangeAsync(before, issue);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var issue = await db.Issues.FindAsync(id);
        if (issue is null) return NotFound();
        db.Issues.Remove(issue);
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public class FilterState
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? Assignee { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; } = "date_desc";
    }
}