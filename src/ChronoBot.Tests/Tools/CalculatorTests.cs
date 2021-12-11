using ChronoBot.Utilities.Tools;
using Xunit;

namespace ChronoBot.Tests.Tools
{
    public class CalculatorTests
    {
        [Fact]
        public void Calculations_Test_Success()
        {
            Calculator calculator = new Calculator();

            var result = calculator.Result("1 + 1", out bool ok);

            Assert.Equal("1 + 1 = 2", result);
            Assert.True(ok);
        }
        
        [Fact]
        public void Calculations_Test_Fail()
        {
            Calculator calculator = new Calculator();

            var result = calculator.Result("fail", out bool ok);

            Assert.Equal("Unable to calculate fail", result);
            Assert.False(ok);
        }
    }
}
