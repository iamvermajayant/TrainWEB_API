using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebApi.Data;
using Microsoft.AspNetCore.Authorization;
using TRS_WebApi.Models;
using WebApi.Models;

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

            return Ok("Added train successfully.");
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
        public async Task<IActionResult> UpdateTrainDetails(int id, TrainDetails trainDetails)
        {
            if (id != trainDetails.Id)
            {
                return BadRequest("Train Id is not matching.");
            }

            context.Entry(trainDetails).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                return Ok("Details updated successfully");
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

            return NoContent();
        }




        [HttpDelete("DeleteTrainDetails/{id}")]
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

            return Ok("Deleted train successfully.");
        }


    }
}
