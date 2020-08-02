using System.Collections.Generic;
using System.Threading.Tasks;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Services
{
    public interface IQuoteProvider
    {
        Task<int> FillQuotes(IEnumerable<IQuote> quotes, IQuote last);
    }
}
