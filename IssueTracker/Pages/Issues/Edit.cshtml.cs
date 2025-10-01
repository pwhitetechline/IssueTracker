using IssueTracker.Data;
using IssueTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IssueTracker.Pages.Issues;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _storage;
    // private readonly NotificationService _notifier; // enable when you want webhooks

    public EditModel(AppDbContext db, FileStorageService storage /*, NotificationService notifier */)
    {
        _db = db;
        _storage = storage;
        // _notifier = notifier;
    }

    [BindProperty]
    public Issue Issue { get; set; } = new();

    // Dropdown options for Issue Types
    public List<SelectListItem> IssueTypeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var issue = await _db.Issues
            .Include(i => i.IssueTypeRef)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (issue is null) return NotFound();

        Issue = issue;
        await LoadIssueTypeOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, IFormFile? screenshot)
    {
        await LoadIssueTypeOptionsAsync();

        var existing = await _db.Issues
            .Include(i => i.IssueTypeRef)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return NotFound();

        // snapshot BEFORE
        var before = new Issue
        {
            Id = existing.Id,
            WebsiteUrl = existing.WebsiteUrl,
            IssuePriority = existing.IssuePriority,
            IssueTypeId = existing.IssueTypeId,
            IssueTypeRef = existing.IssueTypeRef,
            Status = existing.Status,
            AssignedTo = existing.AssignedTo,
            Screenshot = existing.Screenshot,
            DateReported = existing.DateReported,
            DateResolved = existing.DateResolved,
            IssueType = existing.IssueType // if legacy field exists
        };

        // 1) Try upload FIRST so invalid file shows correct message
        try
        {
            if (screenshot is not null && screenshot.Length > 0)
            {
                var path = await _storage.SaveAsync(screenshot); // throws on bad type/size
                existing.Screenshot = path;
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Issue.Screenshot", ex.Message);
            return Page();
        }

        // 2) Validate IssueType selection
        if (Issue.IssueTypeId is null || !await _db.IssueTypes.AnyAsync(t => t.Id == Issue.IssueTypeId))
        {
            ModelState.AddModelError("Issue.IssueTypeId", "Please choose a valid issue type.");
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        // 3) Map edited fields
        existing.DateReported     = Issue.DateReported;
        existing.ReporterName     = Issue.ReporterName;
        existing.WebsiteUrl       = Issue.WebsiteUrl;
        existing.IssueTypeId      = Issue.IssueTypeId;
        existing.IssuePriority    = Issue.IssuePriority;
        existing.IssueDescription = Issue.IssueDescription;
        existing.Status           = Issue.Status;
        existing.AssignedTo       = Issue.AssignedTo;
        existing.DateResolved     = Issue.DateResolved;
        existing.ResolutionNotice = Issue.ResolutionNotice;

        await _db.SaveChangesAsync();

        // Webhooks (re-enable when ready)
        // await _notifier.NotifyIssueUpdatedAsync(before, existing);
        // await _notifier.NotifyStatusChangeAsync(before, existing);

        return RedirectToPage("Index");
    }

    private async Task LoadIssueTypeOptionsAsync()
    {
        IssueTypeOptions = await _db.IssueTypes
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            })
            .ToListAsync();
    }
}
