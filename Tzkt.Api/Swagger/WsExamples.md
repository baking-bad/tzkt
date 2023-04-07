Here are some examples of a simple client app in different languages.

## C# simple client

Install SignalR package:

````sh
> dotnet add package Microsoft.AspNetCore.SignalR.Client
// if you use .NET 5.0 you will likely also need:
> dotnet add package System.Text.Encodings.Web
````

See more details [here](https://docs.microsoft.com/aspnet/core/tutorials/signalr).

````cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace TestConsoleCore
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("https://api.tzkt.io/v1/ws")
                .Build();

            async Task Init(Exception arg = null)
            {
                // open connection
                await connection.StartAsync();
                // subscribe to head
                await connection.InvokeAsync("SubscribeToHead");
                // subscribe to account transactions
                await connection.InvokeAsync("SubscribeToOperations", new
                {
                    address = "KT19kgnqC5VWoxktLRdRUERbyUPku9YioE8W",
                    types = "transaction"
                });
            }

            // auto-reconnect
            connection.Closed += Init;

            connection.On("head", (JsonElement msg) =>
            {
                Console.WriteLine(msg.GetRawText());
            });

            connection.On("operations", (JsonElement msg) =>
            {
                Console.WriteLine(msg.GetRawText());
            });

            await Init();

            Console.ReadLine();

            connection.Closed -= Init;
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
    }
}
````

---

## JS simple client

Install SignalR package via npm:

````sh
> npm install @microsoft/signalr

const signalR = require("@microsoft/signalr");
````

or via CDN:
      
````sh
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/3.1.7/signalr.min.js"></script>
````

See more details [here](https://docs.microsoft.com/aspnet/core/signalr/javascript-client).

````js
const connection = new signalR.HubConnectionBuilder()
	.withUrl("https://api.tzkt.io/v1/ws")
	.build();

async function init() {
	// open connection
	await connection.start();
	// subscribe to head
	await connection.invoke("SubscribeToHead");
	// subscribe to account transactions
	await connection.invoke("SubscribeToOperations", {
		address: 'KT19kgnqC5VWoxktLRdRUERbyUPku9YioE8W',
		types: 'transaction'
	});
};

// auto-reconnect
connection.onclose(init);

connection.on("head", (msg) => {
	console.log(msg);			
});

connection.on("operations", (msg) => {
	console.log(msg);			
});

init();
````

---

## Python simple client

Install pysignalr package via `pypi`:

````sh
> pip install pysignalr
````

See more details [here](https://github.com/baking-bad/pysignalr).

````python
import asyncio
from contextlib import suppress
from typing import Any
from typing import Dict
from typing import List

from pysignalr.client import SignalRClient
from pysignalr.messages import CompletionMessage


async def on_open() -> None:
    print('Connected to the server')


async def on_close() -> None:
    print('Disconnected from the server')


async def on_message(message: List[Dict[str, Any]]) -> None:
    print(f'Received message: {message}')


async def on_error(message: CompletionMessage) -> None:
    print(f'Received error: {message.error}')


async def main() -> None:
    client = SignalRClient('https://api.tzkt.io/v1/ws')

    client.on_open(on_open)
    client.on_close(on_close)
    client.on_error(on_error)
    client.on('operations', on_message)

    await asyncio.gather(
        client.run(),
        client.send('SubscribeToOperations', [{}]),
    )


with suppress(KeyboardInterrupt, asyncio.CancelledError):
    asyncio.run(main())
````
