#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

public class iOSPostProcessBuild 
{
#if UNITY_IOS
#pragma warning disable 0162
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
	{
		if (buildTarget == BuildTarget.iOS)
		{
			string projectPath = PBXProject.GetPBXProjectPath(buildPath);
			string plistPath = Path.Combine(buildPath, "Info.plist");

			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
			PlistElementDict rootDict = plist.root;
			rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
			File.WriteAllText(plistPath, plist.WriteToString());

			var pbxProject = new PBXProject();
			pbxProject.ReadFromFile(projectPath);
			string target = pbxProject.GetUnityMainTargetGuid();
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

			target = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

			target = pbxProject.GetUnityFrameworkTargetGuid();
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

			target = pbxProject.TargetGuidByName("GameAssembly");
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

	        pbxProject.WriteToFile(projectPath);
		}
	}
#pragma warning restore 0162
#endif
}