call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Run C# unit tests, and quit immediately if there's an error.
%BUILD_NUNIT% "cpvc-test\bin\x64\Release\cpvc-test.dll"
if %errorlevel% neq 0 exit /b %errorlevel%

REM Run C++ unit tests.
x64\Release\cpvc-core-test.exe
