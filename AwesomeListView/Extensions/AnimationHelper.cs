using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace AwesomeListView.WindowsPhone.Extensions
{
    public static class AnimationHelper
    {
        public static void FadeIn(this UIElement uiElement)
        {
            if (uiElement.Visibility != Visibility.Visible)
                uiElement.Visibility = Visibility.Visible;
            GetAnimationStoryboard(uiElement, 1.0).Begin();
        }

        private static Storyboard GetAnimationStoryboard(UIElement uiElement, double targetOpacity)
        {
            var storyboard = new Storyboard();

            var opacityAnimation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(opacityAnimation, uiElement);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

            storyboard.Children.Add(opacityAnimation);
            return storyboard;
        }

        public static void FadeOut(this UIElement uiElement)
        {
            GetAnimationStoryboard(uiElement, 0).Begin();
        }
    }
}