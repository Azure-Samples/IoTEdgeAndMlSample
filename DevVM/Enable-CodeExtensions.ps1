#*********************************************************
#
# Copyright (c) Microsoft. All rights reserved.
# This code is licensed under the MIT License (MIT).
# THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
# ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
# IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
# PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
#
#*********************************************************

#docker extension
code --install-extension ms-azuretools.vscode-docker
Write-Host "Starting Docker desktop. When it finishes starting it will prompt you to login."
Write-Host "You'll find the process running in the system tray (near the clock on the taskbar)."
Start-Process -FilePath "c:\Program Files\Docker\Docker\Docker Desktop.exe"

#iot extensions
code --install-extension vsciot-vscode.azure-iot-tools

#python extensions
code --install-extension ms-python.python

#dotnet extensions
code --install-extension ms-dotnettools.csharp

#powershell extension
code --install-extension ms-vscode.PowerShell

#aml extension
code --install-extension ms-toolsai.vscode-ai

#storage explorer
code --install-extension formulahendry.azure-storage-explorer
