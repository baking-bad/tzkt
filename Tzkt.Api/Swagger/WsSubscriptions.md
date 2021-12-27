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

## SubscribeToAccount

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

Same as in [/accounts/{address}](#operation/Accounts_GetByAddress).

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

	types: ''    // comma-separated list of operation types, any of:
	             // 'transaction', 'origination', 'delegation', 'reveal'
				 // 'double_baking', 'double_endorsing', 'nonce_revelation', 'activation'
				 // 'proposal', 'ballot', 'endorsement.
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
	path: ''        // path to the bigmap in the contract strage
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
	account: '',	// address of the account that sends/receives tokens
	contract: '',	// address of the contract that manages tokens
	tokenId: ''		// id of the token within the specified contract
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
// subscribe to all transfers of the 'tz123...' contract
await connection.invoke("SubscribeToTokenTransfers", { account: 'tz123...' });
````

---