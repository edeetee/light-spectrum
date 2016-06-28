using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using System;
using System.Windows.Forms;

namespace LightController
{
    public class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Application.Run(new Main());
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }
    }
}
