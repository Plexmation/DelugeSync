#!/bin/bash
# set -x
torrentId=$1
torrentName=$2
torrentPath=$3

#rabbit credentials
username="" # CHANGE ME
password="" # CHANGE ME
hostname="" # CHANGE ME
port="5672"
virtualhost="/"
queue="deluge-queue"

#logging
touch import.log

fileUrl="${torrentPath}"
send_to_rabbit() {
    json='{
        "TorrentID": "'"$torrentId"'",
        "TorrentName": "'"$torrentName"'",
        "TorrentPath": "'"$1"'",
        "IsSingle": "'"$2"'"
    }'
    echo "$json" >> import.log

    #publish to queue
    ./amqp-publish --uri="amqp://${username}:${password}@${hostname}:${port}${virtualhost}" --exchange="" --routing-key="${queue}" --body="${json}"
}

srcPath="${torrentPath}/${torrentName}"
destPath="${destDir}${label}/${torrentName}"

touch rar.log

if [ -d "${srcPath}" ]
then
    find "${srcPath}" -name '*.rar' -print0 | while read -d $'\0' rarFile
    do
        path="$(dirname "${rarFile}")"
        echo "unrar e -o+ $rarFile $path" >> rar.log
        unrar e -o+ "${rarFile}" "${path}"
    done
    find "${srcPath}" -mindepth 1 \( -name '*.mkv' -o -name '*.avi' -o -name '*.mp4' \) -print0 |  while read -d $'\0' nonRarFile
    do
    echo "un/nonrarred file: $nonRarFile" >> rar.log
        send_to_rabbit $nonRarFile
    done
else
    echo "single file src ${srcPath} dst ${destPath}" >> rar.log
    send_to_rabbit $srcPath true
fi