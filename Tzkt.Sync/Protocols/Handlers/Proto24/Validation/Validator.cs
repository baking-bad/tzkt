using System.Text.Json;
using Tzkt.Data.Models;
using Tzkt.Sync.Services;

namespace Tzkt.Sync.Protocols.Proto24
{
    class Validator(ProtocolHandler protocol) : Proto23.Validator(protocol) { }
}