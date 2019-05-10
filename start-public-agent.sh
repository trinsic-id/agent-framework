#!/usr/bin/env bash

echo 'Starting NGROK'
ngrok http -host-header="localhost:5000" 5000 > /dev/null &
sleep 2s
ngrok_host=$(curl --silent --show-error http://127.0.0.1:4040/api/tunnels | sed -nE 's/.*public_url":"https:..([^"]*).*/\1/p')
export ENDPOINT_HOST="http://$ngrok_host"
echo "NGROK started"

echo "Starting Web Agent with public url $ENDPOINT_HOST"
docker-compose -f docker-compose.public-agent.yaml up