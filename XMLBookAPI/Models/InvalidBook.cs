namespace XMLBookAPI.Models
{
    public sealed class InvalidBook
    {
        public string? Title { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
