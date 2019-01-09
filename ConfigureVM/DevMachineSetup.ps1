#install and configure chocolatey
Start-Process -FilePath "powershell" -Wait -NoNewWindow -ArgumentList "Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))"

#refresh path after chocolatey install
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

choco feature enable -n allowGlobalConfirmation

#install docker for windows
cinst docker-desktop

#install visual studio code
cinst vscode

#install conda to manage python environments
cinst miniconda3

#install dotnet core sdk with cli
cinst dotnetcore-sdk

#install storage explorer
cinst microsoftazurestorageexplorer 

#install and sync git
cinst git
cinst git-credential-manager-for-windows

#refresh path again for chocolatey package installs
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")

mkdir c:\source
Push-Location \source
& "C:\Program Files\Git\cmd\git.exe" clone https://iote2e@dev.azure.com/iote2e/e2e/_git/EdgeAndMl
Pop-Location

#add python scripts to the path
[Environment]::SetEnvironmentVariable("Path", "$($env:Path);$($env:userprofile)\AppData\Roaming\Python\Python37\scripts", [EnvironmentVariableTarget]::Machine)