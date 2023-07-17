#!/bin/bash

rm -r "${BUILD_REPOSITORY_LOCALPATH}/src/Api"
rm -r "${BUILD_REPOSITORY_LOCALPATH}/src/App/AdelaideFuel.Android"

echo ' (i) Installed Xcode Bundles'
ls /Applications | grep Xcode
