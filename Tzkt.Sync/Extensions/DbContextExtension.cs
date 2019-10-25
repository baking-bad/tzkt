using System.Linq;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync
{
    static class DbContextExtension
    {
        public static Account SyncLocalAccount(this Data.TzktContext db, Account account)
        {
            return db.Accounts.Local.FirstOrDefault(x => x.Id == account.Id) ?? account;
        }

        public static void TryAttach(this DbContext db, object obj)
        {
            if (obj != null && db.Entry(obj).State == EntityState.Detached)
                db.Attach(obj);
        }

        public static void TryUpdate(this DbContext db, object obj)
        {
            if (obj != null && db.Entry(obj).State == EntityState.Detached)
                db.Update(obj);
        }
    }
}
