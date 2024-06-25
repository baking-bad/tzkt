# Introduction

TzKT is the most widely used tool in Tezos that provides you with convenient and flexible access to the Tezos blockchain data, processed and indexed by its own indexer. 
You can fetch all historical data via REST API, or subscribe for real-time data via WebSocket API. TzKT was built by the joint efforts of the entire Tezos community 
to help developers build more services and dapps on top of Tezos.

TzKT Indexer and API are [open-source](https://github.com/baking-bad/tzkt), so don't be afraid to depend on the third-party service,
because you can always clone, build and run it yourself to have full control over all the components.

Feel free to contact us if you have any questions or feature requests.
Your feedback is much appreciated!

- Discord: https://discord.gg/aG8XKuwsQd
- Telegram: https://t.me/baking_bad_chat
- Slack: https://tezos-dev.slack.com/archives/CV5NX7F2L
- Twitter: https://twitter.com/TezosBakingBad
- Email: hello@bakingbad.dev

And don't forget to star TzKT [on GitHub](https://github.com/baking-bad/tzkt) if you like it 😊

# Get Started

There are two API services provided for public use:
- **Free TzKT API** with free anonymous access;
- **TzKT Pro** with paid subscriptions with increased rate limits, off-chain data, extended support and business-level SLA.

You can find more details about differences between available tiers [here](https://tzkt.io/api).

## Free TzKT API

Free-tier TzKT API is the best way to get started and explore available Tezos data and API functionality.
It doesn't require authorization and is free for everyone and for both commercial and non-commercial use.

> #### Note: attribution required
If you use free-tier TzKT API, you **must** mention it on your website or application by placing the label
"Powered by TzKT API", or "Built with TzKT API", or "Data provided by TzKT API" with a direct link to [tzkt.io](https://tzkt.io).

It's available for the following Tezos networks with the following base URLs:

- Mainnet: `https://api.tzkt.io/` or `https://api.mainnet.tzkt.io/` ([view docs](https://api.tzkt.io))
- Ghostnet: `https://api.ghostnet.tzkt.io/` ([view docs](https://api.ghostnet.tzkt.io))
- Parisnet: `https://api.parisnet.tzkt.io/` ([view docs](https://api.parisnet.tzkt.io))

### Sending Requests

To send a request to Free TzKT API you need literally nothing. Just take the base URL of the particular network
(for example, Tezos mainnet: `https://api.tzkt.io`) and append the path of the particular endpoint
(for example, chain's head: `/v1/head`), that's pretty much it: 

```bash
curl https://api.tzkt.io/v1/head
```

Read through this documentation to explore available endpoints, query parameters
(note, if you click on a query parameter, you will see available modes, such as `.eq`, `.in`, etc.)
and response models. If you have any questions, do not hesitate to ask for support, Tezos community has always been very friendly! 😉

### Rate Limits

Please, refer to https://tzkt.io/api to check relevant rate limits.

If you exceed the limit, the API will respond with `HTTP 429` status code.

## TzKT Pro

TzKT Pro is intended for professional use, for those who seek for extended capabilities, performance, reliability and business-level SLA.
TzKT Pro service is provided via paid subscriptions. Please, refer to [Pricing Plans](https://tzkt.io/api) to check available tiers.

It's available for the following Tezos networks with the following base URLs:

- Mainnet: `https://pro.tzkt.io/` ([view docs](https://api.tzkt.io))
- Testnets: *let us know if you need TzKT Pro for testnets*

### Authorization

To access TzKT Pro you will need to authorize requests with your personal API key, that you will receive on your email after purchasing a subscription.
This can be done by adding the query string parameter `?apikey={your_key}` or by adding the HTTP header `apikey: {your_key}`.

Note that you can have multiple API keys within a single subscription.

Keep your API keys private, do not publish it anywhere and do not hardcode it, especially in public repositories.
If your key was compromised, just let us know and we will issue a new one.

Also note that passing the API key via HTTP headers is more secure, because in HTTPS headers are encrypted,
but query string is not, so the key can be unintentionally exposed to third parties.

### Sending Requests

Sending a request with the API key passed as a query string parameter:

```bash
curl https://pro.tzkt.io/v1/head?apikey={your_key}
```

Sending a request with the API key passed via an HTTP header:

```bash
curl https://pro.tzkt.io/v1/head \
    -H 'apikey: {your_key}'
```

### Rate Limits

Please, refer to https://tzkt.io/api to check relevant rate limits for different pricing plans.

Also, TzKT Pro provides you with the additional HTTP headers to show the allowed limits, number of available requests
and the time remaining (in seconds) until the quota is reset. Here's an example:

```
RateLimit-Limit: 50
RateLimit-Remaining: 49
RateLimit-Reset: 1
```

It also sends general information about your rate limits per second and per day:

```
X-RateLimit-Limit-Second: 50
X-RateLimit-Remaining-Second: 49
X-RateLimit-Limit-Day: 3000000
X-RateLimit-Remaining-Day: 2994953
```

If you exceed the limit, the API will respond with `HTTP 429` status code.
