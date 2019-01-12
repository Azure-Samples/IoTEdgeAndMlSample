param
(
    [Parameter(Mandatory = $True)]
    [string] $SubscriptionId,

    [Parameter(Mandatory = $True)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $True)]
    [string] $Location,

    [Parameter(Mandatory = $True)]
    [string] $AdminUsername,

    [Parameter(Mandatory=$True)]
    [securestring] $AdminPassword,

    #these are only necessary while the github repo is private
    [Parameter(Mandatory=$True)]
    [string] $GitHubUsername,

    [Parameter(Mandatory=$True)]
    [string] $GitHubPat
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
    
You are about to create a virtual machine in Azure
    Subscription: $SubscriptionId
    Resource group: $ResourceGroupName
    Location: '$Location'

Are you sure you want to continue?
"@
    while ($True) {
        $answer = Read-Host @"
    [Y] Yes [N] No (default is "Y")
"@
        switch ($Answer) {
            "Y" { return}
            "" { return}
            "N" { exit }
        }
    }
}

###########################################################################
#
# Get-ResourceGroup - Finds or creates the resource group to be used by the
# deployment
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
# create a virtual machine.  Returns the name of the virtual machine
# 
Function Invoke-VmDeployment($resourceGroup) {
    # Submit the ARM template deployment
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
    Find the resource group $ResourceGroupName in $SubscriptionId subscription.
    In the Deployments page open deployment $deploymentName.
"@

    $deployment = New-AzureRmResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroup.ResourceGroupName -TemplateFile '.\IoTEdgeMLDemoVMTemplate.json' -TemplateParameterObject $params
    return $deployment.Outputs.vmName.value
}

###########################################################################
#
# Enable-HyperV -- Uses the vmname to enable Hyper-V on the VM
# 
Function Install-Software($vmName) {
    Write-Host "`nEnable Hyper-V on Azure VM"
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Enable-HyperV.ps1'
    Write-Host "`nInstall Chocolatey on Azure VM"
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Install-Chocolatey.ps1'
    Write-Host "`nInstall necessary software Azure VM"
    Invoke-AzureRmVMRunCommand -ResourceGroupName $ResourceGroupName -Name $vmName -CommandId "RunPowerShellScript" -ScriptPath '.\Install-DevMachineSoftware.ps1' -Parameter @{"AdminUserName"=$AdminUsername; "GitHubUserName"=$GitHubUsername; "GitHubPat"=$GitHubPat}
  
    Write-Host "Restart Virtual Machine"
    Restart-AzureRmVM -ResourceGroupName $ResourceGroupName -Name $vmName 
}

###########################################################################
#
# Export-RdpFile -- Uses the vmname to find the virutal machine's FQDN then 
# writes an RDP file to rdpFilePath 
# 
Function Export-RdpFile($vmName, $rdpFilePath) {
    
    Write-Host("`nWrite RDP file to: $rdpFilePath")
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

Connect-AzureSubscription

Confirm-Create

$resourceGroup = Get-ResourceGroup

$vmName = Invoke-VmDeployment $resourceGroup

Install-Software $vmName
$desktop = [Environment]::GetFolderPath("Desktop")
$rdpFilePath = [io.path]::combine($desktop, "$vmName.rdp")
Export-RdpFile $vmName $rdpFilePath

Write-Host @"

The VM is ready.
Visit the Azure Portal (http://portal.azure.com).
    Virtual machine name: $vmName
    Resource group: $ResourceGroupName
    Subscription: $SubscriptionId

Use the RDP file: $rdpFilePath to connect to the virtual machine.

"@
Write-Warning "Please note this VM was configured with a shutdown schedule. Review it on the VM blade to confirm the settings work for you."