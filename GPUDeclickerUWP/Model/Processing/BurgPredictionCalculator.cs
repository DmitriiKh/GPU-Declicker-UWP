using System;

namespace GPUDeclickerUWP.Model.Processing
{
    public static class BurgPredictionCalculator
    {
        /// <summary>
        ///     Calculates one prediction error value for one sample using CPU
        ///     For details please see
        ///     "A tutorial on Burg's method, algorithm and recursion.pdf"
        /// </summary>
        public static void Calculate(
            float[] inputaudio,
            float[] forwardPredictions,
            float[] backwardPredictions,
            int position,
            int coefficientsNumber,
            int historyLengthSamples)
        {
            var b = new double[historyLengthSamples];
            var f = new double[historyLengthSamples];
            var a = new double[coefficientsNumber + 1];

            for (var I = 0; I < historyLengthSamples; I++)
            {
                b[I] = inputaudio[I + position - historyLengthSamples];
                f[I] = b[I];
            }

            var indexN = historyLengthSamples - 1;

            for (var I = 1; I <= coefficientsNumber; I++)
                a[I] = 0.0;
            a[0] = 1.0;

            var d = 0.0;
            for (var I = 0; I < historyLengthSamples; I++)
                d += 2.0 * f[I] * f[I];
            d -= f[0] * f[0] + b[indexN] * b[indexN];

            for (var k = 0; k < coefficientsNumber; k++)
            {
                var mu = 0.0;

                for (var n = 0; n <= indexN - k - 1; n++)
                    mu += f[n + k + 1] * b[n];

                if (Math.Abs(mu) > 0)
                    mu *= -2.0 / d;

                for (var n = 0; n <= (k + 1) / 2; n++)
                {
                    var t1 = a[n] + mu * a[k + 1 - n];
                    var t2 = a[k + 1 - n] + mu * a[n];
                    a[n] = t1;
                    a[k + 1 - n] = t2;
                }

                for (var n = 0; n <= indexN - k - 1; n++)
                {
                    var t1 = f[n + k + 1] + mu * b[n];
                    var t2 = b[n] + mu * f[n + k + 1];
                    f[n + k + 1] = t1;
                    b[n] = t2;
                }

                d = (1.0 - mu * mu) * d -
                    f[k + 1] * f[k + 1]
                    - b[indexN - k - 1] * b[indexN - k - 1];
            }

            var accum = 0.0;
            for (var I = 1; I <= coefficientsNumber; I++)
                accum += inputaudio[position - I] * -1 * a[I];

            forwardPredictions[position] = (float) accum;

            accum = 0.0;
            for (var I = 1; I <= coefficientsNumber; I++)
                accum += inputaudio[position - historyLengthSamples + I] *
                         -1 * a[I];

            backwardPredictions[position - historyLengthSamples] =
                (float) accum;
        }
    }
}