@IF [%1] == [] GOTO NOPARAM

@ECHO -- Building solution in release mode
dotnet build -c Release -p:SignCertificateName="Open Source Developer%%2c Artiom Chilaru"
@IF ERRORLEVEL 1 goto ERROR

@ECHO -- Signing the nuget package
nuget sign src\FlexLabs.EntityFrameworkCore.Upsert\bin\Release\FlexLabs.EntityFrameworkCore.Upsert.%1.nupkg -CertificateSubjectName "Open Source Developer, Artiom Chilaru" -Timestamper http://timestamp.digicert.com
@IF ERRORLEVEL 1 goto ERROR

@EXIT /B %errorlevel%

:NOPARAM
@ECHO -- Version number not passed as an argument --
@EXIT /B 1

:ERROR
@ECHO -- Build FAILED --
@EXIT /B 1
