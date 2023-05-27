using TRS_WebApi.Models;

namespace WebApi.Models
{
    public class BookingHistoryModel
    {
        public int TrainId { get; set; }
        public List<PassengerDetails> PassengerDetails { get; set; }

    }
}
