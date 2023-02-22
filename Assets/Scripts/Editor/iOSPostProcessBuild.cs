#if UNITY_IOS
using UnityEngine;
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
	public static void OnPostprocessBuild(BuildTarget target, string buildPath)
	{
		if (target == BuildTarget.iOS)
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
			pbxProject.SetBuildProperty(pbxProject.GetUnityFrameworkTargetGuid(), "ENABLE_BITCODE", "NO");
			pbxProject.SetBuildProperty(pbxProject.GetUnityMainTargetGuid(), "ENABLE_BITCODE", "NO");
	        pbxProject.WriteToFile(projectPath);
		}
	}
#pragma warning restore 0162
#endif
}