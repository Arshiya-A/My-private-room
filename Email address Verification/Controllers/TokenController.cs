using System.Collections.Specialized;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {

        const int MAX_DIGIT = 32;
        const int MAX_KEY = 4;
        private string? _outputKey;
        private static string? _hashPassword;


        [HttpPost("SendToken")]
        public async Task<ActionResult<string>> SendToken(string emailAddress)
        {
            if (CreateCookie(emailAddress))
                return Ok("Successfully create token.");

            return BadRequest();
        }

        [HttpGet("TokenValidation")]
        public async Task<ActionResult<string>> TokenValidation(string token)
        {
            var request = HttpContext.Request;
            if (BCrypt.Net.BCrypt.Verify(token, _hashPassword))
            {
                request.Cookies.TryGetValue(_hashPassword, out _outputKey);

                if (_outputKey is not null)
                {
                    return Ok("Valid Token.");
                    _hashPassword = "";
                }
            }

            return BadRequest("Invalid Token.");

        }

        private void SendEmail(string toEmail, string code)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("YOUR EMAIL", "YOUR EMAIL GMAIL APPpassword"),
                EnableSsl = true
            };

            MailMessage message = new MailMessage("YOUR EMAIL", toEmail);

            message.IsBodyHtml = true;
            message.Subject = "Verification message";
            message.Body = $"<h1>Hello , wellcome to my program . your code is <b> {code} </b> </h1>";

            smtpClient.Send(message);


        }

        private bool CreateCookie(string emailAddress)
        {
            var response = HttpContext.Response;

            var cookie = new Cookie();

            cookie.Path = "/";
            cookie.Name = "Security Token";
            cookie.Value = BCrypt.Net.BCrypt.HashPassword(CreateToken());
            cookie.Expires = DateTime.UtcNow.AddMinutes(3);

            var cookieOption = new CookieOptions()
            {
                Path = "/",
                Expires = DateTime.UtcNow.AddMinutes(3),
            };

            var trustKey = CreateKey();
            var key = BCrypt.Net.BCrypt.HashPassword(trustKey);
            key.Replace("@", string.Empty);
            key.Replace(",", string.Empty);
            key.TrimStart();
            var firstChar = key.IndexOf("$");
            key.Remove(firstChar);

            _hashPassword = key;
            response.Cookies.Append(key, cookie.Value, cookieOption);

            SendEmail(emailAddress,trustKey);

            return true;



        }


        private string CreateToken()
        {
            Random random = new Random();
            string digit = "";

            for (int i = 0; i < MAX_DIGIT; i++)
                digit += random.Next(10).ToString();

            return digit;
        }

        private string CreateKey()
        {
            Random random = new Random();
            string key = "";

            for (int i = 0; i < MAX_KEY; i++)
                key += random.Next(10).ToString();

            return key;
        }



    }
}
