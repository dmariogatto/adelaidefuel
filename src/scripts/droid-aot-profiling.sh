#!/bin/bash

# exit if a command fails
set -e

csprojDir=$1

dotnet build "$csprojDir" -c Release -f net7.0-android -t:BuildAndStartAotProfiling -p:RunAOTCompilation=false -p:IsAotProfiling=true

while true; do
  echo -n "Press 'Enter' to end AOT profiling..."
  read -s -n 1 key
  if [ $? -eq 0 ] && [ -z "$key" ] ; then
    break
  fi
  echo ""
done

dotnet build "$csprojDir" -c Release -f net7.0-android -t:FinishAotProfiling -p:RunAOTCompilation=false -p:IsAotProfiling=true
mv "$csprojDir/custom.aprof" "$csprojDir/Platforms/Android"