namespace WebApi.Models.DTO
{
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string confirmPassword { get; set; }
        public int otp { get; set; }
    }
}
