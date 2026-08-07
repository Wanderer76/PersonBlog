using System.ComponentModel.DataAnnotations;

namespace Conference.Domain.Models;

public sealed class CreateMessageForm
{
    public Guid ConferenceId { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}
