using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using WebApi.Services;
using WebApi.Models.TableSchema;
using WebApi.Models.DTO;
using Org.BouncyCastle.Ocsp;

namespace YourApp.Controllers.Api
{

    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class UserController : ControllerBase
    {
        private readonly PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();
        private readonly IConfiguration _config;
        private readonly WebApiContext _dbContext;
        private static object random;

        public UserController(IConfiguration config,WebApiContext dbContext)
        {
            _config = config;
            _dbContext = dbContext;
        }


        //------------------------------------------helper methods start here----------------------------------

        private bool UserExists(string name)
        {
            return _dbContext.UserProfileDetails.Any(u => u.UserEmail == name);
        }

        private int GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999);
        }

        private string GenerateJwtToken(UserProfileDetails user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                new Claim("Id",user.UserId.ToString()),
                new Claim("UserName",user.UserName),
                new Claim("RoleName",user.RoleName),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim("Password",user.Password)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_config["Jwt:ExpireDays"]));

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        //------------------------------------------helper methods ends here-----------------------------------

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (UserExists(model.UserEmail))
            {
                ModelState.AddModelError(string.Empty, "User already exists, please log in");
                return BadRequest(ModelState);
            }

            string tempData = model.Password;

            string pass = _passwordHasher.HashPassword(null, model.Password);
            var user = new UserProfileDetails
            {
                UserName = model.UserName,
                UserEmail = model.UserEmail,
                Password = pass
            };

            var result = await _dbContext.UserProfileDetails.AddAsync(user);

            if (result.State != EntityState.Added)
            {
                ModelState.AddModelError(string.Empty, "Unable to register user.");
                return BadRequest(ModelState);
            }
            _dbContext.SaveChanges();

            string emailBody = $"Train Reservation System\n\nDear, {model.UserName},\nYour account has been successfully created with Train Reservation System.\nPlease find below your login credentials:\nEmail : {model.UserEmail}\nPassword : {tempData}\nThank you for choosing our service.\nBest regards,\nThe Train Reservation System team";
            string subject = "Welcome to Train Reservation System";

            EmailService em = new EmailService();
            em.SendEmail(emailBody, model.UserEmail, subject);


            return Ok(new
            {
                StatusCode = 200,
                Message = "User Added Successfully"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _dbContext.UserProfileDetails.FirstOrDefaultAsync(u => u.UserEmail == model.UserEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return BadRequest(ModelState);
            }
            var result = _passwordHasher.VerifyHashedPassword(null, user.Password, model.Password);
            bool req = false;
            if (result == PasswordVerificationResult.Success)
            {
                req = true;
            }

            if (!await _dbContext.UserProfileDetails.AnyAsync(u => u.UserEmail == model.UserEmail && req))
            {
                ModelState.AddModelError(string.Empty, "User not found, please enter valid email or password.");
                return BadRequest(ModelState);
            }

            var token = GenerateJwtToken(user);

            return Ok(new { token });
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] string emailId)
        {
            var user = await _dbContext.UserProfileDetails.FirstOrDefaultAsync(u => u.UserEmail == emailId);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return BadRequest(ModelState);
            }
            int otp = GenerateOTP();

            resetPassword resetPasswordObj = new resetPassword
            {
                UserEmail = user.UserEmail,
                otp = otp
            };

            var result = await _dbContext.ResetPasswords.AddAsync(resetPasswordObj);

            if (result.State != EntityState.Added)
            {
                ModelState.AddModelError(string.Empty, "Unable to reset the password of user.");
                return BadRequest(ModelState);
            }
            _dbContext.SaveChanges();

            string emailBody = $"Train Reservation System\n\nDear, {user.UserName}\nUse this otp : {otp} to reset your password.\n\nBest regards,\nThe Train Reservation System team";
            string subject = "Welcome to Train Reservation System";

            EmailService em = new EmailService();
            em.SendEmail(emailBody, user.UserEmail, subject);

            return Ok(new
            {
                StatusCode = 200,
                Message = "Tested Successfully"
            });
        }
        
    }
}

