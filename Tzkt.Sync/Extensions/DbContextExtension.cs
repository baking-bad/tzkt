using Microsoft.EntityFrameworkCore;

namespace Tzkt.Sync
{
    static class DbContextExtension
    {
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
