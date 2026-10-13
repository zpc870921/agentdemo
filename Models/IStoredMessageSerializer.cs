namespace historydemo1.Models
{
    public interface IStoredMessageSerializer
    {
        string Serialize(StoredAgentMessage message);

        StoredAgentMessage Deserialize(string json);
    }

}
