# ModCheck
## A mod expansion for RimWorld mods

## Why would you want to use this?
For a few reasons:

- display errors if you know your mod won't work due to loaded mods/mod order
- allow patching depending on which mods the user has loaded
- avoid patch mods to make your mod work with some other mods (like EPOE or A Dog Said)

With ModCheck you can include patch mods into your main mod. This is done the same way as patch mods, except you add the condition ModCheck.isModLoaded. As a result, distribution is easier and users will not suffer from missing adding patch mods.


## What does this do?

### Check patch conditions
Test conditions in xml patches can check for the following conditions:
- Is mod loaded
- Is mod loaded before this one
- Is mod of at least version X.X.X

Vanilla applies Success tag, meaning Invert can be used and the checks become "Is mod NOT loaded" etc.

### Writes test fails as errors in log
Adding the bool errorOnFail to the mentioned tests, the following conditions can be written as errors to the log. This will make the user get some nice red text on the screen if the mod requirements aren't present.
- required mod not loaded
- incompatible mod loaded
- mod not loaded before this one
- mod not loaded after this one
- mod version too old

## How to install
There are two ways to use ModCheck. Which option you choose is entirely up to you.

### Included in mod
Download the DLL file and put it in Assemblies in your mod.

- Pro: will always work, even if the user lack ModCheck
- Con: you need to update the DLL in your mod if ModCheck updates and the change affects your mod

### Use ModCheck mod
Load ModCheck as a mod and then make use of it in your mod.

- Pro: Users (steam) will be in charge of updating ModCheck
- Con: you mod will fail if the user fails to load ModCheck and without ModCheck you can't print a nice error message

# Usage guide
### Default tags for all checks
***
modName 

The name tag in ModMetaData inside the mod you want to test on
***
yourMod 

The name tag in ModMetaData of your mod. Used for error messages and checking mod load order
***
errorOnFail 

Bool to tell if fails to be written as errors. Default false
***
customMessageSuccess (added in v1.2)

String to write to the log on success (default: nothing)
***
customMessageFail (added in v1.2)

String to write to the log on failure. Will be red if errorOnFail is true. Will use default strings if empty and errorOnFail is set.
***
altModNames (added in v1.2)

If a mod has multiple names (like exists with both A18 and B18 in the name), then add the additional names in this list. ModCheck adds contents of modName to the list at runtime and modName is used as the name when writing to the log.
usage:

	<altModNames>
		<li>nameA</li>
		<li>nameB</li>
	</altModNames>

***
## Individual keywords for testing
### isModLoaded
Will pass if mod is in the list of loaded mods.

Tag: incompatible (default false)

Will invert result, meaning it passes if the mod is not loaded. Unlike Success=Invert, incompatible will also make the error message display if the mod is loaded and with a different text.

### loadOrder
Will pass if modName is loaded before yourMod.

Tag: yourModFirst (default false)

Will change the result to pass if your mod is loaded first.

Note: if the mod in question is not loaded, this check will return true and will not give any error. The reason is that it should work with the case where the requirements is "if you use both mods, this is the order, but you can use either as a standalone". If you want both order and require the mod to be loaded, then use both isModLoaded and loadOrder.

### isVersion
Will pass if targetVersion of modName is the same or newer than version.

Tag: version (mandatory)

This is the version string from the oldest version you will accept for your mod to work.

### isModSyncVersion
(added in v1.5)

Will pass if ModSync version of modName is the same or newer than version.

Tag: version (mandatory)

This is the version string from the oldest version you will accept for your mod to work.

# Example
Say you make a mod, which is called modX. It requires HugsLib and is incompatible with modY. If the user wants to use both modX and modZ, modX needs to be loaded first.

	<Operation Class="PatchOperationSequence">
		<success>Always</success>
		<operations>
			<li Class="ModCheck.isModLoaded">
				<modName>HugsLib</modName>
				<yourMod>ModX</yourMod>
				<errorOnFail>true</errorOnFail>
			</li>
			<li Class="ModCheck.loadOrder">
				<modName>HugsLib</modName>
				<yourMod>ModX</yourMod>
				<errorOnFail>true</errorOnFail>
			</li>
			<li Class="ModCheck.isModLoaded">
				<modName>ModY</modName>
				<yourMod>ModX</yourMod>
				<errorOnFail>true</errorOnFail>
				<incompatible>true</incompatible>
			</li>
			<li Class="ModCheck.loadOrder">
				<modName>ModZ</modName>
				<yourMod>ModX</yourMod>
				<errorOnFail>true</errorOnFail>
				<yourModFirst>true</yourModFirst>
			</li>
		</operations>
	</Operation>

Notice how there is no action at the end of the checks. This is a pure check for errors setup. However They will work for patching conditions.

Layout for patching:

	<Operation Class="PatchOperationSequence">
		<success>Always</success>
		<operations>
			<li Class="ModCheck.isModLoaded">
				<modName>ModZ</modName>
				<yourMod>ModX</yourMod>
			</li>
			<li Class="PatchOperationAdd">
				// some patching code here
			</li>
		</operations>
	</Operation>

# License
Released under LGPLv3

This means you are free to redistribute the dll. In other words no conditions apply when adding it to your mod (other than not claiming you made the DLL)

If you modify the source code, you have to release the source code together with the modified DLL.
