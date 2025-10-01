using System.ComponentModel.DataAnnotations;

namespace IssueTracker.Models;

public class Issue
{
    public int Id { get; set; }

    [Display(Name="Date Reported")]
    [DataType(DataType.Date)]
    public DateTime? DateReported { get; set; }

    [Required, StringLength(120)]
    public string ReporterName { get; set; } = "";

    [Required, StringLength(500)]
    [Display(Name="Website URL")]
    [DataType(DataType.Url)]
    public string WebsiteUrl { get; set; } = "";

    [Required, StringLength(120)]
    [Display(Name="Issue Type")]
    public string IssueType { get; set; } = "Website Bug";

    [Required, StringLength(50)]
    [Display(Name="Issue Priority")]
    public string IssuePriority { get; set; } = "Medium";

    [Required, StringLength(2000)]
    [Display(Name="Issue Description")]
    public string IssueDescription { get; set; } = "";

    [StringLength(500)]
    public string? Screenshot { get; set; }

    [Required, StringLength(50)]
    public string Status { get; set; } = "Open";

    [Display(Name="Assigned To")]
    [StringLength(120)]
    public string? AssignedTo { get; set; }

    [Display(Name="Date Resolved")]
    [DataType(DataType.Date)]
    public DateTime? DateResolved { get; set; }

    [Display(Name="Resolution Notice")]
    public string? ResolutionNotice { get; set; }

    public int? IssueTypeId { get; set; }
    public IssueType? IssueTypeRef { get; set; }
}