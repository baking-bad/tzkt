# Tezos Indexer by Baking Bad
[![Made With](https://img.shields.io/badge/made%20with-C%23-success.svg?)](https://docs.microsoft.com/en-gb/dotnet/csharp/language-reference/)
[![License: MIT](https://img.shields.io/github/license/baking-bad/netezos.svg)](https://opensource.org/licenses/MIT)

TzKT is a lightweight [Tezos](https://tezos.com/) blockchain indexer with an advanced API created by the [Baking Bad](https://baking-bad.org/docs) team with huge support from the [Tezos Foundation](https://tezos.foundation/).

The indexer fetches raw data from the Tezos node, then processes it and stores in the database in such a way as to provide effective access to the blockchain data. For example, getting operations by hash, or getting all operations of the particular account, or getting detailed baking rewards, etc. None of this can be accessed via node RPC, but TzKT indexer makes this data (and much more) available.

## Features:
- **More detailed data.** TzKT not only collects blockchain data, but also processes and extends it with unique properties or even entities. For example, TzKT was the first indexer introduced synthetic operation types such as "migration" or "revelation penalty", which fill in the gaps in account history (because this data is missed in the blockchain), and the only indexer that correctly distinguishes smart contracts among all contracts.
- **Micheline-to-JSON conversion** TzKT automatically converts raw Micheline JSON to human-readable JSON, so it's extremely handy to work with transaction parameters, contract storages, bigmaps keys, etc.
- **Data quality comes first!** You will never see an incorrect account balance, or total rolls, or missed operations, etc. TzKT was built by professionals who know Tezos from A to Z (or, in other words, from tz to KT 😼).
- **Advanced API.** TzKT provides a REST-like API, so you don't have to connect to the database directly. In addition to basic data access TzKT API has a lot of cool features such as deep filtering, sorting, data selection, exporting .csv statements, calculating historical data (at any block) such as balances or BigMap keys, injecting historical quotes and metadata, optimized caching and much more. See the complete [API documentation](https://api.tzkt.io).
- **WebSocket API.** TzKT allows to subscribe to real-time blockchain data, such as new blocks or new operations, etc. via WebSocket. TzKT uses SignalR, which is very easy to use and for which there are many client libraries for different languages.
- **Low resource consumption.** TzKT is fairly lightweight. The indexer consumes up to 128MB of RAM, and the API up to 256MB-1024MB, depending on the network and configured cache size.
- **No local node needed.** TzKT indexer works well even with remote RPC node. By default it uses [tezos.giganode.io](https://tezos.giganode.io/), the most performant public RPC node in Tezos, which is more than enough for most cases.
- **Quick start.** Indexer bootstrap takes ~15 minutes by using snapshots publicly available for all supported networks. Of course, you can run full synchronization from scratch as well.
- **Validation and diagnostics.** TzKT indexer validates all incoming data so you will never get to the wrong chain and will never commit corrupted data. Also, the indexer performs self-diagnostics after each block, which guarantees the correct commiting.
- **Flexibility and scalability.** There is no requirement to run all TzKT components (database, indexer, API) together and on the same machine. This allows flexible optimization, because you can optimize each component separately and according to your needs. Or you can run all the components on the same machine as well, which is much cheaper.
- **PostgreSQL.** TzKT uses the world's most advanced open source database, that gives a lot of possibilities such as removing unused indexes to reduce storage usage or adding specific indexes to increase performance of specific queries. You can configure replication, clustering, partitioning and much more. You can use a lot of plugins to enable cool features like GraphQL. This is a really powerful database.
- **Friendly support.** We are always happy to help everyone and are open to discussions and feature requests. Feel free to [contact us](https://baking-bad.org/docs#contacts).

## Installation (docker)

First of all, install `git`, `make`, `docker`, `docker-compose`, then run the following commands:

````sh
git clone https://github.com/baking-bad/tzkt.git
cd tzkt/

make init #run this command just once to init database from the latest snapshot
make start

curl http://127.0.0.1:5000/v1/head

make stop
````

## Installation (from source)

This is the preferred way, because you have more control over each TzKT component (database, indexer, API). This guide is for Ubuntu 20.04, but if you are using a different OS, the installation process will probably differ only in the "Install packages" step.

### Install packages

#### Install Git

````
sudo apt update
sudo apt install git
````

#### Install .NET 5.0 SDK

````
wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt update
sudo apt install -y apt-transport-https
sudo apt update
sudo apt install -y dotnet-sdk-5.0
````

#### Install Postgresql 13

````
wget -q -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
echo "deb http://apt.postgresql.org/pub/repos/apt/ `lsb_release -cs`-pgdg main" | sudo tee /etc/apt/sources.list.d/pgdg.list
sudo apt update
sudo apt -y install postgresql-13 postgresql-client-13
````

## Install Tzkt Indexer and API for mainnet

### Prepare database

#### Create an empty database and its user

````
sudo -u postgres psql

postgres=# create database tzkt_db;
postgres=# create user tzkt with encrypted password 'qwerty';
postgres=# grant all privileges on database tzkt_db to tzkt;
postgres=# \q
````

#### Download fresh snapshot

````c
wget "https://tzkt-snapshots.s3.eu-central-1.amazonaws.com/tzkt_v1.5.2.backup" -O /tmp/tzkt_db.backup
````

#### Restore database from the snapshot

````c
// mainnet restoring takes ~10 min (of course, depending on hardware)
sudo -u postgres pg_restore -c --if-exists -v -d tzkt_db -1 /tmp/tzkt_db.backup
````

### Clone, build, configure and run Tzkt Indexer

#### Clone

````
cd ~
git clone https://github.com/baking-bad/tzkt.git
````

#### Build indexer

````
cd ~/tzkt/Tzkt.Sync/
dotnet publish -o ~/tzkt-sync
````

#### Configure indexer

Edit configuration file `~/tzkt-sync/appsettings.json` with your favorite text editor. What you need is to specify `Diagnostics` (just disable it), `TezosNode.ChainId`, `TezosNode.Endpoint` and `ConnectionStrings.DefaultConnection`.

Like this:

````json
{
  "Protocols": {
    "Diagnostics": false,
    "Validation": true
  },

  "TezosNode": {
    "ChainId": "NetXdQprcVkpaWU",
    "Endpoint": "https://mainnet-tezos.giganode.io/",
    "Timeout": 60
  },

  "Quotes": {
    "Async": true,
    "Provider": {
      "Name": "TzktQuotes"
    }
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=tzkt_db;username=tzkt;password=qwerty;"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
````

#### Run indexer

````c
cd ~/tzkt-sync
dotnet Tzkt.Sync.dll

// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/tzkt-sync
// warn: Tzkt.Sync.Services.Observer[0]
//       Observer is started
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 776913
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 776914
// ....
````

That's it. If you want to run the indexer as a daemon, take a look at this guide: https://docs.microsoft.com/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.1#create-the-service-file.

### Build, configure and run Tzkt API for the mainnet indexer

Suppose you have already created database `tzkt_db`, database user `tzkt` and cloned Tzkt repo to `~/tzkt`.

#### Build API

````
cd ~/tzkt/Tzkt.Api/
dotnet publish -o ~/tzkt-api
````

#### Configure API

Edit configuration file `~/tzkt-api/appsettings.json` with your favorite text editor. What you need is to specify `ConnectionStrings.DefaultConnection`, a connection string for the database created above.

Like this:

````js
{
  "Metadata": {
    "AccountsPath": "*",
    "ProposalsPath": "*",
    "ProtocolsPath": "*"
  },

  "Websocket": {
    "Enabled": true,
    "MaxConnections": 100,
    "MaxAccountSubscriptions": 10
  },

  "Cache": {
    "LoadRate": 0.75,
    "MaxAccounts": 32000
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=tzkt_db;username=tzkt;password=qwerty;"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },

  "AllowedHosts": "*"
}
````

#### Run API

````c
cd ~/tzkt-api
dotnet Tzkt.Api.dll

// info: Tzkt.Api.Services.Metadata.AccountMetadataService[0]
//       Accounts metadata not found
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Sync worker initialized with level 776917 and blocks time 60s
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Syncronization started
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: http://localhost:5000
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: https://localhost:5001
// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/tzkt-api
// ....
````

That's it. By default API is available on ports 5000 (HTTP) and 5001 (HTTPS). If you want to use HTTPS, you also need to configure certificates. If you want to run API on a different port, add the `"Kestrel"` section to the `appsettings.json` (see example below).

## Install Tzkt Indexer and API for testnets

In general the steps are the same as for the mainnet, you just need to use different database, different snapshot and different appsettings (chain id and RPC endpoint). Here are some presets for testnets:
 - Edonet:
   - Snapshot: https://tzkt-snapshots.s3.eu-central-1.amazonaws.com/edo2_tzkt_v1.5.1.backup
   - RPC node: https://rpc.tzkt.io/edo2net/
   - Chain id: NetXSgo1ZT2DRUG
 - Florencenet:
   - Snapshot: https://tzkt-snapshots.s3.eu-central-1.amazonaws.com/flor_tzkt_v1.5.1.backup
   - RPC node: https://rpc.tzkt.io/florencenobanet/
   - Chain id: NetXxkAx4woPLyu     
 - Granadanet:
   - Snapshot: https://tzkt-snapshots.s3.eu-central-1.amazonaws.com/granadanet.backup
   - RPC node: https://rpc.tzkt.io/granadanet/
   - Chain id: NetXz969SFaFn8k

Anyway, let's do it, for reference, from scratch for the `florencenet`.

### Prepare database

#### Create an empty database and its user

````
sudo -u postgres psql

postgres=# create database flor_tzkt_db;
postgres=# create user tzkt with encrypted password 'qwerty';
postgres=# grant all privileges on database flor_tzkt_db to tzkt;
postgres=# \q
````

#### Download fresh snapshot

````c
wget "https://tzkt-snapshots.s3.eu-central-1.amazonaws.com/flor_tzkt_v1.5.1.backup" -O /tmp/flor_tzkt_db.backup
````

#### Restore database from the snapshot

````c
// florencenet restoring takes ~1 min
sudo -u postgres pg_restore -c --if-exists -v -d flor_tzkt_db -1 /tmp/flor_tzkt_db.backup
````

### Clone, build, configure and run Tzkt Indexer

#### Clone

````
cd ~
git clone https://github.com/baking-bad/tzkt.git
````

#### Build indexer

````
cd ~/tzkt/Tzkt.Sync/
dotnet publish -o ~/flor-tzkt-sync
````

#### Configure indexer

Edit configuration file `~/flor-tzkt-sync/appsettings.json` with your favorite text editor. What you need is to specify `Diagnostics` (just disable it), `TezosNode.ChainId`, `TezosNode.Endpoint` and `ConnectionStrings.DefaultConnection`.

Like this:

````json
{
  "Protocols": {
    "Diagnostics": false,
    "Validation": true
  },

  "TezosNode": {
    "ChainId": "NetXxkAx4woPLyu",
    "Endpoint": "https://rpc.tzkt.io/florencenobanet/",
    "Timeout": 30
  },

  "Quotes": {
    "Async": true,
    "Provider": null
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=flor_tzkt_db;username=tzkt;password=qwerty;"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
````

#### Run indexer

````c
cd ~/flor-tzkt-sync
dotnet Tzkt.Sync.dll

// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/flor-tzkt-sync
// warn: Tzkt.Sync.Services.Observer[0]
//       Observer is started
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 125790
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 125791
// ....
````

That's it. If you want to run the indexer as a daemon, take a look at this guide: https://docs.microsoft.com/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.1#create-the-service-file.

### Build, configure and run Tzkt API for the florencenet indexer

Suppose you have already created database `flor_tzkt_db`, database user `tzkt` and cloned Tzkt repo to `~/tzkt`.

#### Build API

````
cd ~/tzkt/Tzkt.Api/
dotnet publish -o ~/flor-tzkt-api
````

#### Configure API

Edit configuration file `~/flor-tzkt-api/appsettings.json` with your favorite text editor. What you need is to specify `ConnectionStrings.DefaultConnection`, a connection string for the database created above.

By default API is available on ports 5000 (HTTP) and 5001 (HTTPS). If you want to use HTTPS, you also need to configure certificates.

If you want to run API on a different port, add the `"Kestrel"` section to the `appsettings.json`.

Like this:

````js
{
  "Metadata": {
    "AccountsPath": "*",
    "ProposalsPath": "*",
    "ProtocolsPath": "*"
  },

  "Cache": {
    "LoadRate": 0.75,
    "MaxAccounts": 32000
  },

  "Websocket": {
    "Enabled": true,
    "MaxConnections": 100,
    "MaxAccountSubscriptions": 10
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=flor_tzkt_db;username=tzkt;password=qwerty;"
  },

  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://localhost:5010"
      }
    }
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },

  "AllowedHosts": "*"
}
````

#### Run API

````c
cd ~/flor-tzkt-api
dotnet Tzkt.Api.dll

// info: Tzkt.Api.Services.Metadata.AccountMetadataService[0]
//       Accounts metadata not found
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Sync worker initialized with level 205804 and blocks time 30s
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Syncronization started
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: http://localhost:5020
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: https://localhost:5021
// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/flor-tzkt-api
// ....
````

That's it.

## Have a question?

Feel free to contact us via:
- Discord: https://discord.gg/aG8XKuwsQd
- Telegram: https://t.me/baking_bad_chat
- Slack: https://tezos-dev.slack.com/archives/CV5NX7F2L
- Twitter: https://twitter.com/TezosBakingBad
- Email: hello@baking-bad.org

Cheers! 🍺
