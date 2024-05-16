﻿using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountsController(UserManager<AppUser> userManager,RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
            _roleManager = roleManager;
        }

        // api/accounts/register
        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = new AppUser
            {
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                UserName = registerDto.Email
            };
            var result = await _userManager.CreateAsync(user,registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            if (registerDto.Roles is null)
            {
                await _userManager.AddToRoleAsync(user, "User");
            } else {
                foreach (var role in registerDto.Roles)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            return Ok(new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Account Created Successfully!"
            });
        }

        //api/account/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user is null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found with this email",

                });
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!result)
            {
                return Unauthorized(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid Password."
                });
            }

            var token = GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token= token,
                IsSuccess = true,
                Message = "Login Success." 
            });
        }

        private string GenerateToken(AppUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII
                .GetBytes(_configuration.GetSection("JWTSetting").GetSection("securityKey").Value!);

            var roles = _userManager.GetRolesAsync(user).Result;
            List<Claim> claims =
                [
                    new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                    new(JwtRegisteredClaimNames.Name, user.FullName ?? ""),
                    new(JwtRegisteredClaimNames.NameId, user.Id ?? ""),
                    new(JwtRegisteredClaimNames.Aud,
                    _configuration.GetSection("JWTSetting").GetSection("validAudience").Value!),
                    new(JwtRegisteredClaimNames.Iss, _configuration.GetSection("JWTSetting").GetSection("validIssuer").Value!)

                ];

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        //api/account/detail
        [HttpGet("detail")]
        [Authorize]
        public async Task<ActionResult<UserDetailDto>> GetUserDetail()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user is null)
            {
                return NotFound(new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "User not found"
                });
            }

            return Ok(new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = [..await _userManager.GetRolesAsync(user)],
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                AccessFailedCount = user.AccessFailedCount,
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetUser()
        {
            var users = await _userManager.Users.Select(u => new UserDetailDto {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Roles = _userManager.GetRolesAsync(u).Result.ToArray()
            }).ToListAsync();

            return Ok(users);
        }

    }
}
