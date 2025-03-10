using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tzkt.Data.Models.Base
{
    public interface IOperation
    {
        public long Id { get; }
    }

    public class BaseOperation : IOperation
    {
        public long Id { get; set; }
        public int Level { get; set; }
        public DateTime Timestamp { get; set; }
        public string OpHash { get; set; }
    }
}
