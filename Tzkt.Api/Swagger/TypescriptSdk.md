TzKT API is accompanied by a Typescript SDK you can use both on the client side and on the backend.  
It offers fully typed reponse models and a convenient query builder with autocompletion. Also it is designed to be tree-shakeable and to introduce a minimal overhead to the bundle size.

Check out the following resources:
* [SDK reference](https://sdk.tzkt.io/)
* [Github repo](https://github.com/tzkt/api-sdk-ts)

## Installation

For querying REST API:
```bash
npm i @tzkt/sdk-api
```

For WebSocket API:
```bash
npm i @tzkt/sdk-events
```

## Usage

### Simplest request

Simplest example of getting double baking operations, accused of being such by a certain address.

```ts
import { operationsGetDoubleBaking } from '@tzkt/sdk-api'

await operationsGetDoubleBaking(
  {
    quote: 'Btc',
    accuser: {
      in: ['tz3VEZ4k6a4Wx42iyev6i2aVAptTRLEAivNN']
    }
  }
)
```

### Overriding base API URL

You may override base URL used by the package in the following manner. This may come useful should you want to make requests to a test network or to your custom server.

```ts
import * as api from "@tzkt/sdk-api";

api.defaults.baseUrl = "https://api.ithacanet.tzkt.io/";
```

In case you need to override request headers, this is also possible.

```ts
import * as api from "@tzkt/sdk-api";

api.defaults.headers = {
  access_token: "secret",
};
```

Please refer to the [original documentation](https://github.com/cellular/oazapfts#overriding-the-defaults) for more details on how to configure defaults.

### Subscriptions

Create an instance of events service specifying TzKT WebSocket endpoint.
```js
import { EventsService } from "@tzkt/sdk-events";

const events = new EventsService({ url: "https://api.tzkt.io/v1/ws", reconnect: true });
```

Connection is not initiated until the first request (lazy connection):
```js
const sub = events.operations({ types: [ 'origination' ] })
    .subscribe({ next: console.log });
```

Events service implements subscription router internally (on TzKT your subscriptions are aggregated) hence you can always "unsubscribe" from new updates (however it does not change anything on the TzKT side, just stops firing your observer):
```js
sub.unsubscribe();
```

By default events service will infinitely try to reconnect in case of drop, you can monitor current state by subscribing to status updates:
```js
events.status()
    .subscribe({ next: console.log });
```

In case you need to terminate the connection you can do that (note that event service will start again in case you send a subscription request afterwards):
```js
await events.stop();
```

## More examples

### [Access BigMaps](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/)

Please refer to an article on [BigMaps indexing](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#full-fledged-bigmaps) for a more detailed explanation on what these requests allow you to achieve.

#### Accessing [BigMap by Ptr](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#access-bigmaps)

```ts
import { bigMapsGetBigMapById } from '@tzkt/sdk-api'

const bigMapPtr = 543

await bigMapsGetBigMapById(bigMapPtr)
```

#### Accessing [BigMap by the path in the contract storage](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#example-2)

```ts
import { contractsGetBigMapByName } from '@tzkt/sdk-api'

const contractAddress = 'KT1TtaMcoSx5cZrvaVBWsFoeZ1L15cxo5AEy'
const pathToBigMap = 'ledger'

await contractsGetBigMapByName(
  contractAddress,
  pathToBigMap
)
```

#### Accessing [BigMap keys](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#get-all-bigmap-keys)

```ts
import { bigMapsGetKeys } from '@tzkt/sdk-api'

const bigMapId = 511
const limit = 10

await bigMapsGetKeys(
  bigMapId,
  { limit }
)
```

#### Accessing [All owners of NFT with non-zero balance](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#how-to-get-current-owners-with-balance-0-of-that-nft)

```ts
import { bigMapsGetKeys } from '@tzkt/sdk-api'

const bigMapId = 511
const key = {
  value: 154,
  path: 'nat'
}
const minValue = 0

await bigMapsGetKeys(
  bigMapId,
  {
    key: {
      eq: {
        jsonValue: `${key.value}`,
        jsonPath: key.path
      }
    },
    value: {
      gt: {
        jsonValue: `${minValue}`
      }
    }
  }
)
```

#### Accessing [all updates of all BigMaps of a specific contract](https://baking-bad.org/blog/2021/04/28/tzkt-v15-released-with-bigmaps-and-florence-support/#bigmap-updates)

```ts
import { bigMapsGetBigMapUpdates } from '@tzkt/sdk-api'

const contract = 'KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV'
await bigMapsGetBigMapUpdates({
  contract: {
    eq: contract
  }
})
```

### [Accessing Tokens API](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/)

Please refer to an article on [Tokens indexing](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/) for a more detailed explanation on what these requests allow you to achieve.

#### Accessing [tokens transfers with deep fields selection](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/#deep-selecting)

```ts
import { tokensGetTokenTransfers } from '@tzkt/sdk-api'

const tokenId = 778919
const limit = 2
const sort = 'id'
const fields = [
  'from.address',
  'to.address',
  'amount',
  'token.metadata.symbol',
  'token.metadata.decimals'
]

await tokensGetTokenTransfers({
  tokenId: {
    eq: tokenId
  },
  sort: {
    desc: sort
  },
  limit,
  select: {
    fields
  }
})
```

#### Accessing [FA1.2 tokens with the largest number of holders](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/#examples-of-the-v1-tokens-endpoint-usage)

```ts
import { tokensGetTokens } from '@tzkt/sdk-api'

const standard = 'fa1.2'
const sort = 'holdersCount'
const limit = 10

const r = await tokensGetTokens({
  standard: {
    eq: standard
  },
  sort: {
    desc: sort
  },
  limit
})
```

#### Accessing [all account's NFTs](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/#examples-of-the-v1-tokens-balances-endpoint-usage)

```ts
import { tokensGetTokenBalances } from '@tzkt/sdk-api'

const account = 'tz1SLgrDBpFWjGCnCwyNpCpQC1v8v2N8M2Ks'
const minBalance = 0
const symbol = 'OBJKT'
const limit = 10

const r = await tokensGetTokenBalances({
  account: {
    eq: account
  },
  balance: {
    ne: `${minBalance}`
  },
  tokenMetadata: {
    eq: {
      jsonPath: 'symbol',
      jsonValue: symbol
    }
  },
  limit
})
```

#### Accessing [whale transfers of a token](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/#examples-of-the-v1-tokens-transfers-endpoint-usage)

```ts
import { tokensGetTokenTransfers } from '@tzkt/sdk-api'

const tokenId = 85
const minAmount = '100000000000000000000000'
const sort = 'id'
const limit = 10

const r = await tokensGetTokenTransfers({
  tokenId: {
    eq: tokenId
  },
  amount: {
    gt: minAmount
  },
  sort: {
    desc: sort
  },
  limit
})
```

#### Accessing ["mints" of a token](https://baking-bad.org/blog/2022/01/11/tzkt-v17-with-generic-token-indexing-released/#examples-of-the-v1-tokens-transfers-endpoint-usage)

```ts
import { tokensGetTokenTransfers } from '@tzkt/sdk-api'

const tokenId = 85
const sort = 'id'
const limit = 10

const r = await tokensGetTokenTransfers({
  tokenId: {
    eq: tokenId
  },
  from: {
    null: true
  },
  sort: {
    desc: sort
  },
  limit
})
```

### [Accessing Transactions](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

Please refer to an article on [Transactions querying](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss) for a more detailed explanation on what these requests allow you to achieve.

#### Accessing [incoming transfers for the account](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

```ts
import { operationsGetTransactions } from '@tzkt/sdk-api'

const target = 'KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV'
const parameter = {
  path: 'to',
  value: 'tz1aKTCbAUuea2RV9kxqRVRg3HT7f1RKnp6a'
}

const r = await operationsGetTransactions({
  target: {
    eq: target
  },
  parameter: {
    eq: {
      jsonPath: parameter.path,
      jsonValue: parameter.value
    }
  }
})
```

#### Accessing [Dexter XTZ to USDtz trades with specified amount](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

```ts
import { operationsGetTransactions } from '@tzkt/sdk-api'

const target = 'KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV'
const parameter = {
  path: 'to',
  value: 'tz1aKTCbAUuea2RV9kxqRVRg3HT7f1RKnp6a'
}

const r = await operationsGetTransactions({
  target: {
    eq: target
  },
  parameter: {
    eq: {
      jsonPath: parameter.path,
      jsonValue: parameter.value
    }
  }
})
```

#### Accessing [Atomex atomic swaps with specified refund time](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

```ts
import { operationsGetTransactions } from '@tzkt/sdk-api'

const target = 'KT1VG2WtYdSWz5E7chTeAdDPZNy2MpP8pTfL'
const filterField = 'settings.refund_time'
const timeFrame = '2021-02-*'

const r = await operationsGetTransactions({
  target: {
    eq: target
  },
  parameter: {
    as: {
      jsonPath: filterField,
      jsonValue: timeFrame
    }
  }
})
```

#### Accessing [Dexter wXTZ/XTZ trades](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

```ts
import { operationsGetTransactions } from '@tzkt/sdk-api'

const target = 'KT1D56HQfMmwdopmFLTwNHFJSs6Dsg2didFo'
const entrypoints = ['xtzToToken', 'tokenToXtz', 'tokenToToken']

const r = await operationsGetTransactions({
  target: {
    eq: target
  },
  entrypoint: {
    in: entrypoints
  }
})
```

#### Accessing [operations with tzBTC related to a specific account](https://baking-bad.org/blog/2021/03/03/tzkt-v14-released-with-improved-smart-contract-data-and-websocket-api/#filter-transactions-by-parameter-like-a-boss)

```ts
import { operationsGetTransactions } from '@tzkt/sdk-api'

const target = 'KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn'
const entrypoints = ['mint', 'transfer', 'burn']
const parameter = '*tz1aKTCbAUuea2RV9kxqRVRg3HT7f1RKnp6a*'

const r = await operationsGetTransactions({
  target: {
    eq: target
  },
  entrypoint: {
    in: entrypoints
  },
  parameter: {
    as: {
      jsonValue: parameter
    }
  }
})
```