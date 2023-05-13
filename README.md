
# Fix Issues in RPG Maker Unite (v1.00.00)

RPG Maker Unite (RMU) is a product developed by GGG, available on the Unity Store at: https://assetstore.unity.com/publishers/68473

The initial version of RMU has several performance issues.  
I'm attempting to contribute some fixes to improve the product.

Please note that I am not associated with the developers of RMU.  
All code in this repository is released under the MIT license.

Official RMU code referenced here retains its original licenses.  
Use these code only if you hold a valid RMU license.

The official RMU might address these issues in future release.  
Use this patch at your own risk and always backup.

## 1. SimpleBundle Patch
Read the document inside the 'SimpleBundle' folder for usage.  
On my machine, the full build time for the sample project has been reduced to 3-5 minutes, compared to the previous 40 minutes.

## 2. Remove 'await Task.Delay'
Open the code assets in Visual Studio and open any code file in the "RPGMaker.CodeBase.Editor" project.

Search for **'await Task.Delay'** and replace it with **'{} //await Task.Delay'** (without the quotes).  
Make sure the 'Look in' scope is set to 'Current project'. This search/replace will disable all instances of Delay.Task added in the code.

While this may potentially trigger some bugs in the editor, I didn't encounter any issues on my machine for now.  
Please use this at your own risk, and remember, you can always revert it by replacing it back.  
There could be about 50 places to adjust.

The reason is that Delay has overhead and it wont' be just 1ms for a call like "await Task.Delay(1)".  
There are 6~8 await for each click in RMU Editor, which might create 1000ms delay in total.

## 3. Remove Duplicated Auth and Initialization
Add the lines with a '+' prefix at the place you found in code file.  
Do not include the '+' itself because it is not part of the code, just to show you which line to add.  

Modify **'Assets/RPGMaker/Codebase/Editor/Common/RpgMakerEditor.cs'**
```
// at about line 99
         private static async Task DelayAuth() {
+            if(Auth.IsAuthenticated)
+            {
+                Debug.Log("[FIX] Already authenticated in DelayAuth");
+                return;
+            }
+

// at about line 500
+        private static bool _initing = false;

        /// <summary>
        /// ウィンドウ最大化から復帰時の処理。
        /// </summary>
        public static void WindowMaximizationRecoveringProcess() {
            // 認証済でない場合は何もしない
             if (!Auth.IsAuthenticated)
             {
+                Debug.LogError("[FIX] Not authenticated in WindowMaximizationRecoveringProcess");
                 return;
             }
 
+            if (_initing)
+            {
+                Debug.Log("[FIX] Already initializing in WindowMaximizationRecoveringProcess");
+                return;
+            }
+
+            _initing = true;
```

## Hints
- The auth might be slow and annoying, if you failed to authenticate, just close Unity and reopen the project.
- If you're a Unity user, you'd better to enable Unity UI by menu items in **'Window/RPG Maker/Mode/'**, select the 2nd or 3rd one with **'Unity Editor'**. It shows more panels and let you use Unity features. 
