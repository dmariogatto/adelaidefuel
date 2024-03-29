﻿#!/bin/bash

#export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

#dotnet tool install --global boots
#boots --stable Mono
#boots --stable Xamarin.iOS

scriptPath="${BUILD_REPOSITORY_LOCALPATH}/src/App/AdelaideFuel.iOS/appcenter-update-bundle-version.sh"
appPlistPath="${BUILD_REPOSITORY_LOCALPATH}/src/App/AdelaideFuel.iOS/Info.plist"

chmod u+x $scriptPath

$scriptPath "${appPlistPath}" $APPCENTER_BUILD_ID $BUILD_ID_OFFSET "$VERSION_NAME"

sed -i.bak "s#{AdMobApplicationId}#$ADMOB_APPLICATION_ID#g" $appPlistPath

scriptPath="${BUILD_REPOSITORY_LOCALPATH}/src/App/appcenter-replace-settings.sh"
settingsPath="${BUILD_REPOSITORY_LOCALPATH}/src/App/AdelaideFuel/settings.json"

chmod u+x $scriptPath

$scriptPath $settingsPath

sed -i.bak "s#{AppCenterIosSecret}#$APPCENTER_IOS_SECRET#g" $settingsPath
sed -i.bak "s#{AdMobPricesIosUnitId}#$ADMOB_PRICES_UNIT_ID#g" $settingsPath
sed -i.bak "s#{AdMobMapIosUnitId}#$ADMOB_MAP_UNIT_ID#g" $settingsPath

cat $appPlistPath
cat $settingsPath
