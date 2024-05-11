#!/bin/bash

csprojPath=$1
buildNumber=$2
buildOffset=$3

manifestPath=$4
droidAdmobAppId=$5
droidGoogleMapsKey=$6

plistPath=$7
iosAdmobAppId=$8

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
newVersionName="$(date -u +'%Y.%-m.%-d')"

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
