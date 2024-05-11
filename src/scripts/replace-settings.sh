#!/bin/bash

settingsPath=$1
settingsJson=$2

# exit if a command fails
set -e

if [ ! -f $settingsPath ] ; then
  echo " [!] File doesn't exist at specified path: ${settingsPath}"
  exit 1
else
  rm $settingsPath
fi

(echo $settingsJson) >> $settingsPath

cat $settingsPath

set +v