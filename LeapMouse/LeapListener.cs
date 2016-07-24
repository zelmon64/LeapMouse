using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Leap;

class LeapListener
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);


    private Object thisLock = new Object();

    private void SafeWriteLine(String line)
    {
        lock (thisLock)
        {
            Console.WriteLine(line);
        }
    }

    public float CursorXPos;
    public float CursorYPos;
    public bool MouseOn;
    public void OnInit(Controller controller)
    {
        IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(h, 0);
        //controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
        SafeWriteLine("Initialized");
        CursorXPos = 0;
        CursorYPos = 0; 
        MouseOn = true;
    }

    public void OnConnect(object sender, DeviceEventArgs args)
    {
        Console.WriteLine("Connected");
    }

    public void OnDisconnect(object sender, DeviceEventArgs args)
    {
        Console.WriteLine("Disconnected");
    }

    public void OnExit(Controller controller)
    {
        SafeWriteLine("Exited");
    }

    public void OnServiceConnect(object sender, ConnectionEventArgs args)
    {
        Console.WriteLine("Service Connected");
    }

    public void OnServiceDisconnect(object sender, ConnectionLostEventArgs args)
    {
        Console.WriteLine("Service Disconnected");
    }

    public void OnServiceChange(Controller controller)
    {
        Console.WriteLine("Service Changed");
    }

    public void OnDeviceFailure(object sender, DeviceFailureEventArgs args)
    {
        Console.WriteLine("Device Error");
        Console.WriteLine("  PNP ID:" + args.DeviceSerialNumber);
        Console.WriteLine("  Failure message:" + args.ErrorMessage);
    }

    public void OnLogMessage(object sender, LogEventArgs args)
    {
        switch (args.severity)
        {
            case Leap.MessageSeverity.MESSAGE_CRITICAL:
                Console.WriteLine("[Critical]");
                break;
            case Leap.MessageSeverity.MESSAGE_WARNING:
                Console.WriteLine("[Warning]");
                break;
            case Leap.MessageSeverity.MESSAGE_INFORMATION:
                Console.WriteLine("[Info]");
                break;
            case Leap.MessageSeverity.MESSAGE_UNKNOWN:
                Console.WriteLine("[Unknown]");
                break;
        }
        Console.WriteLine("[{0}] {1}", args.timestamp, args.message);
    }

    public Int64 prevTime;
    public Int64 currentTime;
    public Int64 changeTime;
    public Frame currentFrame;
    public Frame prevFrame;
    //public override void OnFrame(Controller controller)
    public void OnFrame(object sender, FrameEventArgs args)
    {
        prevFrame = currentFrame;
        currentFrame = args.frame;
        currentTime = currentFrame.Timestamp;
        changeTime = currentTime - prevTime;

        //Console.WriteLine("Frame id: {0}, timestamp: {1}, hands: {2}, changeTime: {3}",
        //    currentFrame.Id, currentFrame.Timestamp, currentFrame.Hands.Count, changeTime);

        if (changeTime > 40000 && currentFrame.Hands.Count > 0 && prevFrame.Hands.Count > 0)
        {
            Hand hand = currentFrame.Hands[0];
            Hand prevHand = prevFrame.Hands[0];
            Vector currentPalmPosition = hand.StabilizedPalmPosition;
            Vector prevPalmPosition = prevHand.StabilizedPalmPosition;
            Vector PalmDir = hand.PalmNormal;
            InteractionBox interactionBox = currentFrame.InteractionBox;
            //Vector normalizedHandPosition = interactionBox.NormalizePoint(hand.PalmPosition);
            currentPalmPosition = interactionBox.NormalizePoint(hand.StabilizedPalmPosition);
            prevPalmPosition = interactionBox.NormalizePoint(prevHand.StabilizedPalmPosition);
            int prevExtendedFingers = 0;
            int ExtendedFingers = 0;
            int FingersConfig = 0;
            int prevFingersConfig = 0;
            int FingersMult;
            //Console.Write("HandID: " + hand.Id + ", Confidence:" + (hand.Confidence * 10.0) + ", Time visible: " + (int)hand.TimeVisible/1000000 + "\n");
            for (int f = 0; f < prevHand.Fingers.Count; f++)
            {
                Finger digit = prevHand.Fingers[f];
                if (digit.IsExtended)
                {
                    prevExtendedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < digit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    prevFingersConfig += FingersMult;
                }
            } 
            for (int f = 0; f < hand.Fingers.Count; f++)
            {
                Finger digit = hand.Fingers[f];
                if (digit.IsExtended)
                {
                    ExtendedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < digit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    FingersConfig += FingersMult;
                }
            }
            if (ExtendedFingers > 0)
            {
                //Console.Write("Finger configuration: " + FingersConfig + "\n");
            }
            //Console.Write("Visible fingers: " + hand.Fingers.Count + ", Finger configuration: " + FingersConfig + "\n");

            if (ExtendedFingers != prevExtendedFingers)
            {
                //Console.Write("Visible fingers: " + hand.Fingers.Count + ", Finger configuration: " + FingersConfig + "\n");
            }

            //Hand leapHand = frame.Hands [h];
            /*
            Vector handXBasis = hand.PalmNormal.Cross (hand.Direction).Normalized;
            Vector handYBasis = -hand.PalmNormal;
            Vector handZBasis = -hand.Direction;
            Vector handOrigin = hand.PalmPosition;
            currentFrame.
            Matrix handTransform = new Matrix (handXBasis, handYBasis, handZBasis, handOrigin);
            handTransform = handTransform.RigidInverse ();
            */
            int PressedFingers = 0;
            int PressedFingersConfig = 0;
            //int LowestDigit;
            //Vector AverageFingerPosiion = Vector.Zero;
            for (int f = 0; f < hand.Fingers.Count; f++)
            {
                //currentPalmPosition = interactionBox.NormalizePoint(hand.StabilizedPalmPosition);
                Finger leapFinger = hand.Fingers[f];
                /*
                Vector transformedPosition = handTransform.TransformPoint (leapFinger.TipPosition);
                Vector transformedDirection = handTransform.TransformDirection (leapFinger.Direction);
                */
                Vector transformedFingerPosition = interactionBox.NormalizePoint(leapFinger.StabilizedTipPosition);
                // Do something with the transformed fingers
                //AverageFingerPosiion += transformedFingerPosition - currentPalmPosition;
                if (transformedFingerPosition.y < currentPalmPosition.y - 0.05)
                {
                    PressedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < leapFinger.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    PressedFingersConfig += FingersMult;
                }
            }
            int prevPressedFingers = 0;
            int prevPressedFingersConfig = 0;
            for (int f = 0; f < prevHand.Fingers.Count; f++)
            {
                Finger Digit = prevHand.Fingers[f];
                Vector transformedFingerPosition = interactionBox.NormalizePoint(Digit.StabilizedTipPosition);
                if (transformedFingerPosition.y < currentPalmPosition.y - 0.08)
                {
                    prevPressedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < Digit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    prevPressedFingersConfig += FingersMult;
                }
            }
            //Console.Write("Pressed fingers: " + PressedFingers + ", Pressed fingers config: " + PressedFingersConfig + "\n");

            if (PressedFingers != prevPressedFingers)
            {
                Console.Write("Pressed fingers: " + PressedFingers + ", Pressed fingers config: " + PressedFingersConfig + "\n");
            }

            if (PressedFingers == 1 && prevPressedFingers == 1)
            {
                //float PalmPitch = interactionBox.NormalizePoint(hand.StabilizedPalmPosition.Pitch);
                //float PalmPitch = currentPalmPosition.Pitch;
                //float prevPalmPitch = prevPalmPosition.Pitch;
                /*float PalmYaw = currentPalmPosition.Yaw;
                float prevPalmYaw = prevPalmPosition.Yaw;
                float PalmRoll = currentPalmPosition.Roll;
                float prevPalmRoll = prevPalmPosition.Roll;*/
                /*
                double dPalmPitch = currentPalmPosition.Pitch - prevPalmPosition.Pitch;
                double dPalmYaw = currentPalmPosition.Yaw - prevPalmPosition.Yaw;
                double dPalmRoll = currentPalmPosition.Roll - prevPalmPosition.Roll;
                */
                double dPalmPitch = currentPalmPosition.x - prevPalmPosition.x;
                double dPalmYaw = currentPalmPosition.y - prevPalmPosition.y;
                double dPalmRoll = currentPalmPosition.z - prevPalmPosition.z;
                
                /*
                double dPalmPitch = hand.PalmNormal.Pitch - prevHand.PalmNormal.Pitch;
                double dPalmYaw = hand.PalmNormal.Yaw - prevHand.PalmNormal.Yaw;
                double dPalmRoll = hand.PalmNormal.Roll - prevHand.PalmNormal.Roll;
                */
                double RotateMin = 0.001;
                double pitchmult = 1;

                if (dPalmPitch * dPalmPitch + dPalmYaw * dPalmYaw + dPalmRoll * dPalmRoll < RotateMin * RotateMin)
                {
                    Console.Write("Digit Direction: Stationary\n");
                }
                else if (dPalmPitch > RotateMin * pitchmult)
                {
                    Console.Write("Digit Direction: Pitch\n");
                }
                else if (dPalmPitch < -RotateMin * pitchmult)
                {
                    Console.Write("Digit Direction: -Pitch\n");
                }
                else if (dPalmYaw > RotateMin)
                {
                    Console.Write("Digit Direction: Yaw\n");
                }
                else if (dPalmYaw < -RotateMin)
                {
                    Console.Write("Digit Direction: -Yaw\n");
                }
                else if (dPalmRoll > RotateMin)
                {
                    Console.Write("Digit Direction: Roll\n");
                }
                else if (dPalmRoll < -RotateMin)
                {
                    Console.Write("Digit Direction: -Roll\n");
                }
            }

            /*
            if (PressedFingers == 1 && prevPressedFingers == 1)
            {
                //Vector DigitPosition = Vector.Zero;
                //Vector prevDigitPosition = Vector.Zero;
                Vector DigitVelocity = Vector.Zero;
                Vector prevDigitVelocity = Vector.Zero;
                int DigitID;
                if (PressedFingersConfig == 1)
                {
                    DigitID = 0;
                    //DigitPosition = interactionBox.NormalizePoint(hand.Fingers[DigitID].StabilizedTipPosition);
                    //prevDigitPosition = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].StabilizedTipPosition);
                    //DigitVelocity = hand.Fingers[0].TipVelocity;
                    DigitVelocity = interactionBox.NormalizePoint(hand.Fingers[DigitID].TipVelocity);
                    prevDigitVelocity = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].TipVelocity);
                }
                else if (PressedFingersConfig == 2)
                {
                    DigitID = 1;
                    //DigitPosition = interactionBox.NormalizePoint(hand.Fingers[DigitID].StabilizedTipPosition);
                    //prevDigitPosition = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].StabilizedTipPosition);
                    //DigitVelocity = hand.Fingers[1].TipVelocity;
                    DigitVelocity = interactionBox.NormalizePoint(hand.Fingers[DigitID].TipVelocity);
                    prevDigitVelocity = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].TipVelocity);
                }
                else if (PressedFingersConfig == 4)
                {
                    DigitID = 2;
                    //DigitPosition = interactionBox.NormalizePoint(hand.Fingers[DigitID].StabilizedTipPosition);
                    //prevDigitPosition = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].StabilizedTipPosition);
                    //DigitVelocity = hand.Fingers[2].TipVelocity;
                    DigitVelocity = interactionBox.NormalizePoint(hand.Fingers[DigitID].TipVelocity);
                    prevDigitVelocity = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].TipVelocity);
                }
                else if (PressedFingersConfig == 8)
                {
                    DigitID = 3;
                    //DigitPosition = interactionBox.NormalizePoint(hand.Fingers[DigitID].StabilizedTipPosition);
                    //prevDigitPosition = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].StabilizedTipPosition);
                    //DigitVelocity = hand.Fingers[3].TipVelocity;
                    //DigitVelocity = interactionBox.NormalizePoint(hand.Fingers[DigitID].TipVelocity);
                    //prevDigitVelocity = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].TipVelocity);
                    DigitVelocity = interactionBox.NormalizePoint(hand.PalmVelocity);
                    prevDigitVelocity = interactionBox.NormalizePoint(prevHand.PalmVelocity);
                }
                else if (PressedFingersConfig == 16)
                {
                    DigitID = 4;
                    //DigitPosition = interactionBox.NormalizePoint(hand.Fingers[DigitID].StabilizedTipPosition);
                    //prevDigitPosition = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].StabilizedTipPosition);
                    //DigitVelocity = hand.Fingers[4].TipVelocity;
                    DigitVelocity = interactionBox.NormalizePoint(hand.Fingers[DigitID].TipVelocity);
                    prevDigitVelocity = interactionBox.NormalizePoint(prevHand.Fingers[DigitID].TipVelocity);
                }

                int DigitDirection;
                double minVelocity = 0.465;
                //if (DigitVelocity == Vector.Zero || ((DigitVelocity.z - prevDigitVelocity.z) * (DigitVelocity.x - prevDigitVelocity.x) + (DigitVelocity.y - prevDigitVelocity.y) * (DigitVelocity.y - prevDigitVelocity.y)) < minVelocity * minVelocity)
                if (DigitVelocity == Vector.Zero || (((DigitVelocity.z) * (DigitVelocity.x) + (DigitVelocity.y) * (DigitVelocity.y)) < minVelocity * minVelocity && ((prevDigitVelocity.z) * (prevDigitVelocity.x) + (prevDigitVelocity.y) * (prevDigitVelocity.y)) > minVelocity * minVelocity))
                {
                    Console.Write("Digit Direction: Stationary\n");
                    DigitDirection = 0;
                }
                else if (DigitVelocity.x > prevDigitVelocity.x + minVelocity)
                {
                    Console.Write("Digit Direction: Left\n");
                    DigitDirection = 1;
                }
                else if (DigitVelocity.x < prevDigitVelocity.x - minVelocity)
                {
                    Console.Write("Digit Direction: Right\n");
                    DigitDirection = -1;
                }
                else if (DigitVelocity.z > prevDigitVelocity.z + minVelocity)
                {
                    Console.Write("Digit Direction: Up\n");
                    DigitDirection = 2;
                }
                else if (DigitVelocity.z < prevDigitVelocity.z - minVelocity)
                {
                    Console.Write("Digit Direction: Down\n");
                    DigitDirection = -2;
                }
                else if (DigitVelocity.y > prevDigitVelocity.y + minVelocity)
                {
                    Console.Write("Digit Direction: Forward\n");
                    DigitDirection = 3;
                }
                else if (DigitVelocity.y < prevDigitVelocity.y - minVelocity)
                {
                    Console.Write("Digit Direction: Backward\n");
                    DigitDirection = -3;
                }
                /*
                if (DigitVelocity == Vector.Zero || ((DigitVelocity.x - prevDigitVelocity.x) * (DigitVelocity.x - prevDigitVelocity.x) + (DigitVelocity.y - prevDigitVelocity.y) * (DigitVelocity.y - prevDigitVelocity.y)) < minVelocity * minVelocity)//((DigitVelocity.x - prevDigitVelocity.x) < minVelocity && (DigitVelocity.y - prevDigitVelocity.y) < minVelocity))// && (DigitVelocity.z - prevDigitVelocity.z) < minVelocity))
                {
                    Console.Write("Digit Direction: Stationary\n");
                    DigitDirection = 0;
                }
                else if (DigitVelocity.x > DigitVelocity.y + minVelocity)// && DigitVelocity.x > DigitVelocity.z)
                {
                    Console.Write("Digit Direction: Left\n");
                    DigitDirection = 1;
                }
                else if (DigitVelocity.x < DigitVelocity.y - minVelocity)// && DigitVelocity.x < DigitVelocity.z)
                {
                    Console.Write("Digit Direction: Right\n");
                    DigitDirection = -1;
                }
                else if (DigitVelocity.y > DigitVelocity.x + minVelocity)// && DigitVelocity.y > DigitVelocity.z)
                {
                    Console.Write("Digit Direction: Up\n");
                    DigitDirection = 2;
                }
                else if (DigitVelocity.y < DigitVelocity.x - minVelocity)// && DigitVelocity.y < DigitVelocity.z)
                {
                    Console.Write("Digit Direction: Down\n");
                    DigitDirection = -2;
                }/*
                else if (DigitVelocity.z > DigitVelocity.y && DigitVelocity.z > DigitVelocity.x)
                {
                    Console.Write("Digit Direction: Backwards\n");
                    DigitDirection = 3;
                }
                else if (DigitVelocity.z < DigitVelocity.y && DigitVelocity.z < DigitVelocity.x)
                {
                    Console.Write("Digit Direction: Forwards\n");
                    DigitDirection = -3;
                }*
            }*/
            
            float PalmPosX = currentPalmPosition.x;
            float PalmPosY = currentPalmPosition.y;
            float prePalmPosX = prevPalmPosition.x;
            float prePalmPosY = prevPalmPosition.y;
            float changePalmPosX = PalmPosX - prePalmPosX;
            float changePalmPosY = PalmPosY - prePalmPosY;
            int changemax = 100;
            double scalingFactor = 15000;
            if (currentPalmPosition.z < 0.999 && currentPalmPosition.z > 0.001)// && hand.IsRight && prevHand.IsRight)
            {
                if (!MouseOn)
                {
                    if (FingersConfig == 31 && prevFingersConfig == 3)
                    {
                        MouseOn = true;
                        CursorXPos = 1000;
                        CursorYPos = 500;
                    }
                }
                else
                {
                    if (FingersConfig == 1 && prevFingersConfig == 3 && PalmDir.Roll < 1 && PalmDir.Roll > -1)
                    {
                        MouseOn = false;
                        Console.Write("Mouse off \n");
                    }
                    else if (ExtendedFingers > 3 && ExtendedFingers == prevExtendedFingers)
                    {

                        if (changePalmPosX < changemax && changePalmPosY < changemax && PalmDir.Roll < 1 && PalmDir.Roll > -1 && PalmDir.Pitch < -1.4)
                        {
                            CursorXPos = CursorXPos + (int)(changePalmPosX * scalingFactor);
                            CursorYPos = CursorYPos - (int)(changePalmPosY * scalingFactor);
                        }
                        else if (ExtendedFingers == 5 && (PalmDir.Roll > 2 || PalmDir.Roll < -2))
                        {
                            CursorXPos = 1000;
                            CursorYPos = 500;
                        }
                        SetCursorPos((int)CursorXPos, (int)CursorYPos);
                    }
                    else if (ExtendedFingers == 4 && prevExtendedFingers == 5 && PalmDir.Pitch < -1.4)
                    {
                        for (int f = 0; f < hand.Fingers.Count; f++)
                        {
                            Finger digit = hand.Fingers[f];
                            Finger prevdigit = prevHand.Fingers[f];
                            if (!digit.IsExtended && prevdigit.IsExtended)
                            {
                                if (digit.Type == Finger.FingerType.TYPE_INDEX)
                                {
                                    mouse_event(0x0002, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_MIDDLE)
                                {
                                    mouse_event(0x0008, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_THUMB)
                                {
                                    mouse_event(0x0020, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_RING)
                                {
                                    mouse_event(0x0080, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_PINKY)
                                {
                                    mouse_event(0x0080, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                                }
                            }
                        }

                    }
                    else if (ExtendedFingers == 5 && prevExtendedFingers == 4)
                    {
                        for (int f = 0; f < hand.Fingers.Count; f++)
                        {
                            Finger digit = hand.Fingers[f];
                            Finger prevdigit = prevHand.Fingers[f];
                            if (digit.IsExtended && !prevdigit.IsExtended)
                            {
                                if (digit.Type == Finger.FingerType.TYPE_INDEX)
                                {
                                    mouse_event(0x0004, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_MIDDLE)
                                {
                                    mouse_event(0x0010, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_THUMB)
                                {
                                    mouse_event(0x0040, 0, (int)CursorXPos, (int)CursorYPos, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_RING)
                                {
                                    mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                                }
                                if (digit.Type == Finger.FingerType.TYPE_PINKY)
                                {
                                    mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                                }
                            }
                        }

                    }
                    Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Pitch + ", Fingers: " + FingersConfig + "\n");
                    //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + "\n");
                }
            }
            prevTime = currentTime;
        }
    }
}

