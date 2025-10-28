using ManufacturingSimulation.Core.Models;
using Xunit;

namespace ManufacturingSimulation.Tests
{
    public class PartTests
    {
        [Fact]
        public void Part_Creation_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var route = new List<int> { 1, 2, 3 };
            var part = new Part("P001", route, arrivalTime: 5.0);

            // Assert
            Assert.Equal("P001", part.Id);
            Assert.Equal(0, part.CurrentOperationIndex);
            Assert.Equal(3, part.Route.Count);
            Assert.Equal(5.0, part.ArrivalTime);
            Assert.Equal(PartState.InStorage, part.State);
        }

        [Fact]
        public void GetCurrentMachineId_ShouldReturnCorrectMachine()
        {
            // Arrange
            var route = new List<int> { 1, 2, 3 };
            var part = new Part("P001", route);

            // Act & Assert
            Assert.Equal(1, part.GetCurrentMachineId());
            
            part.MoveToNextOperation();
            Assert.Equal(2, part.GetCurrentMachineId());
            
            part.MoveToNextOperation();
            Assert.Equal(3, part.GetCurrentMachineId());
        }

        [Fact]
        public void HasMoreOperations_ShouldReturnCorrectValue()
        {
            // Arrange
            var route = new List<int> { 1, 2 };
            var part = new Part("P001", route);

            // Act & Assert
            Assert.True(part.HasMoreOperations());
            
            part.MoveToNextOperation();
            Assert.True(part.HasMoreOperations());
            
            part.MoveToNextOperation();
            Assert.False(part.HasMoreOperations());
        }

        [Fact]
        public void MoveToNextOperation_ShouldIncrementIndex()
        {
            // Arrange
            var route = new List<int> { 1, 2, 3 };
            var part = new Part("P001", route);

            // Act
            part.MoveToNextOperation();

            // Assert
            Assert.Equal(1, part.CurrentOperationIndex);
        }
    }
}