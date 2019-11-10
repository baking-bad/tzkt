using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto5
{
    class ManagersMigration : ProtocolCommit
    {
        ManagersMigration(ProtocolHandler protocol) : base(protocol) { }

        public override Task Apply()
        {
            throw new NotImplementedException();
        }

        public override Task Revert()
        {
            throw new NotImplementedException();
        }

        #region static
        public static async Task<ManagersMigration> Apply(ProtocolHandler proto)
        {
            var commit = new ManagersMigration(proto);
            await commit.Apply();

            return commit;
        }

        public static async Task<ManagersMigration> Revert(ProtocolHandler proto)
        {
            var commit = new ManagersMigration(proto);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}
