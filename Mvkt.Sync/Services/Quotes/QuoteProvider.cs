using System.Collections.Generic;
using System.Threading.Tasks;
using Mvkt.Data.Models;

namespace Mvkt.Sync.Services
{
    public interface IQuoteProvider
    {
        Task<int> FillQuotes(IEnumerable<IQuote> quotes, IQuote last);
    }
}
