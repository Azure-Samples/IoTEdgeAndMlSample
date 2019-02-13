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
    [string] $Location
)

$ErrorActionPreference = "Stop"

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
# creation of the Hub and Storage account
# 
Function Confirm-Create() {
    Write-Host @"
    
You are about to create an Azure IoT Hub and Storage account:
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
# Invoke-ArmDeployment - Uses the .\HubAndStorage.json template to 
# create a storage account and IoT hub.  Returns the name of the hub and 
# storage account
# 
Function Invoke-ArmDeployment($resourceGroup) {
    # Submit the ARM template deployment
    $randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_})
    $deploymentName = "IotEdgeHubAndStorage-$randomSuffix"
    $params = @{
        "location" = $Location
    }

    Write-Host @"
`nStarting deployment of the Azure IoT Hub and Azure Storage Account which may take a while.
Progress can be monitored from the Azure Portal (http://portal.azure.com).
    1. Find the resource group $ResourceGroupName in $SubscriptionId ($($subName.Name)) subscription.
    2. In the Deployments page open deployment $deploymentName.
"@

    $deployment = New-AzureRmResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroup.ResourceGroupName -TemplateFile '.\HubAndStorage.json' -TemplateParameterObject $params

    Write-Host @"
`nThe hub and storage account are ready

    Subscription      :  $SubscriptionId ($($subName.Name))
    Resource group    :  $ResourceGroupName
    IoT Hub name      :  $($deployment.Outputs.hubName.value)
    Storage name      :  $($deployment.Outputs.storageAccountName.value)

"@
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
# Main 
# 

Connect-AzureSubscription
$subName = Get-AzureRmSubscription -SubscriptionId $SubscriptionId

Confirm-Create

$resourceGroup = Get-ResourceGroup

Invoke-ArmDeployment $resourceGroup