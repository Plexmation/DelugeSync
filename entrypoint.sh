#!/bin/sh

addgroup --system --gid 1000 customgroup \
&& adduser --system --uid 1000 --ingroup customgroup --shell /bin/sh customuser
exec runuser -u customuser "$@"