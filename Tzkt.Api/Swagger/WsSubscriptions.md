## SubscribeToHead

Sends the blockchain head every time it has been updated.
		
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

	types: ''    // comma-separated list of operation types
	             // such as 'transaction', 'delegation', etc.
}
````

> **Note:** Currently, you can subscribe to no more than 50 addresses per single connection. If you need more, let us know and consider opening more connections in the meantime,
> or subscribe to all operations and filter them on the client side.

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