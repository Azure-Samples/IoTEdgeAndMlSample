#!/bin/bash

export TERM=xterm \
    && ./certGen.sh create_root_and_intermediate \
    && ./certGen.sh create_edge_device_certificate "turbofanGateway" \
    && cat ./certs/new-edge-device.cert.pem \
        ./certs/azure-iot-test-only.intermediate.cert.pem \
        ./certs/azure-iot-test-only.root.ca.cert.pem > \
        ./certs/new-edge-device-full-chain.cert.pem

certDir="$1/certs"
privateDir="$1/private"

if [ -d $certDir ]; then
    rm -r $certDir
fi
if [ -d $privateDir ]; then
    rm -r $privateDir
fi

mkdir -p $certDir
mkdir -p $privateDir

cp  /work/certs/new-edge-device* $certDir
cp  /work/certs/azure-iot-test-only.root.ca.cert.pem $certDir
cp  /work/private/new-edge-device.key.pem $privateDir