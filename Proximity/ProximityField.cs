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
        // A dict of top-level UIElements and the FrameworkElement that are being listened for
        private static Dictionary<UIElement, List<FrameworkElement>> _listeningElements = new Dictionary<UIElement, List<FrameworkElement>>();

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityProperty = DependencyProperty.RegisterAttached(
            "Proximity",
            typeof(int),
            typeof(DependencyObject),
            new PropertyMetadata(null));

        public static void SetProximity(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityProperty, value);
        }

        public static int GetProximity(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityRangeProperty = DependencyProperty.RegisterAttached(
            "ProximityRange",
            typeof(int),
            typeof(DependencyObject),
            new PropertyMetadata(0, new PropertyChangedCallback(ProximityRangeChanged)));

        public static void SetProximityRange(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityRangeProperty, value);
        }

        public static int GetProximityRange(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityRangeProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityModeProperty = DependencyProperty.RegisterAttached(
            nameof(ProximityMode),
            typeof(ProximityMode),
            typeof(DependencyObject),
            new PropertyMetadata(ProximityMode.Edge, new PropertyChangedCallback(ProximityModeChanged)));

        public static void SetProximityMode(DependencyObject element, ProximityMode value)
        {
            (element as FrameworkElement).SetValue(ProximityModeProperty, value);
        }

        public static ProximityMode GetProximityMode(DependencyObject element)
        {
            return (ProximityMode)(element as FrameworkElement).GetValue(ProximityModeProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty ProximityPaddingProperty = DependencyProperty.RegisterAttached(
            "ProximityPadding",
            typeof(int),
            typeof(DependencyObject),
            new PropertyMetadata(0));

        public static void SetProximityPadding(DependencyObject element, int value)
        {
            (element as FrameworkElement).SetValue(ProximityPaddingProperty, value);
        }

        public static int GetProximityPadding(DependencyObject element)
        {
            return (int)(element as FrameworkElement).GetValue(ProximityPaddingProperty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void ProximityModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Element_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var element = sender as UIElement;

            foreach (var target in _listeningElements[element])
            {
                var proximityRange = GetProximityRange(target);
                var proximityPadding = GetProximityPadding(target);
                var mode = GetProximityMode(target);

                Point point;
                Size size;
                var rangeAndPadding = proximityRange + proximityPadding;
                var negativeRangeAndPadding = rangeAndPadding * -1;
                var doubleRangeAndPadding = rangeAndPadding * 2;
                var targetRenderWidth = target.RenderSize.Width;
                var targetRenderHeight = target.RenderSize.Height;
                var targetWidthAndPadding = targetRenderWidth + proximityPadding * 2;
                var targetHeightAndPadding = targetRenderHeight + proximityPadding * 2;

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

                var proximityRect = new Rect(point, size);
                var targetRect = new Rect(new Point(proximityPadding * -1, proximityPadding * -1), new Size(targetWidthAndPadding, targetHeightAndPadding));

                // Get and adjust current point x, y to be proximity from center.
                var position = e.GetCurrentPoint(target).Position;
                var y = Math.Abs(position.Y - ((targetRect.Height / 2) - proximityPadding));
                var x = Math.Abs(position.X - ((targetRect.Width / 2) - proximityPadding));

                int proximity;
                if (proximityRect.Contains(position))
                {
                    if (!targetRect.Contains(position))
                    {
                        if (mode == ProximityMode.Edge)
                        {
                            proximity = Convert.ToInt32(Math.Max(x - targetRenderWidth / 2, y - targetRenderHeight / 2)) - proximityPadding;
                        }
                        else if (mode == ProximityMode.Center)
                        {
                            proximity = Convert.ToInt32(Math.Max(x - proximityPadding, y - proximityPadding));
                        }
                        else
                        {
                            throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");
                        }
                    }
                    else
                    {
                        if (mode == ProximityMode.Edge)
                        {
                            System.Diagnostics.Debug.WriteLine($"In Prox - In Target: {x}");
                            proximity = 0;
                        }
                        else if (mode == ProximityMode.Center)
                        {
                            proximity = Convert.ToInt32(Math.Max(x, y)) - proximityPadding;
                        }
                        else
                        {
                            throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("$Out Prox - Out Target: {x}");
                    proximity = proximityRange;
                }

                SetProximity(target, Math.Max(0, proximity));
            }
        }
    }
}
