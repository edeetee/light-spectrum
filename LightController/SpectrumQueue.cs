using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController
{
    class SpectrumQueue
    {
        public static Func<double, double, double> expo = (val, spectrumFract) =>
        {
            return val * Math.Pow(2, 10 * (spectrumFract - 1));
        };

        public static Func<double, double, double> cubic = (val, spectrumFract) =>
        {
            return val * spectrumFract * spectrumFract * spectrumFract;
        };

        public static Func<double, double, double> circ = (val, spectrumFract) =>
        {
            var invFract = spectrumFract-1;
            return val * Math.Sqrt(1 - invFract * invFract);
        };

        public int length;
        Queue<double[]> queue;
        public SpectrumQueue(int length)
        {
            queue = new Queue<double[]>();
            this.length = length;

        }

        public double getAverage(int i, Func<double, double, double> modifier)
        {
            int arrayI = 0;
            double total = 0;
            foreach(double[] spectrum in queue){
                total += modifier(spectrum[i], (double)arrayI / length);
                arrayI++;
            }
            return total / arrayI;
        }

        public double[] getAverages(Func<double, double, double> modifier)
        {
            int arrayI = 0;
            double[] totals = new double[queue.Peek().Length];
            foreach (double[] spectrum in queue)
            {
                for(var i = 0; i < spectrum.Length; i++)
                {
                    totals[i] += modifier(spectrum[i], (double)arrayI/length)/length;
                }
                arrayI++;
            }
            return totals;
        }

        public void Add(double[] item)
        {
            if (queue.Count == length)
                queue.Dequeue();
            queue.Enqueue(item);
        }
    }
}
