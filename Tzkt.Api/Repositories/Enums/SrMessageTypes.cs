using Tzkt.Data.Models;

namespace Tzkt.Api
{
    static class SrMessageTypes
    {
        public const string LevelStart = "level_start";
        public const string LevelInfo = "level_info";
        public const string LevelEnd = "level_end";
        public const string Transfer = "transfer";
        public const string External = "external";
        public const string Migration = "migration";

        public static bool TryParse(string value, out int res)
        {
            res = value switch
            {
                LevelStart => (int)InboxMessageType.LevelStart,
                LevelInfo => (int)InboxMessageType.LevelInfo,
                LevelEnd => (int)InboxMessageType.LevelEnd,
                Transfer => (int)InboxMessageType.Transfer,
                External => (int)InboxMessageType.External,
                Migration => (int)InboxMessageType.Migration,
                _ => -1
            };
            return res != -1;
        }

        public static string ToString(int value) => value switch
        {
            (int)InboxMessageType.LevelStart => LevelStart,
            (int)InboxMessageType.LevelInfo => LevelInfo,
            (int)InboxMessageType.LevelEnd => LevelEnd,
            (int)InboxMessageType.Transfer => Transfer,
            (int)InboxMessageType.External => External,
            (int)InboxMessageType.Migration => Migration,
            _ => throw new Exception("invalid inbox message type")
        };
    }
}
