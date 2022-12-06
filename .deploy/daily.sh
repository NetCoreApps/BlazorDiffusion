#!/bin/sh
curl https://api.blazordiffusion.com/api/SyncTasks?Daily=true
litestream restore -config /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/litestream.yml -o /tmp/db.sqlite /data/db.sqlite
litestream restore -config /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/litestream.yml -o /tmp/analytics.sqlite /data/analytics.sqlite

latestdbstr=$(sqlite3 /tmp/db.sqlite 'select ModifiedDate from Artifact order by ModifiedDate desc limit 1')
latestdbdate=$(date +%Y%m%d --date="${latestdbstr}" )
if [ -f "/tmp/old/db.sqlite" ]; then
  olddbdate=$(date +%Y%m%d --date="$(sqlite3 /tmp/old/db.sqlite 'select ModifiedDate from Artifact order by ModifiedDate desc limit 1')")
else
  olddbdate=$(date +%Y%m%d -d "yesterday")
fi

if [ ${latestdbdate} -ge ${olddbdate} ]; then
  echo "$(date +%Y%m%dT%H:%M) - Updated db.sqlite\n" >> /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/db-litestreamcheck.txt
else
  echo "$(date +%Y%m%dT%H:%M) - Failed db.sqlite" >> /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/db-litestreamcheck.txt
fi


# Analytics test
latestastr=$(sqlite3 /tmp/analytics.sqlite 'select CreatedDate from ArtifactStat order by CreatedDate desc limit 1')
latestadate=$(date +%Y%m%d --date="${latestastr}" )
if [ -f "/tmp/old/analytics.sqlite" ]; then
  oldadate=$(date +%Y%m%d --date="$(sqlite3 /tmp/old/analytics.sqlite 'select CreatedDate from ArtifactStat order by CreatedDate desc limit 1')")
else
  oldadate=$(date +%Y%m%d -d "yesterday")
fi

if [ ${latestadate} -ge ${oldadate} ]; then
  echo "$(date +%Y%m%dT%H:%M) - Updated analytics.sqlite" >> /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/analytics-litestreamcheck.txt
else
  echo "$(date +%Y%m%dT%H:%M) - Failed analytics.sqlite" >> /var/lib/docker/volumes/deploy_BlazorDiffusionWasm-assets/_data/analytics-litestreamcheck.txt
fi

# Success
mkdir -p /tmp/old
mv /tmp/analytics.sqlite /tmp/old/analytics.sqlite
mv /tmp/db.sqlite /tmp/old/db.sqlite