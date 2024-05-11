#!/bin/bash

symDir='./symbols'

# exit if a command fails
set -e

rm -rf $symDir
mkdir $symDir

# Search for iOS symbols
find . -type d -name '*.dSYM' -exec cp -r '{}' $symDir ';'
if [ "$(ls -A $symDir)" ]; then
  echo " (i) Creating symbols zip"
  cd $symDir ; zip -r symbols.zip * ; cd ..
  # Found them, time to get outta here!
  exit 0
fi

# Search for Android symbols
droidMappingFile='mapping.txt'
droidAppSharedLibs='app_shared_libraries'
droidSymFolders=(
  'android-arm/armeabi-v7a'
  'android-arm64/arm64-v8a'
  'android-x86/x86'
  'android-x64/x86_64'
)

mappingPath=$(find . -type f -name $droidMappingFile -print -quit)
if [ -f "$mappingPath" ] ; then
  echo " (i) Found: $mappingPath"
  cp "$mappingPath" $symDir
fi

appSharedLibsPath=$(find . -type d -name $droidAppSharedLibs -print -quit)
if [ -d "$appSharedLibsPath" ] ; then
  echo " (i) Found: $appSharedLibsPath"
  cp -r "$appSharedLibsPath" $symDir
fi

# Search for Android ABIs
for folderPair in "${droidSymFolders[@]}" ; do
  searchFolder="${folderPair%%/*}"
  symFolder="${folderPair##*/}"
  path=$(find . -type d -name $searchFolder -print -quit)
  if [ -d "$path" ] ; then
    echo " (i) Found: $path"
    symPath=$symDir/aot/$symFolder
    mkdir -p $symPath
    find "$path" -type f -name '*.so' -exec cp '{}' $symPath ';'
  fi
done

if [ "$(ls -A $symDir)" ]; then
  echo " (i) Creating symbols zip"
  cd $symDir ; zip -r symbols.zip * ; cd ..
else
  echo " (!) No symbols found!"
  exit 1
fi
