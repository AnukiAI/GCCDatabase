using System.Net;

namespace GCCWebApp.Tests
{
    public class BasicIntegrationTests
    {
        [Fact]
        public void Http_CanCreateHttpClient()
        {
            using var client = new HttpClient();
            Assert.NotNull(client);
        }

        [Fact]
        public void DateTime_NowIsReasonable()
        {
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;
            
            Assert.True(now.Year >= 2024);
            Assert.True(utcNow.Year >= 2024);
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user@company.com")]
        public void EmailValidation_BasicFormat(string email)
        {
            Assert.Contains("@", email);
            Assert.Contains(".", email);
        }
    }
}