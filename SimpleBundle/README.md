
# SimpleBundle Patch

RMU employs Unity's Addressable Assets (AA) system, which has performance issues with the sample project containing 20k assets. This patch attempts to resolve this by using AssetBundle + FileMapping data to speed up build time.

Build time is significantly improved for sample project:
### Before: 
Approximately 40 minutes per build, consuming 1GB disk space.

### After: 
Initial build takes 5 minutes. Subsequent builds are as fast as 2-3 minutes depending on configuration. Build size can be reduced to 700MB with AssetBundle compression.

## How to Implement 'SimpleBundle' Patch

Modify the RMU code and add code files from this folder to the project. 
#### Step 1. Code Patch
1. Close Unity Editor and navigate to your project folder.
2. Navigate to the '**Assets/RPGMaker/Codebase/CoreSystem/Helper**' directory.
3. Place this patch folder '**SimpleBundle**' into it, including all ".cs" and ".meta" files.
4. Open 'AddressableManager.cs' in the 'Helper' directory with a text editor, and replace marked lines:

 ``` 
- public class AddressableManager
+ public partial class AddressableManager

- public class Load
+ public class LoadBackup

- public class RefreshAssetPath
+ public partial class RefreshAssetPath

- BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
+ BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandlerSimple);
 ```
This marks classes as '**partial**' for extension, renames 'Load' to '**LoadBackup**', and registers the '**BuildPlayerHandlerSimple**' handler.

#### Step 2. Adjust Editor Setting
1. Open Unity Preferences ('**Edit/Preferences**' menu).
2. Click '**Addressables**' and change 'Player Build Settings' to 'Do Not Build Addressables on Player Build'. 
3. If you're using 'Addressables' in other projects, adjust settings in 'Assets/AddressableAssetsData/AddressableAssetSettings.asset' after the initial RMU build.

You can now build the project via the RMU menu or Unity's '**File/Build Setting...**' menu. The build should take 3-5 minutes.

> Note: This patch is useful for large projects such as the sample. For smaller projects, it might not be necessary.

## How to Revert

1. Close Unity and delete the '**SimpleBundle**' folder.
2. Undo modifications in '**Assets/RPGMaker/Codebase/CoreSystem/Helper/AddressableManager.cs**'.
3. Re-enable Addressable Assets build by reverting the setting to 'Build Addressable on Player Build'.

## Extra Info
1. You can tweak the config in '**SimpleBundleHelper.cs**' file.
2. The AssetBundle files are generated into 'Assets/RPGMaker/Storage/SimpleBundle' and copied to build. This folder is safe to delete, it will be regenerated after each build.