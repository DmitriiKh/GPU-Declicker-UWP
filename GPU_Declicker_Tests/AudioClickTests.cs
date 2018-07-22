using GPUDeclickerUWP;
using GPUDeclickerUWP.Model.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioClickTests
    {
        private AudioClick _audioClickDifferentChannel;
        private AudioClick _audioClickDifferentLength;
        private AudioClick _audioClickDifferentPosition;
        private AudioClick _audioClickEqual;
        private AudioClick _audioClickFirst;
        private AudioClick _audioClickLargerPosition;
        private AudioClick _audioClickLesserPosition;
        private AudioClick _audioClickSecond;
        private AudioClick _audioClickTheSame;

        [TestInitialize]
        public void AudioClickBeforeRunningTests()
        {
            var audioData = new AudioDataMono(new float[100]);

            _audioClickFirst = new AudioClick(
                1111, 10, 3.7F, audioData, ChannelType.Left);
            _audioClickTheSame = _audioClickFirst;
            _audioClickEqual = new AudioClick(
                1111, 10, 3.7F, audioData, ChannelType.Left);
            _audioClickSecond = new AudioClick(
                2222, 10, 3.7F, audioData, ChannelType.Left);
            _audioClickDifferentChannel = new AudioClick(
                1111, 10, 3.7F, audioData, ChannelType.Right);
            _audioClickDifferentLength = new AudioClick(
                1111, 22, 3.7F, audioData, ChannelType.Left);
            _audioClickDifferentPosition = new AudioClick(
                2222, 10, 3.7F, audioData, ChannelType.Left);
            _audioClickLargerPosition = new AudioClick(
                2222, 10, 3.7F, audioData, ChannelType.Left);
            _audioClickLesserPosition = new AudioClick(
                0001, 10, 3.7F, audioData, ChannelType.Left);
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
            Assert.IsTrue(_audioClickFirst.CompareTo(_audioClickSecond) < 0,
                "Failed CompareTo: should return negative");
        }

        [TestMethod]
        public void CompareTo_ClickWithSmallerPosition_ReturnsPositive()
        {
            Assert.IsTrue(_audioClickSecond.CompareTo(_audioClickFirst) > 0,
                "Failed CompareTo: should return positive");
        }

        [TestMethod]
        public void CompareTo_TheSameClick_ReturnsZero()
        {
            Assert.IsTrue(_audioClickFirst.CompareTo(_audioClickTheSame) == 0,
                "Failed CompareTo: should be zero (the same)");
        }

        [TestMethod]
        public void CompareTo_EqualClick_ReturnsZero()
        {
            Assert.IsTrue(_audioClickFirst.CompareTo(_audioClickEqual) == 0,
                "Failed CompareTo: should be zero (equal)");
        }

        [TestMethod]
        public void CompareTo_Null_ReturnsPositive()
        {
            Assert.IsTrue(_audioClickFirst.CompareTo(null) > 0,
                "Failed CompareTo: should return positive");
        }

        [TestMethod]
        public void Equals_TheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst.Equals(_audioClickTheSame),
                "Failed Equals: should be true (the same)");
        }

        [TestMethod]
        public void Equals_EqualClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst.Equals(_audioClickEqual),
                "Failed Equals: should be true (equal)");
        }

        [TestMethod]
        public void Equals_DifferentClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst.Equals(_audioClickSecond),
                "Failed Equals: should be false (different)");
        }

        [TestMethod]
        public void Equals_Null_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst.Equals(null),
                "Failed Equals: should be false (null)");
        }

        [TestMethod]
        public void GetHashCode_TheSameClick_ReturnsTheSameHashCode()
        {
            Assert.AreEqual(_audioClickFirst.GetHashCode(),
                _audioClickTheSame.GetHashCode(),
                "Failed GetHashCode: should be equal (the same)");
        }

        [TestMethod]
        public void GetHashCode_EqualClicks_ReturnsTheSameHashCode()
        {
            Assert.AreEqual(_audioClickFirst.GetHashCode(),
                _audioClickEqual.GetHashCode(),
                "Failed GetHashCode: should be equal (equal)");
        }

        [TestMethod]
        public void GetHashCode_ClicksWithDifferentPositions_ReturnDifferentHashCodes()
        {
            Assert.AreNotEqual(_audioClickFirst.GetHashCode(),
                _audioClickDifferentPosition.GetHashCode(),
                "Failed GetHashCode: should be not equal (different position)");
        }

        [TestMethod]
        public void GetHashCode_ClicksWithDifferentLength_ReturnDifferentHaskCodes()
        {
            Assert.AreNotEqual(_audioClickFirst.GetHashCode(), _audioClickDifferentLength.GetHashCode(),
                "Failed GetHashCode: should be not equal (different length)");
        }

        [TestMethod]
        public void GetHashCode_ClicksFromDifferentChannels_ReturnDifferentHashCodes()
        {
            Assert.AreNotEqual(_audioClickFirst.GetHashCode(), _audioClickDifferentChannel.GetHashCode(),
                "Failed GetHashCode: should be not equal (different channel)");
        }

        [TestMethod]
        public void OperatorEqual_TheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst == _audioClickTheSame,
                "Failed Operator ==: should be true (the same)");
        }

        [TestMethod]
        public void OperatorEqual_ClickWithAllTheSameValues_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst == _audioClickEqual,
                "Failed Operator ==: should be true (equal)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksWithDifferentPjsitiions_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst == _audioClickDifferentPosition,
                "Failed Operator ==: should be false (different position)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksWithDifferentLengths_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst == _audioClickDifferentLength,
                "Failed Operator ==: should be false (different length)");
        }

        [TestMethod]
        public void OperatorEqual_ClicksFromDifferentChannels_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst == _audioClickDifferentChannel,
                "Failed Operator ==: should be false (different channel)");
        }

        [TestMethod]
        public void OperatorEqual_Null_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst == null,
                "Failed Operator ==: should be false (null)");
        }

        [TestMethod]
        public void OperatorNotEqual_TheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst != _audioClickTheSame,
                "Failed Operator !=: should be false (the same)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithAllTheSameValues_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst != _audioClickEqual,
                "Failed Operator !=: should be false (equal)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithDifferentPositions_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst != _audioClickDifferentPosition,
                "Failed Operator !=: should be true (different position)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksWithDifferentLength_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst != _audioClickDifferentLength,
                "Failed Operator !=: should be true (different length)");
        }

        [TestMethod]
        public void OperatorNotEqual_ClicksDifferentChannels_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst != _audioClickDifferentChannel,
                "Failed Operator !=: should be true (different channel)");
        }

        [TestMethod]
        public void OperatorNotEqual_Null_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst != null,
                "Failed Operator !=: should be true (null)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToLesserPositionClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst < _audioClickLesserPosition,
                "Failed Operator <: should be false (lesser position)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToTheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst < _audioClickTheSame,
                "Failed Operator <: should be false (the same)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToEqualClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst < _audioClickEqual,
                "Failed Operator <: should be false (equal)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst < _audioClickLargerPosition,
                "Failed Operator <: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorLess_ComparitionToNull_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst < null,
                "Failed Operator <: should be false (null)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToLesserPositionClick_ReturnsFals()
        {
            Assert.IsFalse(_audioClickFirst <= _audioClickLesserPosition,
                "Failed Operator <=: should be false (lesser position)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToTheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst <= _audioClickTheSame,
                "Failed Operator <=: should be true (the same)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToEqualClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst <= _audioClickEqual,
                "Failed Operator <=: should be true (equal)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst <= _audioClickLargerPosition,
                "Failed Operator <=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorLessOrEqual_ComparitionToNull_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst <= null,
                "Failed Operator <=: should be false (null)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToLesserPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst > _audioClickLesserPosition,
                "Failed Operator >: should be true (lesser position)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToTheSameClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst > _audioClickTheSame,
                "Failed Operator >: should be false (the same)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToEqualClick_ReturnsFalse()
        {
            Assert.IsFalse(_audioClickFirst > _audioClickEqual,
                "Failed Operator >: should be false (equal)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsFalse(_audioClickFirst > _audioClickLargerPosition,
                "Failed Operator >: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorMore_ComparitionToNull_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst > null,
                "Failed Operator >: should be true (null)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToLesserPositionClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst >= _audioClickLesserPosition,
                "Failed Operator >=: should be true (lesser position)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToTheSameClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst >= _audioClickTheSame,
                "Failed Operator >=: should be true (the same)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToEqualClick_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst >= _audioClickEqual,
                "Failed Operator >=: should be true (equal)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToLargerPositionClick_ReturnsTrue()
        {
            Assert.IsFalse(_audioClickFirst >= _audioClickLargerPosition,
                "Failed Operator >=: should be true " +
                "(larger position of second operand)");
        }

        [TestMethod]
        public void OperatorMoreOrEqual_ComparitionToNull_ReturnsTrue()
        {
            Assert.IsTrue(_audioClickFirst >= null,
                "Failed Operator >=: should be true (null)");
        }

        [TestMethod]
        public void ChangeAproved_SwitchingTwice_SwitchesAproved()
        {
            var aprovedState = _audioClickFirst.Aproved;
            // first change
            _audioClickFirst.ChangeAproved();
            Assert.AreNotEqual(_audioClickFirst.Aproved, aprovedState,
                "Failed ChangeAproved (first change): should not be "
                + aprovedState);
            // second change
            _audioClickFirst.ChangeAproved();
            Assert.AreEqual(_audioClickFirst.Aproved, aprovedState,
                "Failed ChangeAproved (second change): should be "
                + aprovedState);
        }
    }
}