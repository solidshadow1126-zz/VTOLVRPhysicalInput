# VTOLVR Physical Input
Take to the skies with your physical HOTAS hardware for increased immersion and tighter control
Originally created by GitHub user evilC: https://github.com/evilC/VTOLVRPhysicalInput

Disclaimer: This mod is NOT plug and play and requires basic knowledge of editing XML. Please find the readme pdf file in the mod download and follow the instructions.

Allows usage of HOTAS peripherals (joysticks and throttles) to control aircraft instead of the default virtual joystick and throttle. Featuring fully remappable button and axis inputs for HOTAS functions like weapon select and sensor controls. Customization tutorial file included in download!

## Installation

1. Download the mod and place it in the "VTOLVR_Modloader/mods" folder, rename "mods" to "Mods" with a capital M.
2. Run the mod once.
3. Rename the mod from VTOLVR_Physical_Input to VTOLVRPhysicalInput (Remove Underscores).
4. Move SharpDX.dll & SharpDX.DirectInput.dll from the Dependencies folder (VTOL VR\VTOLVR_ModLoader\Mods\VTOLVRPhysicalInput\Dependencies) to (VTOL VR\VTOLVR_Data\Managed).

6. The Hard Part: Edit the VTOLVRPhysicalInputSettings.xml to your HOTAS. (You can use the PDF included in the mod for this part if my explanation sucks)
	- a. Hit the search button/bar and type joy.cpl if on windows.
	- b. Rename the spot in between <stickName></stickName> to the EXACT name of the HOTAS in joy.cpl.
	- c. Repeat above for the Throttle HOTAS if you have one at the bottom of the file.
	- d. Replace Button Numbers in the <InputButton></InputButton> with the Button number on the HOTAS, shown in joy.cpl. (Use properties in joy.cpl) 
	
Notes: 
- The POV Hat setting is a bit confusing, I got it to work on my HOTAS by setting it to button 1.
- Disabled ButtonToVectorComponent on the throttle because I didn't know what to use it for.

	
