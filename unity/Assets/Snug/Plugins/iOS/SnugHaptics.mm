// Snug haptics for iOS: short UIKit impact taps (Unity's Handheld.Vibrate is a long buzz).
#import <UIKit/UIKit.h>

extern "C" void _SnugImpact(int style)
{
    UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleLight
                            : style == 1 ? UIImpactFeedbackStyleMedium
                                         : UIImpactFeedbackStyleHeavy;
    UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    [g prepare];
    [g impactOccurred];
}
