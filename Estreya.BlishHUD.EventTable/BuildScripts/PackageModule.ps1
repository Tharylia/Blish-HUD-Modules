param ([string]$project, [string]$output, [string]$name)

$noModuleName = $name -replace " Module", "" -replace " module", ""

$docs = [Environment]::GetFolderPath("MyDocuments")

Write-Output "PackageModule will be using the following paths:"
Write-Output "$($project)obj\$($noModuleName).zip"

Write-Output "Building $($noModuleName).bhm..."

Remove-Item -Path "$($project)obj\$($noModuleName).zip" -Force
Compress-Archive -Path "$($project)$($output)*" -DestinationPath "$($project)obj\$($noModuleName).zip" -Update
Copy-Item "$($project)obj\$($noModuleName).zip" "$($project)obj\$($noModuleName).bhm" -Force

Write-Output "$($modulesDest)$($noModuleName).bhm was built!"