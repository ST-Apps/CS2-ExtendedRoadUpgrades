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
$OutputPath = Join-Path -Path (Get-Location) -ChildPath ".\dist"
$ReleasesPath = Join-Path -Path (Get-Location) -ChildPath ".\releases"

# Create the "releases" folder if it doesn't exist
if (-not (Test-Path $ReleasesPath -PathType Container)) {
    New-Item -ItemType Directory -Path $ReleasesPath -Force
}

# Define the dependency versions and build configurations as arrays
$Versions = @("5", "6")
$BuildConfigurations = @("Release", "Debug")

# Function to build and package for a specific version and configuration
function BuildAndPackage($version, $configuration) {
    $outputName = $ProjectName + "_v" + $ProjectVersion + "-BepInEx" + $version
    if ($configuration -ne "Release"){
         $outputName += "_" + $configuration
    }

    $outputDir = $OutputPath + "\" + $outputName + "\" + $ProjectName

    Write-Host "Building for [$ProjectName-$ProjectVersion] with args [$version, $configuration], output: $outputDir"
    dotnet build $CsprojPath.FullName /p:BepInExVersion=$version -c $configuration -o "$outputDir"
    
    # Define the contents for the ZIP files
    $filesToInclude = @("0Harmony.dll", "$ProjectName.dll", "Icons", "CHANGELOG.md", "LICENSE")
    $filesToDelete = Get-ChildItem -Path $outputDir -Exclude $filesToInclude

    # Delete files
    foreach ($file in $filesToDelete) {
        Remove-Item -Path $file.FullName -Force
    }

    # Move icons
    Get-ChildItem -Path "$outputDir\Icons" | Move-Item -Destination "$outputDir" -Force
    Remove-Item -Path "$outputDir\Icons" -Force

    # If on BepInEx5 and Release we also add Thunderstore-related assets for release
    if ($configuration -eq "Release" -and $version -eq "5"){
        # Generate Thunderstore.io manifest.json file and copy it to releases dir
        # FORMAT:
        # {
        #     "name": "TestMod",
        #     "version_number": "1.1.0",
        #     "website_url": "https://github.com/thunderstore-io",
        #     "description": "This is a description for a mod. 250 characters max",
        #     "dependencies": [
        #         "MythicManiac-TestMod-1.1.0"
        #     ]
        # }
        $ManifestTemplate = Get-Content ".\manifest.json.template" -Raw
        $ManifestVariables = @{
            "ProjectVersion" = $ProjectVersion
        }
        $ManifestJson = Replace-Placeholders -template $ManifestTemplate -variables $ManifestVariables
        $ManifestJson | Out-File "$outputDir\manifest.json"

        # Copy icon.png file and readme.md
        Copy-Item ".\icon.png" -Destination "$outputDir\icon.png"
        Copy-Item ".\README.md" -Destination "$outputDir\README.md"

        # Add icon.png and readme.md to files that must be included
        $filesToInclude += ("icon.png", "README.md")

        # Create a zip file
        $zipFileName = $outputName + "-thunderstore.zip"
        Compress-Archive -Path "$outputDir\*" -DestinationPath "$ReleasesPath\$zipFileName" -Force

        # Remove icon.png file and readme.md
        Remove-Item "$outputDir\icon.png"
        Remove-Item "$outputDir\README.md"
        Remove-Item "$outputDir\manifest.json"

        # Remove icon.png and readme.md from files that must be included
        $filesToInclude -= ("icon.png", "README.md")
    }

    # Create a zip file
    $zipFileName = $outputName + ".zip"
    # Compress-Archive -Path "$outputDir" -DestinationPath "$ReleasesPath\$zipFileName" -Force
    Push-Location "$outputDir\.."
    & "C:\Program Files\7-Zip\7z.exe" a -tzip "$ReleasesPath\$zipFileName" "$ProjectName\*" -aoa
    Pop-Location
}

# Function to replace placeholders in the template
function Replace-Placeholders($template, $variables) {
    foreach ($var in $variables.Keys) {
        $template = $template.Replace("{$var}", $variables[$var])
    }
    return $template
}

# Iterate over versions and configurations
foreach ($version in $Versions) {
    foreach ($configuration in $BuildConfigurations) {
        BuildAndPackage $version $configuration
    }
}
