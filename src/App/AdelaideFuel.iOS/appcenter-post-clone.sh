#!/bin/bash

rm -r "${BUILD_REPOSITORY_LOCALPATH}/src/Api"
rm -r "${BUILD_REPOSITORY_LOCALPATH}/src/App/AdelaideFuel.Android"

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet tool install --global boots

# Workaround instead of restarting shell
# see: https://github.com/dotnet/cli/issues/9114#issuecomment-494226139
export PATH="$PATH:~/.dotnet/tools"
export DOTNET_ROOT="$(dirname "$(readlink "$(command -v dotnet)")")"

boots --stable Xamarin.iOS