#!/bin/bash
set -euo pipefail

# Simple burst script for testing network performance

function make-request() {
  local status=`curl \
    --silent \
    --output /dev/null \
    --write-out "%{http_code}" \
    --request POST \
    --url http://localhost:8080/shorten \
    --header 'Content-Type: application/json' \
    --data '{ "url": "https://stackbuilders.com" }'`
  echo "Request $1: $status"
}

for i in {1..10}; do
  make-request $i &
done
wait

