# Execute this script after loading the azure subscription credentials

$accountName = "filehb"
$location = "West Europe"
$handbackContainer = "handback"
$processedContainer = "processedblobs"

New-AzureStorageAccount -StorageAccountName $accountName -Location $location
$storageKey = Get-AzureStorageKey -StorageAccountName $accountName | %{$_.Primary} # Get the primary key from the Primary property only
$storContext = New-AzureStorageContext -StorageAccountName $accountName -StorageAccountKey $storageKey -Protocol Https
New-AzureStorageContainer -Name $handbackContainer -Permission Off -Context $storContext
New-AzureStorageContainer -Name $processedContainer -Permission Off -Context $storContext