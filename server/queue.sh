#!/bin/bash
torrentid=$1
torrentname=$2
torrentpath=$3

#rabbit credentials
username=""
password=""
hostname="localhost"
port="5672"
virtualhost="/"
queue="deluge-queue"
#vars
baseurl="https://downloads.404developments.com" #no "/" at the end of url

#logging
touch script.log
echo "TorrentID: $torrentid" >> script.log
echo "TorrentName: $torrentname" >> script.log
echo "TorrentPath: $torrentpath" >> script.log

#breakdown of path sent
concatflag=false
restofurl="/"
IFS='/' read -r -a array <<< "$torrentpath"
for index in "${!array[@]}"
do
    if [ "${array[index]}" == "sonarr" ]; then
    concatflag=true
    fi
    if [ "${array[index]}" == "radarr" ]; then
    concatflag=true
    fi
    if [ "${array[index]}" == "lidarr" ]; then
    concatflag=true
    fi
    if [ "$concatflag" != false ]; then
    # echo "false"
    restofurl+="${array[index]}/"
    # echo "${array[index]}"
    # echo "$restofurl"
    fi
done

#concat full url
fullurl="${baseurl}${restofurl}"
fileurl=${fullurl::-1}
echo "$fileurl"

#more logging
touch json.txt
json='{
    "TorrentID": "'"$torrentid"'",
    "TorrentName": "'"$torrentname"'",
    "TorrentPath": "'"$fileurl"'"
}'
echo "$json" > json.txt

#publish to queue
amqp-publish --uri="amqp://${username}:${password}@${hostname}:${port}${virtualhost}" --exchange="" --routing-key="${queue}" --body="${json}"