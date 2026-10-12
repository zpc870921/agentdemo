namespace historydemo1.Models
{
    public sealed record StoredChatMessage(
    string Role,
    string Content,
    long Id = 0);
}
