﻿namespace Estreya.BlishHUD.Shared.Utils;

using Gw2Sharp;
using Gw2Sharp.WebApi;
using System.Reflection;

public static class Gw2SharpHelper
{
    public static RenderUrl CreateRenderUrl(IConnection connection, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return default;
        }

        ConstructorInfo ctor = typeof(RenderUrl).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[]
        {
            typeof(IGw2Client),
            typeof(string),
            typeof(string)
        }, null);

        return (RenderUrl)ctor.Invoke(new object[]
        {
            new Gw2Client(connection),
            url,
            connection.RenderBaseUrl
        });
    }
}