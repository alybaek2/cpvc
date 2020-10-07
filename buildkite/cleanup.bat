set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_BUILD_ID%
echo Deleting %BUILDTMP%...

rmdir /s /q %BUILDTMP%
