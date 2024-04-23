using BTKUILib;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using System;

namespace Koneko;
internal class BTKUISupport
{
    public static void Initialize()
    {
        var misc = QuickMenuAPI.MiscTabPage;
        var mainCatagory = misc.AddCategory("CVR Limbs Grabber");
        var limbPage = mainCatagory.AddPage("Enabled Limbs", "", "Enable And Disable Limbs", "CVRLimbsGrabber");
        var limbCatagory = limbPage.AddCategory("Enable And Disable Limbs");
        var settingPage = mainCatagory.AddPage("Settings", "", "LimbGrabber Settings", "CVRLimbsGrabber");
        var settingCatagory = settingPage.AddCategory("LimbGrabber Settings");

        AddToggle(ref limbCatagory, LimbGrabber.EnableHands);
        AddToggle(ref limbCatagory, LimbGrabber.EnableFeet);
        AddToggle(ref limbCatagory, LimbGrabber.EnableHead);
        AddToggle(ref limbCatagory, LimbGrabber.EnableHip);
        AddToggle(ref limbCatagory, LimbGrabber.EnableRoot);

        AddToggle(ref settingCatagory, LimbGrabber.Friend);
        AddToggle(ref settingCatagory, LimbGrabber.EnablePose);
        AddToggle(ref settingCatagory, LimbGrabber.PreserveMomentum);
        AddToggle(ref settingCatagory, LimbGrabber.StayGrounded);
        AddToggle(ref settingCatagory, LimbGrabber.LockNeck);
        AddToggle(ref settingCatagory, LimbGrabber.RagdollRelease).Disabled = !LimbGrabber.PrmExists;
        AddSlider(ref settingPage, LimbGrabber.MinRagdollSpeed, 0, 10).Disabled = !LimbGrabber.PrmExists;
        AddSlider(ref settingPage, LimbGrabber.VelocityMultiplier, 0.1f, 100, 1);
        AddSlider(ref settingPage, LimbGrabber.GravityMultiplier, 0, 100, 1);
        AddSlider(ref settingPage, LimbGrabber.Distance, 0.01f, 1);

        AddToggle(ref mainCatagory, LimbGrabber.Enabled);
        mainCatagory.AddButton("Release All", "", "Release All").OnPress += new Action(() => LimbGrabber.ReleaseAll());
    }

    private static ToggleButton AddToggle(ref Category category, MelonPreferences_Entry<bool> entry)
    {
        var toggle = category.AddToggle(entry.DisplayName, entry.Description, entry.Value);
        toggle.OnValueUpdated += b => entry.Value = b;
        return toggle;
    }

    private static SliderFloat AddSlider(ref Page page, MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    {
        var slider = page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces);
        slider.OnValueUpdated += f => entry.Value = f;
        return slider;
    }
}