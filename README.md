# eiscp2mqtt

Connect your onkyo receiver to mqtt.

## How?

```sh
eiscp2mqtt -m <mqttserver> --host <receiverip>
```

You can specify multiple host by just adding more `--host` parameter.

## Protocol

The command implementation is based on the great work of [onkyo-eiscp](https://github.com/miracle2k/onkyo-eiscp).

All events received are published via mqtt at `<prefix>/<host>/<zone>/<command>` - e.g. `eiscp/onkyo.local/main/master-volume`. To send commands just published them to `<prefix>/<host>`:

```sh
mosquitto_pub -t 'eiscp/onkyo.local' -m 'master-volume query'
```

### Docker

If you want to run this via docker, use:

```sh
docker run -d --restart=unless-stopped -e MQTTHOST=<mqttserver> -e HOST=<receiverip> zivillian/eiscp2mqtt:latest
```
