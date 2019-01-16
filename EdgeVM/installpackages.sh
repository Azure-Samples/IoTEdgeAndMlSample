#!/bin/bash

echo "Installing repository configuration"
curl https://packages.microsoft.com/config/ubuntu/18.04/prod.list > ./microsoft-prod.list
sudo cp ./microsoft-prod.list /etc/apt/sources.list.d/

echo "Installing Microsoft GPG public key"
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo cp ./microsoft.gpg /etc/apt/trusted.gpg.d/

echo "Performing apt upgrade"
sudo apt-get upgrade

echo "Update apt-get"
sudo apt-get update

echo "Install the Moby engine"
sudo apt-get install -y moby-engine

echo "Install the Moby command-line interface (CLI)"
sudo apt-get install -y moby-cli

echo "Update apt-get"
sudo apt-get update

echo "Install the Docker engine"
sudo apt-get install -y docker

echo "Update apt-get"
sudo apt-get update

echo "Install the security daemon"
# package is installed at /etc/iotedge
sudo apt-get install -y iotedge