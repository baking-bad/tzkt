# TzKT (v1-preview)
[![Made With](https://img.shields.io/badge/made%20with-C%23-success.svg?)](https://docs.microsoft.com/en-gb/dotnet/csharp/language-reference/)
[![License: MIT](https://img.shields.io/github/license/baking-bad/netezos.svg)](https://opensource.org/licenses/MIT)

A lightweight, API-first, baking-focused Tezos explorer supported by the Tezos Foundation.

[API Documentation](https://api.tzkt.io/)

### Requirements

- .NET Core 3.1
- Postgresql 12

## Tzkt.Sync

This is a flexible and lightweight Tezos blockchain indexer. At the moment it is only lightweight actually, but it's only a matter of time. It uses Postgresql as its main storage via Npgsql and depends on Tzkt.Data, a library with data models and database migrations, that describes the entire database schema using the code-first approach.

The indexer doesn't require a local node with RPC and can be bootstrapped from the snapshot (~900MB), so there is no need to download a huge amount of data.

### RPC calls

In production mode the indexer (if bootstrapped) uses the following RPC endpoints:

- `/chains/main/blocks/{level}`
- `/chains/main/blocks/{level}/context/constants`
- `/chains/main/blocks/{level}/helpers/baking_rights`
- `/chains/main/blocks/{level}/helpers/endorsing_rights`
 
And there is also a diagnostics option, which does a self test after each block using the following RPC endpoints:
 
- `chains/main/blocks/{level}/context/raw/json/contracts/global_counter`
- `chains/main/blocks/{level}/context/contracts/{address}`
- `chains/main/blocks/{level}/context/delegates/{address}`
 
> **Note:** It is highly recommended not to enable diagnostics until the indexer is fully synchronized with the blockchain. Otherwise synchronization will take a long time.

### Costs

At the end of the 173rd cycle in the Tezos mainnet we have the following values:

- **~70MB RAM** (maximum 100MB during full synchronization)
- **~5GB of disk space** for database: 1GB data + 4GB indexes *(optional)*

## Tzkt.Api

This is a native API server for the Tzkt indexer. It actually doesn't depend on the Tzkt.Sync, but on the Tzkt.Data. That means that you can run an API and an indexer separately.

### Documentation

Tzkt.Api uses swagger to automatically generate documentation with Open API 3 specification. The JSON schema is available at `/v1/swagger.json` and can be easily used with open-source third-party UI solutions like Swagger UI, ReDoc, etc.

### Costs

- **~150-400MB RAM**, depending on cache size settings.
 
 ## Current state
 
TzKT (v1-preview) has just been launched and we are actively testing it and are working on adding additional features. At the same time we are working on the frontend part of the TzKT explorer. There are so many things to do!

## Installation guide

The easiest way to try TzKT Indexer and TzKT API is to use Docker. First of all, install `git`, `make`, `docker`, `docker-compose`, then run the following commands:

````sh
git clone https://github.com/baking-bad/tzkt.git
cd tzkt/

make init #run this command just once to init database from the latest snapshot
make start

curl http://127.0.0.1:5000/v1/head

make stop
````

## Advanced installation guide

This installation guide is for Ubuntu 18.04 because it is a fairly common OS. If you are using a different OS, the installation process will probably differ only in the "Install packages" step.

### Install packages

#### Install Git

````
sudo apt update
sudo apt install git
````

#### Install .NET Core 3.1 SDK

````
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo add-apt-repository universe
sudo apt update
sudo apt install apt-transport-https
sudo apt update
sudo apt -y install dotnet-sdk-3.1
````

#### Install Postgresql 12

````
echo "deb http://apt.postgresql.org/pub/repos/apt/ bionic-pgdg main" | sudo tee /etc/apt/sources.list.d/pgdg.list
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt update
sudo apt -y install postgresql-12 postgresql-client-12
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
cd ~
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1-NbqqaC1KSW2rKXS-YDh1iq301-VKjoS' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1-NbqqaC1KSW2rKXS-YDh1iq301-VKjoS" -O tzkt_db.backup && rm -rf /tmp/cookies.txt
````

#### Restore database from the snapshot

````c
// mainnet restoring takes ~10 min
sudo -u postgres pg_restore -c --if-exists -v -d tzkt_db -1 tzkt_db.backup
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
    "Endpoint": "https://rpc.tzkt.io/mainnet/",
    "Timeout": 30
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
  "Sync": {
    "CheckInterval": 20,
    "UpdateInterval": 10
  },

  "Metadata": {
    "AccountsPath": "*"
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

## Install Tzkt Indexer and API for babylonnet

In general the steps are the same as for the mainnet, you just need to use different database, different snapshot and different appsettings (chain id and RPC endpoint). Anyway, let's do it from scratch.

### Prepare database

#### Create an empty database and its user

````
sudo -u postgres psql

postgres=# create database baby_tzkt_db;
postgres=# create user tzkt with encrypted password 'qwerty';
postgres=# grant all privileges on database baby_tzkt_db to tzkt;
postgres=# \q
````

#### Download fresh snapshot

````c
cd ~
wget --load-cookies /tmp/cookies.txt "https://docs.google.com/uc?export=download&confirm=$(wget --quiet --save-cookies /tmp/cookies.txt --keep-session-cookies --no-check-certificate 'https://docs.google.com/uc?export=download&id=1sJamr1FMbfVn0u9rNOOc_iJpnxioJTg_' -O- | sed -rn 's/.*confirm=([0-9A-Za-z_]+).*/\1\n/p')&id=1sJamr1FMbfVn0u9rNOOc_iJpnxioJTg_" -O baby_tzkt_db.backup && rm -rf /tmp/cookies.txt
````

#### Restore database from the snapshot

````c
// babylonnet restoring takes ~1 min
sudo -u postgres pg_restore -c --if-exists -v -d baby_tzkt_db -1 baby_tzkt_db.backup
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
dotnet publish -o ~/baby-tzkt-sync
````

#### Configure indexer

Edit configuration file `~/baby-tzkt-sync/appsettings.json` with your favorite text editor. What you need is to specify `Diagnostics` (just disable it), `TezosNode.ChainId`, `TezosNode.Endpoint` and `ConnectionStrings.DefaultConnection`. 

Like this:

````json
{
  "Protocols": {
    "Diagnostics": false,
    "Validation": true
  },

  "TezosNode": {
    "ChainId": "NetXUdfLh6Gm88t",
    "Endpoint": "https://rpc.tzkt.io/babylonnet/",
    "Timeout": 30
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=baby_tzkt_db;username=tzkt;password=qwerty;"
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
cd ~/baby-tzkt-sync
dotnet Tzkt.Sync.dll

// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/baby-tzkt-sync
// warn: Tzkt.Sync.Services.Observer[0]
//       Observer is started
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 205790
// info: Tzkt.Sync.Services.Observer[0]
//       Applied 205791
// ....
````

That's it. If you want to run the indexer as a daemon, take a look at this guide: https://docs.microsoft.com/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-3.1#create-the-service-file.

### Build, configure and run Tzkt API for the babylonnet indexer

Suppose you have already created database `baby_tzkt_db`, database user `tzkt` and cloned Tzkt repo to `~/tzkt`.

#### Build API

````
cd ~/tzkt/Tzkt.Api/
dotnet publish -o ~/baby-tzkt-api
````

#### Configure API

Edit configuration file `~/baby-tzkt-api/appsettings.json` with your favorite text editor. What you need is to specify `ConnectionStrings.DefaultConnection`, a connection string for the database created above.

By default API is available on ports 5000 (HTTP) and 5001 (HTTPS). If you want to use HTTPS, you also need to configure certificates. 

If you want to run API on a different port, add the `"Kestrel"` section to the `appsettings.json`.

Like this:

````js
{
  "Sync": {
    "CheckInterval": 20,
    "UpdateInterval": 10
  },

  "Metadata": {
    "AccountsPath": "*"
  },

  "Cache": {
    "LoadRate": 0.75,
    "MaxAccounts": 32000
  },

  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=5432;database=baby_tzkt_db;username=tzkt;password=qwerty;"
  },

  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://localhost:5010"
      },
      "Https": {
        "Url": "https://localhost:5011"
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
cd ~/baby-tzkt-api
dotnet Tzkt.Api.dll

// info: Tzkt.Api.Services.Metadata.AccountMetadataService[0]
//       Accounts metadata not found
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Sync worker initialized with level 205804 and blocks time 30s
// info: Tzkt.Api.Services.Sync.SyncWorker[0]
//       Syncronization started
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: http://localhost:5010
// info: Microsoft.Hosting.Lifetime[0]
//       Now listening on: https://localhost:5011
// info: Microsoft.Hosting.Lifetime[0]
//       Application started. Press Ctrl+C to shut down.
// info: Microsoft.Hosting.Lifetime[0]
//       Hosting environment: Production
// info: Microsoft.Hosting.Lifetime[0]
//       Content root path: /home/tzkt/baby-tzkt-api
// ....
````

That's it.

## Have a question?

Feel free to contact us via:
- Telegram: https://t.me/baking_bad_chat
- Twitter: https://twitter.com/TezosBakingBad
- Email: hello@baking-bad.org

Cheers! 🍺
