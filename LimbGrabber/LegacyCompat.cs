using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Koneko;
internal sealed class LegacyCompat
{
    public static Assembly compatasm = null;
    public static void Initialize()
    {
        Assembly thisasm = Assembly.GetExecutingAssembly();
        Stream compatstream = null;
        try
        {
            compatstream = thisasm.GetManifestResourceStream(thisasm.GetManifestResourceNames().Single(n => n.EndsWith("CVRLimbsGrabber.dll")));
            if (compatstream is null) throw new Exception();
        }
        catch (Exception ex) 
        {
            if (LimbGrabber.Debug.Value)
            {
                MelonLogger.Error($"Legacy assembly resource not available {ex.ToString()} {ex.StackTrace}");
            }
        }

        if (compatstream != null)
        {
            MemoryStream ms = new MemoryStream();
            compatstream.CopyTo( ms );
            compatasm = Assembly.Load(ms.ToArray());
            MelonLogger.Msg($"Loaded compatasm = {compatasm}");
        }
        else
        {
            if (LimbGrabber.Debug.Value)
            {
                MelonLogger.Error($"compatstream is null!");
            }
        }
    }
}

