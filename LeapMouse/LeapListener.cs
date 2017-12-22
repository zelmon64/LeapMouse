﻿using System;
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
    private float currentZ;
    private float previousZ;
    //private bool currentThumb;
    //private bool previousThumb;
    private float CursorXPos;
    private float CursorYPos;
    private bool mouseDown;
    private int drawSkip;

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

    public void OnFrame(object sender, FrameEventArgs args)
    {
        prevFrame = currentFrame;
        currentFrame = args.frame;
        currentTime = currentFrame.Timestamp;
        changeTime = currentTime - prevTime;

        if (changeTime > 3e4) // time in microseconds (1e-6)
        {
            InteractionBox iBox = currentFrame.InteractionBox;

            if (currentFrame.Hands.Count > 0) // && prevFrame.Hands.Count > 0)
            {
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
                        CursorXPos = normalizedPoint.x; // * 65535; // appWidth;
                        CursorYPos = (1 - normalizedPoint.y); // * 65535; // appHeight;
                        int CursorXPosMouse = (int)(CursorXPos * 65535);
                        int CursorYPosMouse = (int)(CursorYPos * 65535);
                        int CursorXPosScreen = (int)(CursorXPos * 1920); // Screen.PrimaryScreen.WorkingArea.Width);
                        int CursorYPosScreen = (int)(CursorYPos * 1080); // Screen.PrimaryScreen.WorkingArea.Height);

                        // Move the mouse.
                        //SetCursorPos((int)CursorXPos, (int)CursorYPos);
                        mouse_event(
                            (MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE),
                            CursorXPosMouse, CursorYPosMouse, 0, 0);

                        if (activeFingerCount == 1)
                        {
                            // Left click drag
                            if (previousZ > 0.5 & currentZ < 0.5)
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                mouseDown = true;
                            }
                            if (previousZ < 0.5 & currentZ > 0.5)
                            {
                                mouse_event(MOUSEEVENTF_LEFTUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                mouseDown = false;
                            }
                        }
                        else if (activeFingerList[0])
                        {
                            if (activeFingerCount == 2)
                            {
                                // Left single click
                                if (previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                }
                            }
                            else if (activeFingerCount == 3)
                            {
                                // Right single click
                                if (activeFingerList[2] && previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                }
                                // X2 single click
                                if (activeFingerList[4] && previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP, (int)CursorXPos, (int)CursorYPos, XBUTTON2, 0);
                                }
                            }
                            else if (activeFingerCount == 4)
                            {
                                if (activeFingerList[2] && activeFingerList[3])
                                {
                                    // Middle click
                                    if (previousZ > 0.5 & currentZ < 0.5)
                                    {
                                        mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                    }
                                }
                            }
                            else if (activeFingerCount == 5)
                            {
                                //if (activeFingerList[2] && activeFingerList[3])
                                {
                                    // X1 click
                                    if (previousZ > 0.5 & currentZ < 0.5)
                                    {
                                        mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP, (int)CursorXPos, (int)CursorYPos, XBUTTON1, 0);
                                    }
                                }
                            }
                        }
                        else if (activeFingerCount == 2)
                        {
                            if (activeFingerList[2])
                            {
                                // Right drag
                                if (previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTDOWN, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                    mouseDown = true;
                                }
                                if (previousZ < 0.5 & currentZ > 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_RIGHTUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                    mouseDown = false;
                                }
                            }
                            else if (activeFingerList[4])
                            {
                                // X2 drag
                                if (previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XDOWN, (int)CursorXPos, (int)CursorYPos, XBUTTON2, 0);
                                    mouseDown = true;
                                }
                                if (previousZ < 0.5 & currentZ > 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XUP, (int)CursorXPos, (int)CursorYPos, XBUTTON2, 0);
                                    mouseDown = false;
                                }
                            }
                        }
                        else if (activeFingerCount == 3)
                        {
                            //if (activeFingerList[2] && activeFingerList[3])
                            {
                                // Middle drag
                                if (previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                    mouseDown = true;
                                }
                                if (previousZ < 0.5 & currentZ > 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_MIDDLEUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                                    mouseDown = false;
                                }
                            }
                        }
                        else if (activeFingerCount == 4)
                        {
                            //if (activeFingerList[2] && activeFingerList[3])
                            {
                                // X1 drag
                                if (previousZ > 0.5 & currentZ < 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XDOWN, (int)CursorXPos, (int)CursorYPos, XBUTTON1, 0);
                                    mouseDown = true;
                                }
                                if (previousZ < 0.5 & currentZ > 0.5)
                                {
                                    mouse_event(MOUSEEVENTF_XUP, (int)CursorXPos, (int)CursorYPos, XBUTTON1, 0);
                                    mouseDown = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    currentZ = 2;
                }

                

                if (currentZ < 1.5) // & currentZ > 0.3
                {
                    drawSkip++;

                    if (drawSkip > 3) // & currentZ < 1 & currentZ > 0) //(false) //
                    {
                        drawSkip = 1;
                        if (currentZ > 1)
                            currentZ = 1;
                        else if (currentZ < 0)
                            currentZ = 0;
                        IntPtr desktopPtr = GetDC(IntPtr.Zero);
                        Graphics g = Graphics.FromHdc(desktopPtr);
                        float penThickness = 8;
                        Pen arcPen = new Pen(Color.Transparent, penThickness);
                        int retSize = 300;
                        Rectangle arcRet = new Rectangle((1920 - retSize) / 2, 50, retSize, retSize); // ((1920- retSize) / 2, (1080- retSize) / 2, retSize, retSize); // (50, 50, 1870, 1030);
                        float startAng; // = 180;
                        float sweepAng; // = 180;

                        float angMult = 20;
                        if (currentZ > 0.5)
                        {
                            sweepAng = (float)(angMult * 2 * (currentZ - 0.5));
                            startAng = 270 - sweepAng;
                            if (sweepAng != angMult)
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
                            if (sweepAng != angMult)
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
                        if (sweepAng > 0)
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
                    //previousThumb = currentThumb;
                }
            }
            else
            {
                //Console.Write("No hands found\n");
                //*
                if (mouseDown)
                {
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    mouseDown = false;
                }//*/
                currentZ = 1;
                previousZ = 1;
                //currentThumb = false;
            }
        }
    }
}
