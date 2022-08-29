TzKT WebSocket API is based on [SignalR](https://docs.microsoft.com/aspnet/signalr/overview/getting-started/introduction-to-signalr)
that enables receiving data from the Tezos blockchain in real-time.

This is a very useful feature, especially when you need to receive updates immediately, as soon as they appear on the node and are indexed by the indexer.
Also it allows to significantly simplify client's logic responsible for updating/synchronizing data.

WebSocket API base URL: `https://{baseUrl}/v1/ws`

## Message types

After connecting to the WebSocket and subscribing, the client starts receiving messages from the server.
There are three message types that the server sends to the client:

#### 1. State message

````js
{
	type: 0,    // 0 - state message
	state: 0    // subscription state
}
````

This is the very first message the client receives from the server after opening a subscription. 
This message is a kind of confirmation that the subscription has been successfully processed.

There is just one meaningful field - `state`, that represents current state of the subscription.
The type of that field depends on a particular subscription (see documentation on each subscription).
For example, if you subscribe to new blocks, the `state` will contain a level of the last sent block.
So if you receive a state message with `state: 1300123`, the next block you may expect to receive will be `1300124`.

State can be used to fetch historical data from the REST API right after opening a subscription, so that there will be no nor misses nor duplicates.

#### 2. Data message	  

````js
{
	type: 1,    // 1 - data message
	state: 0,   // subscription state
	data: {}    // data: object or array, depending on subscription
}
````

After subscription is registered and state message is received, client will receive data messages. As you can see, `data` is an array (array of
blocks, operations, or whatever you have subscribed to), that means you may receive multiple items in a single message,
which is great for performance and network traffic.

TzKT WebSocket API operates with the same data models as the REST API to achieve full compatibility and data consistency.

#### 3. Reorg message	

````js
{			  
	type: 2,    // 2 - reorg message
	state: 0    // subscription state
}
````

Reorg messages signal about chain reorganizations, when some blocks, including all operations, are rolled back
in favor of blocks with higher weight.  Chain reorgs happen quite often, so it's not something you can ignore.
You have to handle such messages correctly, otherwise you will likely accumulate duplicate data or, worse, invalid data.

For example, if you receive blocks `10`, `11`, `12`, `13` and then receive reorg message with `state: 11`,
you should remove blocks `12` and `13` (if you saved them) as they are no longer valid.