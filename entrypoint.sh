#!/bin/sh

addgroup --system --gid 1000 customgroup \
&& adduser --system --uid 1000 --ingroup customgroup --shell /bin/sh customuser
mkdir -p /app/files
mkdir -p /app/files/sonarr
mkdir -p /app/files/radarr
chmod 777 -R /app/files
exec runuser -u customuser "$@"