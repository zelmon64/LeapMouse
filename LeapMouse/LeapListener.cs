using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Leap;

class LeapListener //: Listener
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
    public void OnInit(Controller controller)
    {
        IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(h, 0);
        //controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
        SafeWriteLine("Initialized");
        CursorXPos = 0;
        CursorYPos = 0;
    }
    /*
    public override void OnConnect(Controller controller)
    {
        SafeWriteLine("Connected");
    }

    public override void OnDisconnect(Controller controller)
    {
        SafeWriteLine("Disconnected");
    }*/

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

        if (changeTime > 5000 && currentFrame.Hands.Count > 0 && prevFrame.Hands.Count > 0)
        {
            Hand hand = currentFrame.Hands[0];
            //FingerList fingers = hand.Fingers;
            //PointableList pointables = hand.Pointables;
            Hand prevHand = prevFrame.Hands[0];
            //FingerList prevFingers = prevHand.Fingers;
            //Pointable currentPointable = currentFrame.Pointables[0];
            //Pointable prevPointable = prevFrame.Pointables[0];
            //Vector currentPalmPosition = hand.PalmPosition;
            //Vector prevPalmPosition = prevHand.PalmPosition;
            Vector currentPalmPosition = hand.StabilizedPalmPosition;
            Vector prevPalmPosition = prevHand.StabilizedPalmPosition;
            Vector PalmDir = hand.PalmNormal;
            /*
            int ActiveFingers = 0;
            Frame frame = controller.Frame();
            for (int h = 0; h < frame.Hands.Count; h++)
            {
                Hand leapHand = frame.Hands[h];

                Vector handXBasis = leapHand.PalmNormal.Cross(leapHand.Direction).Normalized;
                Vector handYBasis = -leapHand.PalmNormal;
                Vector handZBasis = -leapHand.Direction;
                Vector handOrigin = leapHand.PalmPosition;
                Matrix handTransform = new Matrix(handXBasis, handYBasis, handZBasis, handOrigin);
                handTransform = handTransform.RigidInverse();

                for (int f = 0; f < leapHand.Fingers.Count; f++)
                {
                    Finger leapFinger = leapHand.Fingers[f];
                    Vector transformedPosition = handTransform.TransformPoint(leapFinger.TipPosition);
                    //Vector transformedDirection = handTransform.TransformDirection(leapFinger.Direction);
                    // Do something with the transformed fingers
                    if ((transformedPosition.x * transformedPosition.x + transformedPosition.z * transformedPosition.z) > 4500)
                    {
                        ActiveFingers++;
                    }
                }
            }*/
            int prevExtendedFingers = 0;
            int ExtendedFingers = 0;
            int FingersConfig = 0;
            int FingersMult;
            //Console.Write("HandID: " + hand.Id + ", Confidence:" + (hand.Confidence * 10.0) + ", Time visible: " + (int)hand.TimeVisible/1000000 + "\n");
            for (int f = 0; f < prevHand.Fingers.Count; f++)
            {
                Finger digit = prevHand.Fingers[f];
                if (digit.IsExtended)
                {
                    prevExtendedFingers++;
                }
            } 
            for (int f = 0; f < hand.Fingers.Count; f++)
            {
                Finger digit = hand.Fingers[f];
                if (digit.IsExtended)// && digit.Type == Finger.FingerType.TYPE_INDEX)
                {
                    ExtendedFingers++;
                    //Console.Write("Digit: " + digit.Type + " ");
                    //Console.Write("DigitID: " + digit.Id + " ");
                    //Console.Write("DigitID: " + (digit.Id - hand.Id * 10) + " ");
                    //FingersConfig += 2 ^ (digit.Id - hand.Id * 10);
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
            //Console.Write("Visible fingers: " + hand.Fingers. + "\n");
            /*
            // Set Up Screen
            Leap.Screen currentScreen = controller.CalibratedScreens.ClosestScreenHit(currentPointable);
            Leap.Screen prevScreen = controller.CalibratedScreens.ClosestScreenHit(prevPointable);
            // Set up Display             
            float DisplayHeight = currentScreen.HeightPixels;
            float DisplayWidth = currentScreen.WidthPixels;
            double scalingFactor = 1.5;
            // Set up Coordinates
            float CurrentLeapXCoordinate = currentScreen.Intersect(currentPointable, true, 1.0F).x;
            float CurrentLeapYCoordinate = currentScreen.Intersect(currentPointable, true, 1.0F).y;
            int CurrentyPixel = (int)(DisplayHeight - (DisplayHeight * (scalingFactor * CurrentLeapYCoordinate)));
            int CurrentxPixel = (int)(DisplayWidth * (scalingFactor * CurrentLeapXCoordinate));
            float PrevLeapXCoordinate = currentScreen.Intersect(prevPointable, true, 1.0F).x;
            float PrevLeapYCoordinate = currentScreen.Intersect(prevPointable, true, 1.0F).y;
            int PrevyPixel = (int)(DisplayHeight - (DisplayHeight * (scalingFactor * PrevLeapYCoordinate)));
            int PrevxPixel = (int)(DisplayWidth * (scalingFactor * PrevLeapXCoordinate));
            float changeyPixel = (PrevyPixel - CurrentyPixel);
            float changexPixel = (PrevxPixel - CurrentxPixel);
            double scalingFactor2 = 1;
            int changemax = 100;
            /*
            if (changexPixel < changemax || changeyPixel < changemax)
            {
                CursorXPos = CursorXPos - (int)(changexPixel * scalingFactor2);
                CursorYPos = CursorYPos - (int)(changeyPixel * scalingFactor2);
            }
            else
            {
                CursorXPos = DisplayWidth / 2;
                CursorYPos = DisplayHeight / 2;
            }*
            if (changeyPixel < 0) { changeyPixel = changeyPixel * -1; }
            if (changexPixel < 0) { changexPixel = changexPixel * -1; }
            bool allowfalse = false;
            if (changeyPixel > 0) { allowfalse = true; }
            if (CurrentyPixel < 0) { CurrentyPixel = 0; }
            if (CurrentxPixel < 0) { CurrentxPixel = 0; }
            */

            float PalmPosX = currentPalmPosition.x;
            float PalmPosY = currentPalmPosition.y;
            float prePalmPosX = prevPalmPosition.x;
            float prePalmPosY = prevPalmPosition.y;
            float changePalmPosX = PalmPosX - prePalmPosX;
            float changePalmPosY = PalmPosY - prePalmPosY;
            int changemax = 100;
            double scalingFactor = 20;
            if (currentPalmPosition.z < 100 && currentPalmPosition.z > -50)
            {
                if (ExtendedFingers > 3 && ExtendedFingers == prevExtendedFingers)
                {

                    if (changePalmPosX < changemax && changePalmPosY < changemax && PalmDir.Roll < 1 && PalmDir.Roll > -1)
                    {
                        CursorXPos = CursorXPos + (int)(changePalmPosX * scalingFactor);
                        CursorYPos = CursorYPos - (int)(changePalmPosY * scalingFactor);
                        //allowfalse = true;
                    }
                    else if (PalmDir.Roll > 2 || PalmDir.Roll < -2)
                    {
                        /*
                        CursorXPos = DisplayWidth / 2;
                        CursorYPos = DisplayHeight / 2;
                         */
                        CursorXPos = 1000;
                        CursorYPos = 500;
                    }
                    SetCursorPos((int)CursorXPos, (int)CursorYPos);
                }
                else if (ExtendedFingers == 4 && prevExtendedFingers == 5)
                {
                    for (int f = 0; f < hand.Fingers.Count; f++)
                    {
                        Finger digit = hand.Fingers[f];
                        Finger prevdigit = prevHand.Fingers[f];
                        if (!digit.IsExtended && prevdigit.IsExtended)
                        {
                            if (digit.Type == Finger.FingerType.TYPE_INDEX)
                            {
                                //mouse_event(0x0002 | 0x0004, 0, (int)CursorXPos, (int)CursorYPos, 0);
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
                        }
                    }

                }
                //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + ", Fingers: " + fingers.Count + ", Pointabless: " + pointables.Count + "\n");
                //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + ", Fingers: " + ExtendedFingers + "\n");
                Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + ", Fingers: " + FingersConfig + "\n");
                //Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + "\n");
            }
            /*
            if (allowfalse)
            {
                if (prevFingers.Count != 2 || prevFingers.Count != 3)
                {

                    if (fingers.Count == 1)
                    {
                        if (changeTime > 500)
                        {
                            //Console.Write("TipPosition: " + fingers[0].TipPosition + " Width: " + CurrentxPixel + " height: " + CurrentyPixel + "\n");
                            Console.Write("deltaX: " + changexPixel + ", deltaY: " + changeyPixel + ", width: " + CursorYPos + ", height: " + CursorYPos + "\n");
                            Console.Write("PalmZ: " + currentPalmPosition.z + ", PalmNorm: " + PalmDir.Roll + "\n");
                            //SetCursorPos(CurrentxPixel, CurrentyPixel);
                            SetCursorPos((int)CursorXPos, (int)CursorYPos);
                        }
                    }/*
                    if (fingers.Count == 2)
                    {
                        if (changeTime > 1000)
                        {
                            mouse_event(0x0002 | 0x0004, 0, CurrentxPixel, CurrentyPixel, 0);
                            Console.Write("Clicked At " + CurrentxPixel + " " + CurrentyPixel + "\n");
                        }
                    }
                    if (fingers.Count == 4)
                    {
                        if (changeTime > 10000)
                        {

                            float Translation = currentFrame.Translation(prevFrame).y;
                            Console.WriteLine(Translation);
                            if (Translation < -50)
                            {
                                SendKeys.SendWait("{LEFT}");
                                System.Threading.Thread.Sleep(700);
                            }
                            if (Translation > 50)
                            {
                                SendKeys.SendWait("{Right}");
                                System.Threading.Thread.Sleep(700);
                            }
                        }
                    }*
                }
            }*/
            prevTime = currentTime;
        }
        //prevTime = currentTime;
    }
}

