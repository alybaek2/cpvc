call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Run C# unit tests, and quit immediately if there's an error.
%BUILD_NUGET% install NUnit.ConsoleRunner -Version 3.11.1
".\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe" "cpvc-test\bin\x64\Release\cpvc-test.dll"
