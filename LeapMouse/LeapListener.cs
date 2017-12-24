using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Leap;

class LeapListener
{
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    [DllImport("user32.dll", SetLastError = true)]
    static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    [DllImport("User32.dll")]
    public static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("User32.dll")]
    public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);

    private Object thisLock = new Object();

    private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const int MOUSEEVENTF_MOVE = 0x0001;
    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int MOUSEEVENTF_XDOWN = 0x0080;
    private const int MOUSEEVENTF_XUP = 0x0100;
    private const int XBUTTON1 = 0x0001;
    private const int XBUTTON2 = 0x0002;

    private const int VK_LWIN = 0x5B;
    private const int VK_OEM_PLUS = 0xBB;
    private const int VK_ESCAPE = 0x1B;

    private void SafeWriteLine(String line)
    {
        lock (thisLock)
        {
            Console.WriteLine(line);
        }
    }

    public void OnInit(Controller controller)
    {
        IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
        //ShowWindow(h, 0);
        //controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
        SafeWriteLine("Initialized");
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

    private Int64 prevTime;
    private Int64 currentTime;
    private Int64 changeTime;
    private Frame currentFrame;
    private Frame prevFrame;
    private float currentZ = 2;
    private float previousZ = 2;
    private float CursorXPos;
    private float CursorYPos;
    private float MagXOffset;
    private float MagYOffset;
    private int mouseDown;
    private int drawSkip;
    public bool magnify = false; //true;
    public int magnifying = 0;

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

    public void Magnify(int dwFlags, int magnification)
    {
        if (magnify)
        {
            if (magnifying == 0)
            {
                //Process.Start("Magnify");
                MagXOffset = CursorXPos;
                MagYOffset = CursorYPos;
                magnifying = magnification;
            }
            else
            {
                mouse_event(dwFlags, 0, 0, 0, 0);
                magnifying = 0;
                //PressVK(VK_ESCAPE);
            }
            //magnifying = !magnifying;
        }
        else
        {
            mouse_event(dwFlags, 0, 0, 0, 0);
        }
    }
    
    public void PressVK(byte key)
    {
        byte bScan = 0;
        {
            keybd_event(VK_LWIN, bScan, 0x0001 | 0, 0);
            keybd_event(key, bScan, 0x0001 | 0, 0);
            keybd_event(key, bScan, 0x0001 | 0x0002, 0);
            keybd_event(VK_LWIN, bScan, 0x0001 | 0x0002, 0);
        }
    }

    public void OnFrame(object sender, FrameEventArgs args)
    {
        prevFrame = currentFrame;
        currentFrame = args.frame;
        currentTime = currentFrame.Timestamp;
        changeTime = currentTime - prevTime;

        if (changeTime > 3e4) // time in microseconds (1e-6)
        {
            if (currentFrame.Hands.Count > 0) // && prevFrame.Hands.Count > 0)
            {
                InteractionBox iBox = currentFrame.InteractionBox;
                Hand hand = currentFrame.Hands[0];
                List<bool> activeFingerList = new List<bool>();
                int activeFingerCount = 0;

                foreach (Finger finger in hand.Fingers)
                {
                    if (finger.IsExtended)
                    {
                        activeFingerCount++;
                        activeFingerList.Add(true);
                    }
                    else
                        activeFingerList.Add(false);
                }

                Leap.Vector leapPoint;
                Leap.Vector normalizedPoint;
                if (activeFingerList[1])
                {
                    Finger indexFinger = hand.Fingers[1];

                    leapPoint = indexFinger.TipPosition; //.StabilizedTipPosition; //
                    normalizedPoint = iBox.NormalizePoint(leapPoint, false);
                    currentZ = normalizedPoint.z;

                    if (currentZ < 1.5)
                    {
                        /*
                        if (currentZ < 1.0 && currentZ > 0.5) // 0.0) //
                        {
                            float d = (currentZ - 0.5f) * (currentZ - 0.5f) * 4;
                            CursorXPos = normalizedPoint.x * d + CursorXPos * (1.0f - d);
                            CursorYPos = (1 - normalizedPoint.y) * d + CursorYPos * (1.0f - d);
                        }
                        else //*/
                        if (currentZ < 0.75 && currentZ > 0.25)
                        {
                            float d = (currentZ - 0.5f) * (currentZ - 0.5f) * 16;
                            CursorXPos = normalizedPoint.x * d + CursorXPos * (1.0f - d);
                            CursorYPos = (1 - normalizedPoint.y) * d + CursorYPos * (1.0f - d);
                        }
                        else
                        {
                            CursorXPos = normalizedPoint.x;
                            CursorYPos = (1 - normalizedPoint.y);
                        }
                        int CursorXPosMouse;
                        int CursorYPosMouse;

                        if (magnifying == 0)
                        {
                            CursorXPosMouse = (int)(CursorXPos * 65535);
                            CursorYPosMouse = (int)(CursorYPos * 65535);
                        }
                        else
                        {
                            CursorXPosMouse = (int)((CursorXPos + MagXOffset * (magnifying - 1)) / magnifying * 65535);
                            CursorYPosMouse = (int)((CursorYPos + MagYOffset * (magnifying - 1)) / magnifying * 65535);
                        }

                        // Move the mouse.
                        mouse_event(
                            (MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE),
                            CursorXPosMouse, CursorYPosMouse, 0, 0);

                        if (activeFingerCount == 1)
                        {
                            // Left click drag
                            if (previousZ > 0.5 && currentZ < 0.5)
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                mouseDown += 1;
                            }
                            else if (previousZ < 0.5 && currentZ > 0.5)
                            {
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                mouseDown -= 1;
                            }
                        }
                        else if (activeFingerList[0])
                        {
                            if (activeFingerCount == 2)
                            {
                                // Left single click
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    Magnify(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 3);
                                }
                            }
                            else if (activeFingerCount == 3)
                            {
                                // Right single click
                                if (activeFingerList[2] && previousZ > 0.5 && currentZ < 0.5)
                                {
                                    Magnify(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 3);
                                }
                                // Open TabTip
                                if (activeFingerList[4] && previousZ > 0.5 && currentZ < 0.5)
                                {
                                    Process.Start("TabTip.exe");
                                }
                            }
                            else if (activeFingerCount == 4)
                            {
                                // Toggle magnify
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    magnify = !magnify;
                                }
                            }
                            else if (activeFingerCount == 5)
                            {
                                //if (activeFingerList[2] && activeFingerList[3])
                                {
                                    // Middle drag
                                    if (previousZ > 0.5 && currentZ < 0.5)
                                    {
                                        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                                        mouseDown += 100;
                                        MagXOffset = CursorXPos;
                                        MagYOffset = CursorYPos;
                                        magnifying = 5; // 20;
                                    }
                                    else if (previousZ < 0.5 && currentZ > 0.5)
                                    {
                                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                                        mouseDown -= 100;
                                        magnifying = 0;
                                    }
                                }
                            }
                        }
                        else if (activeFingerCount == 2)
                        {
                            if (activeFingerList[2])
                            {
                                // Right drag
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                                    mouseDown += 10;
                                }
                                else if (previousZ < 0.5 && currentZ > 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                                    mouseDown -= 10;
                                }
                            }
                            else if (activeFingerList[4])
                            {
                                // X2 click
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP, 0, 0, XBUTTON2, 0);
                                }
                            }
                        }
                        else if (activeFingerCount == 3)
                        {
                            //if (activeFingerList[2] && activeFingerList[3])
                            {
                                // Middle click
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    Magnify(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP, 3);
                                }
                            }
                        }
                        else if (activeFingerCount == 4)
                        {
                            //if (activeFingerList[2] && activeFingerList[3])
                            {
                                // X1 click
                                if (previousZ > 0.5 && currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP, 0, 0, XBUTTON1, 0);
                                }
                            }
                        }
                    }
                }
                else
                {
                    currentZ = 2;
                }

                

                if (currentZ < 1.5) // && !(magnifying)) // && currentZ > 0.3
                {
                    drawSkip++;

                    if (drawSkip > 3) // && currentZ < 1 && currentZ > 0) //(false) //
                    {
                        drawSkip = 1;
                        if (currentZ > 0.75) // 1)
                            currentZ = 1;
                        else if (currentZ < 0.25) // 0) // 
                            currentZ = 0;
                        else //if (currentZ < 0.5)
                            currentZ = (currentZ * 2 - 0.5f);
                        /*//*/
                        IntPtr desktopPtr = GetDC(IntPtr.Zero);
                        Graphics g = Graphics.FromHdc(desktopPtr);
                        float penThickness = 8;
                        Pen arcPen = new Pen(Color.Transparent, penThickness);
                        int retSize = 300;
                        Rectangle arcRet = new Rectangle((1920 - retSize) / 2, 50, retSize, retSize); 
                        float startAng; // = 180;
                        float sweepAng; // = 180;
                        float minSweep = 0.1f;

                        float angMult = 20;
                        if (currentZ > 0.5)
                        {
                            sweepAng = (float)(angMult * 2 * (currentZ - 0.5));
                            startAng = 270 - sweepAng;
                            if (angMult - sweepAng > minSweep)
                                try {
                                    g.DrawArc(arcPen, arcRet, 270 - angMult, angMult - sweepAng);
                                }
                                catch (Exception e) {
                                    Console.WriteLine("Draw Error in frame: " + currentFrame + ":\n"+ e);
                                }
                            g.DrawArc(arcPen, arcRet, 270, angMult);
                            arcPen.Dispose();
                            arcPen = new Pen(Color.LawnGreen, penThickness);
                        }
                        else
                        {
                            startAng = 270;
                            sweepAng = (float)(angMult * 2 * (0.5 - currentZ));
                            if (angMult - sweepAng > minSweep)
                                try {
                                    g.DrawArc(arcPen, arcRet, angMult - 90, sweepAng - angMult);
                                }
                                catch (Exception e) {
                                    Console.WriteLine("Draw Error in frame: " + currentFrame + ":\n" + e);
                                }
                            g.DrawArc(arcPen, arcRet, 270, -angMult);
                            arcPen.Dispose();
                            arcPen = new Pen(Color.Red, penThickness);
                        }
                        if (sweepAng > minSweep)
                            try {
                                g.DrawArc(arcPen, arcRet, startAng, sweepAng);
                            }
                            catch (Exception e) {
                                Console.WriteLine("Draw Error in frame: " + currentFrame + ":\n" + e);
                            }
                        g.Dispose();
                        arcPen.Dispose();

                        ReleaseDC(IntPtr.Zero, desktopPtr);
                    }

                    //Console.Write("X: " + normalizedPoint.x + ", Y: " + normalizedPoint.y + ", Z: " + normalizedPoint.z + "\n");

                    previousZ = currentZ;
                    prevTime = currentTime;
                }
            }
            else
            {
                //Console.Write("No hands found\n");
                if (mouseDown > 0)
                {
                    if (mouseDown % 10 == 1)
                    {
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        mouseDown -= 1;
                    }
                    if (mouseDown % 100 == 10)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                        mouseDown -= 10;
                    }
                    if (mouseDown % 1000 == 100)
                    {
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                        mouseDown -= 100;
                    }
                    mouseDown = 0;
                }
                currentZ = 2;
                previousZ = 2;
                magnifying = 0;
            }
        }
    }
}
