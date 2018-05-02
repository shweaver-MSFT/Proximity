using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Proximity
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ProximityField : DependencyObject
    {
        private const string ProximityPropertyName = "Proximity";
        private const string ProximityRangePropertyName = "ProximityRange";
        private const string ProximityPaddingPropertyName = "ProximityPadding";

        // A dict of top-level UIElements and the FrameworkElement that are being listened for
        private static Dictionary<UIElement, List<FrameworkElement>> _listeningElements = new Dictionary<UIElement, List<FrameworkElement>>();

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityProperty = DependencyProperty.RegisterAttached(ProximityPropertyName, typeof(int), typeof(DependencyObject), new PropertyMetadata(null));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityRangeProperty = DependencyProperty.RegisterAttached(ProximityRangePropertyName, typeof(int), typeof(DependencyObject), new PropertyMetadata(0, new PropertyChangedCallback(ProximityRangeChanged)));
        
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityModeProperty = DependencyProperty.RegisterAttached(nameof(ProximityMode), typeof(ProximityMode), typeof(DependencyObject), new PropertyMetadata(ProximityMode.Edge));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityPaddingProperty = DependencyProperty.RegisterAttached(ProximityPaddingPropertyName, typeof(int), typeof(DependencyObject), new PropertyMetadata(0));

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximitySpreadMethodProperty = DependencyProperty.RegisterAttached(nameof(ProximitySpreadMethod), typeof(ProximitySpreadMethod), typeof(DependencyObject), new PropertyMetadata(ProximitySpreadMethod.Pad));
        #endregion Properties

        #region Property Getters/Setters
        public static void SetProximity(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityProperty, value);
        }

        public static int GetProximity(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityProperty);
        }

        public static void SetProximityRange(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityRangeProperty, value);
        }

        public static int GetProximityRange(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityRangeProperty);
        }

        public static void SetProximityMode(DependencyObject element, ProximityMode value)
        {
            (element as FrameworkElement).SetValue(ProximityModeProperty, value);
        }

        public static ProximityMode GetProximityMode(DependencyObject element)
        {
            return (ProximityMode)(element as FrameworkElement).GetValue(ProximityModeProperty);
        }

        public static void SetProximityPadding(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityPaddingProperty, value);
        }

        public static int GetProximityPadding(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityPaddingProperty);
        }

        public static void SetProximitySpreadMethod(DependencyObject element, ProximitySpreadMethod value)
        {
            (element as FrameworkElement).SetValue(ProximitySpreadMethodProperty, value);
        }

        public static ProximitySpreadMethod GetProximitySpreadMethod(DependencyObject element)
        {
            return (ProximitySpreadMethod)(element as FrameworkElement).GetValue(ProximitySpreadMethodProperty);
        }
        #endregion Property Getters/Setters

        private static void ProximityRangeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var target = sender as FrameworkElement;
            var element = GetRootUIElement(target);

            void handleValueChange(FrameworkElement t, UIElement e, int? newValue)
            {
                if (!newValue.HasValue || newValue <= 0)
                {
                    DeregisterPointerListener(t, e);
                }

                if (newValue.HasValue && newValue > 0)
                {
                    RegisterPointerListener(t, e);
                }
            }

            if (element == target)
            {
                target.Loaded += (s, e) => handleValueChange(target, GetRootUIElement(target), (int?)args.NewValue);
            }
            else
            {
                handleValueChange(target, element, (int?)args.NewValue);
            }
        }

        private static void RegisterPointerListener(FrameworkElement target, UIElement element)
        {
            if (!_listeningElements.TryGetValue(element, out var targets))
            {
                targets = new List<FrameworkElement>();
                element.PointerMoved += Element_PointerMoved;
                _listeningElements.Add(element, targets);
            }

            target.Unloaded += (s, e) => DeregisterPointerListener(target, element);
            targets.Add(target);
        }

        private static void DeregisterPointerListener(FrameworkElement target, UIElement element)
        {
            var targets = _listeningElements[element];

            if (targets.Count == 0)
            {
                element.PointerMoved -= Element_PointerMoved;
                _listeningElements.Remove(element);
            }
            else
            {
                targets.Remove(target);
            }
        }

        private static UIElement GetRootUIElement(FrameworkElement target)
        {
            DependencyObject root = target;
            while (true)
            {
                var temp = VisualTreeHelper.GetParent(root);
                if (temp == null) break;
                else root = temp;
            }

            // Find the top UIElement
            while(true)
            {
                if (root is UIElement) break;
                else root = VisualTreeHelper.GetChild(root, 0);
            }

            // Root is now the highest UIElement
           return root as UIElement;
        }

        private static Rect GetProximityRect(FrameworkElement target)
        {
            var proximityRange = GetProximityRange(target);
            var proximityPadding = GetProximityPadding(target);
            var mode = GetProximityMode(target);

            var rangeAndPadding = proximityRange + proximityPadding;
            var negativeRangeAndPadding = rangeAndPadding * -1;
            var doubleRangeAndPadding = rangeAndPadding * 2;
            var targetRenderWidth = target.RenderSize.Width;
            var targetRenderHeight = target.RenderSize.Height;
            var targetWidthAndPadding = targetRenderWidth + proximityPadding * 2;
            var targetHeightAndPadding = targetRenderHeight + proximityPadding * 2;

            Point point;
            Size size;
            if (mode == ProximityMode.Edge)
            {
                point = new Point(negativeRangeAndPadding, negativeRangeAndPadding);
                size = new Size(doubleRangeAndPadding + targetRenderWidth, doubleRangeAndPadding + targetRenderHeight);
            }
            else if (mode == ProximityMode.Center)
            {
                point = new Point(negativeRangeAndPadding + targetRenderWidth / 2, negativeRangeAndPadding + targetRenderHeight / 2);
                size = new Size(doubleRangeAndPadding, doubleRangeAndPadding);
            }
            else
            {
                throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");
            }

            return new Rect(point, size);
        }

        private static Rect GetTargetRect(FrameworkElement target)
        {
            var proximityPadding = GetProximityPadding(target);
            var targetWidthAndPadding = target.RenderSize.Width + proximityPadding * 2;
            var targetHeightAndPadding = target.RenderSize.Height + proximityPadding * 2;
            return new Rect(new Point(proximityPadding * -1, proximityPadding * -1), new Size(targetWidthAndPadding, targetHeightAndPadding));
        }

        private static double MeasureProximity(FrameworkElement target, Point pointerPosition, double xFromCenter, double yFromCenter)
        {
            var mode = GetProximityMode(target);
            var proximityPadding = GetProximityPadding(target);
            switch (mode)
            {
                case ProximityMode.Edge:
                    return Math.Max(xFromCenter - target.RenderSize.Width / 2, yFromCenter - target.RenderSize.Height / 2) - proximityPadding;
                case ProximityMode.Center:
                    return Math.Max(xFromCenter, yFromCenter) - proximityPadding;
                default:
                    throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");
            }
        }

        private static void Element_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;

            foreach (var target in _listeningElements[element])
            {
                var mode = GetProximityMode(target);
                var proximityPadding = GetProximityPadding(target);
                var proximityRange = GetProximityRange(target);
                var proximityRect = GetProximityRect(target);
                var targetRect = GetTargetRect(target);

                // Get and adjust current point x, y to be proximity from center.
                var position = e.GetCurrentPoint(target).Position;
                var x = Math.Abs(position.X - ((targetRect.Width / 2) - proximityPadding));
                var y = Math.Abs(position.Y - ((targetRect.Height / 2) - proximityPadding));

                double proximity;
                if (proximityRect.Contains(position))
                {
                    proximity = (mode == ProximityMode.Edge && targetRect.Contains(position)) ? 0 : MeasureProximity(target, position, x, y);
                }
                else
                {
                    var spreadMethod = GetProximitySpreadMethod(target);
                    switch(spreadMethod)
                    {
                        case ProximitySpreadMethod.Pad:
                            proximity = proximityRange;
                            break;
                        case ProximitySpreadMethod.Clamp:
                            proximity = 0;
                            break;
                        case ProximitySpreadMethod.Reflect:
                            var p = MeasureProximity(target, position, x, y);
                            var pModRange = p % proximityRange;
                            proximity = ((p - pModRange) / proximityRange % 2 == 0) ? pModRange : proximityRange - pModRange;
                            break;
                        case ProximitySpreadMethod.Repeat:
                            proximity = MeasureProximity(target, position, x, y) % proximityRange;
                            break;
                        default:
                            throw new Exception($"Invalid {nameof(ProximitySpreadMethod)}: {Enum.GetName(typeof(ProximitySpreadMethod), spreadMethod)}");
                    }
                }

                SetProximity(target, Math.Max(0, Convert.ToInt32(proximity)));
            }
        }
    }
}
