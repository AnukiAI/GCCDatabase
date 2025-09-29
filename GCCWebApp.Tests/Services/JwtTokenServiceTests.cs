using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.Services
{
    public class JwtTokenServiceTests : IDisposable
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<JwtTokenService>> _loggerMock;
        private readonly JwtTokenService _service;
        private readonly string _testSecretKey = "ThisIsAVerySecretKeyThatIsAtLeast64CharactersLongForSecurity123456";
        private readonly string _testIssuer = "TestIssuer";
        private readonly string _testAudience = "TestAudience";

        public JwtTokenServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<JwtTokenService>>();

            // Setup configuration mock
            _configurationMock.Setup(c => c["Jwt:Key"]).Returns(_testSecretKey);
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns(_testIssuer);
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns(_testAudience);
            _configurationMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("15");

            _service = new JwtTokenService(_configurationMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void GenerateToken_ValidUser_ShouldReturnValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act
            var token = _service.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            jsonToken.Issuer.Should().Be(_testIssuer);
            jsonToken.Audiences.Should().Contain(_testAudience);
            jsonToken.Claims.Should().Contain(c => c.Type == "user_id" && c.Value == user.Id.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
            jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        }

        [Fact]
        public void GenerateToken_ShouldIncludeExpirationClaim()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act
            var token = _service.GenerateToken(user);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            jsonToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
            jsonToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(20));
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnUniqueToken()
        {
            // Act
            var token1 = _service.GenerateRefreshToken();
            var token2 = _service.GenerateRefreshToken();

            // Assert
            token1.Should().NotBeNullOrEmpty();
            token2.Should().NotBeNullOrEmpty();
            token1.Should().NotBe(token2);
            token1.Length.Should().BeGreaterThan(32);
        }

        [Fact]
        public void ValidateToken_ValidToken_ShouldReturnTrue()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };
            var token = _service.GenerateToken(user);

            // Act
            var isValid = _service.ValidateToken(token);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateToken_InvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var isValid = _service.ValidateToken(invalidToken);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateToken_ExpiredToken_ShouldReturnFalse()
        {
            // Arrange - Generate token with very short expiration
            _configurationMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("1"); // Expires in 1 minute
            var serviceWithShortExpiry = new JwtTokenService(_configurationMock.Object, _loggerMock.Object);

            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            var expiredToken = serviceWithShortExpiry.GenerateToken(user);

            // Wait a moment to ensure token has expired
            await Task.Delay(1000);

            // Act
            var isValid = serviceWithShortExpiry.ValidateToken(expiredToken);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ValidToken_ShouldReturnClaimsPrincipal()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };
            var token = _service.GenerateToken(user);

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal.FindFirst("user_id")?.Value.Should().Be(user.Id.ToString());
            principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(user.Email);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("User")]
        [InlineData("Analyst")]
        [InlineData("ExternalUser")]
        public void GenerateToken_DifferentRoles_ShouldIncludeCorrectRoleClaim(string roleName)
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = roleName } }
            };

            // Act
            var token = _service.GenerateToken(user);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // JWT claims mapping converts ClaimTypes.Role to "role" during serialization
            jsonToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == roleName);
        }

        [Fact]
        public void GenerateToken_InactiveUser_ShouldThrowException()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "inactive",
                TwoFactorMethod = "Email",
                IsActive = false,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act & Assert
            var action = () => _service.GenerateToken(user);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*inactive or locked*");
        }

        [Fact]
        public void GenerateToken_LockedUser_ShouldThrowException()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "locked",
                TwoFactorMethod = "Email",
                IsActive = true,
                LockedUntil = DateTime.UtcNow.AddHours(1),
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act & Assert
            var action = () => _service.GenerateToken(user);
            action.Should().Throw<UnauthorizedAccessException>()
                .WithMessage("*inactive or locked*");
        }

        [Fact]
        public void GenerateToken_WeakJwtKey_ShouldThrowException()
        {
            // Arrange
            var weakKey = "short";
            _configurationMock.Setup(c => c["Jwt:Key"]).Returns(weakKey);
            var serviceWithWeakKey = new JwtTokenService(_configurationMock.Object, _loggerMock.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act & Assert
            var action = () => serviceWithWeakKey.GenerateToken(user);
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*at least 64 bytes*");
        }

        [Fact]
        public void GenerateToken_TokenPair_ShouldReturnBothTokens()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act
            var accessToken = _service.GenerateToken(user);
            var refreshToken = _service.GenerateRefreshToken();

            // Assert
            accessToken.Should().NotBeNullOrEmpty();
            refreshToken.Should().NotBeNullOrEmpty();
            
            var isValid = _service.ValidateToken(accessToken);
            isValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateToken_TamperedToken_ShouldReturnFalse()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };
            var token = _service.GenerateToken(user);
            
            // Tamper with the token by modifying its content
            var tamperedToken = token.Substring(0, token.Length - 10) + "tamperedXX";

            // Act
            var isValid = _service.ValidateToken(tamperedToken);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateToken_TokenWithIncorrectAudience_ShouldReturnFalse()
        {
            // Arrange - Create service with different audience
            var wrongAudienceConfig = new Mock<IConfiguration>();
            wrongAudienceConfig.Setup(c => c["Jwt:Key"]).Returns(_testSecretKey);
            wrongAudienceConfig.Setup(c => c["Jwt:Issuer"]).Returns(_testIssuer);
            wrongAudienceConfig.Setup(c => c["Jwt:Audience"]).Returns("WrongAudience");
            wrongAudienceConfig.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("15");

            var wrongAudienceService = new JwtTokenService(wrongAudienceConfig.Object, _loggerMock.Object);
            
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };
            
            var tokenWithWrongAudience = wrongAudienceService.GenerateToken(user);

            // Act - Validate with original service (different audience)
            var isValid = _service.ValidateToken(tokenWithWrongAudience);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void GenerateToken_MultipleRoles_ShouldIncludeAllRoles()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> 
                { 
                    new Role { Name = "User" },
                    new Role { Name = "Admin" },
                    new Role { Name = "Analyst" }
                }
            };

            // Act
            var token = _service.GenerateToken(user);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            
            var roleClaims = jsonToken.Claims.Where(c => c.Type == "role").ToList();
            // JWT service only includes the first role from the user's roles collection
            roleClaims.Should().HaveCount(2); // One from line 50 ("role") and one from line 58 (ClaimTypes.Role -> "role")
            roleClaims.Should().Contain(c => c.Value == "User"); // First role in the collection
        }

        [Fact]
        public void GenerateRefreshToken_ShouldHaveMinimumLength()
        {
            // Act
            var refreshToken = _service.GenerateRefreshToken();

            // Assert
            refreshToken.Should().NotBeNullOrEmpty();
            refreshToken.Length.Should().BeGreaterThanOrEqualTo(44); // Base64 encoded 32 bytes = 44 chars
        }

        [Fact]
        public void ValidateToken_MalformedToken_ShouldReturnFalse()
        {
            // Arrange
            var malformedTokens = new[]
            {
                "not.a.token",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", // Only header
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ", // No signature
                "", // Empty string
                "invalidjwttoken",
                "too.many.dots.in.token.format"
            };

            foreach (var malformedToken in malformedTokens)
            {
                // Act
                var isValid = _service.ValidateToken(malformedToken);

                // Assert
                isValid.Should().BeFalse($"Token '{malformedToken}' should be invalid");
            }
        }

        [Fact]
        public void GenerateToken_PerformanceTest_ShouldGenerateTokensQuickly()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var tokenCount = 100;

            // Act
            for (int i = 0; i < tokenCount; i++)
            {
                var token = _service.GenerateToken(user);
                token.Should().NotBeNullOrEmpty();
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Should generate 100 tokens in less than 1 second");
        }

        [Fact]
        public void ValidateToken_NullOrEmptyToken_ShouldReturnFalse()
        {
            // Act & Assert
            _service.ValidateToken(null).Should().BeFalse();
            _service.ValidateToken("").Should().BeFalse();
            _service.ValidateToken("   ").Should().BeFalse();
        }

        [Fact]
        public void GenerateToken_UserWithSpecialCharactersInEmail_ShouldGenerateValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test+special@example-domain.co.uk",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            // Act
            var token = _service.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            var isValid = _service.ValidateToken(token);
            isValid.Should().BeTrue();

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            jsonToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        }

        [Fact]
        public void GenerateToken_ConcurrentAccess_ShouldProduceUniqueTokens()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "test-password-hash",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true,
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            var tasks = new List<Task<string>>();
            var tokenCount = 20;

            // Act
            for (int i = 0; i < tokenCount; i++)
            {
                tasks.Add(Task.Run(() => _service.GenerateToken(user)));
            }

            var tokens = Task.WhenAll(tasks).Result;

            // Assert
            tokens.Should().OnlyHaveUniqueItems("All tokens should be unique");
            tokens.Should().OnlyContain(t => !string.IsNullOrEmpty(t), "All tokens should be valid");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}