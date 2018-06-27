namespace GPU_Declicker_UWP_0._01
{
    public static class BurgPredictionCalculator
    {
        /// <summary>
        /// Calculates one prediction error value for one sample using CPU
        /// For details please see 
        /// "A tutorial on Burg's method, algorithm and recursion.pdf"
        /// </summary>
        public static void Calculate(
            float[] inputaudio, 
            float[] forwardPredictions,
            float[] backwardPredictions, 
            int position,
            int coefficientsNumber,
            int historyLengthSamples)
        {
            double[] b = new double[historyLengthSamples];
            double[] f = new double[historyLengthSamples];
            double[] a = new double[(coefficientsNumber + 1)];
            double ACCUM;
            double D = 0.0, mu = 0.0;

            for (int I = 0; I < historyLengthSamples; I++)
            {
                b[I] = inputaudio[I + position - historyLengthSamples];
                f[I] = b[I];
            }

            int N = historyLengthSamples - 1;

            for (int I = 1; I <= coefficientsNumber; I++)
                a[I] = 0.0;
            a[0] = 1.0;

            D = 0.0;
            for (int I = 0; I < historyLengthSamples; I++)
                D += 2.0 * f[I] * f[I];
            D -= f[0] * f[0] + b[N] * b[N];

            for (int k = 0; k < coefficientsNumber; k++)
            {
                mu = 0.0;
                for (int n = 0; n <= N - k - 1; n++)
                    mu += f[n + k + 1] * b[n];

                if (mu != 0)
                    mu *= -2.0 / D;

                for (int n = 0; n <= (k + 1) / 2; n++)
                {
                    double t1 = a[n] + mu * a[k + 1 - n];
                    double t2 = a[k + 1 - n] + mu * a[n];
                    a[n] = t1;
                    a[k + 1 - n] = t2;
                }

                for (int n = 0; n <= N - k - 1; n++)
                {
                    double t1 = f[n + k + 1] + mu * b[n];
                    double t2 = b[n] + mu * f[n + k + 1];
                    f[n + k + 1] = t1;
                    b[n] = t2;
                }

                D = (1.0 - mu * mu) * D -
                    f[k + 1] * f[k + 1]
                    - b[N - k - 1] * b[N - k - 1];
            }

            ACCUM = 0.0;
            for (int I = 1; I <= coefficientsNumber; I++)
                ACCUM += inputaudio[position - I] * (-1) * a[I];

            forwardPredictions[position] = (float)ACCUM;

            ACCUM = 0.0;
            for (int I = 1; I <= coefficientsNumber; I++)
                ACCUM += inputaudio[position - historyLengthSamples + I] *
                    (-1) * a[I];

            backwardPredictions[position - historyLengthSamples] =
                (float)ACCUM;
        }
    }
}
