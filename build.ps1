# Find the first matching .csproj file in the current directory
$CsprojPath = Get-ChildItem -Filter *.csproj | Select-Object -First 1

# Check if a .csproj file is found
if ($CsprojPath -eq $null) {
    Write-Host "Error: No .csproj file found in the current directory."
    Exit
}

# Get the project name and version from the csproj file
$CsprojXml = [xml](Get-Content $CsprojPath.FullName)

# Try to extract PackageId and PackageVersion
$ProjectName = $CsprojXml.SelectSingleNode('//PropertyGroup/AssemblyName').InnerText
$ProjectVersion = $CsprojXml.SelectSingleNode('//PropertyGroup/Version').InnerText

# Define the paths
$OutputPath = "dist"
$ReleasesPath = "releases"

# Create the "releases" folder if it doesn't exist
if (-not (Test-Path $ReleasesPath -PathType Container)) {
    New-Item -ItemType Directory -Path $ReleasesPath -Force
}

# Define the dependency versions and build configurations as arrays
$Versions = @("5", "6")
$BuildConfigurations = @("Release", "Debug")

# Function to build and package for a specific version and configuration
function BuildAndPackage($version, $configuration) {
    $outputName = $ProjectName + "_v" + $ProjectVersion + "-BepInEx" + $version + "_"
    if ($configuration -ne "Release"){
         $outputName += $configuration
    }

    $outputDir = $OutputPath + "\" + $outputName

    Write-Host "Building for [$ProjectName-$ProjectVersion] with args [$version, $configuration], output: $outputDir"
    dotnet build $CsprojPath.FullName /p:BepInExVersion=$version -c $configuration -o "$outputDir"
    
    # Define the contents for the ZIP files
    $filesToInclude = @("$outputDir\0Harmony.dll", "$outputDir\$ProjectName.dll", "$outputDir\Icons")

    # Create a zip file
    $zipFileName = $outputName + ".zip"
    Compress-Archive -Path $filesToInclude -DestinationPath "$ReleasesPath\$zipFileName" -Force
}

# Iterate over versions and configurations
foreach ($version in $Versions) {
    foreach ($configuration in $BuildConfigurations) {
        BuildAndPackage $version $configuration
    }
}

# Delete the entire "output" folder
Remove-Item -Path $OutputPath -Recurse -Force