using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using Microsoft.VisualBasic;
using MPI;
namespace Lab4
{
    class Program
    {
        static void Main(string[] args)
        {
            double f(double x) => x / (x - 1);
            int n = 1000;

            MPI.Environment.Run(ref args, comm =>
            {
                if (comm.Rank == 0)
                {
                    double step = 1 / Convert.ToDouble(comm.Size) ;
                    // program for rank 0
                    List<Value>  values = new List<Value>();
                    double lowCount = 2;
                    for (int i = 0; i < comm.Size; i++)
                    {
                        Value value = new Value(lowCount, lowCount + step, step);
                        lowCount += step;
                        values.Add(value);
                    }
                    var msg = comm.Scatter( values.ToArray(), 0);
                    double result = LeftRectangle(f, msg.Low, msg.Low + msg.Step, n);
                    Console.WriteLine("Rank " + comm.Rank + " calculated value \"" + result + "\".");
                    // receive the final message
                    var finalValues = comm.Gather(new Value(), 0);
                   Console.WriteLine( "Final Value: \"" + (finalValues.Select(x=> x.Result).Sum() + result) + "\".");

                }
                else // not rank 0
                {

                 
                    // program for all other ranks
                    Value msg = comm.Scatter<Value>(0);
                    double result =  LeftRectangle(f, msg.Low, msg.Low + msg.Step, n);
                    msg.Low = msg.Low + msg.Step;
                    msg.Result += result;
                    Console.WriteLine("Rank " + comm.Rank + " calculated value \"" + result + "\".");

                    comm.Gather(msg, 0);

                }
            });

        }
        public static double LeftRectangle(Func<double, double> f, double a = 2, double b = 3, int n = 1000)
        {
            var h = (b - a) / n;
            var sum = 0d;
            for (var i = 0; i <= n - 1; i++)
            {
                var x = a + i * h;
                sum += f(x);
            }

            var result = h * sum;
            return result;
        }
        [Serializable]
        public class Value
        {
            public double High;
            public double Low;
            public double Step;
            public double Result;

            public Value()
            {
            }
            public Value(double low, double high, double step)
            {
                Low = low;
                High = high;
                Step = step;
            }

            public override string ToString()
            {
                return $"High: {High}, Low: {Low}, Step: {Step}";
            }
        }
    }


}
