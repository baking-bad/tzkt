using System;

namespace Tezzycat.Models.Base
{
    public class BaseOperation
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string OpHash { get; set; }
    }
}
