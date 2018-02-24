using GPU_Declicker_UWP_0._01;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioClickTest
    {
        [TestMethod]
        public void AudioClickConstractorTest()
        {
            AudioClick audioClick = new AudioClick(
                1000, 10, 3.7F, null, null, ChannelType.Left);

            Assert.AreEqual(1000, audioClick.Position);
            Assert.AreEqual(10, audioClick.Lenght);
            Assert.AreEqual(3.7F, audioClick.Threshold_level_detected);
            Assert.AreEqual(ChannelType.Left, audioClick.ChannelType);
        }

        [TestMethod]
        public void AudioClickCompareToTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickSecond = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);

            // CompareTo
            // A value that indicates the relative order of the objects being compared. 
            // The return value has these meanings:
            // Less than zero: This instance precedes other in the sort order.
            // Zero: This instance occurs in the same position in the sort order as other.
            // Greater than zero: This instance follows other in the sort order.
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickSecond) < 0,
                "Failed CompareTo: should return negative");
            Assert.IsTrue(audioClickSecond.CompareTo(audioClickFirst) > 0,
                "Failed CompareTo: should return positive");
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickTheSame) == 0,
                "Failed CompareTo: should be zero (the same)");
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickEqual) == 0,
                "Failed CompareTo: should be zero (equal)");
        }

        [TestMethod]
        public void AudioClickEqualsTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickSecond = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);
            
            Assert.IsTrue(audioClickFirst.Equals(audioClickTheSame),
                "Failed Equals: should be true (the same)");
            Assert.IsTrue(audioClickFirst.Equals(audioClickEqual),
                "Failed Equals: should be true (equal)");
            Assert.IsFalse(audioClickFirst.Equals(audioClickSecond),
                "Failed Equals: should be false (the same)");
        }

        [TestMethod]
        public void AudioClickGetHashCodeTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentChannel = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Right);
            AudioClick audioClickDifferentLength = new AudioClick(
                1111, 22, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);

            Assert.AreEqual(audioClickFirst.GetHashCode(), audioClickTheSame.GetHashCode(),
                "Failed GetHashCode: should be equal (the same)");
            Assert.AreEqual(audioClickFirst.GetHashCode(), audioClickEqual.GetHashCode(),
                "Failed GetHashCode: should be equal (equal)");
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), audioClickDifferentPosition.GetHashCode(),
                "Failed GetHashCode: should be not equal (different position)");
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), audioClickDifferentLength.GetHashCode(),
                "Failed GetHashCode: should be not equal (different length)");
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), audioClickDifferentChannel.GetHashCode(),
                "Failed GetHashCode: should be not equal (different channel)");
        }

        [TestMethod]
        public void AudioClickOperatorEqualTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentChannel = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Right);
            AudioClick audioClickDifferentLength = new AudioClick(
                1111, 22, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsTrue(audioClickFirst == audioClickTheSame,
                "Failed Operator ==: should be true (the same)");
            Assert.IsTrue(audioClickFirst == audioClickEqual,
                "Failed Operator ==: should be true (equal)");
            Assert.IsFalse(audioClickFirst == audioClickDifferentPosition,
                "Failed Operator ==: should be false (different position)");
            Assert.IsFalse(audioClickFirst == audioClickDifferentLength,
                "Failed Operator ==: should be false (different length)");
            Assert.IsFalse(audioClickFirst == audioClickDifferentChannel,
                "Failed Operator ==: should be false (different channel)");
        }

        [TestMethod]
        public void AudioClickOperatorNotEqualTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentChannel = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Right);
            AudioClick audioClickDifferentLength = new AudioClick(
                1111, 22, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickDifferentPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsFalse(audioClickFirst != audioClickTheSame,
                "Failed Operator !=: should be false (the same)");
            Assert.IsFalse(audioClickFirst != audioClickEqual,
                "Failed Operator !=: should be false (equal)");
            Assert.IsTrue(audioClickFirst != audioClickDifferentPosition,
                "Failed Operator !=: should be true (different position)");
            Assert.IsTrue(audioClickFirst != audioClickDifferentLength,
                "Failed Operator !=: should be true (different length)");
            Assert.IsTrue(audioClickFirst != audioClickDifferentChannel,
                "Failed Operator !=: should be true (different channel)");
        }

        [TestMethod]
        public void AudioClickOperatorLessTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsFalse(audioClickFirst < audioClickLesserPosition,
                "Failed Operator <: should be false (lesser position)");
            Assert.IsFalse(audioClickFirst < audioClickTheSame,
                "Failed Operator <: should be false (the same)");
            Assert.IsFalse(audioClickFirst < audioClickEqual,
                "Failed Operator <: should be false (equal)");
            Assert.IsTrue(audioClickFirst < audioClickLargerPosition,
                "Failed Operator <: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void AudioClickOperatorLessOrEqualTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsFalse(audioClickFirst <= audioClickLesserPosition,
                "Failed Operator <=: should be false (lesser position)");
            Assert.IsTrue(audioClickFirst <= audioClickTheSame,
                "Failed Operator <=: should be true (the same)");
            Assert.IsTrue(audioClickFirst <= audioClickEqual,
                "Failed Operator <=: should be true (equal)");
            Assert.IsTrue(audioClickFirst <= audioClickLargerPosition,
                "Failed Operator <=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void AudioClickOperatorMoreTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsTrue(audioClickFirst > audioClickLesserPosition,
                "Failed Operator >: should be true (lesser position)");
            Assert.IsFalse(audioClickFirst > audioClickTheSame,
                "Failed Operator >: should be false (the same)");
            Assert.IsFalse(audioClickFirst > audioClickEqual,
                "Failed Operator >: should be false (equal)");
            Assert.IsFalse(audioClickFirst > audioClickLargerPosition,
                "Failed Operator >: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void AudioClickOperatorMoreOrEqualTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickTheSame = audioClickFirst;
            AudioClick audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, null, null, ChannelType.Left);
            AudioClick audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, null, null, ChannelType.Left);

            Assert.IsTrue(audioClickFirst >= audioClickLesserPosition,
                "Failed Operator >=: should be true (lesser position)");
            Assert.IsTrue(audioClickFirst >= audioClickTheSame,
                "Failed Operator >=: should be true (the same)");
            Assert.IsTrue(audioClickFirst >= audioClickEqual,
                "Failed Operator >=: should be true (equal)");
            Assert.IsFalse(audioClickFirst >= audioClickLargerPosition,
                "Failed Operator >=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void AudioClickChangeAprovedTest()
        {
            AudioClick audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, null, ChannelType.Left);

            bool aprovedState = audioClickFirst.Aproved;
            // first change
            audioClickFirst.ChangeAproved();
            Assert.AreNotEqual(audioClickFirst.Aproved, aprovedState, 
                "Failed ChangeAproved (first change): should not be " 
                + aprovedState.ToString());
            // second change
            audioClickFirst.ChangeAproved();
            Assert.AreEqual(audioClickFirst.Aproved, aprovedState,
                "Failed ChangeAproved (second change): should be "
                + aprovedState.ToString());
        }

        [TestMethod]
        public void AudioClickGetInputSampleTest()
        {
            float[] inputAudio = new float[100];

            for (int index = 0; index < inputAudio.Length; index++)
                inputAudio[index] = (float)Math.Sin(
                    2 * Math.PI * index / (inputAudio.Length / 5.3));
            AudioData audioDataForTest = new AudioDataMono(inputAudio);

            AudioClick audioClickForTest = new AudioClick(
                audioDataForTest.LengthSamples() / 3,
                audioDataForTest.LengthSamples() / 10,
                10F,
                audioDataForTest,
                null,
                ChannelType.Left);

            for (int index = 0; index < audioDataForTest.LengthSamples(); index++)
                Assert.AreEqual(
                    audioClickForTest.GetInputSample(index),
                    audioDataForTest.GetInputSample(index),
                    "Failed GetInputSample for index" +
                    index.ToString());
        }

        [TestMethod]
        public void AudioClickGetOutputSampleTest()
        {
            float[] inputAudio = new float[100];

            for (int index = 0; index < inputAudio.Length; index++)
                inputAudio[index] = (float)Math.Sin(
                    2 * Math.PI * index / (inputAudio.Length / 5.3));
            AudioData audioDataForTest = new AudioDataMono(inputAudio);
            for (int index = 0; index < inputAudio.Length; index++)
                audioDataForTest.SetOutputSample(
                    index,
                    audioDataForTest.GetInputSample(index));

            AudioClick audioClickForTest = new AudioClick(
            audioDataForTest.LengthSamples() / 3,
            audioDataForTest.LengthSamples() / 10,
            10F,
            audioDataForTest,
            null,
            ChannelType.Left);

            for (int index = 0; index < audioDataForTest.LengthSamples(); index++)
                Assert.AreEqual(
                    audioClickForTest.GetOutputSample(index),
                    audioDataForTest.GetOutputSample(index),
                    "Failed GetInputSample for index" +
                    index.ToString());
        }

        [TestMethod]
        public void AudioClickShrinkLeftTest()
        {
            float[] inputAudio = new float[4000];

            for (int index = 0; index < inputAudio.Length; index++)
                inputAudio[index] = (float)Math.Sin(
                    2 * Math.PI * index / (inputAudio.Length / 5.3));
            AudioData audioDataForTest = new AudioDataMono(inputAudio);
            AudioProcessing audioProcessingForTest = new AudioProcessing(
                512,
                4,
                3.5F,
                250);

            int initialPosition = 2000;
            int initialLength = 50;
            AudioClick audioClickForTest = new AudioClick(
                initialPosition, 
                initialLength, 
                10,
                audioDataForTest,
                audioProcessingForTest, 
                ChannelType.Left);

            audioClickForTest.ShrinkLeft();

            Assert.AreEqual(audioClickForTest.Position,
                initialPosition + 1,
                "Failed ShrinkLeft: position is not right");
            Assert.AreEqual(audioClickForTest.Lenght,
                initialLength - 1,
                "Failed ShrinkLeft: length is not right");
        }
    }
}
