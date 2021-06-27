param([switch]$Elevated)
function Check-Admin {
$currentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
$currentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
}
if ((Check-Admin) -eq $false) {
if ($elevated)
{
# could not elevate, quit
}
else {
Start-Process powershell.exe -Verb RunAs -ArgumentList ('-noprofile -noexit -file "{0}" -elevated' -f ($myinvocation.MyCommand.Definition))
}
exit
}


$drive = get-psdrive
foreach ($a in $drive) 
{
 
 $Path = ":\Steam\steamapps\common\VTOL VR\VTOLVR_ModLoader\mods\VTOLVRPhysicalInput"
 $WantPath = (""+ $a+$Path)
 $PathFound = Test-Path $WantPath
 Write-Host ("Searching " + $WantPath)
 
 if ($PathFound -eq $True )
 {
  Write-Host ("Found mod at " + $WantPath)
  cd $WantPath
  Get-ChildItem -Path '$WantPath' -Recurse  | Unblock-File
  Write-Host "Operation Successful!"
  break
 
 }
 else
 {
	Write-Host ("Not found in " + $a + " drive")
 }

}