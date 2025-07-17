using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTender.Application.DTOs;
using SmartTender.Domain;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTender.Application.Common;
using SmartTender.Infrastructure;
using SmartTender.Infrastructure.Email;

namespace SmartTender_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
       

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailSender _emailSender;
        private readonly SmartTenderDbContext _dbContext;

        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<AppUser> userManager, 
            SmartTenderDbContext dbContext,
            SignInManager<AppUser> signInManager, 
            IConfiguration configuration, 
            RoleManager<IdentityRole> roleManager,
            EmailSender emailSender
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _roleManager= roleManager;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var existingUserByUserName = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUserByUserName != null)
                return BadRequest("Bu istifadəçi adı artıq mövcuddur.");

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("Bu email artıq istifadə olunub.");

            var user = new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                Voen = dto.Voen,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);
            
            if (!await _roleManager.RoleExistsAsync(Role.Admin))
                await _roleManager.CreateAsync(new IdentityRole(Role.Admin));
            
            if (!await _roleManager.RoleExistsAsync(Role.User))
                await _roleManager.CreateAsync(new IdentityRole(Role.User));

            var addRoleResult = await _userManager.AddToRoleAsync(user, Role.User);
            if (!addRoleResult.Succeeded)
                return BadRequest(addRoleResult.Errors);

            return Ok("Qeydiyyat uğurla tamamlandı.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
                return Unauthorized("İstifadəçi tapılmadı.");
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Məlumat yanlışdır!");
            
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(refreshToken); // RefreshToken AppUser modelində var
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                token = accessToken,
                refreshToken = refreshToken.Token
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return NotFound("Bu email ilə istifadəçi tapılmadı.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"https://localhost:3000/reset-password?token={Uri.EscapeDataString(token)}&email={dto.Email}";

            var subject = "Şifrəni sıfırlamaq üçün link";
            var body = $"<p>Şifrəni sıfırlamaq üçün link: <a href='{resetLink}'>Şifrəni sıfırla</a></p>";

            await _emailSender.SendAsync(dto.Email, subject, body);

            return Ok("Şifrəni sıfırlamaq üçün email göndərildi.");
        }
        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("İstifadəçi tapılmadı.");

            var decodedToken = Uri.UnescapeDataString(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Parol uğurla yeniləndi.");
        }

        
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshTokenDto.RefreshToken));

            if (user == null)
                return Unauthorized("Refresh token tapılmadı.");

            var storedToken = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshTokenDto.RefreshToken);

            if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.Expires < DateTime.UtcNow)
                return Unauthorized("Refresh token keçərsizdir.");

            storedToken.IsUsed = true;
            storedToken.IsRevoked = true;

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(newRefreshToken);

            _dbContext.RefreshTokens.Update(storedToken);
            _dbContext.RefreshTokens.Add(newRefreshToken);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken.Token
            });
        }


        private RefreshToken GenerateRefreshToken()
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(10), 
                IsUsed = false,
                IsRevoked = false
            };
        }


        private string GenerateJwtToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
                
            };
            var roles = _userManager.GetRolesAsync(user).Result; 
            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            if (!string.IsNullOrEmpty(user.FullName))
            {
                claims.Add(new Claim("fullName", user.FullName));  
            }
            if (!string.IsNullOrEmpty(user.Voen))
            {
                claims.Add(new Claim("voen", user.Voen));
            }
    
            // ISO 8601 formatında saxla
            claims.Add(new Claim("createdDate", user.CreatedDate.ToString("o")));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddSeconds(60),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 