# path to exe
cd c:\Webapps\third-party-notifications\ThirdPartyNotifications\NotificationAPI\
$appPath = "c:\Webapps\third-party-notifications\ThirdPartyNotifications\NotificationAPI\bin\Debug\NotificationAPI.exe"

# section to encrypt
$sectionName = "appSettings" 

# the provider for our type of configuration algorithm
$prov = "DataProtectionConfigurationProvider"

#The System.Configuration assembly must be loaded
$configurationAssembly = "System.Configuration, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b03f5f7f11d50a3a"
[void] [Reflection.Assembly]::Load($configurationAssembly)

# get ConfugrationManager
$configuration = [System.Configuration.ConfigurationManager]::OpenExeConfiguration($appPath)
$section = $configuration.GetSection($sectionName)

if (-not $section.SectionInformation.IsProtected)
{
    $section.SectionInformation.ProtectSection($prov)
    $section.SectionInformation.ForceSave = [System.Boolean]::True
    $configuration.Save([System.Configuration.ConfigurationSaveMode]::Modified)
}

# decrypting...