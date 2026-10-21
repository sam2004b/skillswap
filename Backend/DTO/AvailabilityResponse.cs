namespace Backend.DTOs;
public class AvailabilityResponse
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset StartAtUtc { get; set; }

    public DateTimeOffset EndAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
