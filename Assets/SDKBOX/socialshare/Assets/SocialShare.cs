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
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using AOT;

namespace Sdkbox {

	[Serializable]
	public class SocialShare : MonoBehaviour {

		public enum SocialPlatform {
			Platform_Unknow = 0,
			Platform_Twitter = 1,
			Platform_Facebook = 2,
			Platform_Select = 3,
			Platform_All = 4
		};

		public enum SocialShareState {
			SocialShareStateNone,
			SocialShareStateUnkonw,
			SocialShareStateBegin,
			SocialShareStateSuccess,
			SocialShareStateFail,
			SocialShareStateCancelled,
			SocialShareStateSelectShow,
			SocialShareStateSelected,
			SocialShareStateSelectCancelled
		};

		public struct SocialShareResponse {
			public SocialShareState state;
			public string error;
			public SocialPlatform platform;

			public static SocialShareResponse createFromJson(Json j) {
				SocialShareResponse r = new SocialShareResponse();
				try {
					r.state = (SocialShareState)j["state"].int_value();
					r.error = j["error"].string_value();
					r.platform = (SocialPlatform)j["platform"].int_value();
				} catch (Exception e) {
					Debug.LogException(e);
					Debug.Log("Json: " + j.dump());
				}
				return r;
			}
		};

		[Serializable]
		public struct SocialShareInfo {
			public string text;
			public string title;
			public string image;
			public string link;
			public SocialPlatform platform;

			override public string ToString() {
				StringBuilder sb = new StringBuilder();
				sb.Append ("{")
					.Append ("\"text\":").Append ("\"").Append (text).Append ("\",")
					.Append ("\"title\":").Append ("\"").Append (title).Append ("\",")
					.Append ("\"image\":").Append ("\"").Append (image).Append ("\",")
					.Append ("\"link\":").Append ("\"").Append (link).Append ("\",")
					.Append ("\"platform\":").Append ((int)platform)
					.Append ("}");
				return sb.ToString ();
			}
		}

		[Serializable]
		public struct SocialShareConfig {
			public string Twitter_Key_IOS;
			public string Twitter_Secret_IOS;
			public string Twitter_Key_Android;
			public string Twitter_Secret_Android;
			public bool Facebook_enable;
			public string Share_Panel_Title;
			public string Share_Panel_Cancel;
		}

		[Serializable]
		public class Callbacks {
			[Serializable]
			public class SocialShareResponseEvent: UnityEvent<SocialShareResponse> {}

			public SocialShareResponseEvent onShareState = null;

			Callbacks() {
				if (null == onShareState) {
					onShareState = new SocialShareResponseEvent();
				}
			}
		};

		public SocialShareConfig config;
		public SocialShareInfo socialShareInfo;
		public Callbacks callbacks;

		private List<SocialPlatform> platforms;

		// iOS requires a static callback due to AOT compilation.
		// We cache the IAP instance to redirect the callback to the instance.
		private static SocialShare _this;

		// delegate signature for callbacks from SDKBOX runtime.
		public delegate void CallbackDelegate(string method, string json);

		#if UNITY_ANDROID
		// we need to access the Unity java player to run methods
		// on the UI thread, so we cache this at initialization time.
		private static AndroidJavaClass _player;
		#endif

		// Currently we load the configuration from JSON.
		// In future versions this will be configurable in the editor.
		private string _config;

		void Awake() {
			// This may not be needed, but the object will be initialized twice without it.
			DontDestroyOnLoad(transform.gameObject);

			// cache the instance for the callbacks
			_this = this;
		}

		// Use this for initialization
		void Start() {
			init();
		}

		[MonoPInvokeCallback(typeof(CallbackDelegate))]
		public static void sdkboxSocialShareCallback(string method, string jsonString) {
			//Debug.Log("sdkboxSocialShareCallback: " + method + " => " + jsonString);
			if (null != _this) {
				_this.handleCallback(method, jsonString);
			} else {
				Debug.Log("Missed callback " + method + " => " + jsonString);
			}
		}

		private void handleCallback(string method, string jsonString) {
			Json json = Json.parse(jsonString);
			if (json.is_null()) {
				Debug.LogError("Failed to parse JSON callback payload");
				throw new System.ArgumentException("Invalid JSON payload");
			}

			Debug.Log("Dispatching SocialShare callback method: " + method);

			switch (method) {
			case "onShareState": {
				if (callbacks.onShareState != null) {
					SocialShareResponse resp = SocialShareResponse.createFromJson (json);
					Debug.Log ("share state:" + resp.state + " platform:" + resp.platform + " error:" + resp.error);
					callbacks.onShareState.Invoke (resp);
				}
				break;
			}
			default: {
				throw new System.ArgumentException ("Unknown callback type: " + method);
			}
			}
		}

		private Json newObject() {
			Dictionary<string, Json> o = new Dictionary<string, Json>();
			return new Json(o);
		}

