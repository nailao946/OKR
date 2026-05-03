using System;
using System.Collections.Generic;
using ME.Core;

namespace ME.Core
{
    public class NavigationService
    {
        private static NavigationService _instance;
        public static NavigationService Instance => _instance ?? (_instance = new NavigationService());

        private readonly Dictionary<string, Type> _pages = new Dictionary<string, Type>();

        public void RegisterPage(string key, Type pageType)
        {
            _pages[key] = pageType;
        }

        public Type GetPageType(string key)
        {
            return _pages.ContainsKey(key) ? _pages[key] : null;
        }
    }
}
