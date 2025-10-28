using ManufacturingSimulation.Core.Models;
using Xunit;
using PartBuffer = ManufacturingSimulation.Core.Models.Buffer;  // Add this line

namespace ManufacturingSimulation.Tests
{
    public class BufferTests
    {
        [Fact]
        public void Buffer_Creation_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var buffer = new PartBuffer(capacity: 5, machineId: 1);  // Change Buffer to PartBuffer

            // Assert
            Assert.Equal(5, buffer.Capacity);
            Assert.Equal(1, buffer.MachineId);
            Assert.Equal(0, buffer.Count);
            Assert.True(buffer.IsEmpty);
            Assert.False(buffer.IsFull);
        }

        [Fact]
        public void Buffer_WithZeroCapacity_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PartBuffer(0, 1));  // Change here
        }

        [Fact]
        public void TryAdd_WhenNotFull_ShouldAddPart()
        {
            // Arrange
            var buffer = new PartBuffer(3, 1);  // Change here
            var part = new Part("P001", new List<int> { 1 });

            // Act
            bool result = buffer.TryAdd(part);

            // Assert
            Assert.True(result);
            Assert.Equal(1, buffer.Count);
            Assert.Equal(PartState.InBuffer, part.State);
            Assert.False(buffer.IsEmpty);
        }

        [Fact]
        public void TryAdd_WhenFull_ShouldReturnFalse()
        {
            // Arrange
            var buffer = new PartBuffer(2, 1);  // Change here
            var part1 = new Part("P001", new List<int> { 1 });
            var part2 = new Part("P002", new List<int> { 1 });
            var part3 = new Part("P003", new List<int> { 1 });

            buffer.TryAdd(part1);
            buffer.TryAdd(part2);

            // Act
            bool result = buffer.TryAdd(part3);

            // Assert
            Assert.False(result);
            Assert.Equal(2, buffer.Count);
            Assert.True(buffer.IsFull);
        }

        [Fact]
        public void TryRemove_WhenNotEmpty_ShouldReturnPart()
        {
            // Arrange
            var buffer = new PartBuffer(3, 1);  // Change here
            var part1 = new Part("P001", new List<int> { 1 });
            var part2 = new Part("P002", new List<int> { 1 });
            
            buffer.TryAdd(part1);
            buffer.TryAdd(part2);

            // Act
            var removedPart = buffer.TryRemove();

            // Assert
            Assert.NotNull(removedPart);
            Assert.Equal("P001", removedPart.Id);  // FIFO order
            Assert.Equal(1, buffer.Count);
        }

        [Fact]
        public void TryRemove_WhenEmpty_ShouldReturnNull()
        {
            // Arrange
            var buffer = new PartBuffer(3, 1);  // Change here

            // Act
            var removedPart = buffer.TryRemove();

            // Assert
            Assert.Null(removedPart);
        }

        [Fact]
        public void Peek_ShouldNotRemovePart()
        {
            // Arrange
            var buffer = new PartBuffer(3, 1);  // Change here
            var part = new Part("P001", new List<int> { 1 });
            buffer.TryAdd(part);

            // Act
            var peekedPart = buffer.Peek();

            // Assert
            Assert.NotNull(peekedPart);
            Assert.Equal("P001", peekedPart.Id);
            Assert.Equal(1, buffer.Count);  // Still in buffer
        }

        [Fact]
        public void Utilization_ShouldCalculateCorrectly()
        {
            // Arrange
            var buffer = new PartBuffer(4, 1);  // Change here
            var part1 = new Part("P001", new List<int> { 1 });
            var part2 = new Part("P002", new List<int> { 1 });

            // Act & Assert
            Assert.Equal(0.0, buffer.Utilization);

            buffer.TryAdd(part1);
            Assert.Equal(0.25, buffer.Utilization);

            buffer.TryAdd(part2);
            Assert.Equal(0.5, buffer.Utilization);
        }

        [Fact]
        public void Clear_ShouldRemoveAllParts()
        {
            // Arrange
            var buffer = new PartBuffer(3, 1);  // Change here
            buffer.TryAdd(new Part("P001", new List<int> { 1 }));
            buffer.TryAdd(new Part("P002", new List<int> { 1 }));

            // Act
            buffer.Clear();

            // Assert
            Assert.Equal(0, buffer.Count);
            Assert.True(buffer.IsEmpty);
        }
    }
}