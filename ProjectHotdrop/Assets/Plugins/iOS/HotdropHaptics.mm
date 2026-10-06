#import <UIKit/UIKit.h>
#import <AudioToolbox/AudioToolbox.h>
#import <math.h>

extern "C" void HotdropHapticPulse(float intensity)
{
    float amount = fminf(1.0f, fmaxf(0.0f, intensity));
    if (amount <= 0.01f) return;
    if (@available(iOS 13.0, *)) {
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [generator prepare];
        [generator impactOccurredWithIntensity:amount];
    } else {
        AudioServicesPlaySystemSound(kSystemSoundID_Vibrate);
    }
}
