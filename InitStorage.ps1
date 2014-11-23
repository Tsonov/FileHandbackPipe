# Execute this script after loading the azure subscription credentials

$accountName = "filehb"
$location = "West Europe"
$handbackContainer = "handback"
$processedContainer = "processedblobs"
$handbackQueue = "handbackqueue"

New-AzureStorageAccount -StorageAccountName $accountName -Location $location
$storageKey = Get-AzureStorageKey -StorageAccountName $accountName | %{$_.Primary} # Get the primary key only
$storContext = New-AzureStorageContext -StorageAccountName $accountName -StorageAccountKey $storageKey -Protocol Https

# Create the containers for the blobs
New-AzureStorageContainer -Name $handbackContainer -Permission Off -Context $storContext
New-AzureStorageContainer -Name $processedContainer -Permission Off -Context $storContext

# Create the queue used for messaging to the worker roles
New-AzureStorageQueue -Name $handbackqueue -Context $storContext