using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
namespace LeapMouse
{
    class LeapMouse
    {
        public static void Main()
        {
            using (Leap.IController controller = new Leap.Controller())
            {
                controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_ALLOW_PAUSE_RESUME);

                // Set up our listener:
                LeapListener listener = new LeapListener();
                controller.Connect += listener.OnServiceConnect;
                controller.Disconnect += listener.OnServiceDisconnect;
                controller.FrameReady += listener.OnFrame;
                controller.Device += listener.OnConnect;
                controller.DeviceLost += listener.OnDisconnect;
                controller.DeviceFailure += listener.OnDeviceFailure;
                controller.LogMessage += listener.OnLogMessage;

                // Keep this process running until Enter is pressed
                Console.WriteLine("Press return key to quit...");
                Console.ReadLine();
            }
        }
    }
}
