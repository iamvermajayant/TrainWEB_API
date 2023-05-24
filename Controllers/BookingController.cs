using Microsoft.AspNetCore.Mvc;
using NETCore.MailKit.Core;
using System.Security.Claims;
using TRS_WebApi.Models;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : Controller
    {
        private readonly IConfiguration _config;
        private readonly WebApiContext _context;
        private readonly IHttpContextAccessor _contentAccessor;

        public BookingController(IConfiguration config, WebApiContext dbContext, IHttpContextAccessor contentAccessor)
        {
            _config = config;
            _context = dbContext;
            _contentAccessor = contentAccessor;
        }

        //---------------------------------helper methods starts here----------------------------------------------


        private bool PNRExists(int PNR)
        {
            return _context.Bookings.Any(u => u.PNR == PNR);
        }

        public static int GeneratePNR()
        {
            Random rnd = new Random();
            return rnd.Next(10000000, 99999999);
        }


        //---------------------------------helper method ends here-------------------------------------------------


        [HttpPost("BookTicket")]
        public async Task<ActionResult<BookingHistory>> BookTicket(int TrainId, BookingHistory bghObj)
        {

            string userID = _contentAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            UserProfileDetails user = _context.UserProfileDetails.Find(userID);

            TrainDetails trainObj = _context.TrainDetails.Find(TrainId);

            if (trainObj == null)
            {
                return BadRequest("Train not found");
            }

            if (bghObj.ticketCount > trainObj.SeatCapacity)
            {
                return BadRequest("Tickets not avaialable");
            }

            BookingHistory bgh = new BookingHistory();

            double price = (trainObj.SeatRate * bghObj.ticketCount);

            bgh.UserProfileDetails = user;
            bgh.TrainDetails = trainObj;
            bgh.BookingDate = DateTime.Now;

            bool status = true;
            int tempPNR = 0;
            while (status)
            {
                tempPNR = GeneratePNR();
                if (!PNRExists(tempPNR))
                {
                    status = false;
                }

            }

            bgh.PNR = tempPNR;
            bgh.ticketCount = bghObj.ticketCount;


            trainObj.SeatCapacity -= bgh.ticketCount;

            _context.Bookings.Add(bgh);
            _context.SaveChanges();

            string emailBody = $"Booking successfull, here is the PNR number for your booking {tempPNR},\nTrain Details \nTrain Number : {trainObj.TrainId}\nTrain Name : {trainObj.TrainName}\n Travel Date : {trainObj.Departure} - {trainObj.Arrival}\nTickets : {bgh.ticketCount}";

            //EmailService em = new EmailService();
            //em.SendEmail(emailBody, user.Email);


            return Ok($"Booking for the train {trainObj.TrainName} succeeded, details sent to the email address");
        }

    }
}
