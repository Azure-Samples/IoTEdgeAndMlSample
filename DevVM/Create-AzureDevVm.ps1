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
    [string] $SubscriptionId,

    [Parameter(Mandatory = $True)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $True)]
    [ValidateSet("Australia East", "East US", "East US 2", "South Central US", "Southeast Asia", "West Europe", "West Central US", "West US 2")]
    [string] $Location,

    [Parameter(Mandatory = $True)]
    [string] $AdminUsername,

    [Parameter(Mandatory = $True)]
    [securestring] $AdminPassword
)

$ErrorActionPreference = "Stop"


###########################################################################
#
# Install-AzurePowerShell - ensures that Azure Powershell module is 
# installed and configured.
# 
Function Install-AzurePowerShell() {
    if (Get-Module -ListAvailable -Name AzureRM) { 
        Write-Host "Found AzureRm; skipping install..." 
        return
    }

    $azModule = Get-Module -ListAvailable -Name Az.Accounts
    if (!$azModule -or !$azModule.version -lt 0.7.0.0)
    {
        Write-Host "Installing Az PowerShell"
        Install-Module -Name Az -AllowClobber
    }

    Write-Host "Enabling AzureRm PowerShell alias"
    Enable-AzureRmAlias -Scope Process
}

###########################################################################
#
# Connect-AzureSubscription - gets current Azure context or triggers a 
# user log in to Azure. Selects the Azure subscription for creation of 
# the virtual machine
# 
Function Connect-AzureSubscription() {
    # Ensure the user is logged in
    try {
        $azureContext = Get-AzureRmContext
    }
    catch {
    }

    if (!$azureContext -or !$azureContext.Account) {
        Write-Host "Please login to Azure..."
        Login-AzureRmAccount
        $azureContext = Get-AzureRmContext
    }

    # Ensure the desired subscription is selected
    if ($azureContext.Subscription.SubscriptionId -ne $SubscriptionId) {
        Write-Host "Selecting subscription $SubscriptionId"
        Select-AzureRmSubscription -SubscriptionId $SubscriptionId | Out-Null
    }
}

###########################################################################
#
# Confirm-Create - confirms that the user wants to continue with the 
# creation of the virtual machine.
# 
Function Confirm-Create() {
    Write-Host @"
    
You are about to create a virtual machine in Azure:
    - Subscription $SubscriptionId ($($subName.Name))
    - Resource group $ResourceGroupName
    - Location '$Location'

Are you sure you want to continue?
"@

    while ($True) {
        $answer = Read-Host @"
    [Y] Yes [N] No (default is "Y")
"@

        switch ($Answer) {
            "Y" { return }
            "" { return }
            "N" { exit }
        }
    }
}

###########################################################################
#
# Get-ResourceGroup - Finds or creates the resource group to be used by the
# deployment.
# 
Function Get-ResourceGroup() {
    $rg = Get-AzureRmResourceGroup $ResourceGroupName -ErrorAction Ignore
    if (!$rg) {
        $rg = New-AzureRmResourceGroup $ResourceGroupName -Location $Location
    }
    return $rg
}

###########################################################################
#
# Invoke-VmDeployment - Uses the .\IoTEdgeMlDemoVMTemplate.json template to 
# create a virtual machine.  Returns the name of the virtual machine.
# 
Function Invoke-VmDeployment($resourceGroup) {
    # Create a unique deployment name
    $randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_})
    $deploymentName = "IotEdgeMlDemoVm-$randomSuffix"
    $params = @{
        "location"      = $Location
        "adminUsername" = $AdminUsername
        "adminPassword" = $AdminPassword
    }

    Write-Host @"
`nStarting deployment of the demo VM which may take a while.
Progress can be monitored from the Azure Portal (http://portal.azure.com).
    1. Find the resource group $ResourceGroupName in $SubscriptionId ($($subName.Name)) subscription.
    2. In the Deployments page open deployment $deploymentName.
"@

    $deployment = New-AzureRmResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroup.ResourceGroupName -TemplateFile '.\IoTEdgeMLDemoVMTemplate.json' -TemplateParameterObject $params
    return $deployment.Outputs.vmName.value
}

###########################################################################
#
# Enable-HyperV -- Uses the vmname to enable Hyper-V on the VM.
# 
Function Install-Software($vmName) {
    $vmInfo = Get-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName -Status
    foreach ($state in $vmInfo.Statuses) {
        if (!$state.Code.StartsWith("PowerState")) {
            continue
        }

        if ($state.Code.Contains("running")) {
            Write-Host "VM $vmName is running"
            break
        }
        else {
            Write-Host "Starting VM $vmName"
            Start-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName
        }
    }

    Write-Host "`nEnabling Hyper-V in Windows on Azure VM..."
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Enable-HyperV.ps1' 2>&1>$null
    Write-Host "`nInstalling Chocolatey on Azure VM..."
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Install-Chocolatey.ps1' 2>&1>$null
    Write-Host "`nInstalling necessary software on Azure VM..."
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Install-DevMachineSoftware.ps1' -Parameter @{ "AdminUserName" = $AdminUsername; } 2>&1>$null
  
    Write-Host "`nRestarting the VM..."
    Restart-AzureRmVM -Id "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Compute/virtualMachines/$vmName" 2>&1>$null
}

###########################################################################
#
# Export-RdpFile -- Uses the vmname to find the virutal machine's FQDN then 
# writes an RDP file to rdpFilePath.
# 
Function Export-RdpFile($vmName, $rdpFilePath) {
    
    Write-Host "`nWriting the VM RDP file to $rdpFilePath"
    $vmFQDN = (Get-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName | Get-AzureRmPublicIpAddress).DnsSettings.FQDN 3> $null

    $rdpContent = @"
full address:s:$($vmFQDN):3389
prompt for credentials:i:1
username:s:$vmName\$AdminUsername
"@
    
    Set-Content -Path $rdpFilePath -Value $rdpContent
}

###########################################################################
#
# Main 
# 

Install-AzurePowerShell

Connect-AzureSubscription
$subName = Get-AzureRmSubscription -SubscriptionId $SubscriptionId

Confirm-Create

$resourceGroup = Get-ResourceGroup

$vmName = Invoke-VmDeployment $resourceGroup

Install-Software $vmName
$desktop = [Environment]::GetFolderPath("Desktop")
$rdpFilePath = [IO.Path]::Combine($desktop, "$vmName.rdp")
Export-RdpFile $vmName $rdpFilePath

Write-Host @"

The VM is ready.
Visit the Azure Portal (http://portal.azure.com).
    - Virtual machine name: $vmName
    - Resource group: $ResourceGroupName
    - Subscription: $SubscriptionId ($($subName.Name))

Use the RDP file: $rdpFilePath to connect to the virtual machine.

"@
Write-Warning "Please note this VM was configured with a shutdown schedule. Review it on the VM blade to confirm the settings work for you."