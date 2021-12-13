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
#vars
baseurl="" #no "/" at the end of url # CHANGE ME

#logging
touch ~/script.log
touch ~/import.log
echo "TorrentID: $torrentId" >> ~/script.log
echo "TorrentName: $torrentName" >> ~/script.log
echo "TorrentPath: $torrentPath" >> ~/script.log

### BEGIN LOGIC
srcDir="" # CHANGE ME
destDir="${srcDir}/linked"

label="${torrentPath#$srcDir}"
# note that srcPath may be a file, not necessarily a
# directory. Which means the same is true for destPath.
srcPath="${torrentPath}/${torrentName}"
destPath="${destDir}${label}/${torrentName}"

send_to_rabbit() {
    echo "rabbit $1" >> ~/import.log
    #breakdown of path sent
    concatflag=false
    restofurl="/"
    IFS='/' read -r -a array <<< "$1"
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
        restofurl+="${array[index]}/"
        fi
    done
    
    #concat full url
    fullurl="${baseurl}${restofurl}"
    fileurl=${fullurl::-1}
    echo "$fileurl"
    
    #more logging
    touch ~/json.txt
    json='{
        "TorrentID": "'"$torrentId"'",
        "TorrentName": "'"$torrentName"'",
        "TorrentPath": "'"$fileurl"'"
    }'
    echo "$json" > ~/json.txt
    
    #publish to queue
    amqp-publish --uri="amqp://${username}:${password}@${hostname}:${port}${virtualhost}" --exchange="" --routing-key="${queue}" --body="${json}"
}

if [ -d "${srcPath}" ]
then
    # multiple rar files may be found in subdirectories, so handle each one, preserving hierarchy
    find "${srcPath}" -name '*.rar' -print0 | while read -d $'\0' rarFile
    do
        path="$(dirname "${rarFile}")"
        subDir="${path#$srcDir}"
        mkdir -p "${destDir}/${subDir}"
        unrar e -o+ "${rarFile}" "${path}"
        echo "unrar e $rarFile $path" >> ~/import.log
        # send_to_rabbit "${destDir}/${subDir}/${rarFile}" # this will link the rar file - pointless
    done

    # hardlink everything in the source directory (not rar-related), to the destination directory
    # find "${srcPath}" -mindepth 1 ! -regex '.*\.r[a0-9][r0-9]' -print0 |  while read -d $'\0' nonRarFile
    # find "${srcPath}" -mindepth 1 -name '*.mkv' -print0 |  while read -d $'\0' nonRarFile
    find "${srcPath}" -mindepth 1 \( -name '*.mkv' -o -name '*.avi' -o -name '*.mp4' \) -print0 |  while read -d $'\0' nonRarFile
    do
    echo "$nonRarFile"
        path="$(dirname "${nonRarFile}")"
        subDir="${path#$srcDir}"
        mkdir -p "${destDir}/${subDir}"
        # ln -v "${nonRarFile}" "${destDir}/${subDir}"
        send_to_rabbit $nonRarFile
    done
else
    # we were passed a single file, not a directory
    cp -la "${srcPath}" "${destPath}"
    echo "single file src ${srcPath} dst ${destPath}" >> ~/import.log
    send_to_rabbit $destPath
fi