		private string buildConfiguration() {
			/*
			 *
			 "Share":{
	            "platforms":{
	                "Twitter":{
	                    "params": {
	                        "key":"BUJTV6NEM7BAhhm82B12VbKGy",
	                        "secret":"haVcKarM96Sr4390XLQoHjyRUSyuHdkMX6letcc38h8TOWyiR9"
	                    }
	                },
	                "Facebook":{}
	            }
	        },
	        "Facebook":{
	            "debug":true
	        }
			 *
			 */
			Json json = newObject();
			Json cur;

			cur = json;
			#if UNITY_ANDROID
			cur["android"]   = newObject(); cur = cur["android"];
			#elif UNITY_IOS
			cur["ios"]   = newObject(); cur = cur["ios"];
			#endif
			Json platformNode = cur;

			//add facebook config
			cur["Facebook"] = newObject();cur = cur["Facebook"];
			cur ["debug"] = new Json (false);

			cur = platformNode;

			cur["Share"] = newObject(); cur = cur["Share"];
			cur["platforms"] = newObject(); cur = cur["platforms"];

			platformNode = cur;
			#if UNITY_IOS
			if (!string.IsNullOrEmpty (config.Twitter_Key_IOS) && !string.IsNullOrEmpty (config.Twitter_Secret_IOS)) {
				cur ["Twitter"] = newObject ();
				cur = cur ["Twitter"];
				Json j = newObject ();
				j ["key"] = new Json (config.Twitter_Key_IOS);
				j ["secret"] = new Json (config.Twitter_Secret_IOS);
				cur ["params"] = j;
				platforms.Add(SocialPlatform.Platform_Twitter);
			}
			cur = platformNode;
			if (config.Facebook_enable) {
				cur ["Facebook"] = newObject ();
				platforms.Add(SocialPlatform.Platform_Facebook);
			}
			#elif UNITY_ANDROID
			if (!string.IsNullOrEmpty (config.Twitter_Key_Android) && !string.IsNullOrEmpty (config.Twitter_Secret_Android)) {
				cur ["Twitter"] = newObject ();
				cur = cur ["Twitter"];
				Json j = newObject ();
				j ["key"] = new Json (config.Twitter_Key_Android);
				j ["secret"] = new Json (config.Twitter_Secret_Android);
				cur ["params"] = j;
				platforms.Add(SocialPlatform.Platform_Twitter);
			}
			cur = platformNode;
			if (config.Facebook_enable) {
				cur ["Facebook"] = newObject ();
				platforms.Add(SocialPlatform.Platform_Facebook);
			}
			#endif

			return json.dump();
		}

		private void init() {
			Debug.Log("SDKBOX SocialShare starting.");

			platforms = new List<SocialPlatform>();
			SDKBOX.Instance.init(); // reference the SDKBOX singleton to ensure shared init.

			_config = buildConfiguration();
			Debug.Log("configuration: " + _config);

			#if UNITY_ANDROID
			SocialShare._player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = SocialShare._player.GetStatic<AndroidJavaObject>("currentActivity");
			activity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
				sdkbox_socialshare_set_unity_callback(sdkboxSocialShareCallback);
				sdkbox_socialshare_init(_config);
				if (!string.IsNullOrEmpty(config.Share_Panel_Title)) {
					sdkbox_socialshare_setSharePanelTitle (config.Share_Panel_Title);
				}
				if (!string.IsNullOrEmpty (config.Share_Panel_Cancel)) {
					sdkbox_socialshare_setSharePanelCancel (config.Share_Panel_Cancel);
				}
				Debug.Log("SDKBOX SocialShare Initialized.");
			}));
			#elif UNITY_IOS
			sdkbox_socialshare_set_unity_callback(sdkboxSocialShareCallback);
			sdkbox_socialshare_init(_config);
			if (!string.IsNullOrEmpty(config.Share_Panel_Title)) {
				sdkbox_socialshare_setSharePanelTitle (config.Share_Panel_Title);
			}
			if (!string.IsNullOrEmpty (config.Share_Panel_Cancel)) {
				sdkbox_socialshare_setSharePanelCancel (config.Share_Panel_Cancel);
			}
			Debug.Log("SDKBOX SocialShare Initialized.");
			#endif
		}

		public void share() {
			share (socialShareInfo);
		}

		public void share(SocialShareInfo info) {
			Debug.Log ("share: " + info.ToString());

			#if !UNITY_EDITOR
			#if UNITY_ANDROID
			AndroidJavaObject activity = SocialShare._player.GetStatic<AndroidJavaObject>("currentActivity");
			if (null == activity) {
				Debug.Log ("share activity is null");
				return;
			}
			activity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
				sdkbox_socialshare_share(info.ToString ());
			}));
			#else
			sdkbox_socialshare_share(info.ToString ());
			#endif
			#endif // !UNITY_EDITOR
		}

		public void setSharePanelTitle(string s) {
			sdkbox_socialshare_setSharePanelTitle (s);
		}

		public void setSharePanelCancel(string s) {
			sdkbox_socialshare_setSharePanelCancel (s);
		}

		#if UNITY_IOS
		[DllImport("__Internal")]
		#else
		[DllImport("socialshare")]
		#endif
		private static extern void sdkbox_socialshare_init(string jsonconfig);

		#if UNITY_IOS
		[DllImport("__Internal")]
		#else
		[DllImport("socialshare")]
		#endif
		private static extern void sdkbox_socialshare_share(string json);

		#if UNITY_IOS
		[DllImport("__Internal")]
		#else
		[DllImport("socialshare")]
		#endif
		private static extern void sdkbox_socialshare_set_unity_callback(CallbackDelegate callback);

		#if UNITY_IOS
		[DllImport("__Internal")]
		#else
		[DllImport("socialshare")]
		#endif
		private static extern void sdkbox_socialshare_setSharePanelTitle(string s);

		#if UNITY_IOS
		[DllImport("__Internal")]
		#else
		[DllImport("socialshare")]
		#endif
		private static extern void sdkbox_socialshare_setSharePanelCancel(string s);
	}
}
