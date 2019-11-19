call ..\buildkite\restore.bat cpvc-test\bin
call ..\buildkite\restore.bat x64

REM Run unit tests.
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" /platform:x64 cpvc-test\bin\x64\Release\cpvc-test.dll
x64\Release\cpvc-core-test.exe
