J:\tools\kill.exe xbdiagservice.exe
J:\tools\kill.exe XLens.Agent.exe
J:\tools\kill.exe xray.exe
sc stop etwuploaderservice
sc stop DiagTrack
sc stop XBBlackbox
sc stop xbdiagservice
sc stop wersvc
sc delete etwuploaderservice
sc delete DiagTrack
sc delete XBBlackbox
sc delete xbdiagservice
sc delete wersvc
REG ADD "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f
rmdir /s /q \\.\WER:\\