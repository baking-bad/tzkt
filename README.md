# Mavryk Network Indexer
[![Made With](https://img.shields.io/badge/made%20with-C%23-success.svg?)](https://docs.microsoft.com/en-gb/dotnet/csharp/language-reference/)
[![License: MIT](https://img.shields.io/github/license/baking-bad/netezos.svg)](https://opensource.org/licenses/MIT)

MvKT is the most advanced [Mavryk](https://mavryk.org/) blockchain indexer with powerful API created by the [Mavryk Dynamics](https://mavrykdynamics.com/) team. Forked from [MvKT](https://github.com/baking-bad/tzkt) made by [Baking Bad](https://bakingbad.dev)

The indexer fetches raw data from the Mavryk blockchain, processes it, and saves it to its database to provide efficient access to the blockchain data. Using indexers is necessary part for most blockchain-related applications, because indexers expose much more data and cover much more use-cases than native node RPC, for example getting operations by hash, or operations related to particular accounts and smart contracts, or created NFTs, or token balances, or baking rewards, etc.

## Features:
- **More detailed data.** MvKT not only collects blockchain data, but also processes and extends it to make it more convenient to work with. For example, MvKT was the first indexer introduced synthetic operation types such as "migration" or "revelation penalty", which fill in the gaps in account's history, because this data is simply not available from the node.
- **Micheline-to-JSON conversion** MvKT automatically converts raw Micheline JSON to human-readable JSON, so it's extremely handy to work with transaction parameters, contract storages, bigmaps keys, etc.
- **Tokens support** MvKT also indexes FA1.2 and FA2 tokens (including NFTs), token balances, and token transfers (including mints and burns), as well as token metadata, even if it is stored in IPFS.
- **Data quality comes first!** You will never see an incorrect account balance, or contract storage, or missed operations, etc. MvKT was built by professionals who know Mavryk from A to Z (or from mv to KT 😼).
- **Advanced API.** MvKT provides a REST-like API, so you don't have to connect to the database directly (but you can, if you want). In addition to basic data access MvKT API has a lot of cool features such as "deep filtering", "deep selection", "deep sorting", exporting .csv statements, calculating historical data (at some block in the past) such as balances, storages, and bigmap keys, injecting historical quotes and metadata, built-in response cache, and much more. See the complete [API documentation](https://api.mavryk.network).
- **WebSocket API.** MvKT allows to subscribe to real-time blockchain data, such as new blocks or new operations, etc. via WebSocket. MvKT uses SignalR, which is very easy to use and for which there are many client libraries for different languages.
- **No local node needed.** There is no need to run your own local node. Also, the indexer does not create much load on the node RPC, so it's ok to use any public one. By default it uses [rpc.mavryk.network](https://rpc.mavryk.network/basenet/chains/main/blocks/head/header).
- **No archive node needed.** There is no need to use an archive node (running in "archive" mode). If you bootstrap the indexer from the most recent snapshot, using a simple rolling node will be enough.
- **Easy to start.** Indexer bootstrap is very simple and quite fast, because you can easily restore it from a fresh snapshot, publicly available for all supported networks, so you don't need to index the whole blockchain from scratch. But of course, you can do that, if you want.
- **Validation and diagnostics.** MvKT indexer validates all incoming data so you will never get to the wrong chain and will never commit corrupted data because of invalid response from the node. Also, the indexer performs self-diagnostics after each block, which guarantees the correctness of its state after committing new data.
- **Flexibility and scalability.** MvKT is split into 3 components: indexer, database, and API, which enables quite efficient horizontal scalability ([see example](https://baking-bad.org/blog/2019/12/03/tezos-explorer-tzkt-2-overview-of-architecture-and-core-components/#general-picture)). This also enables flexible optimization, because you can optimize each component separately and according to your needs.
- **PostgreSQL.** MvKT uses the world's most advanced open source database, that gives a lot of possibilities such as removing unused indexes to reduce storage usage or adding specific indexes to increase performance of specific queries. You can configure replication, clustering, partitioning and much more. You can use a lot of plugins to enable cool features like GraphQL. This is a really powerful database.
- **Friendly support.** We are always happy to help and open for discussions and feature requests. Feel free to [contact us](https://bakingbad.dev).

## Installation (docker)

First of all, install `git`, `make`, `docker`, `docker-compose`, then run the following commands:

````sh
git clone https://github.com/mavryk-network/mvkt.git
cd mvkt/

make init  # Restores DB from the latest snapshot. Skip it, if you want to index from scratch.
make start # Starts DB, indexer, and API. By default, the API will be available at http://127.0.0.1:5000.
make stop  # Stops DB, indexer, and API.
````

You can configure MvKT via `Mvkt.Sync/appsettings.json` (indexer) and `Mvkt.Api/appsettings.json` (API). All the settings can also be passed via env vars or command line args. See an example of how to [provide settings via env vars](https://github.com/mavryk-network/mvkt/blob/master/docker-compose.yml#L25) and read some tips about [indexer configuration](#configure-indexer-example-for-mainnet) and [API configuration](#configure-api).

## Installation (from source)

This guide is for Ubuntu 22.04, but even if you use a different OS, the installation process will likely be the same, except for the "Install packages" part.

### Install packages

#### Install Git

````
sudo apt update
sudo apt install git
````

#### Install .NET

````
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-7.0
````

#### Install Postgresql

````
sudo sh -c 'echo "deb https://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt update
sudo apt -y install postgresql-16 postgresql-contrib-16
````

---

### Prepare database

#### Create an empty database and its user

````
sudo -u postgres psql

postgres=# create database mvkt_db;
postgres=# create user mvkt with encrypted password 'qwerty';
postgres=# grant all privileges on database mvkt_db to mvkt;
postgres=# \q
````

#### Download fresh snapshot (example for mainnet)

````
wget "https://snapshots.tzkt.io/tzkt_v1.13_mainnet.backup" -O /tmp/mvkt_db.backup
````

#### Restore database from the snapshot

````c
sudo -u postgres pg_restore -c --if-exists -v -1 -d mvkt_db /tmp/mvkt_db.backup
````

Notes:
- to speed up the restoration replace `-1` with `-e -j {n}`, where `{n}` is a number of parallel workers (e.g., `-e -j 8`);
- in case of Docker use you may need to add `-U mvkt` parameter.

#### Grant access to the database to our user

````
sudo -u postgres psql mvkt_db

mvkt_db=# grant all privileges on all tables in schema public to mvkt;
mvkt_db=# \q
````

---

### Build, configure and run MvKT Indexer

#### Clone repo

````
git clone https://github.com/baking-bad/mvkt.git ~/mvkt
````

#### Build indexer

````
cd ~/mvkt/Mvkt.Sync/
dotnet publish -o ~/mvkt-sync
````

#### Configure indexer (example for mainnet)

Edit the configuration file `~/mvkt-sync/appsettings.json`. What you basically need is to adjust the `MavrykNode.Endpoint` and `ConnectionStrings.DefaultConnection`, if needed:

````json
{
  "MavrykNode": {
    "Endpoint": "https://rpc.mavryk.network/basenet/"
  },
  "ConnectionStrings": {
    "DefaultConnection": "host=localhost;port=5432;database=mvkt_db;username=mvkt;password=qwerty;command timeout=600;"
  }
}
````

[Read more](https://www.npgsql.org/doc/connection-string-parameters.html) about connection string and available parameters.

##### Chain reorgs and indexing lag

To avoid reorgs (chain reorganizations) you can set the indexing lag `MavrykNode.Lag` (1-2 blocks lag is enough):

````json
{
  "MavrykNode": {
    "Lag": 1
  }
}
````

##### Collect metrics

You can enable/disable Prometheus metrics by setting `MetricsOptions.Enabled`. By default, they will be available at `http://localhost:5001/metrics` (protobuf) and `http://localhost:5001/metrics-text` (plain text):

````json
  "MetricsOptions": {
    "Enabled": true
  }
````

#### Run indexer

````c
cd ~/mvkt-sync
dotnet Mvkt.Sync.dll
````

That's it. If you want to run the indexer as a daemon, take a look at this guide: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-7.0#create-the-service-file.

---

### Build, configure and run MvKT API

Suppose, you have already cloned the repo to `~/mvkt` during the steps above.

#### Build API

````
cd ~/mvkt/Mvkt.Api/
dotnet publish -o ~/mvkt-api
````

#### Configure API

Edit the configuration file `~/mvkt-api/appsettings.json`. What you basically need is to adjust the `ConnectionStrings.DefaultConnection`, if needed:

Like this:

````js
{
  "ConnectionStrings": {
    "DefaultConnection": "host=localhost;port=5432;database=mvkt_db;username=mvkt;password=qwerty;command timeout=600;"
  },
}
````

[Read more](https://www.npgsql.org/doc/connection-string-parameters.html) about connection string and available parameters.

##### Response cache

The API has built-in response cache, enabled by default. You can control the cache size limit by setting the `ResponseCache.CacheSize` (MB), or disable it by setting to `0`:
````json
{
   "ResponseCache": {
      "CacheSize": 1024
   }
}
````

##### RPC helpers (example for mainnet)

The API provides RPC helpers - endpoints proxied directly to the node RPC, specified in the API settings. The Rpc helpers can be enabled in the `RpcHelpers` section:

`````json
{
   "RpcHelpers": {
      "Enabled": true,
      "Endpoint": "https://rpc.mavryk.network/basenet/"
   }
}
`````

Please, notice, the API's `RpcHelpers.Endpoint` must point to the same network (with the same `chain_id`) as `MavrykNode.Endpoint` in the indexer. Otherwise, an exception will be thrown.

##### Collect metrics

You can enable/disable Prometheus metrics by setting `MetricsOptions.Enabled`. By default, they will be available at `http://localhost:5000/metrics` (protobuf) and `http://localhost:5000/metrics-text` (plain text):

````json
  "MetricsOptions": {
    "Enabled": true
  }
````

##### TCP port

By default, the API is available at the port `5000`. You can configure it at `Kestrel.Endpoints.Http.Url`:

````json
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
````

#### Run API

````
cd ~/mvkt-api
dotnet Mvkt.Api.dll
````

That's it. If you want to run the API as a daemon, take a look at this guide: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-7.0#create-the-service-file.

## Install Mvkt Indexer and API for testnets

In general the steps are the same as for the mainnet, you will just need to use a different RPC endpoint and DB snapshot. Here are presets for the current testnets:
 - Ghostnet:
   - Snapshot: https://snapshots.tzkt.io/tzkt_v1.13_ghostnet.backup
   - RPC node: https://rpc.mavryk.network/ghostnet/
 - Oxfordnet:
   - Snapshot: https://snapshots.tzkt.io/tzkt_v1.13_oxfordnet.backup
   - RPC node: https://rpc.mavryk.network/oxfordnet/

### Testnets & docker

First of all, install `git`, `make`, `docker`, `docker-compose`, then run the following commands:

````sh
git clone https://github.com/mavryk-network/mvkt.git
cd mvkt/

make ghost-init  # Restores DB from the latest snapshot. Skip it, if you want to index from scratch.
make ghost-start # Starts DB, indexer, and API. By default, the API will be available at http://127.0.0.1:5010.
make ghost-stop  # Stops DB, indexer, and API.
````

## Have a question?

Feel free to contact us via:
- Discord: https://discord.gg/BDBYA4ASf2
- Telegram: https://t.me/+skFJjewPdRU5ZGQ0
- Twitter: https://x.com/MavrykDynamics
- Email: hello@bakingbad.dev

Cheers! 🍺
