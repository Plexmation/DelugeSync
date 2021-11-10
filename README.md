# DelugeSync

A simple tool to download files from a seedbox/server which has an nginx index of links

## Prerequisites

* Docker on client/home server
* Deluge setup and running ([tutorial](https://www.linuxbabe.com/bittorrent/install-deluge-bittorrent-client-ubuntu-18-04) if you need it.) with both labels and execute plugins enabled (reboot first if first time install).
* All links need to contain the search word in the url for DelugeSync to pick it up from the profiles

## Server-side setup

### Setup [rabbitMQ](https://rabbitmq.com/)

#### docker-compose
More info (if you want) [here](https://hub.docker.com/_/rabbitmq).

Don't forget to change `<username>` and `<password>` to you own!
```SHELL
rabbitmq:
    image: rabbitmq:management
    container_name: rabbitmq
    hostname: rabbitmq
    ports:
      - "4369:4369"
      - "5671:5671"
      - "5672:5672"
      - "15671:15671"
      - "15672:15672"
      - "25672:25672"
    environment:
      RABBITMQ_DEFAULT_USER: <username>
      RABBITMQ_DEFAULT_PASS: <password>
    restart: unless-stopped
```

### Setup [amqp-publish](https://github.com/selency/amqp-publish)

Here is an easy command you can run on the server:
```SHELL
wget https://github.com/selency/amqp-publish/releases/download/v1.0.0/amqp-publish.linux-amd64 -O /usr/local/bin/amqp-publish
```

### Deluge labels

While not a requirement, it will make it easier to filter out downloads in deluge - making for a cleaner deluge UI as well.

the label names are irrelevant - just make sure your path includes the name of the search profile ie includes "sonarr" or "radarr"

![alt text](https://github.com/Plexmation/DelugeSync/blob/324ed2f6ea7a54d459b957dde00d519e396c58ee/images/deluge-labels.png "Deluge Screenshot")
### Deluge execute - Queue script

## Client-side/home setup

### Baremetal

Not a fan - here's a [tutorial](https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux).

### Docker

#### docker run

#### docker-compose
