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
        //int appWidth = Screen.PrimaryScreen.WorkingArea.Width;
        //int appHeight = Screen.PrimaryScreen.WorkingArea.Height;

        InteractionBox iBox = currentFrame.InteractionBox;
        //if (currentFrame.Hands.Count > 0)
        currentTime = currentFrame.Timestamp;
        changeTime = currentTime - prevTime;
        /*
        Console.WriteLine("Frame id: {0}, timestamp: {1}, hands: {2}, changeTime: {3}",
            currentFrame.Id, currentFrame.Timestamp, currentFrame.Hands.Count, changeTime);
        */
        if (currentFrame.Hands.Count > 0) // && prevFrame.Hands.Count > 0)
        {
            Hand hand = currentFrame.Hands[0];
            Finger finger = hand.Fingers[1];

            Leap.Vector leapPoint = finger.TipPosition; //.StabilizedTipPosition; //
            Leap.Vector normalizedPoint = iBox.NormalizePoint(leapPoint, false);
            currentZ = normalizedPoint.z;

            if (changeTime > 3e3 & currentZ < 1.2) // & currentZ > 0.3
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

                if (drawSkip > 10)
                {
                    drawSkip = 1;
                    //Point pt = Cursor.Position; // Get the mouse cursor in screen coordinates
                    /*
                    using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        g.DrawEllipse(Pens.Black, CursorXPos - 10, CursorYPos - 10, 20, 20);
                    }
                    */
                    IntPtr desktopPtr = GetDC(IntPtr.Zero);
                    Graphics g = Graphics.FromHdc(desktopPtr);

                    //SolidBrush b = new SolidBrush(Color.Black);
                    // Create pen.
                    Pen blackPen = new Pen(Color.Black, 5);
                    Pen redPen = new Pen(Color.Red, 5);
                    //g.FillRectangle(b, new Rectangle(0, 0, 1920, 1080));
                    g.DrawRectangle(Pens.Transparent, new Rectangle(0, 0, 1920, 1080));
                    //g.FillEllipse(b, new Rectangle(CursorXPosScreen - 10, CursorYPosScreen - 10, CursorXPosScreen + 10, CursorYPosScreen + 10));
                    //g.DrawEllipse(Pens.Black, new Rectangle(CursorXPosScreen - 10, CursorYPosScreen - 10, 20, 20));
                    int sizeMult = 100;
                    if (currentZ > 0.5)
                        g.DrawEllipse(blackPen, new Rectangle(CursorXPosScreen - (int)(sizeMult * (currentZ - 0.5)), CursorYPosScreen - (int)(sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5))));
                    //g.FillEllipse(b, new Rectangle(CursorXPosScreen - (int)(sizeMult * (currentZ - 0.5)), CursorYPosScreen - (int)(sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5)), (int)(2 * sizeMult * (currentZ - 0.5))));
                    else
                        g.DrawEllipse(redPen, new Rectangle(CursorXPosScreen - (int)(sizeMult * (0.5 - currentZ)), CursorYPosScreen - (int)(sizeMult * (0.5 - currentZ)), (int)(2 * sizeMult * (0.5 - currentZ)), (int)(2 * sizeMult * (0.5 - currentZ))));

                    g.Dispose();
                    ReleaseDC(IntPtr.Zero, desktopPtr);
                }

                // Left click
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
                }

                //mouse_event(0x8000, (int)CursorXPos, (int)CursorYPos, 0, 0);
                //mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0001, 0);
                //mouse_event(0x0100, (int)CursorXPos, (int)CursorYPos, 0x0002, 0);
                //The z-coordinate is not used
                //Console.Write("X: " + CursorXPos + ", Y: " + CursorYPos + "\n");
                //Console.Write("X: " + normalizedPoint.x + ", Y: " + normalizedPoint.y + "\n");
                //Console.Write("X: " + normalizedPoint.x + ", Y: " + normalizedPoint.y + ", Z: " + normalizedPoint.z + "\n");
                //Console.Write("X: " + leapPoint.x + ", Y: " + leapPoint.y + "\n");

                previousZ = currentZ;
                prevTime = currentTime;
                //previousThumb = currentThumb;
            }
        }
        else
        {
            //Console.Write("No hands found\n");
            if (mouseDown)
            {
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                mouseDown = false;
            }
            currentZ = 1;
            previousZ = 1;
            //currentThumb = false;
        }
    }
}
