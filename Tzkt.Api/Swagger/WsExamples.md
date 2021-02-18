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
                .WithUrl("https://staging.api.tzkt.io/v1/events")
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
````

or via CDN:
      
````sh
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/3.1.7/signalr.min.js"></script>
````

See more details [here](https://docs.microsoft.com/aspnet/core/signalr/javascript-client).

````js
const connection = new signalR.HubConnectionBuilder()
	.withUrl("https://staging.api.tzkt.io/v1/events")
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