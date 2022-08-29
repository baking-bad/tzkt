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

Install SignalR package via pypi:

````sh
> pip install signalrcore
````

See more details [here](https://github.com/mandrewcito/signalrcore#signalr-core-client).

````python
from signalrcore.hub_connection_builder import HubConnectionBuilder
from time import sleep
from pprint import pprint

connection = HubConnectionBuilder()\
    .with_url('https://api.tzkt.io/v1/ws')\
    .with_automatic_reconnect({
        "type": "interval",
        "keep_alive_interval": 10,
        "intervals": [1, 3, 5, 6, 7, 87, 3]
    })\
    .build()
  
def init():
    print("connection established, subscribing to blocks and operations")
    connection.send('SubscribeToHead', [])
    connection.send('SubscribeToOperations', 
                    [{'address': 'KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton', 
                      'types': 'transaction'}])

connection.on_open(init)
connection.on("head", pprint)
connection.on("operations", pprint)

connection.start()

try:
    while True:
        sleep(1)
except KeyboardInterrupt:
    pass
finally:
    print('shutting down...')
    connection.stop()
````