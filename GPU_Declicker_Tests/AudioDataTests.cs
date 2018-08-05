using GPUDeclickerUWP;
using GPUDeclickerUWP.Model.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioDataTests
    {
        private AudioClick _audioClick;
        private AudioData _audioData;
        private float[] inputAudio;

        [TestInitialize]
        public void AudioDataBeforeRunningTest()
        {
            inputAudio = new float[4096];
            for (var index = 0; index < inputAudio.Length; index++)
                inputAudio[index] = (float)System.Math.Sin(
                    2 * System.Math.PI * index / (512 / 5.2));
            
            _audioData = new AudioDataMono((float[])inputAudio.Clone());

            // damages samples
            for (var index = 1051; index < 1059; index++)
                _audioData.SetInputSample(index, -0.5f);

            for (var index = 0; index < inputAudio.Length; index++)
                _audioData.SetOutputSample(
                    index,
                    _audioData.GetInputSample(index));

            _audioClick = new AudioClick(
                1051,
                10,
                111,
                _audioData, //new AudioDataMono(inputAudio),
                ChannelType.Left);
        }

        [TestMethod]
        public void OnClickChanged_Shrinked_RestoreSampleAndRepair()
        {
            _audioData.SetOutputSample(_audioClick.Position - 1, 0);
            _audioData.SetOutputSample(_audioClick.Position + _audioClick.Length, 0);

            _audioData.OnClickChanged(
                _audioClick,
                new ClickEventArgs {Shrinked = true});

            for (var index = 0; index < _audioData.LengthSamples(); index++)
                Assert.AreEqual(
                    inputAudio[index],
                    _audioData.GetOutputSample(index),
                    0.0001,
                    "error at index " + index);
        }

        [TestMethod]
        public void OnClickChanged_NotShrinked_Repair()
        {
            _audioData.OnClickChanged(
                _audioClick,
                new ClickEventArgs {Shrinked = false});

            for (var index = 0; index < _audioData.LengthSamples(); index++)
                Assert.AreEqual(
                    inputAudio[index],
                    _audioData.GetOutputSample(index),
                    0.0001,
                    "error at index " + index);
        }
    }
}