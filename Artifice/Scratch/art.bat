mklink /J s:\settings t:\programdata\ssh

xbdiagcap start -p FileCopyPlugin
xbdiagcap stop -d d:\developmentfiles\artssh

devtoolslauncher launchforprofiling telnetd "cmd.exe 24"

rmdir s:\settings