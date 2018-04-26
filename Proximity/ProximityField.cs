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
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void ProximityRangeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var element = sender as FrameworkElement;
            element.Loaded += (s, e) =>
            {
                if (args.NewValue == null || (int)args.NewValue <= 0)
                {
                    DeregisterPointerListener(element);
                }

                if ((int)args.NewValue != 0)
                {
                    RegisterPointerListener(element);
                }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        private static void RegisterPointerListener(FrameworkElement target)
        {
            var element = GetRootUIElement(target);

            if (!_listeningElements.TryGetValue(element, out var targets))
            {
                targets = new List<FrameworkElement>();
                element.PointerMoved += Element_PointerMoved;
                _listeningElements.Add(element, targets);
            }

            targets.Add(target);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        private static void DeregisterPointerListener(FrameworkElement target)
        {
            var element = GetRootUIElement(target);
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
                var mode = GetProximityMode(target);

                var proximityRect = (mode == ProximityMode.Edge)
                    ? new Rect(
                        new Point(proximityRange * -1, proximityRange * -1),
                        new Size(proximityRange * 2 + target.RenderSize.Width, proximityRange * 2 + target.RenderSize.Height))
                    : (mode == ProximityMode.Center) 
                        ? new Rect(
                            new Point(proximityRange * -1 + target.RenderSize.Width / 2, proximityRange * -1 + target.RenderSize.Height / 2),
                            new Size(proximityRange * 2, proximityRange * 2))
                        : throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");

                var targetRect = new Rect(new Point(0,0), target.RenderSize);

                var position = e.GetCurrentPoint(target).Position;
                var y = position.Y - targetRect.Height / 2;
                var x = position.X - targetRect.Width / 2;

                if (proximityRect.Contains(position))
                {
                    if (!targetRect.Contains(position))
                    {
                        if (mode == ProximityMode.Edge)
                        {
                            var proximity = Convert.ToInt32(Math.Max(Math.Abs(x) - targetRect.Width / 2, Math.Abs(y) - targetRect.Height / 2));
                            SetProximity(target, proximity);
                        }
                        else if (mode == ProximityMode.Center)
                        {
                            var proximity = Convert.ToInt32(Math.Max(Math.Abs(x), Math.Abs(y)));
                            SetProximity(target, proximity);
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
                            SetProximity(target, 0);
                        }
                        else if (mode == ProximityMode.Center)
                        {
                            var proximity = Convert.ToInt32(Math.Max(Math.Abs(x), Math.Abs(y)));
                            SetProximity(target, proximity);
                        }
                        else
                        {
                            throw new Exception($"Invalid ProximityMode: {Enum.GetName(typeof(ProximityMode), mode)}");
                        }
                    }
                }
                else
                {
                    SetProximity(target, proximityRange);
                }
            }
        }
    }
}
