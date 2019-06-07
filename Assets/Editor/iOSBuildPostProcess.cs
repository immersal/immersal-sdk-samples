#if UNITY_IOS
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class MyBuildPostprocess
{
	[PostProcessBuildAttribute(999)]
	public static void OnPostProcessBuild( BuildTarget buildTarget, string path)
	{
		if(buildTarget == BuildTarget.iOS)
		{
			string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

			PBXProject pbxProject = new PBXProject();
			pbxProject.ReadFromFile(projectPath);

			string target = pbxProject.TargetGuidByName("Unity-iPhone");            
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

			pbxProject.AddFrameworkToProject(target, "UserNotifications.framework", false);

			pbxProject.WriteToFile (projectPath);

			// Get plist
			string plistPath = path + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));

			// Get root
			PlistElementDict rootDict = plist.root;

			const string capsKey = "UIRequiredDeviceCapabilities";
			PlistElementArray capsArray;
			PlistElement element;

			capsArray = rootDict.values.TryGetValue(capsKey, out element) ? element.AsArray() : rootDict.CreateArray(capsKey);

			const string arch = "armv7";
			capsArray.values.RemoveAll(x => arch.Equals(x.AsString()));

			capsArray.AddString("arm64");
			File.WriteAllText(plistPath, plist.WriteToString());
		}
	}
}
#endif
