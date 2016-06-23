using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

using ColorMine.ColorSpaces;

namespace LightController
{
    public class Program
    {
        static SerialPort port;

        public static int num_leds = 100 / 1;

        static int i = 0;
        static int num_samples = 3;
        static float[] samples;
        static float max;

        static Thread mainThread;

        static FftProvider provider;
        static LightStripSpectrum spectrum;
        static Stopwatch runningStopwatch = new Stopwatch();

        private static void Main(string[] args)
        {
            port = new SerialPort("COM3", 115200);
            port.Open();

            port.DataReceived += Port_DataReceived;

            samples = new float[num_samples];

            using (var capture = new WasapiLoopbackCapture())
            {
                capture.Device = MMDeviceEnumerator.DefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                WaveFormat format = capture.Device.DeviceFormat;
                capture.Initialize();

                provider = new BasicSpectrumProvider(2, format.BytesPerSecond / format.BytesPerSample, FftSize.Fft2048);

                var inSource = new SoundInSource(capture);
                ISampleSource sampleSource = inSource.ToSampleSource();
                var notification = new SingleBlockNotificationStream(sampleSource);
                notification.SingleBlockRead += (s, a) =>
                {
                    provider.Add(a.Left, a.Right);
                };

                spectrum = new LightStripSpectrum(provider.FftSize)
                {
                    SpectrumProvider = (ISpectrumProvider)provider,
                    UseAverage = true,
                    Lights = num_leds,
                    IsXLogScale = false,
                    ScalingStrategy = ScalingStrategy.Sqrt,
                };

                var finalSource = notification.ToWaveSource();

                byte[] buffer = new byte[finalSource.WaveFormat.BytesPerSecond / 2];
                inSource.DataAvailable += (s, e) =>
                {
                    int read;
                    while ((read = finalSource.Read(buffer, 0, buffer.Length)) > 0)
                    {

                    }
                };

                capture.Start();
                mainThread = new Thread(() =>
                {
                    while (true)
                    {
                        //showSimpleValue();
                        //showSpectrum();
                        showSpectrumControlled();
                        //showClock();
                        //showScreenColor();
                    }
                });
                runningStopwatch.Start();
                mainThread.Start();

                while (true)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Escape)
                    {
                        port.Close();
                        mainThread.Abort();
                        break;
                    }
                }
            }

        }

        static private Stopwatch lastHeaderError = new Stopwatch();
        private static void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string line = port.ReadLine();
            if (line.StartsWith("HeaderError"))
            {
                if (!lastHeaderError.IsRunning)
                    lastHeaderError.Start();
                else if (10 < lastHeaderError.ElapsedMilliseconds && lastHeaderError.ElapsedMilliseconds < 1000)
                {
                    averages++;
                    Console.WriteLine("Now uses {0} averages", averages);
                    lastHeaderError.Reset();
                }
                else if (1000 < lastHeaderError.ElapsedMilliseconds)
                    lastHeaderError.Restart();
            }
            Console.WriteLine(line);
        }

        private static void showClock()
        {
            byte[] writes = new byte[num_leds * 3];
            DateTime now = DateTime.Now;
            for (int i = 0; i < num_leds; i++)
            {
                bool isPreHour = ((double)i / num_leds) < (double)now.Hour / 24;
                double hourHue = ((double)(now.Hour + (isPreHour ? 1 : 0)) / 24);
                bool isPreMinute = (double)i / num_leds < (double)now.Minute / 60;
                bool isSecond = i == Math.Round((double)num_leds*now.Second / 60);
                var color = new Hsv()
                {
                    H = (hourHue + (isPreMinute ? 0.5 : 0))*360 % 360,
                    V = isSecond ? 0 : 1,
                    S = 1
                }.ToRgb();
                writes[i * 3] = (byte)(color.R);
                writes[i * 3 + 1] = (byte)(color.G);
                writes[i * 3 + 2] = (byte)(color.B);
            }
            sendLeds(writes);
            Thread.Sleep(100);
        }

        private static void showScreenColor()
        {
            var color = ScreenGrab.getAverageColor(1920, 1080, 32, 32);
            byte[] writes = new byte[num_leds*3];
            for(int i = 0; i < num_leds; i++)
            {
                writes[i * 3] = (byte)(color.R);
                writes[i * 3 + 1] = (byte)(color.G);
                writes[i * 3 + 2] = (byte)(color.B);
            }
            sendLeds(writes);
            Thread.Sleep(100);
        }


        static byte[] header = Encoding.ASCII.GetBytes("LEDSTRIP");
        private static void showSpectrum()
        {
            var spectrumArray = spectrum.getSpectrumLine();

            if(spectrumArray != null)
            {
                //byte[] writeArray = new byte[header.Length + spectrumArray.Length];
                //header.CopyTo(writeArray, 0);
                //spectrumArray.CopyTo(writeArray, header.Length);
                //port.Write(writeArray, 0, writeArray.Length);
            }
            Thread.Sleep(20);
        }

        static double[] totalSpectrum;
        static double[] maxSpectrum;
        static double maxSpectrumFrac = 0.8;
        static private int averages = 1;
        static private int averageI = 0;
        static private double totalAvgProgress = 0;
        static private Stopwatch spectrumWriteWatch = new Stopwatch();
        private static void showSpectrumControlled()
        {
            var spectrumArray = spectrum.getSpectrumLine();

            if (spectrumArray != null)
            {
                if (totalSpectrum == null)
                    totalSpectrum = new double[spectrumArray.Length];
                if(maxSpectrum == null)
                    maxSpectrum = new double[spectrumArray.Length];

                double avgProgress = 0.5 + (double)averageI / averages;
                totalAvgProgress += avgProgress;
                for (var i = 0; i < spectrumArray.Length; i++)
                {
                    //double thisVal = spectrumArray[i] * (0.5 + Math.Pow(2, 10 * (avgProgress - 1)));
                    double thisVal = spectrumArray[i] * avgProgress;
                    //double thisVal = spectrumArray[i];
                    totalSpectrum[i] += thisVal;
                }

                if (averageI == averages)
                {
                    var HSVArray = new IRgb[totalSpectrum.Length];
                    for (var i = 0; i < totalSpectrum.Length; i++)
                    {
                        var val = Math.Max(totalSpectrum[i]/totalAvgProgress, maxSpectrum[i]*maxSpectrumFrac);
                        maxSpectrum[i] = val;

                        var expo = Math.Pow(2, 10 * (val-1));
                        //var newRgb = new Hsv { H = (-0.1 + 0.1 * val + ((double)i / totalSpectrum.Length / 4)) * 360, V = val }.ToRgb();
                        var hue = 0.9 + 0.7 * (double)i / totalSpectrum.Length + 0.5*expo + ((double)runningStopwatch.ElapsedMilliseconds/50000);
                        var rainbow = (double)i / totalSpectrum.Length * 16;
                        HSVArray[i] = new Hsv {
                            //H = (hue < rainbow ? rainbow - val * (rainbow - hue) : hue - val * (hue - rainbow)) * 360,
                            H = (hue*360) % 360,
                            //S = 1-0.5*val,
                            S = 1,
                            V = val
                        }.ToRgb();
                    }

                    byte[] writeArray = new byte[num_leds * 3];
                    for (int i = 0; i < num_leds; i++)
                    {
                        var curVal = HSVArray[i];

                        writeArray[i * 3] = (byte)(curVal.R);
                        writeArray[i * 3 + 1] = (byte)(curVal.G);
                        writeArray[i * 3 + 2] = (byte)(curVal.B);
                    }

                    sendLeds(writeArray);
                    averageI = 0;
                    totalAvgProgress = 0;
                    totalSpectrum = null;
                    Console.WriteLine("{0}fps", 1000/Math.Max(spectrumWriteWatch.ElapsedMilliseconds, 1));
                    spectrumWriteWatch.Restart();
                }
                else
                    averageI++;
            }
            //Thread.Sleep(10);
        }

        private static void sendLeds(byte[] bytes)
        {
            byte[] writeArray = new byte[header.Length + bytes.Length];
            header.CopyTo(writeArray, 0);
            bytes.CopyTo(writeArray, header.Length);
            port.Write(writeArray, 0, writeArray.Length);
        }

        private static void showSimpleValue()
        {
            float val = getCurrentValue();
            samples[i] = val;
            if (max < val)
                max = val;

            i = (i + 1) % num_samples;
            //Console.WriteLine(new string('#', (int)(val*100)));
            string sendString = getAverage().ToString() + '\n';
            Console.WriteLine(sendString);
            port.Write(sendString);
            Thread.Sleep(1000);
        }

        private static float getAverage()
        {
            float total = 0;
            foreach(float sample in samples)
            {
                total += sample;
            }
            return total / num_samples / max;
        }

        private static float getCurrentValue()
        {
            float max = 0f;
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                        {
                            if (max < audioMeterInformation.GetPeakValue())
                                max = audioMeterInformation.GetPeakValue();
                        }
                    }
                }
            }
            return max;
        }

        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    Debug.WriteLine("DefaultDevice: " + device.FriendlyName);
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }
    }
}
