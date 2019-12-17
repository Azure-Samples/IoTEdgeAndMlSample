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

param
(
    [Parameter(Mandatory = $True)]
    [string]
    $AdminUsername
)

# refresh path after chocolatey install
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

choco feature enable -n allowGlobalConfirmation

# install docker for windows
cinst docker-desktop

# add user to local docker users group
Add-LocalGroupMember -Group docker-users -Member $AdminUsername

# install visual studio code
cinst vscode

# install python
cinst python

# install dotnet core sdk with cli
cinst dotnetcore-sdk --version=3.1.100

# install azure powershell
cinst azurepowershell

# install and sync git
cinst git
cinst git-credential-manager-for-windows

# refresh path again for chocolatey package installs
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

$sourceRoot = "c:\source"
if (!(Test-Path $sourceRoot)) {
    mkdir $sourceRoot
}

# Clone this repo, if not already present
Push-Location $sourceRoot
$repoName = "IoTEdgeAndMlSample"
if (!(Test-Path $repoName)) {
    & "C:\Program Files\Git\cmd\git.exe" clone https://github.com/Azure-Samples/$repoName.git
}
Pop-Location

# add python scripts to the path
$pythonPath = "$($env:Path);$($env:userprofile)\AppData\Roaming\Python\Python37\scripts"
if (!$env:Path.Contains($pythonPath)) {
    [Environment]::SetEnvironmentVariable("Path", $pythonPath, [EnvironmentVariableTarget]::Machine)
}
