param
(
    [Parameter(Mandatory = $True)]
    [string] $SubscriptionId,

    [Parameter(Mandatory = $True)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $True)]
    [string] $Location,

    [Parameter(Mandatory = $True)]
    [string] $AdminUsername
)

$ErrorActionPreference = "Stop"

# Ensure the user is logged in
try {
    $azureContext = Get-AzureRmContext
}
catch {
}

if (!$azureContext -or !$azureContext.Account) {
    Login-AzureRmAccount
    $azureContext = Get-AzureRmContext
}

# Ensure the desired subscription is selected
if ($azureContext.Subscription.SubscriptionId -ne $SubscriptionId) {
    Write-Host "Selecting subscription $SubscriptionId"
    Select-AzureRmSubscription -SubscriptionId $SubscriptionId | Out-Null
}

Write-Host "You are about to create a virtual machine in Azure in subscription $SubscriptionId, resource group $ResourceGroupName, in the '$Location' region."
$confirmation = Read-Host -Prompt "Do you wish to proceed? Type 'yes' to confirm"
if ($confirmation -ne 'yes') {
    Write-Host "Exiting"
    return
}

# Get or create resource group
$rg = Get-AzureRmResourceGroup $ResourceGroupName -ErrorAction Ignore
if (!$rg) {
    $rg = New-AzureRmResourceGroup $ResourceGroupName -Location $Location
}

# Submit the ARM template deployment
$randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | % {[char]$_})
$deploymentName = "IotEdgeMlDemoVm-$randomSuffix"
$params = @{
    "location"      = $Location
    "adminUsername" = $AdminUsername
}

Write-Host "`nStarting deployment of the demo VM which may take a while."
Write-Host "`nProgress can be monitored from the Azure Portal (portal.azure.com)."
Write-Host "Find the resource group $ResourceGroupName in $SubscriptionId subscription, and look in the Deployments menu for a deployment named $deploymentName."
$deployment = New-AzureRmResourceGroupDeployment -Name $deploymentName -ResourceGroupName $rg.ResourceGroupName -TemplateFile '.\IoTEdgeMLDemoVMTemplate.json' -TemplateParameterObject $params
$vmName = $deployment.Outputs.vmName.value

Write-Host "Turning on Hyper-V"
Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Enable-HyperV.ps1'

Write-Host "Restart Virtual Machine"
Restart-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName 

$azureVM = Get-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName
$vmFQDN = ($azureVM | Foreach-Object{Get-AzureRmPublicIpAddress -ResourceGroupName $_.ResourceGroupName} | Select-Object @{Name="FQDN"; Expression={$_.DnsSettings.Fqdn}}).FQDN

$rdpContent = @"
full address:s:$($vmFQDN):3389
prompt for credentials:i:1
username:s:$vmName\$AdminUsername
"@

Set-Content -Path ".\$($vmName).rdp" -Value $rdpContent

Write-Host "The VM is ready."
Write-Host "Visit the Azure Portal (http://portal.azure.com)."
Write-Host "Find the resource group $ResourceGroupName in $SubscriptionId subscription"
Write-Host "In $ResourceGroupName and find your VM named $vmName."
Write-Host "Use the Connect button to download an RDP file."
Write-Host "When logging in, remember to use the username and password supplied to this script."

Write-Warning "`nPlease note this VM was configured with a shutdown schedule. Review it on the VM blade to confirm the settings work for you."