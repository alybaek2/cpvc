call .\buildkite\restore.bat cpvc-test\bin
call .\buildkite\restore.bat x64

REM Generate code coverage files for cpvc-core (C++) and cpvc (C#).
%BUILD_OPENCPPCOVERAGE% --modules cpvc-core-test --sources=cpvc-core --excluded_sources=cpvc-core-test --export_type cobertura:cpvc-core-coverage.xml "x64\Debug\cpvc-core-test.exe"
%BUILD_OPENCOVER% -target:%BUILD_NUNIT% -targetargs:"cpvc-test\bin\x64\Debug\cpvc-test.dll" -filter:"+[cpvc]* +[cpvc-core-clr]* -[cpvc-test]*" -excludebyfile:"d:\agent\*";"c:\program files*";"*App.g.cs";"*.xaml.cs";"*.xaml";"*.Designer.cs" -hideskipped:All -register:user -output:cpvc-coverage.xml

REM Generate ReportGenerator reports from the original Cobertura files.
%BUILD_NUGET% restore
".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:coverage-report-xml -reporttypes:Xml -sourcedirs:. -reports:cpvc-coverage.xml;cpvc-core-coverage.xml
".\packages\ReportGenerator.4.3.6\tools\net47\ReportGenerator.exe" -targetdir:. -reporttypes:Cobertura -sourcedirs:. -reports:cpvc-coverage.xml;cpvc-core-coverage.xml

REM Get rid of this file as it seems to cause duplicates in Coveralls.
del coverage-report-xml\Summary.xml

REM Before uploading to Coveralls, correct the casing of all the filenames. The XML reports all have lowercased filenames, due to
REM the way they're stored in the PDB files. This results in Coveralls not being able to show the source code. Note that this only
REM seems to happen with C++ code, not C# code.
REM CorrectCase will also strip the folder prefix from all filenames in these files. Coveralls shows duplicates when these prefixes
REM differ between builds.
dir /b /s /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h > tokens.txt
dir /b /a:-D .\cpvc-core\*.cpp .\cpvc-core\*.h >> tokens.txt
dir /b /s /a:-D coverage-report-xml\*.xml > files.txt
%BUILD_CORRECTCASE% files.txt tokens.txt "%cd%"

REM Get the commit message.
git show -s --format=%%B %BUILDKITE_COMMIT% > commitmsg.txt
set /P COMMIT_MESSAGE=< commitmsg.txt
REM Escape the double quotes.
set COMMIT_MESSAGE=%COMMIT_MESSAGE:"=\"%

REM Upload to Coveralls!
%BUILD_COVERALLS% -i coverage-report-xml --reportgenerator --useRelativePaths --serviceName buildkite --serviceNumber %BUILDKITE_BUILD_NUMBER% --commitMessage "%COMMIT_MESSAGE%" --commitAuthor=%BUILDKITE_BUILD_CREATOR% --commitId %BUILDKITE_COMMIT% --commitBranch %BUILDKITE_BRANCH% --commitEmail %BUILDKITE_BUILD_CREATOR_EMAIL% --jobId=%BUILDKITE_JOB_ID%

REM Upload to Codecov... only do the OpenCover output; Codecov doesn't seem to be able to handle the OpenCppCoverage output, despite the fact it's supposed to be cobertura format.
%BUILD_CODECOV% --branch %BUILDKITE_BRANCH% --build %BUILDKITE_BUILD_NUMBER% --sha %BUILDKITE_COMMIT% --file "Cobertura.xml"
