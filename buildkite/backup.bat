set BUILDTMP=%NASTMP%\BuildKite\%BUILDKITE_JOB_ID%

xcopy .\%1 %BUILDTMP%\%1 /y /s /h /i
