try{
  $chocoPath = $env:ChocolateyInstall
  $chocoPath = Join-Path $chocoPath 'bin'

  $thisDir = (Split-Path -parent $MyInvocation.MyCommand.Definition)
  $path = Join-Path $thisDir 'elevate.exe'

  Generate-BinFile 'el' $path

  Start-ChocolateyProcessAsAdmin '/c "el /createTask"' 'cmd.exe' -validExitCodes (0)

  write-host "elevate tool is now ready. You can type 'el' from any command line at any path to run application as elevated."


  Write-ChocolateySuccess 'elevate'
} catch {
  Write-ChocolateyFailure 'elevate' "$($_.Exception.Message)"
  throw
}
