# Nicely format XML output
Function Elastic-Format-XML($xml)
{
  $Doc = New-Object System.Xml.XmlDocument 
  $doc.LoadXml($xml)
  $sw = New-Object System.IO.StringWriter 
  $writer = New-Object System.Xml.XmlTextWriter($sw) 
  $writer.Formatting = [System.Xml.Formatting]::Indented 
  $doc.WriteContentTo($writer) 
  return $sw.ToString() 
}

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

$releaseNotes = Read-Host -Prompt 'Input release notes'

[xml]$xmlpublish = Get-Content "ElasticLogger\ElasticLogger.template.nuspec" -Encoding UTF8
$xmlpublish.package.metadata.releaseNotes = $releaseNotes.ToString()
$xmlOutput = Elastic-Format-XML($xmlpublish.OuterXml.ToString())
Set-Content "ElasticLogger\ElasticLogger.nuspec" $xmlOutput -Encoding UTF8

get-childitem | where {$_.extension -eq ".nupkg"} | foreach ($_) {remove-item $_.fullname}
nuget pack $projectPath -Version $newVersion -Prop Configuration=Release -IncludeReferencedProjects
$package = get-childitem | where {$_.extension -eq ".nupkg"}

Remove-Item Function:\Elastic*