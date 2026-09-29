using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinance.Api.Data;
using PersonalFinance.Api.Entities;
using PersonalFinance.Api.Services;

namespace PersonalFinance.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _frontendUrl;
    private readonly JwtService _jwtService;

    public AuthController(
        AppDbContext db,
        IConfiguration configuration,
        JwtService jwtService)
    {
        _db = db;

        _frontendUrl = configuration["FrontendUrl"]
            ?? throw new InvalidOperationException(
                "FrontendUrl is not configured."
            );

        _jwtService = jwtService;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action(
            nameof(GoogleCallback),
            "Auth",
            null,
            Request.Scheme
        );

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(
            properties,
            GoogleDefaults.AuthenticationScheme
        );
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("External");

        if (!result.Succeeded || result.Principal == null)
        {
            return Unauthorized(new
            {
                message = "Google authentication failed."
            });
        }

        var principal = result.Principal;

        var googleId =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        var email =
            principal.FindFirstValue(ClaimTypes.Email);

        var fullName =
            principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name");

        if (string.IsNullOrWhiteSpace(googleId))
        {
            return Unauthorized(new
            {
                message = "Google ID was not found."
            });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized(new
            {
                message = "Email was not found from Google."
            });
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = email;
        }

        // Cari user berdasarkan Google ID
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

        // Kalau belum ada, cari berdasarkan email
        if (user == null)
        {
            user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        // Kalau belum ada, buat user baru
        if (user == null)
        {
            user = new User
            {
                FullName = fullName,
                Email = email,
                GoogleId = googleId,
                PasswordHash = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);

            await _db.SaveChangesAsync();
        }
        else
        {
            // Pastikan Google ID tersimpan
            if (string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.GoogleId = googleId;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        // Hapus external authentication cookie
        await HttpContext.SignOutAsync("External");

        // Buat JWT aplikasi
        var token = _jwtService.GenerateToken(user);

        // Simpan JWT di HttpOnly Cookie
        Response.Cookies.Append(
            "accessToken",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(60),
            }
        );

        // Redirect ke frontend TANPA token di URL
        return Redirect(
            $"{_frontendUrl}/auth/google-callback"
        );
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Invalid authentication."
            });
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "User not found."
            });
        }

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
        };

        Response.Cookies.Delete("accessToken", cookieOptions);

        return Ok(new { message = "Logout successful." });
    }
}