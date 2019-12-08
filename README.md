# TzKT (v1-preview)
[![Made With](https://img.shields.io/badge/made%20with-C%23-success.svg?)](https://docs.microsoft.com/en-gb/dotnet/csharp/language-reference/)
[![License: MIT](https://img.shields.io/github/license/baking-bad/netezos.svg)](https://opensource.org/licenses/MIT)

A lightweight, API-first, baking-focused Tezos explorer supported by the Tezos Foundation.

[API Documentation](https://api.tzkt.io/)

### Requirements

- .NET Core 3.0+
- Postgresql 9.6+

## Tzkt.Sync

This is a flexible and lightweight Tezos blockchain indexer. At the moment it is only lightweight actually, but it's only a matter of time. It uses Postgresql as its main storage via Npgsql and depends on Tzkt.Data, a library with data models and database migrations, that describes the entire database schema using the code-first approach.

The indexer doesn't require a local node with RPC and can be bootstrapped from [the snapshot](https://drive.google.com/file/d/1B-5NfOGebnjgie_eDWxpckOv6OL6jWjS) (~800MB), so there is no need to download a huge amount of data.

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
