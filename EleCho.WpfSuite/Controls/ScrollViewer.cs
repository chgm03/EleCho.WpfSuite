﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EleCho.WpfSuite
{
    public class ScrollViewer : System.Windows.Controls.ScrollViewer
    {
        static ScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollViewer), new FrameworkPropertyMetadata(typeof(ScrollViewer)));

#if NETCOREAPP
            _propertyHandlesMouseWheelScrollingGetter = typeof(ScrollViewer)
                .GetProperty("HandlesMouseWheelScrolling", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetGetMethod(true)!
                .CreateDelegate<GetBool>();
#else
            _propertyHandlesMouseWheelScrollingGetter = (GetBool)typeof(ScrollViewer)
                .GetProperty("HandlesMouseWheelScrolling", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetGetMethod(true)!
                .CreateDelegate(typeof(GetBool));
#endif
        }

        private delegate bool GetBool(ScrollViewer scrollViewer);
        private static readonly GetBool _propertyHandlesMouseWheelScrollingGetter;
        private static readonly IEasingFunction _scrollingAnimationEase = new CubicEase(){ EasingMode = EasingMode.EaseOut };
        private const long _millisecondsBetweenTouchpadScrolling = 100;

        private bool _animationRunning = false;
        private int _lastScrollDelta = 0;
        private int _lastVerticalScrollingDelta = 0;
        private int _lastHorizontalScrollingDelta = 0;
        private long _lastScrollingTick;

        private void CoreScrollWithWheelDelta(MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            var handlesMouseWheelScrolling = _propertyHandlesMouseWheelScrollingGetter.Invoke(this);
            if (!handlesMouseWheelScrolling)
            {
                return;
            }

            bool vertical = ExtentHeight > 0;
            bool horizontal = ExtentWidth > 0;

            var tickCount = Environment.TickCount;
            var isTouchpadScrolling =
                    e.Delta % Mouse.MouseWheelDeltaForOneLine != 0 ||
                    (tickCount - _lastScrollingTick < _millisecondsBetweenTouchpadScrolling && _lastScrollDelta % Mouse.MouseWheelDeltaForOneLine != 0);

            if (vertical)
            {
                double scrollDelta = e.Delta;
                if (ScrollInfo is IScrollInfo scrollInfo)
                {
                    // 考虑到 VirtualizingPanel 可能是虚拟的大小, 所以这里需要校正 Delta
                    scrollDelta *= scrollInfo.ViewportHeight / ActualHeight;
                }

                var sameDirectionAsLast = Math.Sign(e.Delta) == Math.Sign(_lastVerticalScrollingDelta);
                var nowOffset = sameDirectionAsLast && _animationRunning ? VerticalOffsetTarget : VerticalOffset;
                var newOffset = nowOffset - scrollDelta;

                if (newOffset < 0)
                    newOffset = 0;
                if (newOffset > ScrollableHeight)
                    newOffset = ScrollableHeight;

                SetValue(VerticalOffsetTargetPropertyKey, newOffset);
                BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);

                if (!EnableScrollingAnimation || isTouchpadScrolling)
                {
                    ScrollToVerticalOffset(newOffset);
                }
                else
                {
                    var diff = newOffset - VerticalOffset;
                    var absDiff = Math.Abs(diff);
                    var duration = ScrollingAnimationDuration;
                    if (absDiff < Mouse.MouseWheelDeltaForOneLine)
                    {
                        duration = new Duration(TimeSpan.FromTicks((long)(duration.TimeSpan.Ticks * absDiff / Mouse.MouseWheelDeltaForOneLine)));
                    }

                    DoubleAnimation doubleAnimation = new DoubleAnimation()
                    {
                        EasingFunction = _scrollingAnimationEase,
                        Duration = duration,
                        From = VerticalOffset,
                        To = newOffset,
                    };

                    doubleAnimation.Completed += DoubleAnimation_Completed;

                    _animationRunning = true;
                    BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, doubleAnimation, HandoffBehavior.SnapshotAndReplace);
                }

                _lastVerticalScrollingDelta = e.Delta;
            }
            else if (horizontal)
            {
                double scrollDelta = e.Delta;
                if (ScrollInfo is IScrollInfo scrollInfo)
                {
                    // 考虑到 VirtualizingPanel 可能是虚拟的大小, 所以这里需要校正 Delta
                    scrollDelta *= scrollInfo.ViewportWidth / ActualWidth;
                }

                var sameDirectionAsLast = Math.Sign(e.Delta) == Math.Sign(_lastHorizontalScrollingDelta);
                var nowOffset = sameDirectionAsLast && _animationRunning ? HorizontalOffsetTarget : HorizontalOffset;
                var newOffset = nowOffset - scrollDelta;

                if (newOffset < 0)
                    newOffset = 0;
                if (newOffset > ScrollableWidth)
                    newOffset = ScrollableWidth;

                SetValue(HorizontalOffsetTargetPropertyKey, newOffset);
                BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);

                if (!EnableScrollingAnimation || isTouchpadScrolling)
                {
                    ScrollToHorizontalOffset(newOffset);
                }
                else
                {
                    var diff = newOffset - HorizontalOffset;
                    var absDiff = Math.Abs(diff);
                    var duration = ScrollingAnimationDuration;
                    if (absDiff < Mouse.MouseWheelDeltaForOneLine)
                    {
                        duration = new Duration(TimeSpan.FromTicks((long)(duration.TimeSpan.Ticks * absDiff / Mouse.MouseWheelDeltaForOneLine)));
                    }

                    DoubleAnimation doubleAnimation = new DoubleAnimation()
                    {
                        EasingFunction = _scrollingAnimationEase,
                        Duration = duration,
                        From = HorizontalOffset,
                        To = newOffset,
                    };

                    doubleAnimation.Completed += DoubleAnimation_Completed;

                    _animationRunning = true;
                    BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, doubleAnimation, HandoffBehavior.SnapshotAndReplace);
                }

                _lastHorizontalScrollingDelta = e.Delta;
            }

            _lastScrollingTick = tickCount;
            _lastScrollDelta = e.Delta;

            e.Handled = true;
        }

        private void DoubleAnimation_Completed(object? sender, EventArgs e)
        {
            _animationRunning = false;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!ScrollWithWheelDelta)
            {
                base.OnMouseWheel(e);
            }
            else
            {
                Debug.WriteLine(e.Delta);

                CoreScrollWithWheelDelta(e);
            }
        }

        public double HorizontalOffsetTarget
        {
            get { return (double)GetValue(HorizontalOffsetTargetProperty); }
        }

        public double VerticalOffsetTarget
        {
            get { return (double)GetValue(VerticalOffsetTargetProperty); }
        }

        public bool ScrollWithWheelDelta
        {
            get { return (bool)GetValue(ScrollWithWheelDeltaProperty); }
            set { SetValue(ScrollWithWheelDeltaProperty, value); }
        }

        public bool EnableScrollingAnimation
        {
            get { return (bool)GetValue(EnableScrollingAnimationProperty); }
            set { SetValue(EnableScrollingAnimationProperty, value); }
        }

        public Duration ScrollingAnimationDuration
        {
            get { return (Duration)GetValue(ScrollingAnimationDurationProperty); }
            set { SetValue(ScrollingAnimationDurationProperty, value); }
        }



        public static readonly DependencyPropertyKey HorizontalOffsetTargetPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HorizontalOffsetTarget), typeof(double), typeof(ScrollViewer), new PropertyMetadata(0.0));

        public static readonly DependencyPropertyKey VerticalOffsetTargetPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(VerticalOffsetTarget), typeof(double), typeof(ScrollViewer), new PropertyMetadata(0.0));

        public static readonly DependencyProperty HorizontalOffsetTargetProperty = 
            HorizontalOffsetTargetPropertyKey.DependencyProperty;

        public static readonly DependencyProperty VerticalOffsetTargetProperty =
            VerticalOffsetTargetPropertyKey.DependencyProperty;



        public static bool GetScrollWithWheelDelta(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollWithWheelDeltaProperty);
        }

        public static void SetScrollWithWheelDelta(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollWithWheelDeltaProperty, value);
        }

        public static bool GetEnableScrollingAnimation(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableScrollingAnimationProperty);
        }

        public static void SetEnableScrollingAnimation(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableScrollingAnimationProperty, value);
        }

        public static Duration GetScrollingAnimationDuration(DependencyObject obj)
        {
            return (Duration)obj.GetValue(ScrollingAnimationDurationProperty);
        }

        public static void SetScrollingAnimationDuration(DependencyObject obj, Duration value)
        {
            obj.SetValue(ScrollingAnimationDurationProperty, value);
        }



        public static readonly DependencyProperty ScrollWithWheelDeltaProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollWithWheelDelta), typeof(bool), typeof(ScrollViewer), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty EnableScrollingAnimationProperty =
            DependencyProperty.RegisterAttached(nameof(EnableScrollingAnimation), typeof(bool), typeof(ScrollViewer), 
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty ScrollingAnimationDurationProperty =
            DependencyProperty.RegisterAttached(nameof(ScrollingAnimationDuration), typeof(Duration), typeof(ScrollViewer), 
                new FrameworkPropertyMetadata(new Duration(TimeSpan.FromMilliseconds(300)), FrameworkPropertyMetadataOptions.Inherits), ValidateScrollingAnimationDuration);

        private static bool ValidateScrollingAnimationDuration(object value)
            => value is Duration duration && duration.HasTimeSpan;
    }
}
