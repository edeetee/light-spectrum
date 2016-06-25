﻿using ColorMine.ColorSpaces;
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

        public int num_leds = 200;

        Thread mainThread;

        FftProvider provider;
        LightStripSpectrum spectrum;
        Stopwatch runningStopwatch = new Stopwatch();

        WasapiLoopbackCapture capture;

        bool running = true;

        public Main()
        {
            InitializeComponent();
            port = new SerialPort("COM3", 115200);
            port.Open();

            port.DataReceived += Port_DataReceived;

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
                MinimumFrequency = 80,
                UseAverage = false,
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
                while (running)
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
                            Thread.Sleep(50);
                            break;
                        case Mode.Debug:
                            showDebug();
                            break;
                        default:
                            break;
                    }
                    //showSimpleValue();
                    //showSpectrum();
                    //showScreenColor();
                }
                port.Close();
                capture.Stop();
                Application.Exit();
                Environment.Exit(1);
            });
            runningStopwatch.Start();
            spectrumWriteWatch.Start();
            mainThread.Start();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void Main_FormClosing(Object sender, FormClosingEventArgs e)
        {
            running = false;
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

        private void showDebug()
        {
            byte[] writes = new byte[num_leds * 3];
            DateTime now = DateTime.Now;
            int secondI = (int)now.Second % num_leds;
            for (int i = 0; i < num_leds; i++)
            {
                //bool isSecond = secondI - 1 <= i && i <= secondI + 1 && i != secondI;
                bool isSecond = i != secondI;
                var color = new Hsv()
                {
                    H = 0,
                    V = isSecond ? 0 : 1,
                    S = 0
                }.ToRgb();
                writes[i * 3] = (byte)(color.R);
                writes[i * 3 + 1] = (byte)(color.G);
                writes[i * 3 + 2] = (byte)(color.B);
            }
            sendLeds(writes);
            Thread.Sleep(100);
        }

        private void showClock()
        {
            byte[] writes = new byte[num_leds * 3];
            DateTime now = DateTime.Now;
            int secondI = (int)Math.Round((double)num_leds * now.Second / 60);
            for (int i = 0; i < num_leds; i++)
            {
                bool isPreHour = ((double)i / num_leds) < (double)now.Hour / 24;
                double hourHue = ((double)(now.Hour + (isPreHour ? 1 : 0)) / 24);
                bool isPreMinute = (double)i / num_leds < (double)now.Minute / 60;

                //bool isSecond = secondI - 1 <= i && i <= secondI + 1 && i != secondI;
                bool isSecond = i != secondI;
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
            byte[] writeBytes = new byte[num_leds];
            for(int i = 0; i < num_leds; i++)
            {
                writeBytes[i] = (byte)(color.To<Hsv>().H / 360 * 255);
            //    writeBytes[i*3] = (byte)color.R;
            //    writeBytes[i * 3+1] = (byte)color.G;
            //    writeBytes[i * 3+2] = (byte)color.B;
            }
            sendBytes(writeBytes);
        }

        private void showScreenColor()
        {
            var color = ScreenGrab.getAverageColor(1920, 1080, 32, 32);
            showColor(color);
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
        
        double[] maxSpectrum;
        SpectrumQueue totalSpectrums = new SpectrumQueue(30);
        double maxVal = 0;
        private int averages = 1;
        private int averageI = 0;
        private Stopwatch spectrumWriteWatch = new Stopwatch();
        private void showSpectrumControlled()
        {
            var spectrumArray = spectrum.getSpectrumLine();

            if (spectrumArray != null)
            {
                if (maxSpectrum == null)
                    maxSpectrum = new double[spectrumArray.Length];

                totalSpectrums.Add(spectrumArray);

                if (averageI == averages)
                {
                    var byteWrite = new byte[num_leds];
                    int i = 0;
                    foreach (var average in totalSpectrums.getAverages(SpectrumQueue.circ))
                    {
                        double val = average;
                        //val = Math.Max(average, maxSpectrum[i] * maxSpectrumFrac);
                        if (maxSpectrum[i] < val)
                        {
                            maxSpectrum[i] = val;
                        }
                        else
                        {
                            maxSpectrum[i] *= 0.9999999;
                        }
                        byteWrite[i] = (byte)(val/maxSpectrum[i] * 255);
                        //byteWrite[i] = (byte)(val * 255);
                        i++;
                    }
                    // sendLeds(writeArray);
                    sendBytes(byteWrite);
                    averageI = 0;
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

        private void sendBytes(byte[] bytes)
        {
            if (bytes.Length != num_leds)
                throw new IndexOutOfRangeException("The given sendBytes array is the incorrect size");
            byte[] writeArray = new byte[header.Length + bytes.Length];
            header.CopyTo(writeArray, 0);
            bytes.CopyTo(writeArray, header.Length);
            port.Write(writeArray, 0, writeArray.Length);
            setFPS();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        enum Mode
        {
            Debug,
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

        Label fpsLabel;
        private void fps_value_Paint(object sender, PaintEventArgs e)
        {
            fpsLabel = (Label)sender;
        }

        private void setFPS()
        {
            if(fpsLabel != null)
            {
                fpsLabel.BeginInvoke((MethodInvoker)delegate ()
                {
                    fpsLabel.Text = string.Format("{0}Hz", 1000 / Math.Max(1, spectrumWriteWatch.ElapsedMilliseconds));
                    spectrumWriteWatch.Restart();
                });

            }
        }
    }
}
