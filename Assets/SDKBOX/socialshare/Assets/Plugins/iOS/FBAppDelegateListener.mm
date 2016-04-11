//
//  FBAppDelegateListener.m
//  Unity-iPhone
//
//  Created by hugo on 16/4/8.
//
//

#import "FBAppDelegateListener.h"

#import <FBSDKCoreKit/FBSDKCoreKit.h>


static FBAppDelegateListener *_instance = [FBAppDelegateListener sharedInstance];

@interface FBAppDelegateListener()
@property (nonatomic, copy) NSString *openURLString;
@end

@implementation FBAppDelegateListener

#pragma mark Object Initialization

+ (FBAppDelegateListener *)sharedInstance {
    return _instance;
}

+ (void)initialize {
    if(!_instance) {
        _instance = [[FBAppDelegateListener alloc] init];
    }
}

- (id)init {
    if(_instance != nil) {
        return _instance;
    }

    if ((self = [super init])) {
        _instance = self;

        UnityRegisterAppDelegateListener(self);
    }
    return self;
}

#pragma mark - App (Delegate) Lifecycle

- (void)didFinishLaunching:(NSNotification *)notification {
    [[FBSDKApplicationDelegate sharedInstance] application:[UIApplication sharedApplication]
                             didFinishLaunchingWithOptions:notification.userInfo];
}

- (void)didBecomeActive:(NSNotification *)notification {
    [FBSDKAppEvents activateApp];
}

- (void)onOpenURL:(NSNotification *)notification {
    NSURL *url = notification.userInfo[@"url"];
    BOOL isHandledByFBSDK = [[FBSDKApplicationDelegate sharedInstance] application:[UIApplication sharedApplication]
                                                                           openURL:url
                                                                 sourceApplication:notification.userInfo[@"sourceApplication"]
                                                                        annotation:notification.userInfo[@"annotation"]];
    if (!isHandledByFBSDK) {
        [FBAppDelegateListener sharedInstance].openURLString = [url absoluteString];
    }
}

#pragma mark - Implementation

- (void)configureAppId:(const char *)appId
             urlSuffix:(const char *)urlSuffix {
    if(appId && strlen(appId)) {
        [FBSDKSettings setAppID:[NSString stringWithUTF8String:appId]];
    }

    if(urlSuffix && strlen(urlSuffix) > 0) {
        [FBSDKSettings setAppURLSchemeSuffix:[NSString stringWithUTF8String:urlSuffix]];
    }
}

#pragma mark - Actual Unity C# interface (extern C)

extern "C" {

    void sdkbox_socialshare_fb_config(const char *_appId, const char *_urlSuffix) {
        [[FBAppDelegateListener sharedInstance] configureAppId:_appId
                                                urlSuffix:_urlSuffix];
    }

}

@end
