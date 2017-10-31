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
Download the DLL file and put it in Assemblies in your mod. You may have to create this folder and it doesn't matter if you already have dll files in it or not. Unlike standalone mods, this approach ensures that the added features are always available to your mod.

# Usage guide
### Default tags for all checks
***
modName 

The name of the mod you want to test on
***
yourMod 

The name of your mod. Used for error messages and checking mod load order
***
errorOnFail 

Bool to tell if fails to be written as errors. Default false
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
Will pass if version of modName is the same or newer than version.

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
