# VTOL VR Physical Input
A mod for [VTOL VR](https://store.steampowered.com/app/667970/VTOL_VR/) to allow using physical input devices (ie use a real joystick instead of the virtual reality joystick)  
Uses [MarshMello0/VTOLVR-ModLoader](https://github.com/MarshMello0/VTOLVR-ModLoader)  

Discussion: Contact me in the [VTOL VR Discord channel](https://discord.gg/WPvdZzG)

# Usage
1. Install the Mod Manager
1. Downoad a release zip from the [Releases Page](https://github.com/evilC/VTOLVRPhysicalInput/releases)  
**DO NOT use the green "Clone or Download" button on the main page!**
1. Unzip the contents to your `VTOL VR\VTOLVR_ModLoader\Mods` folder
1. Right-click `VTOL VR\VTOLVR_ModLoader\mods\VTOLVRPhysicalInput\Unblocker.ps1` and Run As Admin to unblock the DLLs
1. Edit the `VTOLVR\VTOLVR_ModLoader\mods\VTOLVRPhysicalInput\VTOLVRPhysicalInputSettings.xml` config file as appropriate  
Your joystick name should exactly match as it appears in the Joy.cpl window (Click the Windows Start  button, type `Joy.cpl` and hit Enter)  
There are currently no instructions on how to do so, as nobody else has expressed an interest in using it yet and it is an early POC, so I do not see the point in spending hours documenting what will likely change or never be used by anyone else - feel free to contact me in the Discord channel linked above  
The supplied example file is configured for a Thrustmaster T.16000M / TWCS combo
1. Launch the game using the Mod Manager
1. Enable the mod

# Limitations
If there is one mapping to the virtual stick or the virtual throttle, then all input for that device must come from a physical device - the virtual device will no longer function at all
