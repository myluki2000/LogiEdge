using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace LogiEdge.WebUI.Shared.Shared
{
    public static class ExtensionMethods
    {
        extension(NavigationManager nav)
        {
            public void NavigateToRoute<T>() where T : IComponent
            {
                nav.NavigateTo(RouteTo<T>());
            }

            public void NavigateToRoute<T>(params object[] parameters) where T : IComponent
            {
                nav.NavigateTo(RouteTo<T>(parameters));
            }

            public void NavigateToRoute<T>(Dictionary<string, object> parameters) where T : IComponent
            {
                nav.NavigateTo(RouteTo<T>(parameters));
            }
        }
    }
}
