//
//  FBAppDelegateListener.h
//  Unity-iPhone
//
//  Created by hugo on 16/4/8.
//
//

#ifndef FBAppDelegateListener_h
#define FBAppDelegateListener_h

#import <UIKit/UIKit.h>
#import "AppDelegateListener.h"


@interface FBAppDelegateListener : NSObject <AppDelegateListener> {
}

+ (FBAppDelegateListener *)sharedInstance;
@end


#endif /* FBAppDelegateListener_h */
