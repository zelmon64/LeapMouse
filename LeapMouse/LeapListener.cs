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
    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

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
    public bool KbOn;
    public void OnInit(Controller controller)
    {
        IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(h, 0);
        //controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
        SafeWriteLine("Initialized");
        CursorXPos = 0;
        CursorYPos = 0;
        MouseOn = false;
        KbOn = false;
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

    public void PressVK(byte key)
    {
        keybd_event(key, 0, 0, 0);
        keybd_event(key, 0, 0x0002, 0);
    }

    public void DigitButtonEvent(string SymbolString, bool[] DeletePrevious,
        byte[] VKey) //byte VKey0, byte VKey1, byte VKey2, byte VKey3, byte VKey4, byte VKey5, byte VKey6)
    {
        //double TravMin = 0.0002;
        double TravMinNew = 300 * TravMin;
        //TravMinew /= 2;
        /*
        double TravMinButton1 = 0.5 * 2;
        double TravMinButton2 = 1.5 * 2;
        double TravMinButton3 = 2.5 * 2;
        */
        for (int f = 0; f < VKey.Length; f++)
        {
            if (DigitTravel.x < TravMinNew * (f + 1 - VKey.Length/2.0))
            {
                newDigitButton = f;
                if (DigitButton != newDigitButton)
                {
                    Console.Write(SymbolString[newDigitButton]);
                    //Console.Write("a");
                    //keybd_event(0x41, 0, 0x0002, 0);
                    if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                    PressVK(VKey[newDigitButton]);
                    //PressVK(VKey0);
                    //keybd_event(0x41, 0, 0, 0);
                    DigitButton = newDigitButton;
                }
                f = VKey.Length;
            }
        }
        /*
        if (DigitTravel.x < -TravMinNew * TravMinButton3)
        {
            newDigitButton = 0;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("a");
                //keybd_event(0x41, 0, 0x0002, 0);
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey0);
                //keybd_event(0x41, 0, 0, 0);
                DigitButton = newDigitButton;
            }
        }
        else if (DigitTravel.x < -TravMinNew * TravMinButton2)
        {
            newDigitButton = 1;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("b");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey1);
                //PressVK(0x42);
                DigitButton = newDigitButton;
            }
        }
        else if (DigitTravel.x < -TravMinNew * TravMinButton1)
        {
            newDigitButton = 2;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("c");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey2);
                //PressVK(0x43);
                DigitButton = newDigitButton;
            }
        }
        else if (DigitTravel.x < TravMinNew * TravMinButton1)
        {
            newDigitButton = 3;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("d");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey3);
                //PressVK(0x44);
                DigitButton = newDigitButton;
            }
        }
        else if (DigitTravel.x < TravMinNew * TravMinButton2)
        {
            newDigitButton = 4;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("e");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey4);
                //PressVK(0x45);
                DigitButton = newDigitButton;
            }
        }
        else if (DigitTravel.x < TravMinNew * TravMinButton3)
        {
            newDigitButton = 5;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("f");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey5);
                //PressVK(0x46);
                DigitButton = newDigitButton;
            }
        }
        else
        {
            newDigitButton = 6;
            if (DigitButton != newDigitButton)
            {
                Console.Write(SymbolString[newDigitButton]);
                //Console.Write("g");
                if (DigitButton != -10 && DeletePrevious[DigitButton]) { PressVK(0x08); }
                PressVK(VKey[newDigitButton]);
                //PressVK(VKey6);
                //PressVK(0x47);
                DigitButton = newDigitButton;
            }
        }*/
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
    public Vector DigitTravel;
    public Vector DigitOrig;
    public Int32 DigitButton;
    public Int32 newDigitButton;
    public double TravMin = 0.0002;
    //public override void OnFrame(Controller controller)
    public void OnFrame(object sender, FrameEventArgs args)
    {
        prevFrame = currentFrame;
        currentFrame = args.frame;
        currentTime = currentFrame.Timestamp;
        changeTime = currentTime - prevTime;
        /*
        Console.WriteLine("Frame id: {0}, timestamp: {1}, hands: {2}, changeTime: {3}",
            currentFrame.Id, currentFrame.Timestamp, currentFrame.Hands.Count, changeTime);
        */
        if (changeTime > 500 && currentFrame.Hands.Count > 0 && prevFrame.Hands.Count > 0)
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
            int PressedFingers = 0;
            int PressedFingersConfig = 0;
            int prevPressedFingers = 0;
            int prevPressedFingersConfig = 0;
            double pressedmin = 0.025;
            //pressedmin = 0.05;
            double pitchmax = -1.2;
            int DigitID = 0;
            int prevDigitID = 0;
            int eDigitID = 0;
            int preveDigitID = 0;
            //Console.Write("HandID: " + hand.Id + ", Confidence:" + (hand.Confidence * 10.0) + ", Time visible: " + (int)hand.TimeVisible/1000000 + "\n");
            
            for (int f = 0; f < hand.Fingers.Count; f++)
            {
                Finger digit = hand.Fingers[f];
                Finger prevdigit = prevHand.Fingers[f];

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
                else { eDigitID = f; }
                
                if (prevdigit.IsExtended)
                {
                    prevExtendedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < digit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    prevFingersConfig += FingersMult;
                }
                else { preveDigitID = f; }

                Vector transformedFingerPosition = interactionBox.NormalizePoint(digit.StabilizedTipPosition);
                if (transformedFingerPosition.y < currentPalmPosition.y - pressedmin)
                {
                    PressedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < digit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    PressedFingersConfig += FingersMult;
                    DigitID = f;
                }

                Vector prevtransformedFingerPosition = interactionBox.NormalizePoint(prevdigit.StabilizedTipPosition);
                if (prevtransformedFingerPosition.y < currentPalmPosition.y - pressedmin)
                {
                    prevPressedFingers++;
                    FingersMult = 1;
                    for (int m = 0; m < prevdigit.Id - hand.Id * 10; m++)
                    {
                        FingersMult *= 2;
                    }
                    prevPressedFingersConfig += FingersMult;
                    prevDigitID = f;
                }
            }
            
            float PalmPosX = currentPalmPosition.x;
            float PalmPosY = currentPalmPosition.y;
            float prePalmPosX = prevPalmPosition.x;
            float prePalmPosY = prevPalmPosition.y;
            double changePalmPosX = PalmPosX - prePalmPosX;
            double changePalmPosY = PalmPosY - prePalmPosY;
            int changemax = 100;
            //double scalingFactor = 15000;
            double mouseLinearScaling = 15000;
            double mousePowerScaling = 1;
            double mouseDirX = 1;
            double mouseDirY = 1;
            /*
            double TravMin = 0.1;
            TravMin = 0.0002;
            /*
            FingersConfig = 31 - PressedFingersConfig;
            prevFingersConfig = 31 - prevPressedFingersConfig;
            */
            PressedFingers = 5 - ExtendedFingers;
            prevPressedFingers = 5 - prevExtendedFingers;
            PressedFingersConfig = 31 - FingersConfig;
            prevPressedFingersConfig = 31 - prevFingersConfig;
            /*
            DigitID = eDigitID;
            prevDigitID = preveDigitID;
            */

            if (currentPalmPosition.z < 0.999 && currentPalmPosition.z > 0.001)// && hand.IsRight && prevHand.IsRight)
            {
                if (!MouseOn && !KbOn)
                {
                    //Console.Write(".");
                    if (FingersConfig != prevFingersConfig || PressedFingersConfig != prevPressedFingersConfig)
                    {
                        Console.Write("\nExtended finger configuration: " + FingersConfig + ", Pressed finger configuration: " + PressedFingersConfig + "\n");
                    }
                    //if (FingersConfig == 31 && prevFingersConfig == 3)
                    if (FingersConfig == 7 && prevFingersConfig == 3)
                    {
                        MouseOn = true;
                        CursorXPos = 1000;
                        CursorYPos = 500;
                        Console.Write("Mouse on \n");
                    }
                    else if (FingersConfig == 6 && prevFingersConfig == 2)
                    {
                        KbOn = true;
                        Console.Write("Keyboard on \n");
                        DigitTravel = Vector.Zero;
                    }
                }
                else if (KbOn)
                {
                    if (FingersConfig == 14 && prevFingersConfig == 15) //&& PalmDir.Roll < 1 && PalmDir.Roll > -1)
                    {
                        KbOn = false;
                        Console.Write("Keyboard off \n");
                    }
                    if (PressedFingers != prevPressedFingers)
                    {
                        if (PressedFingers > 3 && prevPressedFingersConfig > 3)//(hand.Fingers[DigitID].Type != Finger.FingerType.TYPE_INDEX || prevHand.Fingers[prevDigitID].Type != Finger.FingerType.TYPE_INDEX)
                        {
                            Console.Write("Pressed fingers: " + PressedFingers + ", Pressed fingers config: " + PressedFingersConfig + ", Digit Id: " + DigitID + "\n");
                        }
                        DigitTravel = Vector.Zero;
                        DigitOrig = currentPalmPosition;
                        DigitButton = -10;
                    }/*
                    if (PressedFingers == 3 && prevPressedFingers != 3)
                    {
                        DigitTravel = Vector.Zero;
                        DigitOrig = currentPalmPosition;
                        DigitButton = -10;
                    }*/
                    //if (PressedFingers == 3 && prevPressedFingers == PressedFingers && FingersConfig == prevFingersConfig)
                    if (FingersConfig == prevFingersConfig)
                    {
                        /*
                        double dPalmPitch = hand.Fingers[DigitID].StabilizedTipPosition.x - prevHand.Fingers[DigitID].StabilizedTipPosition.x;
                        double dPalmYaw = hand.Fingers[DigitID].StabilizedTipPosition.y - prevHand.Fingers[DigitID].StabilizedTipPosition.y;
                        double dPalmRoll = hand.Fingers[DigitID].StabilizedTipPosition.z - prevHand.Fingers[DigitID].StabilizedTipPosition.z;
                        *
                        double dPalmPitch = currentPalmPosition.x - prevPalmPosition.x;
                        double dPalmYaw = currentPalmPosition.y - prevPalmPosition.y;
                        double dPalmRoll = currentPalmPosition.z - prevPalmPosition.z;
                        */
                        //double TravMin = 0.1;

                        //Vector dDigitPos = hand.Fingers[DigitID].StabilizedTipPosition - prevHand.Fingers[DigitID].StabilizedTipPosition;
                        /*
                        Vector dDigitPos = currentPalmPosition - prevPalmPosition;

                        if (dDigitPos.Magnitude > TravMin)
                        {
                            DigitTravel += dDigitPos;
                        }*/

                        //int currDigitID;
                        DigitTravel = currentPalmPosition - DigitOrig;
                        /*
                        Leap.Finger.FingerType DigitType = prevHand.Fingers[prevDigitID].Type;
                        //Leap.Finger.FingerType DigitType = hand.Fingers[currDigitID].Type;
                        if (FingersConfig == 3)
                        {
                            DigitType = Leap.Finger.FingerType.TYPE_MIDDLE;
                        }*/
                        
                        //if (DigitType == Finger.FingerType.TYPE_INDEX)
                        if(FingersConfig == 3)
                        {
                            /*
                            DigitButtonEvent("abcdefg", 
                                new bool[] {true, true, true, true, true, true, true},
                                new byte[] {0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47});
                             */
                            DigitButtonEvent("abcdefghijklm",
                                new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true },
                                new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D });
                        }
                        else if (FingersConfig == 30) // (DigitType == Finger.FingerType.TYPE_MIDDLE)
                        {
                            /*
                            DigitButtonEvent("hijklmn",
                                new bool[] {true, true, true, true, true, true, true},
                                new byte[] {0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E});
                             */
                            DigitButtonEvent("nopqrstuvwxyz",
                                new bool[] { true, true, true, true, true, true, true, true, true, true, true, true, true },
                                new byte[] { 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A });
                        }
                        else if (FingersConfig == 19)
                        {
                            DigitButtonEvent("BCC_.,N",
                                new bool[] { false, false, false, true, true, true, true },
                                new byte[] { 0x08, 0x14, 0x14, 0x20, 0xBE, 0xBC, 0x0D });
                        }
                        /*
                        else if (DigitType == Finger.FingerType.TYPE_RING)
                        {
                            DigitButtonEvent("opqrstu",
                                new bool[] {true, true, true, true, true, true, true},
                                new byte[] {0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55});
                        }
                        else if (DigitType == Finger.FingerType.TYPE_PINKY)
                        {
                            DigitButtonEvent("vwxyz.,",
                                new bool[] {true, true, true, true, true, true, true},
                                new byte[] {0x56, 0x57, 0x58, 0x59, 0x5A, 0xBE, 0xBC});
                        }
                        else if (DigitType == Finger.FingerType.TYPE_THUMB)
                        {
                            DigitButtonEvent("TCB_.,N",
                                new bool[] {true, false, false, true, true, true, true},
                                new byte[] {0x09, 0x14, 0x08, 0x20, 0xBE, 0xBC, 0x0D});
                        }
                        else
                        {
                            Console.Write("Digit {0} moved ({1}, {2}, {3})\n",
                                FingersConfig, DigitTravel.x, DigitTravel.y, DigitTravel.z);
                        }*/
                    } 
                }
                else if (MouseOn)
                {
                    if (FingersConfig == 1 && prevFingersConfig == 3 && PalmDir.Roll < 1 && PalmDir.Roll > -1)
                    {
                        MouseOn = false;
                        Console.Write("Mouse off \n");
                        mouse_event(0x0004, (int)CursorXPos, (int)CursorYPos, 0, 0);
                        //mouse_event(0x0010, (int)CursorXPos, (int)CursorYPos, 0, 0);
                        mouse_event(0x0040, (int)CursorXPos, (int)CursorYPos, 0, 0);
                        mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                        mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);                       
                    }
                    else if (ExtendedFingers > 3 && ExtendedFingers == prevExtendedFingers)
                    {
                        if (changePalmPosX < changemax && changePalmPosY < changemax && PalmDir.Roll < 1 && PalmDir.Roll > -1 && PalmDir.Pitch < pitchmax)
                        {
                            changePalmPosX *= mouseLinearScaling;
                            changePalmPosY *= mouseLinearScaling;
                            for (int m = 1; m < mousePowerScaling; m++)
                            {
                                if (changePalmPosX < 0)
                                {
                                    mouseDirX = -1;
                                }
                                if (changePalmPosY < 0)
                                {
                                    mouseDirY = -1;
                                }
                                changePalmPosX *= changePalmPosX;
                                changePalmPosY *= changePalmPosY;
                            }
                            CursorXPos = CursorXPos + (int)(changePalmPosX * mouseDirX);
                            CursorYPos = CursorYPos - (int)(changePalmPosY * mouseDirY);
                        }
                        else if (ExtendedFingers == 5 && (PalmDir.Roll > 2 || PalmDir.Roll < -2))
                        {
                            CursorXPos = 1000;
                            CursorYPos = 500;
                            mouse_event(0x0004, (int)CursorXPos, (int)CursorYPos, 0, 0);
                            //mouse_event(0x0010, (int)CursorXPos, (int)CursorYPos, 0, 0);
                            mouse_event(0x0040, (int)CursorXPos, (int)CursorYPos, 0, 0);
                            mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                            mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                        }
                        SetCursorPos((int)CursorXPos, (int)CursorYPos);
                    }
                    else if (ExtendedFingers == 4 && prevExtendedFingers == 5 && PalmDir.Pitch < pitchmax)
                    {
                        if (FingersConfig == 29)
                        {
                            mouse_event(0x0002, 0, (int)CursorXPos, (int)CursorYPos, 0);
                            Console.Write("Left down\n");
                        }
                        if (FingersConfig == 27)
                        {
                            mouse_event(0x0008, 0, (int)CursorXPos, (int)CursorYPos, 0);
                            Console.Write("Right down\n");
                        }
                        if (FingersConfig == 30)
                        {
                            mouse_event(0x0020, 0, (int)CursorXPos, (int)CursorYPos, 0);
                            Console.Write("Middle down\n");
                        }
                        if (FingersConfig == 23)
                        {
                            mouse_event(0x0080, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                            Console.Write("4 down\n");
                        }
                        if (FingersConfig == 15)
                        {
                            mouse_event(0x0080, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                            Console.Write("5 down\n");
                        }
                    }
                    else if (ExtendedFingers == 5 && prevExtendedFingers == 4)
                    {
                        if (prevFingersConfig == 29)
                        {
                            Console.Write("Left up\n");
                            mouse_event(0x0004, 0, (int)CursorXPos, (int)CursorYPos, 0);
                        }
                        if (prevFingersConfig == 27)
                        {
                            mouse_event(0x0010, 0, (int)CursorXPos, (int)CursorYPos, 0);
                            Console.Write("Right up\n");
                        }
                        if (prevFingersConfig == 30)
                        {
                            mouse_event(0x0040, 0, (int)CursorXPos, (int)CursorYPos, 0);
                            Console.Write("Middle up\n");
                        }
                        if (prevFingersConfig == 23)
                        {
                            mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                            Console.Write("4 up\n");
                        }
                        if (prevFingersConfig == 15)
                        {
                            mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                            Console.Write("5 up\n");
                        }
                    }
                    if (FingersConfig != prevFingersConfig || PressedFingersConfig != prevPressedFingersConfig)
                    {
                        Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Pitch + ", Fingers: " + FingersConfig + "\n");
                    }
                    //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Pitch + ", Fingers: " + FingersConfig + "\n");
                    //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + "\n");
                }
            }
            prevTime = currentTime;
        }
        else
        {
            if (!MouseOn && !KbOn)
            {
                Console.Write(".");
            }
        }
    }
}

