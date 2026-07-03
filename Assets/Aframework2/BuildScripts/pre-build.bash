#!/bin/bash

# Assign the filename
filename="$WORKSPACE/ProjectSettings/ProjectSettings.asset"
search="m_ShowUnitySplashScreen: 1"
replace="m_ShowUnitySplashScreen: 0"

sed -i "s/$search/$replace/" $filename

exit 1