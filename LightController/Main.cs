using ColorMine.ColorSpaces;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.DSP;
using CSCore.SoundIn;
using CSCore.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightController
{
    public partial class Main : Form
    {
        SerialPort port;

        public int num_leds = 100 / 1;

        int i = 0;
        int num_samples = 3;
        float[] samples;
        float max;

        Thread mainThread;

        FftProvider provider;
        LightStripSpectrum spectrum;
        Stopwatch runningStopwatch = new Stopwatch();

        WasapiLoopbackCapture capture;

        public Main()
        {
            InitializeComponent();
            port = new SerialPort("COM3", 115200);
            port.Open();

            port.DataReceived += Port_DataReceived;

            samples = new float[num_samples];

            capture = new WasapiLoopbackCapture();
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
                    switch (mode)
                    {
                        case Mode.Spectrum:
                            showSpectrumControlled();
                            break;
                        case Mode.Clock:
                            showClock();
                            break;
                        case Mode.Screen:
                            showScreenColor();
                            break;
                        case Mode.Off:
                            showColor(new Rgb() { R = 0, G = 0, B = 0 });
                            break;
                        default:
                            break;
                    }
                    //showSimpleValue();
                    //showSpectrum();
                    //showScreenColor();
                }
            });
            runningStopwatch.Start();
            mainThread.Start();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private Stopwatch lastHeaderError = new Stopwatch();
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
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

        private void showClock()
        {
            byte[] writes = new byte[num_leds * 3];
            DateTime now = DateTime.Now;
            for (int i = 0; i < num_leds; i++)
            {
                bool isPreHour = ((double)i / num_leds) < (double)now.Hour / 24;
                double hourHue = ((double)(now.Hour + (isPreHour ? 1 : 0)) / 24);
                bool isPreMinute = (double)i / num_leds < (double)now.Minute / 60;
                bool isSecond = i == Math.Round((double)num_leds * now.Second / 60);
                var color = new Hsv()
                {
                    H = (hourHue + (isPreMinute ? 0.5 : 0)) * 360 % 360,
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

        private void showColor(IRgb color)
        {
            byte[] writeBytes = new byte[num_leds * 3];
            for(int i = 0; i < num_leds; i++)
            {
                writeBytes[i*3] = (byte)color.R;
                writeBytes[i * 3+1] = (byte)color.G;
                writeBytes[i * 3+2] = (byte)color.B;
            }
            sendLeds(writeBytes);
            Thread.Sleep(1000);
        }

        private void showScreenColor()
        {
            var color = ScreenGrab.getAverageColor(1920, 1080, 32, 32);
            showColor(color);
            Thread.Sleep(100);
        }


        byte[] header = Encoding.ASCII.GetBytes("LEDSTRIP");
        private void showSpectrum()
        {
            var spectrumArray = spectrum.getSpectrumLine();

            if (spectrumArray != null)
            {
                //byte[] writeArray = new byte[header.Length + spectrumArray.Length];
                //header.CopyTo(writeArray, 0);
                //spectrumArray.CopyTo(writeArray, header.Length);
                //port.Write(writeArray, 0, writeArray.Length);
            }
            Thread.Sleep(20);
        }

        double[] totalSpectrum;
        double[] maxSpectrum;
        double maxSpectrumFrac = 0.8;
        private int averages = 1;
        private int averageI = 0;
        private double totalAvgProgress = 0;
        private Stopwatch spectrumWriteWatch = new Stopwatch();
        private void showSpectrumControlled()
        {
            var spectrumArray = spectrum.getSpectrumLine();

            if (spectrumArray != null)
            {
                if (totalSpectrum == null)
                    totalSpectrum = new double[spectrumArray.Length];
                if (maxSpectrum == null)
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
                        var val = Math.Max(totalSpectrum[i] / totalAvgProgress, maxSpectrum[i] * maxSpectrumFrac);
                        maxSpectrum[i] = val;

                        var expo = Math.Pow(2, 10 * (val - 1));
                        var valueHue = 0.32 + expo * 0.2 + val * 0.2;
                        //var newRgb = new Hsv { H = (-0.1 + 0.1 * val + ((double)i / totalSpectrum.Length / 4)) * 360, V = val }.ToRgb();
                        var hue = 0.9 + 0.4 * (double)i / totalSpectrum.Length + 0.5 * expo + ((double)runningStopwatch.ElapsedMilliseconds / 50000);
                        var rainbow = (double)i / totalSpectrum.Length * 16;
                        HSVArray[i] = new Hsv
                        {
                            //H = (hue < rainbow ? rainbow - val * (rainbow - hue) : hue - val * (hue - rainbow)) * 360,
                            // H = (hue*360) % 360,
                            H = (valueHue * 360) % 360,
                            //S = 1-0.5*val,
                            S = 1,
                            V = val
                        }.ToRgb();
                    }

                    byte[] writeArray = new byte[num_leds * 3];
                    for (int i = 0; i < num_leds; i++)
                    {
                        Func<double, double> valFunc = (double val) => {
                            var valNorm = val / 255;
                            return valNorm * valNorm * (3.0 - 2.0 * valNorm);
                        };
                        var curVal = HSVArray[i];
                        var rMod = 1 - 0.5 * curVal.R / 360;
                        writeArray[i * 3] = (byte)(curVal.R);
                        writeArray[i * 3 + 1] = (byte)(curVal.G * rMod);
                        writeArray[i * 3 + 2] = (byte)(curVal.B * rMod);
                        //writeArray[i * 3] = (byte)(valFunc(curVal.R));
                        //writeArray[i * 3 + 1] = (byte)(valFunc(curVal.G));
                        //writeArray[i * 3 + 2] = (byte)(valFunc(curVal.B));
                    }

                    sendLeds(writeArray);
                    averageI = 0;
                    totalAvgProgress = 0;
                    totalSpectrum = null;
                    Console.WriteLine("{0}fps", 1000 / Math.Max(spectrumWriteWatch.ElapsedMilliseconds, 1));
                    spectrumWriteWatch.Restart();
                }
                else
                    averageI++;
            }
            //Thread.Sleep(10);
        }

        private void sendLeds(byte[] bytes)
        {
            if (bytes.Length != num_leds * 3)
                throw new IndexOutOfRangeException("The given sendLeds array is the incorrect size");
            byte[] writeArray = new byte[header.Length + bytes.Length];
            header.CopyTo(writeArray, 0);
            bytes.CopyTo(writeArray, header.Length);
            port.Write(writeArray, 0, writeArray.Length);
        }

        private void showSimpleValue()
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

        private float getAverage()
        {
            float total = 0;
            foreach (float sample in samples)
            {
                total += sample;
            }
            return total / num_samples / max;
        }

        private float getCurrentValue()
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

        private AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        enum Mode
        {
            Spectrum,
            Clock,
            Screen,
            Off
        }
        Mode mode = Mode.Off;

        private void mode_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox)sender;
            mode = (Mode)Enum.Parse(typeof(Mode), listBox.SelectedItem.ToString());
        }
    }
}
