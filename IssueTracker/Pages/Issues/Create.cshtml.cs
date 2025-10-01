using IssueTracker.Data;
using IssueTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Pages.Issues;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _storage;
    private readonly NotificationService _notifier;

    public CreateModel(AppDbContext db, FileStorageService storage, NotificationService notifier)
    {
        _db = db;
        _storage = storage;
        _notifier = notifier;
    }

    // The issue bound to the form
    [BindProperty]
    public Issue Issue { get; set; } = new();

    // Dropdown options (Id + Name)
    public List<SelectListItem> IssueTypeOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadIssueTypeOptionsAsync();

        // Optional defaults
        Issue.Status = "Open";
        Issue.IssuePriority = "Medium";
        Issue.DateReported = DateTime.UtcNow.Date;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile? screenshot)
    {
        // Always (re)load options when returning the page
        await LoadIssueTypeOptionsAsync();

        // Validate selected IssueTypeId
        if (Issue.IssueTypeId is null || !await _db.IssueTypes.AnyAsync(t => t.Id == Issue.IssueTypeId))
        {
            ModelState.AddModelError("Issue.IssueTypeId", "Please choose a valid issue type.");
        }

        if (!ModelState.IsValid)
        {
            // Page() will use IssueTypeOptions already loaded above
            return Page();
        }

        // Optional screenshot upload
        try
        {
            if (screenshot is not null && screenshot.Length > 0)
                Issue.Screenshot = await _storage.SaveAsync(screenshot);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Issue.Screenshot", ex.Message);
            return Page(); // redisplay with error
        }

        // Sensible defaults if not provided
        if (Issue.DateReported is null) Issue.DateReported = DateTime.UtcNow.Date;
        if (string.IsNullOrWhiteSpace(Issue.Status)) Issue.Status = "Open";
        if (string.IsNullOrWhiteSpace(Issue.IssuePriority)) Issue.IssuePriority = "Medium";

        _db.Issues.Add(Issue);
        await _db.SaveChangesAsync();
        await _notifier.NotifyIssueCreatedAsync(Issue);

        return RedirectToPage("Index");
    }

    private async Task LoadIssueTypeOptionsAsync()
    {
        IssueTypeOptions = await _db.IssueTypes
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();
    }
}
