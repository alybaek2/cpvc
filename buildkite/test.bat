call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Run unit tests.
"C:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" "cpvc-test\bin\x64\Debug\cpvc-test.dll"
x64\Release\cpvc-core-test.exe
