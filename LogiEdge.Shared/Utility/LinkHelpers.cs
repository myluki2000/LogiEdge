using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing.Template;

namespace LogiEdge.Shared.Utility
{
    public static class LinkHelpers
    {
        private static readonly Dictionary<Type, string> _routeCache = new();

        public static string RouteTo<T>() where T : IComponent
        {
            return GetRouteTemplate<T>();
        }

        public static string RouteTo<T>(params object[] parameters) where T : IComponent
        {
            if (parameters.Length % 2 != 0)
                throw new ArgumentException(
                    "Parameters list must be of the form [param1Name, param1Value, param2Name, param3Value] etc.");

            return RouteTo<T>(parameters.Chunk(2).ToDictionary(x => (string)x[0], x => x[1]));
        }

        public static string RouteTo<T>(Dictionary<string, object> parameters) where T : IComponent
        {
            RouteTemplate routeTemplate = TemplateParser.Parse(GetRouteTemplate<T>());

            List<string> partsResult = new();

            foreach (TemplateSegment segment in routeTemplate.Segments)
            {
                foreach (TemplatePart part in segment.Parts)
                {
                    if (part.IsLiteral)
                    {
                        partsResult.Add(part.Text);
                    }
                    else
                    {
                        if (part.Name == null)
                            throw new Exception("Name of part of route template " + routeTemplate.TemplateText + " was NULL.");

                        if (part.IsParameter && parameters.TryGetValue(part.Name, out object? value))
                        {
                            partsResult.Add(value.ToString());
                        }
                    }
                }
            }

            string result = string.Join("/", partsResult);

            if (!result.StartsWith("/"))
                result = "/" + result;

            return result;
        }

        private static string GetRouteTemplate<T>() where T : IComponent
        {
            if (_routeCache.TryGetValue(typeof(T), out string? link))
                return link;

            RouteAttribute? routeAttribute = typeof(T).GetCustomAttributes(typeof(RouteAttribute), false).FirstOrDefault() as RouteAttribute;

            if (routeAttribute == null)
                throw new Exception("Could not find route attribute for type " + typeof(T).FullName);

            _routeCache.Add(typeof(T), routeAttribute.Template);

            return routeAttribute.Template;
        }
    }
}
