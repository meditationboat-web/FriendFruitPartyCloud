using UnityEditor;
public static class FriendCloudSetup {
 public static void Configure(){
 EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/MediaPipeUnity/Samples/Scenes/Face Landmark Detection/Face Landmark Detection.unity",true)};
 PlayerSettings.companyName="GestureGames";PlayerSettings.productName="双人水果派对";
 PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.gesturegames.fruitparty");
 PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
 PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
 PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel24;
 PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
 PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
 PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
 PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
 PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
 AssetDatabase.SaveAssets();UnityEngine.Debug.Log("FRIEND_CLOUD_SETUP_PASS");
 }
}
