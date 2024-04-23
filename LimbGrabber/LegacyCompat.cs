using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Koneko;
internal class LegacyCompat
{
    public static void Initialize()
    {
        // https://stackoverflow.com/a/16324781
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
    }

    private static Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
    {
        if (LimbGrabber.Debug.Value)
        {
            MelonLogger.Msg($"LimbGrabber AssemblyResolveEventHandler: {args}");
        }
        if (args.Name.StartsWith("CVRLimbsGrabber"))
        {
            MelonLogger.Msg($"Redirecting failing assembly load for {args.Name} to {Assembly.GetExecutingAssembly().FullName}");
            return Assembly.GetExecutingAssembly();
        }
        return null;
    }
}

