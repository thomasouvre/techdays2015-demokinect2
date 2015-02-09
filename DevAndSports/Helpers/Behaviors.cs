using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DevAndSports.Helpers
{
    public abstract class FrameworkElementBehavior : DependencyObject
    {
        public static FrameworkElementBehavior GetBehavior(DependencyObject obj)
        {
            return (FrameworkElementBehavior)obj.GetValue(BehaviorProperty);
        }

        public static void SetBehavior(DependencyObject obj, bool value)
        {
            obj.SetValue(BehaviorProperty, value);
        }

        public static readonly DependencyProperty BehaviorProperty =
            DependencyProperty.RegisterAttached("Behavior", typeof(FrameworkElementBehavior), typeof(FrameworkElementBehavior), new PropertyMetadata(null, OnBehaviorChanged));

        private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;
            (e.OldValue as FrameworkElementBehavior)?.Detach(element);
            (e.NewValue as FrameworkElementBehavior)?.Attach(element);
        }

        public virtual void Attach(FrameworkElement element)
        {
        }

        public virtual void Detach(FrameworkElement element)
        {
        }
    }
}
