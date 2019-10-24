﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tzkt.Data.Models;

namespace Tzkt.Sync.Protocols.Proto2
{
    class DeactivationCommit : ProtocolCommit
    {
        public List<Data.Models.Delegate> Delegates { get; private set; }

        DeactivationCommit(ProtocolHandler protocol) : base(protocol) { }

        public async Task Init(RawBlock rawBlock)
        {
            foreach (var delegat in rawBlock.Metadata.Deactivated)
                Delegates.Add((Data.Models.Delegate)await Cache.GetAccountAsync(delegat));
        }

        public async Task Init(Block block)
        {
            Delegates = await Db.Delegates.Where(x => x.DeactivationLevel == block.Level).ToListAsync();
        }

        public override Task Apply()
        {
            foreach (var delegat in Delegates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        public override Task Revert()
        {
            foreach (var delegat in Delegates)
            {
                throw new NotImplementedException();
            }

            return Task.CompletedTask;
        }

        #region static
        public static async Task<DeactivationCommit> Apply(ProtocolHandler proto, RawBlock rawBlock)
        {
            var commit = new DeactivationCommit(proto);
            await commit.Init(rawBlock);
            await commit.Apply();

            return commit;
        }

        public static async Task<DeactivationCommit> Revert(ProtocolHandler proto, Block block)
        {
            var commit = new DeactivationCommit(proto);
            await commit.Init(block);
            await commit.Revert();

            return commit;
        }
        #endregion
    }
}