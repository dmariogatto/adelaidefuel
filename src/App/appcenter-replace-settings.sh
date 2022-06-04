#!/bin/bash

settingsPath=$1

# exit if a command fails
set -e

if [ ! -f $settingsPath ] ; then
  echo " [!] File doesn't exist at specified path: ${settingsPath}"
  exit 1
fi

sed -i.bak "s#{ApiUrlBase}#$API_BASE_URL#g" $settingsPath
sed -i.bak "s#{ApiUrlIapBase}#$API_BASE_IAP_URL#g" $settingsPath

sed -i.bak "s#{SubscriptionProductId}#$SUBSCRIPTION_PRODUCT_ID#g" $settingsPath

sed -i.bak "s#{ApiKeyBrands}#$API_KEY_BRANDS#g" $settingsPath
sed -i.bak "s#{ApiKeyFuels}#$API_KEY_FUELS#g" $settingsPath
sed -i.bak "s#{ApiKeySites}#$API_KEY_SITES#g" $settingsPath
sed -i.bak "s#{ApiKeySitePrices}#$API_KEY_SITE_PRICES#g" $settingsPath
sed -i.bak "s#{ApiKeyBrandImg}#$API_KEY_BRAND_IMG#g" $settingsPath

sed -i.bak "s#{AdMobPublisherId}#$ADMOB_PUB_ID#g" $settingsPath

set +v