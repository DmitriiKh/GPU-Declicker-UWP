using GPU_Declicker_UWP_0._01;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioClickTests
    {
        private AudioClick audioClickFirst;
        private AudioClick audioClickTheSame;
        private AudioClick audioClickEqual;
        private AudioClick audioClickSecond;
        private AudioClick audioClickDifferentChannel;
        private AudioClick audioClickDifferentLength;
        private AudioClick audioClickDifferentPosition;
        private AudioClick audioClickLargerPosition;
        private AudioClick audioClickLesserPosition;

        [TestInitialize]
        public void AudioClickBeforeRunningTests()
        {
            audioClickFirst = new AudioClick(
                1111, 10, 3.7F, null, ChannelType.Left);
            audioClickTheSame = audioClickFirst;
            audioClickEqual = new AudioClick(
                1111, 10, 3.7F, null, ChannelType.Left);
            audioClickSecond = new AudioClick(
                2222, 10, 3.7F, null, ChannelType.Left);
            audioClickDifferentChannel = new AudioClick(
                1111, 10, 3.7F, null, ChannelType.Right);
            audioClickDifferentLength = new AudioClick(
                1111, 22, 3.7F, null, ChannelType.Left);
            audioClickDifferentPosition = new AudioClick(
                2222, 10, 3.7F, null, ChannelType.Left);
            audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, null, ChannelType.Left);
            audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, null, ChannelType.Left);
        }

        [TestMethod]
        public void CompareTo_ClickWithBiggerPosition_ReturnsNegative()
        {
            // CompareTo
            // A value that indicates the relative order of the objects being compared. 
            // The return value has these meanings:
            // Less than zero: This instance precedes other in the sort order.
            // Zero: This instance occurs in the same position in the sort order as other.
            // Greater than zero: This instance follows other in the sort order.
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickSecond) < 0,
                "Failed CompareTo: should return negative");
        }

        [TestMethod]
        public void CompareTo_ClickWithSmallerPosition_ReturnsPositive()
        {
            Assert.IsTrue(audioClickSecond.CompareTo(audioClickFirst) > 0,
                "Failed CompareTo: should return positive");
        }

        [TestMethod]
        public void CompareTo_TheSameClick_ReturnsZero()
        { 
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickTheSame) == 0,
                "Failed CompareTo: should be zero (the same)");
        }

        [TestMethod]
        public void CompareTo_EqualClick_ReturnsZero()
        {
            Assert.IsTrue(audioClickFirst.CompareTo(audioClickEqual) == 0,
                "Failed CompareTo: should be zero (equal)");
        }

        [TestMethod]
        public void Equals_TheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst.Equals(audioClickTheSame),
                "Failed Equals: should be true (the same)");
        }

        [TestMethod]
        public void Equals_EqualClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst.Equals(audioClickEqual),
                "Failed Equals: should be true (equal)");
        }

        [TestMethod]
        public void Equals_DifferentClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst.Equals(audioClickSecond),
                "Failed Equals: should be false (different)");
        }

        [TestMethod]
        public void GetHashCode_TheSameClick_ReturnsTheSameHashCode()
        {
            Assert.AreEqual(audioClickFirst.GetHashCode(), 
                audioClickTheSame.GetHashCode(),
                "Failed GetHashCode: should be equal (the same)");
        }

        [TestMethod]
        public void GetHashCode_EqualClicks_ReturnsTheSameHashCode()
        {
            Assert.AreEqual(audioClickFirst.GetHashCode(), 
                audioClickEqual.GetHashCode(),
                "Failed GetHashCode: should be equal (equal)");
        }

        [TestMethod]
        public void GetHashCode_ClicksWithDifferentPositions_ReturnDifferentHashCodes()
        {
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), 
                audioClickDifferentPosition.GetHashCode(),
                "Failed GetHashCode: should be not equal (different position)");
        }

        [TestMethod]
        public void GetHashCode_ClicksWithDifferentLength_ReturnDifferentHaskCodes()
        {
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), audioClickDifferentLength.GetHashCode(),
                "Failed GetHashCode: should be not equal (different length)");
        }

        [TestMethod]
        public void GetHashCode_ClicksFromDifferentChannels_ReturnDifferentHashCodes()
        {
            Assert.AreNotEqual(audioClickFirst.GetHashCode(), audioClickDifferentChannel.GetHashCode(),
                "Failed GetHashCode: should be not equal (different channel)");
        }

        [TestMethod]
        public void OperatorEqual_TheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst == audioClickTheSame,
                "Failed Operator ==: should be true (the same)");
        }

        [TestMethod]
        public void OperatorEqual_ClickWithAllTheSameValues_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst == audioClickEqual,
                "Failed Operator ==: should be true (equal)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksWithDifferentPjsitiions_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst == audioClickDifferentPosition,
                "Failed Operator ==: should be false (different position)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksWithDifferentLengths_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst == audioClickDifferentLength,
                "Failed Operator ==: should be false (different length)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksFromDifferentChannels_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst == audioClickDifferentChannel,
                "Failed Operator ==: should be false (different channel)");
        }

        [TestMethod]
        public void OperatorNotEqual_TheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst != audioClickTheSame,
                "Failed Operator !=: should be false (the same)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithAllTheSameValues_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst != audioClickEqual,
                "Failed Operator !=: should be false (equal)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithDifferentPositions_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst != audioClickDifferentPosition,
                "Failed Operator !=: should be true (different position)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithDifferentLength_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst != audioClickDifferentLength,
                "Failed Operator !=: should be true (different length)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksDifferentChannels_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst != audioClickDifferentChannel,
                "Failed Operator !=: should be true (different channel)");
        }
        
        [TestMethod]
        public void OperatorLess_ComparitionToLesserPositionClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst < audioClickLesserPosition,
                "Failed Operator <: should be false (lesser position)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToTheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst < audioClickTheSame,
                "Failed Operator <: should be false (the same)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToEqualClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst < audioClickEqual,
                "Failed Operator <: should be false (equal)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst < audioClickLargerPosition,
                "Failed Operator <: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToLesserPositionClick_ReturnsFals()
        {
            Assert.IsFalse(audioClickFirst <= audioClickLesserPosition,
                "Failed Operator <=: should be false (lesser position)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToTheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst <= audioClickTheSame,
                "Failed Operator <=: should be true (the same)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToEqualClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst <= audioClickEqual,
                "Failed Operator <=: should be true (equal)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst <= audioClickLargerPosition,
                "Failed Operator <=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToLesserPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst > audioClickLesserPosition,
                "Failed Operator >: should be true (lesser position)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToTheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst > audioClickTheSame,
                "Failed Operator >: should be false (the same)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToEqualClick_ReturnsFalse()
        {
            Assert.IsFalse(audioClickFirst > audioClickEqual,
                "Failed Operator >: should be false (equal)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsFalse(audioClickFirst > audioClickLargerPosition,
                "Failed Operator >: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToLesserPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst >= audioClickLesserPosition,
                "Failed Operator >=: should be true (lesser position)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToTheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst >= audioClickTheSame,
                "Failed Operator >=: should be true (the same)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToEqualClick_ReturnsTrue()
        {
            Assert.IsTrue(audioClickFirst >= audioClickEqual,
                "Failed Operator >=: should be true (equal)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsFalse(audioClickFirst >= audioClickLargerPosition,
                "Failed Operator >=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void ChangeAproved_SwitchingTwice_SwitchesAproved()
        {
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
    }
}
