using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DevAndSports.Helpers
{
    public class GenericTemplateSelector<T> : DataTemplateSelector
    {
        private Dictionary<T, DataTemplate> _map = new Dictionary<T, DataTemplate>();
        private DataTemplate _defaultTemplate;
        private Func<object, T> _convertCallback;

        public GenericTemplateSelector(Dictionary<T, DataTemplate> map, DataTemplate defaultTemplate, Func<object, T> convertCallback = null)
        {
            _map = map;
            _defaultTemplate = defaultTemplate;
            _convertCallback = convertCallback;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = _convertCallback == null ? item : _convertCallback(item);
            if (data is T)
            {
                var tmp = (T)data;
                if (_map.ContainsKey(tmp)) return _map[tmp];
            }
            return _defaultTemplate;
        }
    }
}
