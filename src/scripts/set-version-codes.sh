#!/bin/bash

csprojPath=$1
buildNumber=$2
buildOffset=$3
versionName=$4

manifestPath=$5
droidAdmobAppId=$6
droidGoogleMapsKey=$7

plistPath=$8
iosAdmobAppId=$9

appExtPlistPath=$10

# exit if a command fails
set -e

if [ ! -f $csprojPath ] ; then
  echo " [!] File doesn't exist at specified path: ${csprojPath}"
  exit 1
fi

echo " (i) Provided project path: ${csprojPath}"
echo " (i) Provided build number: ${buildNumber}"
echo " (i) Provided build offset: ${buildOffset}"

newVersionCode=$((buildNumber + buildOffset))
newVersionName="${versionName:-$(date -u +'%Y.%-m.%-d')}"

echo " (i) New version code: ${newVersionCode}"
echo " (i) New version name: ${newVersionName}"

sed -r -i".bak" "s#<ApplicationVersion>.*</ApplicationVersion>#<ApplicationVersion>$newVersionCode</ApplicationVersion>#g" $csprojPath
sed -r -i".bak" "s#<ApplicationDisplayVersion>.*</ApplicationDisplayVersion>#<ApplicationDisplayVersion>$newVersionName</ApplicationDisplayVersion>#g" $csprojPath

echo " (i) Android Manifest '$manifestPath'"
echo " (i) iOS plist '$plistPath'"

# Android Settings

if ([ ! -z $manifestPath ] && [ -f $manifestPath ]) ; then
  echo " (i) Updating Android Manifest '$manifestPath'"

  sed -i".bak" "s#{GoogleMapsApiKey}#$droidGoogleMapsKey#g" $manifestPath
  sed -i".bak" "s#{AdMobApplicationId}#$droidAdmobAppId#g" $manifestPath

  echo " (i) Updated Android Manifest '$manifestPath'"
fi

## iOS Settings

if ([ ! -z $plistPath ] && [ -f $plistPath ]) ; then
  echo " (i) Updating iOS plist '$plistPath'"

  sed -i".bak" "s#{AdMobApplicationId}#$iosAdmobAppId#g" $plistPath

  echo " (i) Updated iOS plist '$plistPath'"
fi

## AppExt iOS Settings

if ([ ! -z $appExtPlistPath ] && [ -f $appExtPlistPath ]) ; then
  echo " (i) Updating AppExt iOS plist '$appExtPlistPath'"

  /usr/libexec/PlistBuddy -c "Set :CFBundleVersion ${newVersionCode}" "${appExtPlistPath}"
  /usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString ${newVersionName}" "${appExtPlistPath}"

  echo " (i) Updated AppExt iOS plist '$appExtPlistPath'"
fi
