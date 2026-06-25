#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import <TapTapRelationLiteSDK/TapTapRelationLiteSDK-Swift.h>
#import "UnityAppController.h"
#import "AppDelegateListener.h"
#import <dlfcn.h>

typedef void (*TapSDKSceneOpenURLReplayFunc)(id listener);

static void TapSDKReplayPendingSceneOpenURLIfAvailable(id listener) {
    static TapSDKSceneOpenURLReplayFunc replayFunc = NULL;
    if (!replayFunc) {
        replayFunc = reinterpret_cast<TapSDKSceneOpenURLReplayFunc>(
            dlsym(RTLD_DEFAULT, "TapSDKUnitySceneOpenURLReplayPendingURLsToListener")
        );
    }
    if (replayFunc) {
        replayFunc(listener);
    }
}

@interface TapTapSDKRelationLiteAppDelegateListener : NSObject<AppDelegateListener>

+ (instancetype)sharedInstance;

@end

@implementation TapTapSDKRelationLiteAppDelegateListener

+ (instancetype)sharedInstance {
    static TapTapSDKRelationLiteAppDelegateListener *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[TapTapSDKRelationLiteAppDelegateListener alloc] init];
    });
    return sharedInstance;
}

- (void)onOpenURL:(NSNotification *)notification {
    NSDictionary *userInfo = notification.userInfo;
    NSURL *url = [userInfo valueForKey:@"url"];
    if (url) {
        [TapTapRelationLite openWithUrl:url];
    }
}

@end

extern "C" void RegisterTapTapSDKRelationLiteAppDelegateListener() {
    TapTapSDKRelationLiteAppDelegateListener *listener = TapTapSDKRelationLiteAppDelegateListener.sharedInstance;
    UnityRegisterAppDelegateListener(listener);
    TapSDKReplayPendingSceneOpenURLIfAvailable(listener);
}
