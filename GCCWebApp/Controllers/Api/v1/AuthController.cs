using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;

namespace GCCWebApp.Controllers.Api.v1
{
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class AuthController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IJwtTokenService jwtTokenService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResult<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive user: {Username}", request.Username);
                    return Unauthorized(new { message = "Account is deactivated" });
                }

                var token = _jwtTokenService.GenerateToken(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = new TokenResponse
                {
                    AccessToken = token,
                    RefreshToken = refreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60") * 60,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Roles.FirstOrDefault()?.Name ?? "User"
                    }
                };

                _logger.LogInformation("Successful login for user: {Username}", request.Username);
                return SuccessResponse(response, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
                return HandleError(ex, "Login failed");
            }
        }

        /// <summary>
        /// Refresh JWT token
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResult<TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                    return BadRequestResponse("Refresh token is required");

                var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                var username = principal.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { message = "Invalid token" });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null || !user.IsActive)
                    return Unauthorized(new { message = "User not found or inactive" });

                var newToken = _jwtTokenService.GenerateToken(user);
                var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

                var response = new TokenResponse
                {
                    AccessToken = newToken,
                    RefreshToken = newRefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60") * 60,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Role = user.Roles.FirstOrDefault()?.Name ?? "User"
                    }
                };

                return SuccessResponse(response, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return HandleError(ex, "Token refresh failed");
            }
        }

        /// <summary>
        /// Validate JWT token
        /// </summary>
        [HttpPost("validate")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResult<UserInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var userId = User.FindFirst("user_id")?.Value;
                
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
                    return Unauthorized(new { message = "Invalid token" });

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

                if (user == null)
                    return Unauthorized(new { message = "User not found or inactive" });

                var userInfo = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Roles.FirstOrDefault()?.Name ?? "User"
                };

                return SuccessResponse(userInfo, "Token is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return HandleError(ex, "Token validation failed");
            }
        }
    }

    public class LoginRequest
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        public required string AccessToken { get; set; }

        [Required]
        public required string RefreshToken { get; set; }
    }

    public class TokenResponse
    {
        public required string AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
    }
}