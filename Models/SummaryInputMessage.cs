namespace historydemo1.Models
{
    public sealed record SummaryInputMessage(
    long Id,
    string Role,
    string Content);
}
