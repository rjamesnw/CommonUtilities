using System;
using System.Windows;
using System.Windows.Threading;

namespace Common.XAML.Effects
{
    public static class Effects
    {
        public static void Fade(FrameworkElement element, double targetOpacity, double step, TimeSpan interval)
        {
            targetOpacity = Math.Abs(targetOpacity);
            step = Math.Abs(step);

            var timer = new DispatcherTimer();
            timer.Interval = interval;
            timer.Tick += (s, e) =>
                {
                    bool done = false;
                    if (element.Opacity > targetOpacity)
                    {
                        element.Opacity -= step;
                        if (element.Opacity <= targetOpacity) done = true;
                    }
                    else if (element.Opacity < targetOpacity)
                    {
                        element.Opacity += step;
                        if (element.Opacity >= targetOpacity) done = true;
                    } else done = true;
                    if (done) { element.Opacity = targetOpacity; timer.Stop(); }
                };
            timer.Start();
        }
    }
}
