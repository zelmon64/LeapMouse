using System;
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
                Finger finger = hand.Fingers[1];

                Leap.Vector leapPoint = finger.TipPosition; //.StabilizedTipPosition; //
                Leap.Vector normalizedPoint = iBox.NormalizePoint(leapPoint, false);
                currentZ = normalizedPoint.z;

                if (currentZ < 1.5) // & currentZ > 0.3
                {
                    drawSkip++;

                    //currentThumb = hand.Fingers[0].IsExtended;

                    float CursorXPos = normalizedPoint.x; // * 65535; // appWidth;
                    float CursorYPos = (1 - normalizedPoint.y); // * 65535; // appHeight;
                    int CursorXPosMouse = (int)(CursorXPos * 65535);
                    int CursorYPosMouse = (int)(CursorYPos * 65535);
                    int CursorXPosScreen = (int)(CursorXPos * 1920); // Screen.PrimaryScreen.WorkingArea.Width);
                    int CursorYPosScreen = (int)(CursorYPos * 1080); // Screen.PrimaryScreen.WorkingArea.Height);

                    // Move the mouse.
                    //SetCursorPos((int)CursorXPos, (int)CursorYPos);
                    mouse_event(
                        (MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE),
                        CursorXPosMouse, CursorYPosMouse, 0, 0);

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
                                try
                                {
                                    g.DrawArc(arcPen, arcRet, 270 - angMult, angMult - sweepAng);
                                }
                                catch (Exception e)
                                {
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
                                try
                                {
                                    g.DrawArc(arcPen, arcRet, angMult - 90, sweepAng - angMult);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Draw Error in frame: " + currentFrame + ":\n" + e);
                                }
                            g.DrawArc(arcPen, arcRet, 270, -angMult);
                            arcPen.Dispose();
                            arcPen = new Pen(Color.Red, penThickness);
                        }
                        if (sweepAng > 0)
                            try
                            {
                                g.DrawArc(arcPen, arcRet, startAng, sweepAng);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Draw Error in frame: " + currentFrame + ":\n" + e);
                            }
                        //g.DrawArc(new Pen(Color.Transparent, 5), acrRet, (startAng + sweepAng), (360 - sweepAng));
                        /*
                        // Create pens.
                        Pen blackPen = new Pen(Color.Black, 5);
                        Pen redPen = new Pen(Color.Red, 5);
                        // Clear screen
                        g.DrawRectangle(Pens.Transparent, new Rectangle(0, 0, 1920, 1080));
                        int sizeMult = 150;
                        if (currentZ > 0.5)
                            g.DrawEllipse(blackPen, new Rectangle(CursorXPosScreen - (int)(sizeMult * (currentZ - 0.5)), CursorYPosScreen - (int)(sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5))));
                        else
                            g.DrawEllipse(redPen, new Rectangle(CursorXPosScreen - (int)(sizeMult * (0.5 - currentZ)), CursorYPosScreen - (int)(sizeMult * (0.5 - currentZ)), (int)(2 * sizeMult * (0.5 - currentZ)), (int)(2 * sizeMult * (0.5 - currentZ))));
                        //*/
                        g.Dispose();
                        arcPen.Dispose();

                        ReleaseDC(IntPtr.Zero, desktopPtr);
                    }

                    // Left click
                    //*
                    //if (!previousThumb & currentThumb)
                    if (previousZ > 0.5 & currentZ < 0.5)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, (int)CursorXPos, (int)CursorYPos, 0, 0);
                        mouseDown = true;
                    }
                    //if (previousThumb & !currentThumb)
                    if (previousZ < 0.5 & currentZ > 0.5)
                    {
                        mouse_event(MOUSEEVENTF_LEFTUP, (int)CursorXPos, (int)CursorYPos, 0, 0);
                        mouseDown = false;
                    }//*/

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
