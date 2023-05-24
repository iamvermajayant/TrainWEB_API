using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TRS_WebApi.Models;
using WebApi.Models;

namespace WebApi.Data
{
    public class WebApiContext : DbContext
    {
        public WebApiContext (DbContextOptions<WebApiContext> options)
            : base(options)
        {
        }
        
        public DbSet<UserProfileDetails> UserProfileDetails { get; set; }

        public DbSet<BookingHistory> Bookings { get; set; }
        public DbSet<TrainDetails> TrainDetails { get; set; }
        public DbSet<OlderTrainDetails> OlderTrainDetails { get; set; }

        public DbSet<PassengerDetails> PassengerDetails { get; set; }

    }
}
