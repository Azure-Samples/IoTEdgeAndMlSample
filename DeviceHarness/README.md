# Device Harness

This .NET Core project can be used to send simulated Turbofan device data to an Azure IoT Hub.  As part of the walk-through for the [IoT  Edge for Machine Learning](aka.ms/IoTEdgeMLPaper) white paper.

## Prerequisites

If you are not following the walk-through and using the recommended [development machine](../ConfigureVm/README.md) you will need to ensure you have:

- Installed the [.NET Core SDK](https://dotnet.microsoft.com/download) for building and running the project

- Access to an [Azure Subscription](https://azure.microsoft.com/en-us/free/)

- Created a [Azure IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal) 

- Configured [message routing](https://docs.microsoft.com/en-us/azure/iot-hub/tutorial-routing) to an [Azure Storage Account](https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account?tabs=azure-portal)

- [Visual Studio Code](https://code.visualstudio.com/Download) with extensions:

  - [Azure IoT Tools for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)

  - [Azure Account and Sign-In](https://marketplace.visualstudio.com/items?itemName=ms-vscode.azure-account)

  - [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

## Build and Run

To build the project:

1. Open the DeviceHarness folder in Visual Studio Code **File -> OpenFolder**

1. If prompted to reload window, restore dependencies, and/or add required assets do so

1. Run the build using **Terminal -> Run Build Task...** (Ctrl+Shift+B) and choose "build" when prompted

To run the project:

1. Copy your IoT Hub connection string to your clipboard see [documentation](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-vscode-iot-toolkit-cloud-device-messaging) for connecting Visual Studio Code to IoT Hub

1. Open a terminal window in Visual Studio Code (Ctrl+Shift+`)

1. Type "**dotnet run**" into the terminal

1. When prompted provide IoT Hub connection string
