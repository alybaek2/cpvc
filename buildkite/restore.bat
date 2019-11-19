set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_JOB_ID%

xcopy %BUILDTMP%\%1 .\%1 /y /s /h /i
