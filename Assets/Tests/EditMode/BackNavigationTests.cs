using GoldenMend.Flow;
using NUnit.Framework;

namespace GoldenMend.Tests
{
    public class BackNavigationTests
    {
        [TestCase(FlowState.Playing, BackAction.Pause)]
        [TestCase(FlowState.Paused, BackAction.Resume)]
        [TestCase(FlowState.Museum, BackAction.GoHome)]
        [TestCase(FlowState.LevelComplete, BackAction.GoHome)]
        [TestCase(FlowState.WorkbenchFull, BackAction.GoHome)]
        [TestCase(FlowState.Home, BackAction.LeaveApp)]
        public void Back_StepsOutOfEachScreen(FlowState state, BackAction expected)
        {
            Assert.That(BackNavigation.For(state, false, false, false), Is.EqualTo(expected));
        }

        [Test]
        public void Back_ClosesTheTopmostPopupFirst()
        {
            // Details opens over the museum; settings can open over pause; an offer opens over the board.
            Assert.That(BackNavigation.For(FlowState.Museum, true, false, false), Is.EqualTo(BackAction.CloseDetails));
            Assert.That(BackNavigation.For(FlowState.Paused, false, true, false), Is.EqualTo(BackAction.CloseSettings));
            Assert.That(BackNavigation.For(FlowState.Home, false, true, false), Is.EqualTo(BackAction.CloseSettings));
            Assert.That(BackNavigation.For(FlowState.Playing, false, false, true), Is.EqualTo(BackAction.CloseOffer));
        }
    }
}
