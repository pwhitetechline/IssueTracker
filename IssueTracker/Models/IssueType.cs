using System.ComponentModel.DataAnnotations;

namespace IssueTracker.Models;

public class IssueType
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = "";
}
