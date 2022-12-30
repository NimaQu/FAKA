﻿using faka.Auth;
using JWTAuthentication.NET6._0.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using faka.Filters;

namespace faka.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [CustomResultFilter]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model.Username == null || model.Password == null) return BadRequest("Username or password is null");
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password)) return Unauthorized("Invalid username or password");
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            authClaims.AddRange(userRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));

            var token = GetToken(authClaims);
            //add token to header
            Response.Headers.Add("Authorization", $"Bearer {new JwtSecurityTokenHandler().WriteToken(token)}");

            return Ok();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model.Username == null || model.Password == null) return BadRequest("Username or password is null");
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return BadRequest("User already exists!");

            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
            //发送验证邮件
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action("ConfirmEmail", "Auth", new {code = code}, protocol: HttpContext.Request.Scheme);
            // dev only
            return Ok(code);
        }

        [HttpPost]
        [Route("register-admin")]
        // dev only
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
        {
            if (model.Username == null || model.Password == null) return BadRequest("Username or password is null");
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, "Account already exists!");

            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, "User create failed");

            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            if (!await _roleManager.RoleExistsAsync(Roles.User))
                await _roleManager.CreateAsync(new IdentityRole(Roles.User));
            if (await _roleManager.RoleExistsAsync(Roles.Admin))
                await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (await _roleManager.RoleExistsAsync(Roles.User))
                await _userManager.AddToRoleAsync(user, Roles.User);
            return Ok("Account created successfully!");
        }
        
        [HttpGet]
        [Route("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("请先登录");
            var result = await _userManager.ConfirmEmailAsync(user, code);
            Console.WriteLine(result.Errors);
            if (!result.Succeeded) return BadRequest("链接无效或已过期");
            if (await _roleManager.RoleExistsAsync(Roles.User))
                await _userManager.AddToRoleAsync(user, Roles.User);
            return Ok("邮箱验证成功");
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}