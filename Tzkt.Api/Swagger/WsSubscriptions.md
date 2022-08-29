## SubscribeToHead

Sends the blockchain head every time it is changed.
		
### Method 

`SubscribeToHead`

### Channel 

`head`

### Parameters

No parameters.

### Data model

Same as in [/head](#tag/Head)

### State

State contains level (`int`) of the last processed head.

### Example

````js
connection.on("head", (msg) => { console.log(msg); });
await connection.invoke("SubscribeToHead");
````

---

## SubscribeToCycles

Notifies of the start of a new cycle with a specified delay.

### Method

`SubscribeToCycles`

### Channel

`cycles`

### Parameters

````js
{
	delayBlocks: 2,    // number of blocks (2 by default) to delay a new cycle notification
                       // should be >= 2 (to not worry abour reorgs) and < cycle size
}
````

### Data model

Same as in [/cycle](#operation/Cycles_GetByIndex)

### State

State contains an index (`int`) of the last processed cycle.

### Example

````js
connection.on("cycles", (msg) => { console.log(msg); });
await connection.invoke("SubscribeToCycles");
````

---

## SubscribeToBlocks  

Sends blocks added to the blockchain
		
### Method 

`SubscribeToBlocks`

### Channel 

`blocks`

### Parameters

No parameters.

### Data model

Same as in [/blocks](#operation/Blocks_GetHead)

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("blocks", (msg) => { console.log(msg); });
await connection.invoke("SubscribeToBlocks");
````

---

## SubscribeToAccounts

Sends touched accounts (affected by any operation in any way).

### Method

`SubscribeToAccounts`

### Channel

`accounts`

### Parameters

````js
{
	addresses: [], // [required] array of address you want to subscribe to
}
````

> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

`data` is an array of items described in response section of [/accounts/{address}](#operation/Accounts_GetByAddress) request.

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("accounts", (msg) => { console.log(msg); });
// subscribe to an account
await connection.invoke("SubscribeToAccounts", { addresses: [ 'tz1234...' ] });
````

---			

## SubscribeToOperations 
														  
Sends operations of specified types or related to specified accounts, included into the blockchain
		
### Method 

`SubscribeToOperations`

### Channel 

`operations`

### Parameters

````js
{
    address: '', // address you want to subscribe to,
                 // or null if you want to subscribe for all operations

    codeHash: 0, // hash of the code of the contract to which the operation is related
                 // (can be used with 'transaction', 'origination', 'delegation' types only)

    types: ''    // comma-separated list of operation types, any of:
                 // 'transaction', 'origination', 'delegation', 'reveal', 'register_constant', 'set_deposits_limit', 'increase_paid_storage'
				 // 'tx_rollup_origination', 'tx_rollup_submit_batch', 'tx_rollup_commit', 'tx_rollup_return_bond', 'tx_rollup_finalize_commitment',
				 // 'tx_rollup_remove_commitment', 'tx_rollup_rejection', 'tx_rollup_dispatch_tickets', 'transfer_ticket',
                 // 'double_baking', 'double_endorsing', 'double_preendorsing', 'nonce_revelation', 'vdf_revelation', 'activation'
                 // 'proposal', 'ballot', 'endorsement', 'preendorsement'.
}
````
																	 
> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

Same as in [/operations/transactions](#operation/Operations_GetTransactions), [/operations/delegations](#operation/Operations_GetDelegations), etc.

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("operations", (msg) => { console.log(msg); });
// subscribe to all transactions
await connection.invoke("SubscribeToOperations", { types: 'transaction' });
// subscribe to all transactions of the entire contract family
await connection.invoke("SubscribeToOperations", { types: 'transaction', codeHash: 1928472 });
// subscribe to all delegations and originations related to the address 'tz1234...'
await connection.invoke("SubscribeToOperations", { address: 'tz1234...', types: 'delegation,origination' });
````

---			

## SubscribeToBigMaps 
														  
Sends bigmap updates
		
### Method 

`SubscribeToBigMaps`

### Channel 

`bigmaps`

### Parameters

This method accepts the following parameters:

````js
{
	ptr: 0,         // ptr of the bigmap you want to subscribe to
	tags: [],       // array of bigmap tags ('metadata' or 'token_metadata')
	contract: '',   // contract address
	path: ''        // path to the bigmap in the contract storage
}
````

You can set various combinations of these fields to configure what you want to subscribe to. For example:

````js
// subscribe to all bigmaps
{
}	 

// subscribe to all bigmaps with specific tags
{		
	tags: ['metadata', 'token_metadata']
}

// subscribe  to all bigmaps of the specific contract
{
	contract: 'KT1...'
}

// subscribe to all bigmaps of the specific contract with specific tags
{
	contract: 'KT1...',
	tags: ['metadata']
}

// subscribe to specific bigmap by ptr
{
	ptr: 123
}
	
// subscribe to specific bigmap by path
{
	contract: 'KT1...',
	path: 'ledger'
}
````

> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

Same as in [/bigmaps/updates](#operation/BigMaps_GetBigMapUpdates).

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("bigmaps", (msg) => { console.log(msg); });
// subscribe to all bigmaps of the 'KT123...' contract
await connection.invoke("SubscribeToBigMaps", { contract: 'KT123...' });
// subscribe to bigmap with ptr 123
await connection.invoke("SubscribeToBigMaps", { ptr: 123 });
````

---			

## SubscribeToTokenBalances
														  
Sends token balances when they are updated
		
### Method 

`SubscribeToTokenBalances`

### Channel 

`token_balances`

### Parameters

This method accepts the following parameters:

````js
{
	account: '',    // address of the account that holds tokens
	contract: '',   // address of the contract that manages tokens
	tokenId: ''     // id of the token within the specified contract
}
````

You can set various combinations of these fields to configure what you want to subscribe to. For example:

````js
// subscribe to all token balance updates
{
}

// subscribe to balance updates of all tokens within the contract
{
	contract: 'KT1...'
}

// subscribe to balance updates of a particular token
{
	contract: 'KT1...',
	tokenId: '0'
} 	 

// subscribe to token balance updates for the account
{		
	account: 'tz1...'
} 	 

// subscribe to balance updates of all tokens within the contract for the account
{		
	account: 'tz1...',
	contract: 'KT1...'
} 

// subscribe to a particular token balance updates for the account
{		
	account: 'tz1...',
	contract: 'KT1...',
	tokenId: '0'
}
````

> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

Same as in [/tokens/balances](#operation/Tokens_GetTokenBalances).

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("token_balances", (msg) => { console.log(msg); });
// subscribe to all token balances of the 'tz123...' account
await connection.invoke("SubscribeToTokenBalances", { account: 'tz123...' });
````

---			

## SubscribeToTokenTransfers
														  
Sends token transfers
		
### Method 

`SubscribeToTokenTransfers`

### Channel 

`transfers`

### Parameters

This method accepts the following parameters:

````js
{
	account: '',    // address of the account that sends/receives tokens
	contract: '',   // address of the contract that manages tokens
	tokenId: ''     // id of the token within the specified contract
}
````

You can set various combinations of these fields to configure what you want to subscribe to. For example:

````js
// subscribe to all transfers
{
}

// subscribe to transfers of all tokens within the contract
{
	contract: 'KT1...'
}

// subscribe to transfers of a particular token
{
	contract: 'KT1...',
	tokenId: '0'
} 	 

// subscribe to transfers from/to the account
{		
	account: 'tz1...'
} 	 

// subscribe to transfers of all tokens within the contract from/to the account
{		
	account: 'tz1...',
	contract: 'KT1...'
} 

// subscribe to transfers of a particular token from/to the account
{		
	account: 'tz1...',
	contract: 'KT1...',
	tokenId: '0'
}
````

> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

Same as in [/tokens/transfers](#operation/Tokens_GetTokenTransfers).

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("transfers", (msg) => { console.log(msg); });
// subscribe to all transfers of the 'tz123...' account
await connection.invoke("SubscribeToTokenTransfers", { account: 'tz123...' });
````

---			

## SubscribeToEvents 
														  
Sends contract events
		
### Method 

`SubscribeToEvents`

### Channel 

`events`

### Parameters

This method accepts the following parameters:

````js
{
	codeHash: 0,    // hash of the contract code
	contract: '',   // contract address
	tag: '',        // event tag
}
````

You can set various combinations of these fields to configure what you want to subscribe to. For example:

````js
// subscribe to all events
{
}	 

// subscribe to events with specific tag
{		
	tag: 'transfer'
}

// subscribe  to events of the specific contract
{
	contract: 'KT1...'
}

// subscribe to events of the specific contract with specific tag
{
	contract: 'KT1...',
	tag: 'transfer'
}

// subscribe to events of the 'family' of contracts
{
	codeHash: 1234
}
	
// subscribe to events of the 'family' of contracts with specific tag
{
	codeHash: 1234,
	tag: 'transfer'
}
````

> **Note:** you can invoke this method multiple times with different parameters to register multiple subscriptions.

### Data model

Same as in [/contracts/events](#operation/Events_GetContractEvents).

### State

State contains level (`int`) of the last processed block.

### Example

````js
connection.on("events", (msg) => { console.log(msg); });
// subscribe to all events of the 'KT123...' contract
await connection.invoke("SubscribeToEvents", { contract: 'KT123...' });
````

---
