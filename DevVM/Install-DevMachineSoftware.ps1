
param
(
    [Parameter(Mandatory = $True)]
    [string]
    $AdminUsername,
    
    #only necessary until we make the github repo public
    [Parameter(Mandatory = $True)]
    [string]
    $GitHubUserName,

    [Parameter(Mandatory = $True)]
    [string]
    $GitHubPat
)


#refresh path after chocolatey install
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

choco feature enable -n allowGlobalConfirmation

#install docker for windows
cinst docker-desktop

#add user to local docker users group
Add-LocalGroupMember -Group docker-users -Member $AdminUsername

#install visual studio code
cinst vscode

#install python
cinst python

#install dotnet core sdk with cli
cinst dotnetcore-sdk

#install azure powershell
cinst azurepowershell

#install storage explorer
cinst microsoftazurestorageexplorer 

#install and sync git
cinst git
cinst git-credential-manager-for-windows

#refresh path again for chocolatey package installs
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

$sourceRoot = "c:\source"
if (!(Test-Path $sourceRoot)) {
    mkdir $sourceRoot
}

Push-Location $sourceRoot
$repoName = "IoTEdgeAndMlSample"
if (!(Test-Path $repoName)) {
    & "C:\Program Files\Git\cmd\git.exe" clone https://$($GitHubUserName):$($GitHubPat)@github.com/Azure-Samples/$repoName.git
}
Pop-Location

#add python scripts to the path
$pythonPath = "$($env:Path);$($env:userprofile)\AppData\Roaming\Python\Python37\scripts"
if (!$env:Path.Contains($pythonPath)) {
    [Environment]::SetEnvironmentVariable("Path", $pythonPath, [EnvironmentVariableTarget]::Machine)
}
