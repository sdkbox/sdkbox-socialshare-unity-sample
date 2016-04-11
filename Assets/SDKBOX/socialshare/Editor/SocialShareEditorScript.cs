/*****************************************************************************
Copyright © 2015 SDKBOX.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*****************************************************************************/

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using Sdkbox;

namespace Sdkbox {
	[InitializeOnLoad]
	public class SocialShareEditorScript {
		static SocialShareEditorScript() {
			Sdkbox.Setup.Register("SocialShare");
		}

		[MenuItem("Window/SDKBOX/Documentation/SocialShare")]
		static void OpenDocumentation(MenuCommand menuCommand) {
			Sdkbox.Setup.OpenDocumentation("socialshare");
		}

		[PostProcessBuild(999)]
		public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {
			if (target != BuildTarget.iOS) {
				return;
			}

			string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));
			string targetGUID = proj.TargetGuidByName("Unity-iPhone");
			proj.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC"); 
			File.WriteAllText(projPath, proj.WriteToString());
		}
	}
}
