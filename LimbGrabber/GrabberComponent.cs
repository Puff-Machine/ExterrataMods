using System.Collections.Generic;
using System;
using UnityEngine;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;
using MelonLoader;

namespace Koneko;
public class GrabberComponent : MonoBehaviour
{
    internal PlayerAvatarMovementData MovementData;
    internal PlayerDescriptor PlayerDescriptor;
    internal int grabber = 0;
    internal int Limb = -1;
    internal int Gesture;
    public bool Grab;

    void Update()
    {
        int gesture = 0;
        if (grabber == 0) gesture = Grab ? 1 : 0;
        else if (!Friends.FriendsWith(PlayerDescriptor.ownerId) && LimbGrabber.Friend.Value) return;
        else if (grabber == 1) {
            if((int)MovementData.AnimatorGestureLeft == 1 || MovementData.LeftMiddle1Stretched < -0.2 && MovementData.LeftThumb1Stretched < -0.2) gesture = 1; 
            else if((int)MovementData.AnimatorGestureLeft == 2 || MovementData.LeftMiddle1Stretched < -0.2 && MovementData.LeftThumb1Stretched > 0.2) gesture = 2;
        }
        else if (grabber == 2) { 
            if((int)MovementData.AnimatorGestureRight == 1 || MovementData.RightMiddle1Stretched < -0.2 && MovementData.RightThumb1Stretched < -0.2) gesture = 1;
            else if((int)MovementData.AnimatorGestureRight == 2 || MovementData.RightMiddle1Stretched < -0.2 && MovementData.RightThumb1Stretched > 0.2) gesture = 2;
        }

        if (gesture == 1 && Gesture != 1) // fist
        {
            LimbGrabber.Grab(this); 
        }
        if (gesture == 0 && Gesture == 1)
        {
            LimbGrabber.Release(this);
        }
        if (gesture == 2 && Gesture != 2) // thumbs up
        {
            LimbGrabber.Pose(this);
        }
        Gesture = gesture;
    }

    void OnDestroy()
    {
        LimbGrabber.Release(this);
    }
}