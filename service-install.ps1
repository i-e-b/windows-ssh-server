# Check whether to install or uninstall service.
$installutilArgs = ""

if ($args[0] -eq "/u")
{
    $installutilArgs += "/uninstall"
}

# Install server executable as Windows Service.
installutil $installutilArgs ".\Server\bin\Release\Windows SSH Server.exe"
