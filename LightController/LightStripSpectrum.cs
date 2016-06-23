using CSCore.DSP;
using System;
using System.Collections.Generic;

namespace LightController
{
    public class LightStripSpectrum : SpectrumBase
    {
        int _lights;
        public int Lights
        {
            get
            {
                return _lights;
            }
            set
            {
                if (value < 1)
                    throw new System.ArgumentOutOfRangeException("Lights");
                _lights = value;
                SpectrumResolution = value;// *innerRes;
                UpdateFrequencyMapping();
            }
        }

        private int innerRes = 4;

        public LightStripSpectrum(FftSize fftSize)
        {
            FftSize = fftSize;
        }
        
        public double[] getSpectrumLine()
        {
            var fftBuffer = new float[(int)FftSize];
            if(SpectrumProvider.GetFftData(fftBuffer, this))
            {
                double[] spectrum = new double[Lights];
                SpectrumPointData[] data = CalculateSpectrumPoints(1.0, fftBuffer);
                for (int i = 0; i < Lights; i++)
                {
                    //                    double total = 0;
                    //                    for(var j = 0; j < innerRes; j++)
                    //                    {
                    //                        total += data[i + j].Value;
                    //;                   }
                    //                    spectrum.Add(total/innerRes);
                    spectrum[i] = data[i].Value;
                }
                return spectrum;
            }
            return null;
        }
    }
}
