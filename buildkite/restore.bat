set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_BUILD_ID%
echo Restoring %BUILDTMP% to %1...

mkdir .\%1
xcopy %BUILDTMP%\%1 .\%1 /y /s /h /i
