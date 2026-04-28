using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Web.Models;

public sealed class CreateTicketVm
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    // 1..3 (null = auto)
    [Range(1, 3)]
    public int? Priority { get; set; }

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = "customer@local";
}

