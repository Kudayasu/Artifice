del d:\success.txt
del d:\sshReady.txt
del d:\ssh2.txt
del d:\SSH2Success.txt
del d:\developmentfiles\acl.bat
del d:\developmentfiles\acl2.bat
del d:\developmentfiles\icacls.exe
rmdir d:\temp /s /q
rmdir d:\developmentfiles\en-us /s /q
rmdir d:\developmentfiles\ssh /s /q
rmdir d:\developmentfiles\ssh2 /s /q

start /b "" cmd /c del "%~f0"&exit /b