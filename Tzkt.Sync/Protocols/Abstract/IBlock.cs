namespace Tzkt.Sync.Protocols
{
    public interface IBlock
    {
        int Level { get; }
        string Hash { get; }
        string Chain { get; }
        string Protocol { get; }
        string Predecessor { get; }
    }
}
