# Execute this script after loading the azure subscription credentials


# Enabling the Antimalware restarts the service the first time and requires a deployment in the slot used
# This script enables the extension in production (default)
$virusScanningCloudServiceName = "VirusScanner"
[xml] $config = Get-Content "Antimalware_config.xml"
Set-AzureServiceAntimalwareExtension $virusScanningCloudServiceName -AntimalwareConfiguration $config

"Updated antimalware config. Doing a get to verify the configuration exists"
Get-AzureServiceAntimalwareConfig $virusScanningCloudServiceName