Run()

sub Run()
set WSshell = createobject("wscript.shell")
WSshell.run "30SiteBalance.exe /AUTO",1
end sub

