# Credit to https://dzone.com/articles/using-powershell-publish-nuget

$packageName = "ElasticLogger"
$projectPath = "ElasticLogger\ElasticLogger.csproj"

& "${Env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe" $projectPath /p:Configuration=Release

$latestRelease = nuget list $packageName
$version = $latestRelease.split(" ")[1];
$versionTokens = $version.split(".")

Write-Host $latestRelease

if ($versionTokens.Length -gt 1) 
{
    $buildNumber = [System.Double]::Parse($versionTokens[$versionTokens.Count -1]) 
    $versionTokens[$versionTokens.Count -1] = $buildNumber +1
    $newVersion = [string]::join('.', $versionTokens)
}
else
{
    Write-Host "Package not found!"
    $newVersion = "1.0.0"
}

Write-Host $newVersion

get-childitem | where {$_.extension -eq ".nupkg"} | foreach ($_) {remove-item $_.fullname}
nuget pack $projectPath -Version $newVersion -Prop Configuration=Release -IncludeReferencedProjects
$package = get-childitem | where {$_.extension -eq ".nupkg"}