using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NETCore.MailKit.Core;
using System.Data;
using System.Security.Claims;
using TRS_WebApi.Models;
using WebApi.Data;
using WebApi.Models;
using WebApi.Services;

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

        WebApi.Services.EmailService em = new WebApi.Services.EmailService();

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
        [Authorize(Roles = "user")]
        public async Task<ActionResult<BookingHistory>> BookTicket(int TrainId, int ticketCount)
        {

            string userName = _contentAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            string userId = _contentAccessor.HttpContext.User.FindFirstValue("Id");

            int userRetrievedId = Convert.ToInt32(userId);

            UserProfileDetails user = _context.UserProfileDetails.FirstOrDefault(user => user.UserId == userRetrievedId);

            TrainDetails trainObj = _context.TrainDetails.Find(TrainId);

            if (trainObj == null)
            {
                return BadRequest("Train not found");
            }

            if (ticketCount > trainObj.SeatCapacity)
            {
                return BadRequest("Tickets not available");
            }

            if (ticketCount > 6)
            {
                return BadRequest("Ticket count shouldn't be more than 6");
            }

            if (ticketCount < 1)
            {
                return BadRequest("Please enter valid ticket count");
            }

            BookingHistory bgh = new BookingHistory();

            double price = (trainObj.SeatRate * ticketCount);

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
            bgh.ticketCount = ticketCount;

            bgh.TrainId = trainObj.TrainId;
            bgh.UserId = userRetrievedId;


            trainObj.SeatCapacity -= bgh.ticketCount;

            _context.Bookings.Add(bgh);
            _context.SaveChanges();

            string emailBody = $"Booking successfull, here is the PNR number for your booking {tempPNR},\nTrain Details \nTrain Number : {trainObj.TrainId}\nTrain Name : {trainObj.TrainName}\n Travel Date : {trainObj.Departure} - {trainObj.Arrival}\nTickets : {bgh.ticketCount}";


            em.SendEmail(emailBody, user.UserEmail);


            return Ok( new { ticketCount, tempPNR, Message = $"Booking for the train {trainObj.TrainName} succeeded, details sent to the email address" });
        }



        [HttpPost("PassengerDetails")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> PassengerDetails(List<PassengerDetails> tempPassengerDetails)
        {
            List<PassengerDetails> passengerDetails = new List<PassengerDetails>();

            foreach(PassengerDetails  passenger in tempPassengerDetails)
            {
                PassengerDetails passObj = new PassengerDetails();

                passObj.Name = passenger.Name;
                passObj.Age = passenger.Age;
                passObj.Gender = passenger.Gender;
                passObj.PNR = passenger.PNR;

                passengerDetails.Add(passObj);
            }

            _context.PassengerDetails.AddRangeAsync(passengerDetails);
            _context.SaveChanges();

            return Ok(new { Message = "Passenger details added successfully." });
        }


        [HttpPost("CancelTicket")]
        [Authorize(Roles = "user")]
        public async Task<IActionResult> CancelTicket(int id)
        {
            var bookingHistory = _context.Bookings.SingleOrDefault(b => b.Id == id);

            if (bookingHistory == null)
            {
                return BadRequest( new { Message = $"Train related to the id {id} is not available" });
            }

            var trainDetails = _context.TrainDetails.SingleOrDefault(x => x.Id == bookingHistory.TrainId);

            // Increase the seat capacity of the train
            trainDetails.SeatCapacity += bookingHistory.ticketCount;

            // Remove passenger details associated with the booking
            var passengerDetails = _context.PassengerDetails.Where(p => p.PNR == bookingHistory.PNR).ToList();
            _context.PassengerDetails.RemoveRange(passengerDetails);

            string userId = _contentAccessor.HttpContext.User.FindFirstValue("Id");

            int Id = Convert.ToInt32(userId);
            UserProfileDetails user = _context.UserProfileDetails.FirstOrDefault(user => user.UserId == Id);

            string emailBody = $"Cancel successfull, here is the PNR number for your cancelled booking {bookingHistory.PNR},\nTrain Details \nTrain Number : {trainDetails.TrainId}\nTrain Name : {trainDetails.TrainName}\n Travel Date : {trainDetails.Departure} - {trainDetails.Arrival}\nTickets : {bookingHistory.ticketCount}";

            em.SendEmail(emailBody, user.UserEmail);

            // Remove the booking
            _context.Bookings.Remove(bookingHistory);

            _context.SaveChanges();

            return Ok( new { Message = $"Ticket with PNR {bookingHistory.PNR} has been cancelled successfully." });
        }


        [HttpGet("BookedTicketHistory")]
        [Authorize(Roles = "user")]
        public async Task<ActionResult<IEnumerable<BookingHistory>>> BookedTicketHistory()
        {
            string tempUserId = _contentAccessor.HttpContext.User.FindFirstValue("Id");
            int UserId = Convert.ToInt32(tempUserId);

            var bookingHistory = _context.Bookings.Where(x => x.UserId == UserId).ToList();

            if (bookingHistory.Count == 0)
            {
                return NotFound("No booking history found");
            }

            return bookingHistory;
        }

        [HttpGet("GetTrainUser")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TrainDetails>>> GetTrainUser()
        {
            if (_context.TrainDetails == null)
            {
                return NotFound("No trains are found");
            }
            return await _context.TrainDetails.ToListAsync();
        }

        [HttpGet("GetBookingByPNR/{pnr}")]
        [AllowAnonymous]
        public async Task<ActionResult<PNR_ClassMembers>> GetBookingByPNR(int pnr)
        {
            PNR_ClassMembers pNR_Main_ClassMembers = new PNR_ClassMembers();
            List<PNR_PassengerDetails> pNR_Passengers = new List<PNR_PassengerDetails>();

            var bookingHistory = _context.Bookings.FirstOrDefault(b => b.PNR == pnr);

            if (bookingHistory == null)
            {
                return BadRequest($"Booking for the pnr {pnr} not found.");
            }

            TrainDetails trainDetails = _context.TrainDetails.FirstOrDefault(b => b.Id == bookingHistory.TrainId);
            BookingHistory bookingDetails = _context.Bookings.FirstOrDefault(b => b.PNR == pnr);

            var temp_PNR_Passengers = _context.PassengerDetails.Where(b => b.PNR == pnr).ToList();

            foreach (var item in temp_PNR_Passengers)
            {
                PNR_PassengerDetails member = new PNR_PassengerDetails()
                {
                    Name = item.Name,
                    Age = item.Age,
                    Gender = item.Gender
                };
                pNR_Passengers.Add(member);
            }

            pNR_Main_ClassMembers.PNR = pnr;

            pNR_Main_ClassMembers.PassengerDetails = pNR_Passengers;
            pNR_Main_ClassMembers.TrainName = trainDetails.TrainName;
            pNR_Main_ClassMembers.TrainId = trainDetails.TrainId;
            pNR_Main_ClassMembers.Origin = trainDetails.Origin;
            pNR_Main_ClassMembers.Destination = trainDetails.Destination;
            pNR_Main_ClassMembers.Departure = trainDetails.Departure;
            pNR_Main_ClassMembers.Arrival = trainDetails.Arrival;

            pNR_Main_ClassMembers.BookingDate = bookingDetails.BookingDate;
            pNR_Main_ClassMembers.TicketCount = bookingDetails.ticketCount;

            return Ok(pNR_Main_ClassMembers);
        }
    }
}