using UotanToolbox.Features;
using System;

namespace UotanToolbox.Services;

public class PageNavigationService
{
    public Action<Type>? NavigationRequested { get; set; }

    public void RequestNavigation<T>() where T : MainPageBase
    {
        NavigationRequested?.Invoke(typeof(T));
    }
}