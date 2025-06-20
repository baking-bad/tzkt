﻿namespace Tzkt.Sync
{
    class HashableBytes(byte[] bytes)
    {
        readonly byte[] Bytes = bytes;

        public static implicit operator HashableBytes(byte[] array) => new(array);
        public override bool Equals(object? obj) => obj is HashableBytes hb && hb.Bytes.IsEqual(Bytes);
        public override int GetHashCode() => Bytes.GetHashCodeExt();
    }
}
