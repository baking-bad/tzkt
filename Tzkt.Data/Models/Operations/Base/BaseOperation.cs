namespace Tzkt.Data.Models.Base
{
    public interface IOperation
    {
        public long Id { get; }
    }

    public class BaseOperation : IOperation
    {
        public required long Id { get; set; }
        public required int Level { get; set; }
        public required DateTime Timestamp { get; set; }
        public required string OpHash { get; set; }
    }
}
