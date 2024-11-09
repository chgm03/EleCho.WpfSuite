﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EleCho.WpfSuite.Media.Transition
{
    /// <summary>
    /// Slide and fade transition
    /// </summary>
    public class SlideFadeTransition : ContentTransition
    {
        /// <summary>
        /// Slide orientation
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Slide distance
        /// </summary>
        public double Distance
        {
            get { return (double)GetValue(DistanceProperty); }
            set { SetValue(DistanceProperty, value); }
        }

        /// <summary>
        /// Reverse this transition
        /// </summary>
        public bool Reverse
        {
            get { return (bool)GetValue(ReverseProperty); }
            set { SetValue(ReverseProperty, value); }
        }

        private double GetDistance(UIElement container, UIElement self)
        {
            var distance = 10.0;
            if (container is FrameworkElement containerFrameworkElement)
            {
                distance = Orientation == Orientation.Horizontal ?
                    containerFrameworkElement.ActualWidth :
                    containerFrameworkElement.ActualHeight;
            }

            if (self is FrameworkElement selfFrameworkElement &&
                (double.IsNaN(distance) || distance == 0))
            {
                distance = Orientation == Orientation.Horizontal ?
                    selfFrameworkElement.ActualWidth :
                    selfFrameworkElement.ActualHeight;
            }

            if (Distance is double customDistance &&
                !double.IsNaN(customDistance))
            {
                distance = customDistance;
            }

            return distance;
        }

        /// <inheritdoc/>
        protected override Freezable CreateInstanceCore() => new SlideFadeTransition();

        /// <inheritdoc/>
        protected override Storyboard CreateNewContentStoryboard(UIElement container, UIElement newContent, bool forward)
        {
            if (newContent.RenderTransform is not TranslateTransform)
                newContent.RenderTransform = new TranslateTransform();

            var distance = GetDistance(container, newContent);
            DoubleAnimation translateAnimation = new()
            {
                EasingFunction = EasingFunction,
                Duration = Duration,
                From = distance,
                To = 0,
            };

            DoubleAnimation opacityAnimation = new()
            {
                EasingFunction = EasingFunction,
                Duration = Duration,
                From = 0,
                To = 1
            };

            if (Reverse ^ !forward)
            {
                translateAnimation.From *= -1;
            }

            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(nameof(FrameworkElement.Opacity)));

            if (Orientation == Orientation.Horizontal)
            {
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("RenderTransform.X"));
            }
            else
            {
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("RenderTransform.Y"));
            }

            return new Storyboard()
            {
                Duration = Duration,
                Children =
                {
                    translateAnimation,
                    opacityAnimation
                }
            };
        }

        /// <inheritdoc/>
        protected override Storyboard CreateOldContentStoryboard(UIElement container, UIElement oldContent, bool forward)
        {
            if (oldContent.RenderTransform is not TranslateTransform)
                oldContent.RenderTransform = new TranslateTransform();

            var distance = GetDistance(container, oldContent);
            DoubleAnimation translateAnimation = new()
            {
                EasingFunction = EasingFunction,
                Duration = Duration,
                To = -distance,
            };

            DoubleAnimation opacityAnimation = new()
            {
                EasingFunction = EasingFunction,
                Duration = Duration,
                To = 0
            };

            if (Reverse ^ !forward)
            {
                translateAnimation.To *= -1;
            }

            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(nameof(FrameworkElement.Opacity)));

            if (Orientation == Orientation.Horizontal)
            {
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("RenderTransform.X"));
            }
            else
            {
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("RenderTransform.Y"));
            }

            return new Storyboard()
            {
                Duration = Duration,
                Children =
                {
                    translateAnimation,
                    opacityAnimation
                }
            };
        }

        /// <summary>
        /// The DependencyProperty of <see cref="Orientation"/> property
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(SlideFadeTransition), new PropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// The DependencyProperty of <see cref="Distance"/> property
        /// </summary>
        public static readonly DependencyProperty DistanceProperty =
            DependencyProperty.Register(nameof(Distance), typeof(double), typeof(SlideFadeTransition), new PropertyMetadata(ValueBoxes.DoubleNaNBox));

        /// <summary>
        /// The DependencyProperty of <see cref="Reverse"/> property
        /// </summary>
        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(SlideFadeTransition), new PropertyMetadata(ValueBoxes.FalseBox));
    }
}
