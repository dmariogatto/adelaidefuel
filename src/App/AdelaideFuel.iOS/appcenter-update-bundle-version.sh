#!/bin/bash

plistPath=$1
buildNumber=$2
buildOffset=$3
buildShortVersion=$4

# exit if a command fails
set -e

if [ ! -f $plistPath ] ; then
  echo " [!] File doesn't exist at specified Info.plist path: ${plistPath}"
  exit 1
fi

buildVersion=$((buildNumber + buildOffset))

if [ -z "$buildShortVersion" ] ; then
    buildShortVersion="$(date -u +'%Y.%-m.%-d')"
fi

echo " (i) Provided Info.plist path: ${plistPath}"

bundleVerCmd="/usr/libexec/PlistBuddy -c \"Print CFBundleVersion\" \"${plistPath}\""
bundleShortVerCmd="/usr/libexec/PlistBuddy -c \"Print CFBundleShortVersionString\" \"${plistPath}\""

# verbose / debug print commands
set -v

# ---- Current Bundle Version:
eval $bundleVerCmd
# ---- Set Bundle Version:
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion ${buildVersion}" "${plistPath}"
# ---- New Bundle Version:
eval $bundleVerCmd

# ---- Current Bundle Short Version String:
eval $bundleShortVerCmd
# ---- Set Bundle Short Version String:
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString ${buildShortVersion}" "${plistPath}"
# ---- New Bundle Short Version String:
eval $bundleShortVerCmd

set +v