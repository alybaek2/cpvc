call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Run unit tests.
%BUILD_NUNIT% "cpvc-test\bin\x64\Debug\cpvc-test.dll"
x64\Release\cpvc-core-test.exe
