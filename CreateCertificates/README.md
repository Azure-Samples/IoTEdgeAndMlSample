# Generate test certificates for Edge Gateway

## Introduction

The assets in this folder generate **non-production** certificates suitable for IoT Edge transparent gateway scenario and are meant to streamline the process described in the [IoT  Edge for Machine Learning](aka.ms/IoTEdgeMLPaper).

For details on how to configure IoT Edge transparent gateways, see [Configure an IoT Edge device to act as a transparent gateway](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway).

In production scenarios the certificates need to be issued by a trusted certificate authority like Baltimore, Verisign, DigiCert, etc. See [Azure IoT Edge certificate usage detail](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-certs) for a detailed discussion of certificates with IoT Edge devices.

## Prerequisites

The scripts in this folder expect that you have set up a machine as found in [IoT  Edge for Machine Learning](aka.ms/IoTEdgeMLPaper) and in the [Device Harness ReadMe](../../DeviceHarness/README.md).

## Creating certificates

There are 2 steps to generating certificates.

1. Build a Docker image that contains [openssl](https://www.openssl.org/) and the [scripts](https://github.com/Azure/azure-iot-sdk-c/tree/master/tools/CACertificates)

1. Run a container using the image to copy the files onto your local machine

### Build the Docker image

The Docker image can be built using either the Docker CLI or directly from Visual Studio Code.

#### Docker CLI

1. Open the Docker CLI (any shell where docker is in the PATH)

1. Change to the directory containing this README

1. Run the command:

    ```cmd
    docker build --rm -f "openssl.dockerfile" -t mledgeopenssl:latest .
    ```

#### Visual Studio Code

1. Open this folder in Visual Studio Code File -> Open Folder...

1. Right-click on openssl.dockerfile and choose 'Build Image' from the context menu

1. Type "mledgeopenssl:latest" into the 'Tag image as...' field and hit enter

### Generate the certificates

1. Create a local directory for the certificates called c:\edgeCertificates

1. Open Docker CLI

1. Run the command:

    ```cmd
    docker run --name mledgeopenssl --rm -v c:\edgeCertificates:/edgeCertificates mledgeopenssl /edgeCertificates
    ```

### Output

Docker will run the container, generate the certificates using openssl, and then copy the certificates to the mounted volume (c:\edgeCertificates).  After the run completes, you will have the following files:

- C:\edgeCertificates\certs\azure-iot-test-only.root.ca.cert.pem

- C:\edgeCertificates\certs\new-edge-device.cert.pem

- C:\edgeCertificates\certs\new-edge-device.cert.pfx

- C:\edgeCertificates\private\new-edge-device.key.pem
