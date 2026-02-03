namespace WateringController.Frontend.Models;

public sealed record Schedule
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string StartTimeUtc { get; set; } = "07:00:00";
    public int RunSeconds { get; set; } = 30;
    public string? DaysOfWeek { get; set; }
    public string? LastRunDateUtc { get; set; }
}
