using Xunit;

namespace Illatmodul.Tests
{
    public class HelloWorldTest
    {
        [Fact]
        public void AlwaysPasses()
        {
            // Arrange + Act + Assert
            Assert.True(true); // Ez mindig igaz, tehát mindig sikeres
        }
    }
}
