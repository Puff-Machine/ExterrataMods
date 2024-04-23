using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Koneko;
internal sealed class LegacyCompat
{
    //public static AssemblyBuilder assembly;
    //public static ModuleBuilder module;
    public static Assembly compatasm = null;
    public static void Initialize()
    {
        // this did not work, i guess it doesnt try to load the assembly
        // https://stackoverflow.com/a/16324781
        // AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
        
        // this never gets called either
        //AppDomain.CurrentDomain.TypeResolve += (object sender, ResolveEventArgs args) => { MelonLogger.Msg($"TypeResolve!: {args}"); return null; };
        
        // also no
        /*
        assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CVRLimbsGrabber"), AssemblyBuilderAccess.RunAndCollect);
        module = assembly.DefineDynamicModule("CVRLimbsGrabber");
        
        var grabbercomp = module.DefineType("Koneko.GrabberComponent", TypeAttributes.Public, typeof(GrabberComponent));
        grabbercomp.CreateType();
        */

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

    /*
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
    */
}

