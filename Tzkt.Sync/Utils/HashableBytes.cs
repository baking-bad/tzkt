using Netezos.Encoding;

namespace Tzkt.Sync
{
    public class HashableBytes(byte[] bytes)
    {
        readonly byte[] Bytes = bytes;

        public static implicit operator HashableBytes(byte[] array) => new(array);
        public override bool Equals(object? obj) => obj is HashableBytes hb && hb.Bytes.IsEqual(Bytes);
        public override int GetHashCode() => Bytes.GetHashCodeExt();
        public override string ToString() => Hex.Convert(Bytes);

        public static HashableBytes? From(byte[]? array) => array == null ? null : new(array);
    }
}
