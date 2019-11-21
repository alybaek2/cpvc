call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Generate code coverage files for cpvc-core (C++) and cpvc (C#).
"C:\Program Files\OpenCppCoverage\OpenCppCoverage.exe" --modules cpvc-core-test --sources=cpvc-core --excluded_sources=cpvc-core-test --export_type cobertura:cpvc-core-coverage.xml "x64\Debug\cpvc-core-test.exe"
"C:\Tools\OpenCover\OpenCover.Console.exe" -target:"c:\Program Files (x86)\NUnit.org\nunit-console\nunit3-console.exe" -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -excludebyfile:"d:\agent\*";"c:\program files*";"*App.g.cs";"*.xaml.cs";"*.xaml";"*.Designer.cs" -hideskipped:All -register:user -output:cpvc-coverage.xml

REM Generate ReportGenerator reports from the original Cobertura files.
"C:\Tools\NuGet\nuget.exe" restore
".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:coverage-report-xml -reporttypes:Xml -sourcedirs:. -reports:cpvc-coverage.xml;cpvc-core-coverage.xml

REM Get rid of this file as it seems to cause duplicates in Coveralls.
del coverage-report-xml\Summary.xml

REM Before uploading to Coveralls, correct the casing of all the filenames. The XML reports all have lowercased filenames, due to
REM the way they're stored in the PDB files. This results in Coveralls not being able to show the source code. Note that this only
REM seems to happen with C++ code, not C# code.
REM CorrectCase will also strip the folder prefix from all filenames in these files. CoverAlls shows duplicates when these prefixes
REM differ between builds.
dir /b /s /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h > tokens.txt
dir /b /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h >> tokens.txt
dir /b /s /a:-D coverage-report-xml\*.xml > files.txt
"C:\Tools\CorrectCase\CorrectCase.exe" files.txt tokens.txt "%cd%"

REM Get the commit message.
git show -s --format=%%B %BUILDKITE_COMMIT% > commitmsg.txt
set /P COMMIT_MESSAGE=< commitmsg.txt
REM Escape the double quotes.
set COMMIT_MESSAGE=%COMMIT_MESSAGE:"=\"%

REM Upload to Coveralls!
"C:\Tools\Coveralls.NET\csmacnz.Coveralls.exe" -i coverage-report-xml --reportgenerator --useRelativePaths --commitMessage "%COMMIT_MESSAGE%" --commitAuthor=%BUILDKITE_BUILD_CREATOR% --commitId %BUILDKITE_COMMIT% --commitBranch %BUILDKITE_BRANCH% --commitEmail %BUILDKITE_BUILD_CREATOR_EMAIL% --jobId=%BUILDKITE_JOB_ID%

REM Upload to Codecov
"C:\Tools\Codecov\codecov-windows-x64.exe" --branch %BUILDKITE_BRANCH% --build %BUILDKITE_BUILD_NUMBER% --sha %BUILDKITE_COMMIT% --file "cpvc-core-coverage.xml cpvc-coverage.xml"
