namespace Tzkt.Sync
{
    class BytesComparer : IComparer<byte[]>
    {        
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null)
                return y == null ? 0 : -1;

            if (y == null)
                return 1;

            if (x.Length != y.Length)
                return x.Length.CompareTo(y.Length);

            // TODO: can be optimized by using unsafe comparison
            for (int i = 0; i < x.Length; i++)
                if (x[i] != y[i])
                    return x[i].CompareTo(y[i]);
            
            return 0;
        }
    }
}
