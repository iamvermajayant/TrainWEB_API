using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using WebApi.Models.TableSchema;
using WebApi.Models.DTO;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly WebApiContext context;
        public AdminController(WebApiContext context1)
        {
            context = context1;
        }

        //--------------------------helper methods start here-----------------------------------------

        private bool TrainExists(int id)
        {
            return (context.TrainDetails?.Any(e => e.Id == id)).GetValueOrDefault();
        }
        
        private bool TrainNumberExists(int number)
        {
            return (context.TrainDetails?.Any(e => e.TrainId == number)).GetValueOrDefault();
        }

        //--------------------------helper methods ends here------------------------------------------



        //[Authorize(Roles = "admin")]
        //[HttpGet()]
        //public async Task<IActionResult> GetOrdersForAdmin()
        //{
        //    if (context.Bookings == null)
        //    {
        //        return NotFound();
        //    }
        //    var order = await context.Bookings.ToListAsync();
        //    return Ok(order);
        //}
        //[Authorize(Roles = "admin")]
        //[HttpGet("{useremail}")]
        //public async Task<IActionResult> GetOrdersForAdminByUser(string useremail)
        //{
        //    if (context.Bookings == null)
        //    {
        //        return NotFound();
        //    }
        //    if (useremail == "")
        //    {
        //        var trainModel = context.Bookings.ToList();
        //        return Ok(trainModel);
        //    }

        //    return BadRequest();
        //}

        [HttpPost("CreateTrain")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateTrain([FromBody] TrainDetailsModelToDB obj)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            if (TrainNumberExists(obj.TrainId))
            {
                return BadRequest("Train numnber already exists, please give another train number");
            }

            if (obj.Departure < DateTime.Now || obj.Arrival < obj.Departure)
            {
                return BadRequest("Need valid departure or arrival time.");
            }

            TrainDetails trainDetails = new TrainDetails();
            trainDetails.TrainName = obj.TrainName;
            trainDetails.TrainId = obj.TrainId;
            trainDetails.Origin = obj.Origin;
            trainDetails.Destination = obj.Destination;
            trainDetails.Departure = obj.Departure;
            trainDetails.Arrival = obj.Arrival;
            trainDetails.SeatCapacity = obj.SeatCapacity;
            trainDetails.SeatRate = obj.SeatRate;

            await context.TrainDetails.AddAsync(trainDetails);
            context.SaveChanges();

            return Ok( new { Message = "Added train successfully." });
        }


        [HttpGet("GetTrain")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<TrainDetails>>> GetTrain()
        {
            if (context.TrainDetails == null)
            {
                return NotFound();
            }
            return await context.TrainDetails.ToListAsync();
        }



        [HttpGet("GetTrainById/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<TrainDetails>> GetTrainById(int id)
        {
            if (context.TrainDetails == null)
            {
                return NotFound();
            }
            var trainModel = await context.TrainDetails.FindAsync(id);

            if (trainModel == null)
            {
                return NotFound();
            }

            return trainModel;
        }


        [HttpGet("GetTrainByName/{name}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<TrainDetails>> GetTrainByName(string name)
        {
            if (context.TrainDetails == null)
            {
                return NotFound();
            }
            var trainObj = context.TrainDetails.Where(w => w.TrainName.Contains(name)).FirstOrDefault();

            if (trainObj == null)
            {
                return NotFound();
            }

            return trainObj;
        }


        [HttpPut("UpdateTrainDetails/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateTrainDetails(int id, TrainDetails trainDetails)
        {
            if (id != trainDetails.Id)
            {
                return BadRequest( new { Message = "Train Id is not matching." });
            }

            context.Entry(trainDetails).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                return Ok( new { Message = "Details updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }




        [HttpDelete("DeleteTrainDetails/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteTrainDetails(int id)
        {
            if (context.TrainDetails == null)
            {
                return NotFound();
            }
            var TrainModel = await context.TrainDetails.FindAsync(id);
            if (TrainModel == null)
            {
                return NotFound();
            }

            context.TrainDetails.Remove(TrainModel);
            await context.SaveChangesAsync();

            return Ok( new { Message = "Deleted train successfully." });
        }


        [HttpGet("AllBookings")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<BookingHistory>>> AllBookings()
        {
            List<BookingHistory> bookings = context.Bookings.ToList();

            if (bookings.Count == 0)
            {
                return Ok( new { Message = "No bookings found" });
            }
            return await context.Bookings.ToListAsync();
        }

        [HttpDelete("DeleteBooking/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            if (context.Bookings == null)
            {
                return NotFound();
            }
            var BookingsModel = await context.Bookings.FindAsync(id);
            var TrainDetails = await context.TrainDetails.FindAsync(BookingsModel.TrainId);

            List<PassengerDetails> PassengerModel = new List<PassengerDetails>();

            if (BookingsModel != null)
            {
                PassengerModel = context.PassengerDetails.Where(x => x.PNR == BookingsModel.PNR).ToList();
            }
            else
            {
                return NotFound();
            }

            TrainDetails.SeatCapacity += BookingsModel.ticketCount;

            context.Bookings.Remove(BookingsModel);
            context.PassengerDetails.RemoveRange(PassengerModel);

            await context.SaveChangesAsync();

            return Ok(new { Message = $"Deleted Booking with PNR {BookingsModel.PNR} successfully." });
        }
    }
}
