using HarmonyLib;
using MelonLoader;

// TODO: Putting these in this separate file is a hacky workaround, remove it when source generation is a thing

[assembly: MelonGame(null, "ChilloutVR")]
[assembly: MelonInfo(typeof(Koneko.LimbGrabber), Koneko.MyPluginInfo.PLUGIN_NAME, Koneko.MyPluginInfo.PLUGIN_VERSION, "Exterrata, Puff Machine")]
[assembly: MelonAdditionalCredits("Khodrin")]
//[assembly: MelonAdditionalDependencies("DesktopVRIK")]
[assembly: MelonOptionalDependencies("PlayerRagdollMod")]
[assembly: HarmonyDontPatchAll]