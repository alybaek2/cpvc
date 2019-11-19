set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_BUILD_ID%
echo Backing up %1 to %BUILDTMP%...

xcopy .\%1 %BUILDTMP%\%1 /y /s /h /i
