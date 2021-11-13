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

![alt text](https://github.com/Plexmation/DelugeSync/blob/16c73f2fc14d6e5b653bc7cdafd28cb026f23976/images/deluge-labels.png "Deluge Screenshot")
### Deluge execute - Queue script

## Client-side/home setup

### Baremetal

Not a fan - here's a [tutorial](https://swimburger.net/blog/dotnet/how-to-run-a-dotnet-core-console-app-as-a-service-using-systemd-on-linux).

### Docker-compose

#### building locally

```SHELL
https://github.com/Plexmation/DelugeSync.git
cd DelugeSync/
```

Build context is set within the yml, make sure to change capital variables

local.docker-compose.yml source:
```SHELL
version: "3"
services:
  delugesync-local:
    container_name: delugesync-local
    build:
      context: .
      dockerfile: ./Dockerfile
    networks:
      - delugesync1
    volumes:
      - /opt/ProgramData/DelugeSync:/app/files
    environment:
      - RabbitMQ__UserName=USERNAME #required
      - RabbitMQ__Password=PASSWORD #required
      - RabbitMQ__HostName=HOSTNAME #required
      - RabbitMQ__VirtualHost=/ #optional
      - RabbitMQ__Port=5672 #optional
      - RabbitMQ__Queue=deluge-queue #optional
      - DownloadProfiles__HTTP__UserName=USERNAME #required
      - DownloadProfiles__HTTP__Password=PASSWORD #required
      - DownloadProfiles__HTTP__BaseUrl=https://downloads.mydomain.example/ #required
      - DownloadProfiles__HTTP__DownloadChunks=16 #optional
      - DownloadProfiles__HTTP__MaxConnections=1000 #optional
      - DownloadProfiles__HTTP__ConnectionIdleTimeout=10 #optional
      - General__LocalSaveLocation=files #optional
      - General__CreateSubDirectories=true #optional
networks:
  delugesync1:
    driver: bridge
```

build and run:
```SHELL
sudo docker-compose -f local.docker-compose.yml up -d
```

#### docker-compose

```SHELL
version: "3"
services:
  delugesync:
    container_name: delugesync
    image: ghcr.io/plexmation/delugesync:master
    networks:
      - delugesync0
    volumes:
      - /opt/ProgramData/DelugeSync:/app/files
    environment:
      - RabbitMQ__UserName=USERNAME #required
      - RabbitMQ__Password=PASSWORD #required
      - RabbitMQ__HostName=HOSTNAME #required
      - RabbitMQ__VirtualHost=/ #optional
      - RabbitMQ__Port=5672 #optional
      - RabbitMQ__Queue=deluge-queue #optional
      - DownloadProfiles__HTTP__UserName=USERNAME #required
      - DownloadProfiles__HTTP__Password=PASSWORD #required
      - DownloadProfiles__HTTP__BaseUrl=https://downloads.mydomain.example/ #required
      - DownloadProfiles__HTTP__DownloadChunks=16 #optional
      - DownloadProfiles__HTTP__MaxConnections=1000 #optional
      - DownloadProfiles__HTTP__ConnectionIdleTimeout=10 #optional
      - General__LocalSaveLocation=files #optional
      - General__CreateSubDirectories=true #optional
networks:
  delugesync0:
    driver: bridge
```

Then run
```SHELL
sudo docker-compose up -d
```
to get up and running or
```SHELL
sudo docker-compose -f gcr.docker-compose.yml up -d
```
if you are using the provided compose (remember to change to your variables first)
