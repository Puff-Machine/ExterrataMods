﻿using System.Collections.Generic;
using System;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using RootMotion.FinalIK;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.Movement;
using ABI_RC.Core.Util.AssetFiltering;
using System.Linq;
using System.Runtime.CompilerServices;
#if BIE
using BepInEx;
#endif

#if ML
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonInfo(typeof(Koneko.LimbGrabber), "LimbGrabber", "1.3.0", "Exterrata, Puff Machine")]
//[assembly: MelonAdditionalDependencies("DesktopVRIK")]
[assembly: MelonOptionalDependencies("ml_prm", "BTKUILib")]
[assembly: HarmonyDontPatchAll]
#endif

namespace Koneko;

#if BIE
[BepInDependency("BTKUILib")]
[BepInPlugin("LimbGrabber", "LimbGrabber", "1.3.0")]
public class LimbGrabber : HybridMod
#elif ML
public class LimbGrabber : MelonMod
#else
#error Modloader not defined!
#endif
{
    public static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory("LimbGrabber");
    public static readonly MelonPreferences_Entry<bool> Enabled = Category.CreateEntry<bool>("Enabled", true, description: "Enable LimbGrabber");
    public static readonly MelonPreferences_Entry<bool> EnableHands = Category.CreateEntry<bool>("EnableHands", true, "Enable Hands", "Allow your hands to be grabbed");
    public static readonly MelonPreferences_Entry<bool> EnableFeet = Category.CreateEntry<bool>("EnableFeet", true, "Enable Feet", "Allow your feet to be grabbed");
    public static readonly MelonPreferences_Entry<bool> EnableHead = Category.CreateEntry<bool>("EnableHead", true, "Enable Head", "Allow your head to be grabbed");
    public static readonly MelonPreferences_Entry<bool> EnableHip = Category.CreateEntry<bool>("EnableHip", true, "Enable Hip", "Allow your hip to be grabbed");
    public static readonly MelonPreferences_Entry<bool> EnableRoot = Category.CreateEntry<bool>("EnableRoot", true, "Enable Root", "Allow your entire body to be grabbed from the root");
    public static readonly MelonPreferences_Entry<bool> EnablePose = Category.CreateEntry<bool>("EnablePosing", true, "Enable Posing", "Allow posing bones in place");
    public static readonly MelonPreferences_Entry<bool> PreserveMomentum = Category.CreateEntry<bool>("PreserveMomentum", true, "Preserve Momentum", "Keep your velocity when thrown by the root");
    public static readonly MelonPreferences_Entry<bool> Friend = Category.CreateEntry<bool>("FriendsOnly", true, "Friends Only", "Only allow friends to grab you");
    public static readonly MelonPreferences_Entry<bool> RagdollRelease = Category.CreateEntry<bool>("RagdollOnRelease", true, "Ragdoll", "Ragdoll when your root bone is released");
    public static readonly MelonPreferences_Entry<bool> StayGrounded = Category.CreateEntry<bool>("StayGrounded", true, "Stay Grounded", "Stay in a grounded state while your root bone is being grabbed");
    public static readonly MelonPreferences_Entry<bool> LockNeck = Category.CreateEntry<bool>("LockNeck", true, "Lock Neck", "Lock the relative position of the neck bone while grabbed by the root, disable this if not being able to translate your head makes you want to fall over.");
    public static readonly MelonPreferences_Entry<bool> Debug = Category.CreateEntry<bool>("Debug", false, "Debug", "Enable additional logging");
    public static readonly MelonPreferences_Entry<float> VelocityMultiplier = Category.CreateEntry<float>("VelocityMultiplier", 1f, "Velocity Multiplier", "Multiply your velocity when thrown");
    public static readonly MelonPreferences_Entry<float> GravityMultiplier = Category.CreateEntry<float>("GravityMultiplier", 1f, "Gravity Multiplier", "Multiply your gravity when thrown");
    public static readonly MelonPreferences_Entry<float> Distance = Category.CreateEntry<float>("GrabDistance", 0.15f, "Grab Distance", "From how far away should each point be grabbable");
    public static readonly MelonPreferences_Entry<float> MinRagdollSpeed = Category.CreateEntry<float>("MinRagdollSpeed", 0.2f, "Minimum Ragdoll Speed", "Only ragdoll when thrown faster than this speed");

    public static MelonPreferences_Entry<bool>[] enabled;
    public static bool[] tracking;
    public static Limb[] Limbs;
    public static Transform PlayerLocal;
    public static Transform Neck;
    public static Transform RootParent;
    public static List<Transform> AdditionalRootPoints;
    public static bool RootGrabbed;
    public static Vector3 NeckOffset;
    public static Vector3 RootOffset;
    public static Vector3 LastRootPosition;
    public static Vector3[] AverageVelocities;
    public static int VelocityIndex;
    public static IKSolverVR IKSolver;
    public static bool Initialized;
    public static bool IsAirborn;
    public static bool WasRagdolled;
    public static bool PrmExists;
    public static bool BTKExists;

    public struct Limb
    {
        public Transform limb;
        public Transform Parent;
        public Transform Target;
        public Transform PreviousTarget;
        public Quaternion RotationOffset;
        public Vector3 PositionOffset;
        public bool Grabbed;
        public List<Transform> AdditionalPoints;
    }

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Starting");
        tracking = new bool[6];
        AverageVelocities = new Vector3[5];
        enabled = new MelonPreferences_Entry<bool>[7] {
            EnableHands,
            EnableFeet,
            EnableHands,
            EnableFeet,
            EnableHead,
            EnableHip,
            EnableRoot
        };
        try {
            HarmonyInstance.PatchAll(typeof(Patches));
        } catch(Exception e) { 
            MelonLogger.Error(e);
        }

        InitWhitelist();

        WhitelistComponent(typeof(GrabberComponent));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InitWhitelist()
    {
        // Before whitelisting any components call the public getters once to ensure they are initialised
        _ = SharedFilter.SpawnableWhitelist;
        _ = SharedFilter.AvatarWhitelist;
    }

    public static void WhitelistComponent(Type type)
    {
        var propWhitelist = Traverse.Create(typeof(SharedFilter)).Field<HashSet<Type>>("_spawnableWhitelist").Value;
        propWhitelist.Add(type);

        var avatarWhitelist = Traverse.Create(typeof(SharedFilter)).Field<HashSet<Type>>("_avatarWhitelist").Value;
        avatarWhitelist.Add(type);
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (Debug.Value) MelonLogger.Msg($"OnSceneWasInitialized was called, buildIndex={buildIndex}");
        if (buildIndex == 2)
        {
            Limbs = new Limb[6];
            PlayerLocal = GameObject.Find("_PLAYERLOCAL").transform;
            AdditionalRootPoints = new List<Transform>();

            for (int i = 0; i < Limbs.Length; i++)
            {
                Limbs[i].AdditionalPoints = new List<Transform>();
                
                var limb = new GameObject("LimbGrabberTarget").transform;
                Limbs[i].Target = limb;
                limb.parent = PlayerLocal;
            }
            //if (RegisteredMelons.Any(it => it.Info.Name == "PlayerRagdollMod"))
            if(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "ml_prm"))
            {
                RagdollSupport.Initialize();
                PrmExists = true;
            }
            //if (RegisteredMelons.Any(it => it.Info.Name == "BTKUILib"))
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "BTKUILib"))
            {
                BTKUISupport.Initialize();
                BTKExists = true;
            }

            LegacyCompat.Initialize();
        }
    }

    public override void OnUpdate()
    {
        if (!Initialized || !Enabled.Value) return;
        for (int i = 0; i < Limbs.Length; i++)
        {
            if (Limbs[i].Grabbed && Limbs[i].Parent != null)
            {
                Vector3 offset = Limbs[i].Parent.rotation * Limbs[i].PositionOffset;
                Limbs[i].Target.position = Limbs[i].Parent.position + offset;
                Limbs[i].Target.rotation = Limbs[i].Parent.rotation * Limbs[i].RotationOffset;
            }
        }
        if (EnableRoot.Value && Neck != null && BetterBetterCharacterController.Instance.FlightAllowedInWorld)
        {
            if (PreserveMomentum.Value)
            {
                AverageVelocities[VelocityIndex] = PlayerLocal.position - LastRootPosition;
                LastRootPosition = PlayerLocal.position;
                VelocityIndex++;
                if (VelocityIndex == AverageVelocities.Length)
                {
                    VelocityIndex = 0;
                }
            }
            if (RootGrabbed)
            {
                if (LockNeck.Value)
                    PlayerLocal.position = RootParent.position + NeckOffset + (PlayerLocal.position - Neck.position);
                else
                    PlayerLocal.position = RootParent.position + RootOffset;
            }
        }
    }

    public static void Grab(GrabberComponent grabber)
    {
        if (!Enabled.Value || !Initialized || BodySystem.isCalibrating) return;
        if (Debug.Value) MelonLogger.Msg("grab was detected");
        int closest = 0;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < 7; i++)
        {
            float dist = 0;
            if (i == 6) dist = Vector3.Distance(grabber.transform.position, Neck.position);
            else dist = Vector3.Distance(grabber.transform.position, Limbs[i].limb.position);
            if (dist < distance)
            {
                distance = dist;
                closest = i;
            }

            if (i == 6)
            {
                foreach (var additionPoint in AdditionalRootPoints)
                {
                    if (!additionPoint.gameObject.activeInHierarchy) continue;
                    dist = Vector3.Distance(grabber.transform.position, additionPoint.position);
                    if (dist < distance)
                    {
                        distance = dist;
                        closest = i;
                    }
                }
                
                continue;
            }
            
            foreach (var additionPoint in Limbs[i].AdditionalPoints)
            {
                if (!additionPoint.gameObject.activeInHierarchy) continue;
                dist = Vector3.Distance(grabber.transform.position, additionPoint.position);
                if (dist < distance)
                {
                    distance = dist;
                    closest = i;
                }
            }
        }
        if (distance < Distance.Value)
        {
            if (!enabled[closest].Value) return;
            if (closest == 6)
            {
                if (!BetterBetterCharacterController.Instance.FlightAllowedInWorld) return;
                grabber.Limb = closest;
                if (Debug.Value) MelonLogger.Msg("limb " + Neck.name + " was grabbed by " + grabber.transform.name);
                NeckOffset = Neck.position - grabber.transform.position;
                RootOffset = PlayerLocal.position - grabber.transform.position;
                RootParent = grabber.transform;
                BetterBetterCharacterController.Instance.SetImmobilized(true);
                RootGrabbed = true;
                IsAirborn = true;
                WasRagdolled = false;
                return;
            }
            grabber.Limb = closest;
            if (Debug.Value) MelonLogger.Msg("limb " + Limbs[closest].limb.name + " was grabbed by " + grabber.transform.name);
            Limbs[closest].PositionOffset = Quaternion.Inverse(grabber.transform.rotation) * (Limbs[closest].limb.position - grabber.transform.position);
            Limbs[closest].RotationOffset = Quaternion.Inverse(grabber.transform.rotation) * Limbs[closest].limb.rotation;
            Limbs[closest].Parent = grabber.transform;
            Limbs[closest].Grabbed = true;
            SetTarget(closest, Limbs[closest].Target);
            SetTracking(closest, true);
        }
    }

    public static void Pose(GrabberComponent grabber)
    {
        int limb = grabber.Limb;
        if (limb == -1) return;
        if (limb == 6 || !EnablePose.Value || !Initialized)
        {
            Release(grabber);
            return;
        }
        grabber.Limb = -1;
        if (grabber.transform != Limbs[limb].Parent) return;
        if (Debug.Value) MelonLogger.Msg("limb " + Limbs[limb].limb.name + " was posed by " + grabber.transform.name);
        Limbs[limb].Grabbed = false;
    }

    public static void Release(GrabberComponent grabber)
    {
        int limb = grabber.Limb;
        if (limb == -1) return;
        grabber.Limb = -1;
        if (limb == 6)
        {
            if (grabber.transform != RootParent) return;
            if (Debug.Value) MelonLogger.Msg("limb " + Neck.name + " was released by " + grabber.transform.name);
            BetterBetterCharacterController.Instance.SetImmobilized(false);
            Vector3 Velocity = Vector3.zero;
            if (PreserveMomentum.Value)
            {
                for (int i = 0; i < AverageVelocities.Length; i++)
                {
                    Velocity += AverageVelocities[i];
                }
                Velocity /= AverageVelocities.Length;
                Velocity *= VelocityMultiplier.Value * 100;
                BetterBetterCharacterController.Instance.LaunchCharacter(Velocity);
            }
            RootGrabbed = false;
            if (PrmExists && RagdollRelease.Value && (Velocity.magnitude > MinRagdollSpeed.Value || !PreserveMomentum.Value))
            {
                RagdollSupport.ToggleRagdoll();
                WasRagdolled = true;
            }
            return;
        }
        if (grabber.transform != Limbs[limb].Parent) return;
        if (Debug.Value) MelonLogger.Msg("limb " + Limbs[limb].limb.name + " was released by " + grabber.transform.name);
        Limbs[limb].Grabbed = false;
        SetTarget(limb, Limbs[limb].PreviousTarget);
        if (!tracking[limb]) SetTracking(limb, false);
    }

    public static void ReleaseAll()
    {
        if (Debug.Value) MelonLogger.Msg("releasing all limbs");
        for (int i = 0; i < Limbs.Length; i++)
        {
            Limbs[i].Grabbed = false;
            Limbs[i].Parent = null;
            SetTarget(i, Limbs[i].PreviousTarget);
            if (!tracking[i]) SetTracking(i, false);
        }
    }

    public static void SetTarget(int index, Transform Target)
    {
        switch (index)
        {
            case 0:
                IKSolver.leftArm.target = Target;
                break;
            case 1:
                IKSolver.leftLeg.target = Target;
                break;
            case 2:
                IKSolver.rightArm.target = Target;
                break;
            case 3:
                IKSolver.rightLeg.target = Target;
                break;
            case 4:
                IKSolver.spine.headTarget = Target;
                break;
            case 5:
                IKSolver.spine.pelvisTarget = Target;
                break;
        }
    }

    public static void SetTracking(int index, bool value)
    {
        switch (index)
        {
            case 0:
                BodySystem.TrackingLeftArmEnabled = value;
                break;
            case 1:
                BodySystem.TrackingLeftLegEnabled = value;
                break;
            case 2:
                BodySystem.TrackingRightArmEnabled = value;
                break;
            case 3:
                BodySystem.TrackingRightLegEnabled = value;
                break;
            case 4:
                IKSolver.spine.positionWeight = value ? 1 : 0;
                break;
            case 5:
                IKSolver.spine.pelvisPositionWeight = value ? 1 : 0;
                break;
        }
    }
}