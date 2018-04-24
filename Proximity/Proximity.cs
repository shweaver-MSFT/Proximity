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
        // A dict of top-level UIElements and the UIElements that are being listened for
        private static Dictionary<UIElement, List<UIElement>> _listeningElements = new Dictionary<UIElement, List<UIElement>>();

        public static readonly DependencyProperty ProximityProperty = DependencyProperty.RegisterAttached(
            "Proximity",
            typeof(int),
            typeof(UIElement),
            new PropertyMetadata(null));

        public static void SetProximity(UIElement element, int value)
        {
            element.SetValue(ProximityProperty, value);
        }

        public static int GetProximity(UIElement element)
        {
            return (int)element.GetValue(ProximityProperty);
        }

        public static readonly DependencyProperty ProximityRangeProperty = DependencyProperty.RegisterAttached(
            "ProximityRange",
            typeof(int),
            typeof(UIElement),
            new PropertyMetadata(0, new PropertyChangedCallback(ProximityRangeChanged)));

        public static void SetProximityRange(UIElement element, int value)
        {
            element.SetValue(ProximityRangeProperty, value);
        }

        public static int GetProximityRange(UIElement element)
        {
            return (int)element.GetValue(ProximityRangeProperty);
        }

        private static void ProximityRangeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var element = sender as FrameworkElement;
            element.Loaded += (s, e) =>
            {
                if (args.NewValue == null || (int)args.NewValue <= 0)
                {
                    DeregisterPointerListener(sender as UIElement);
                }

                if ((int)args.NewValue != 0)
                {
                    RegisterPointerListener(sender as UIElement);
                }
            };
        }

        private static void RegisterPointerListener(UIElement target)
        {
            var element = GetRootUIElement(target);

            if (!_listeningElements.TryGetValue(element, out var targets))
            {
                targets = new List<UIElement>();
                element.PointerMoved += Element_PointerMoved;
            }

            targets.Add(target);
            _listeningElements.Add(element, targets);
        }

        private static void DeregisterPointerListener(UIElement target)
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

        private static UIElement GetRootUIElement(DependencyObject target)
        {
            var root = target;
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
                var point = e.GetCurrentPoint(target);
                var position = point.Position;
                var y = position.Y;
                var x = position.X;

                var proximityRect = new Rect(new Point(proximityRange * -1, proximityRange * -1), new Size(proximityRange * 2 + target.RenderSize.Width, proximityRange * 2 + target.RenderSize.Height));
                var targetRect = new Rect(new Point(0,0), target.RenderSize);

                if (proximityRect.Contains(position) && !targetRect.Contains(position))
                {
                    var proximity = Convert.ToInt32(Math.Max(Math.Abs(x), Math.Abs(y)));
                    SetProximity(target, proximity);
                }
            }
        }
    }
}
