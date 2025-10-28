using ManufacturingSimulation.Core.Models;
using Xunit;

namespace ManufacturingSimulation.Tests
{
    public class MachineTests
    {
        [Fact]
        public void Machine_Creation_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var machine = new Machine(1, "M1");

            // Assert
            Assert.Equal(1, machine.Id);
            Assert.Equal("M1", machine.Name);
            Assert.Equal(MachineState.Idle, machine.State);
            Assert.True(machine.IsAvailable());
            Assert.Equal(0, machine.PartsCompleted);
        }

        [Fact]
        public void StartProcessing_WhenIdle_ShouldStartCorrectly()
        {
            // Arrange
            var machine = new Machine(1, "M1");
            var part = new Part("P001", new List<int> { 1 });
            double currentTime = 10.0;
            double processingTime = 5.0;

            // Act
            machine.StartProcessing(part, currentTime, processingTime);

            // Assert
            Assert.Equal(MachineState.Busy, machine.State);
            Assert.Equal(part, machine.CurrentPart);
            Assert.Equal(10.0, machine.ProcessingStartTime);
            Assert.Equal(15.0, machine.ProcessingEndTime);
            Assert.Equal(PartState.Processing, part.State);
            Assert.False(machine.IsAvailable());
        }

        [Fact]
        public void StartProcessing_WhenBusy_ShouldThrowException()
        {
            // Arrange
            var machine = new Machine(1, "M1");
            var part1 = new Part("P001", new List<int> { 1 });
            var part2 = new Part("P002", new List<int> { 1 });
            
            machine.StartProcessing(part1, 10.0, 5.0);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                machine.StartProcessing(part2, 15.0, 5.0));
        }

        [Fact]
        public void CompleteProcessing_ShouldReleasePartAndUpdateState()
        {
            // Arrange
            var machine = new Machine(1, "M1");
            var part = new Part("P001", new List<int> { 1, 2 });
            machine.StartProcessing(part, 10.0, 5.0);

            // Act
            var completedPart = machine.CompleteProcessing(15.0);

            // Assert
            Assert.Equal(part, completedPart);
            Assert.Equal(1, completedPart.CurrentOperationIndex);
            Assert.Equal(MachineState.Idle, machine.State);
            Assert.Null(machine.CurrentPart);
            Assert.Equal(1, machine.PartsCompleted);
            Assert.True(machine.IsAvailable());
        }

        [Fact]
        public void GetProcessingProgress_ShouldCalculateCorrectly()
        {
            // Arrange
            var machine = new Machine(1, "M1");
            var part = new Part("P001", new List<int> { 1 });
            machine.StartProcessing(part, 10.0, 10.0);  // 10 second processing

            // Act & Assert
            Assert.Equal(0, machine.GetProcessingProgress(10.0));    // 0% at start
            Assert.Equal(50, machine.GetProcessingProgress(15.0));   // 50% at midpoint
            Assert.Equal(100, machine.GetProcessingProgress(20.0));  // 100% at end
        }
    }
}