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
# Connect-AzureSubscription - gets current Azure context or triggers a 
# user log in to Azure. Selects the Azure subscription for creation of 
# the virutal machine
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
# creation of the ivirtual machine.
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
    # Get or create resource group
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
    # Submit the ARM template deployment
    $randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_})
    $deploymentName = "IotEdgeVm-$randomSuffix"
    $params = @{
        "location"      = $Location
        "adminUsername" = $AdminUsername
        "adminPassword" = $AdminPassword
    }

    Write-Host @"
`nStarting deployment of the demo VM which may take a while.
Progress can be monitored from the Azure Portal (http://portal.azure.com).
    1. Find the resource group $ResourceGroupName in $SubscriptionId subscription.
    2. In the Deployments page open deployment $deploymentName.
"@

    $deployment = New-AzureRmResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroup.ResourceGroupName -TemplateFile '.\IoTEdgeVMTemplate.json' -TemplateParameterObject $params
    return $deployment.Outputs.vmName.value
}

###########################################################################
#
# Install-Software -- Installs apt-get packages on the target VM
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
    
    Write-Host "`nInstalling apt-get packages..."
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunShellScript" -ScriptPath '.\installpackages.sh'
}

###########################################################################
#
# Get-SshCommand -- Uses the vmname to find the virutal machine's FQDN then 
# returns the ssh command to connect to the VM console.
# 
Function Get-SshCommand($user, $vmName) {
    
    $vmFQDN = (Get-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName | Get-AzureRmPublicIpAddress).DnsSettings.FQDN 3> $null

    return "ssh -l $user $vmFQDN"
}

###########################################################################
#
# Main 
# 

Connect-AzureSubscription
$subName = Get-AzureRmSubscription -SubscriptionId $SubscriptionId

Confirm-Create

$resourceGroup = Get-ResourceGroup

$vmName = Invoke-VmDeployment $resourceGroup

Install-Software $vmName

$ssh = Get-SshCommand $AdminUsername $vmName

Write-Host @"

The VM is ready.
Visit the Azure Portal (http://portal.azure.com).
    - Virtual machine name: $vmName
    - Resource group: $ResourceGroupName
    - Subscription: $SubscriptionId ($($subName.Name))
    - Connect with: $ssh

"@
Write-Warning "Please note this VM was configured with a shutdown schedule. Review it on the VM blade to confirm the settings work for you."