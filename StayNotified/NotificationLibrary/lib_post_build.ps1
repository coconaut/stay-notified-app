# edit root dir and mode (debug vs release)
$root_dir = 'D:\stay-notified-app\StayNotified\'
$mode = 'Debug'

# build paths
$lib_dir = $root_dir + 'NotificationLibrary\'
$api_dir = $root_dir + 'NotificationAPI\'
$sources = $lib_dir +'sources'

# copy source files - needed in NotificationAPI actual source folder for build
$path = $sources
$dest = $api_dir
Copy-Item -Path $path -Destination $dest -force -recurse