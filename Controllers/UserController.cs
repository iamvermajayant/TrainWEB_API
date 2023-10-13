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
using System.Text.RegularExpressions;
using Org.BouncyCastle.Math;

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

        private bool IsValidEmail(string email)
        {
            string emailPattern = @"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$";
            return Regex.IsMatch(email, emailPattern);
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

            if (!IsValidEmail(emailId))
            {
                ModelState.AddModelError(string.Empty, "Invalid email format.");
                return BadRequest(ModelState);
            }

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
                Message = "OTP sent successfully"
            });
        }

        [HttpPost("ResetPasswordPostOtp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordPostOtp(ResetPasswordDto resetPasswordDtoObj)
        {
            var databaseEntry = await _dbContext.ResetPasswords.FirstOrDefaultAsync(x => x.UserEmail == resetPasswordDtoObj.Email);
            if (databaseEntry == null)
            {
                ModelState.AddModelError(string.Empty, "Email not found to reset the password, please reset again.");
                return BadRequest(ModelState);
            }

            if (databaseEntry.failedTries > 2 || databaseEntry.expiry < DateTime.Now)
            {
                _dbContext.ResetPasswords.Remove(databaseEntry);
                await _dbContext.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, "otp for the current email address expired, please try again");
                return BadRequest(ModelState);
            }

            if (databaseEntry.otp != resetPasswordDtoObj.otp)
            {
                databaseEntry.failedTries += 1;
                int tries = 3 - databaseEntry.failedTries;
                await _dbContext.SaveChangesAsync();

                if (tries == 0)
                {
                    _dbContext.ResetPasswords.Remove(databaseEntry);
                    await _dbContext.SaveChangesAsync();
                    ModelState.AddModelError(string.Empty, $"otp for the current email address expired, please try again");
                }
                else if ( tries > 1)
                {
                    ModelState.AddModelError(string.Empty, $"Please enter valid otp, you have {tries} more tries left.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Please enter valid otp, you have {tries} more try left.");
                }
                return BadRequest(ModelState);
            }

            if (resetPasswordDtoObj.Password != resetPasswordDtoObj.confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Please enter same password in both password fields");
                return BadRequest(ModelState);
            }

            var user = await _dbContext.UserProfileDetails.FirstOrDefaultAsync(x => x.UserEmail == resetPasswordDtoObj.Email);
            string pass = _passwordHasher.HashPassword(null, resetPasswordDtoObj.Password);
            user.Password = pass;
            _dbContext.ResetPasswords.Remove(databaseEntry);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                StatusCode = 200,
                Message = "Password has been changed successfully"
            });
        }
        
    }
}

