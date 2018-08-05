using GPUDeclickerUWP.Model.Data;
using GPUDeclickerUWP.View;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioViewerTests
    {
        [UITestMethod]
        public void Fill_HighFreqSignal_DrawsMaxAndMins()
        {
            var audioViewer = new AudioViewer
            {
                WaveFormWidth = 100,
                WaveFormHeight = 100
            };

            // length big enough to have 100 audio samples per 
            // one sample on WaveForm
            var audioLength = (int) audioViewer.WaveFormWidth * 100;

            var inputAudio = new float[audioLength];

            for (var i = 0; i < inputAudio.Length; i++)
                inputAudio[i] = (float) Math.Sin(2 * Math.PI * i /
                                                  (audioLength /
                                                   (audioViewer.WaveFormWidth * 5) 
                                                      // 5 waves per one sample 
                                                      // on WaveForm
                                                  ));

            var audioData =
                new AudioDataMono(inputAudio);

            audioViewer.Fill(); ////////audioData);

            foreach (var p in audioViewer.LeftChannelWaveFormPoints)
                // each point should be at max (top) 
                // or at min (bottom of audioViewer)
                Assert.IsTrue(
                    Math.Abs(p.Y) < 0.001 || 
                    Math.Abs(p.Y - audioViewer.WaveFormHeight) < 0.001);
        }
    }
}