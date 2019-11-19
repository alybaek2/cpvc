set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_JOB_ID%
echo Restoring %1 to %BUILDTMP%...

xcopy %BUILDTMP%\%1 .\%1 /y /s /h /i
