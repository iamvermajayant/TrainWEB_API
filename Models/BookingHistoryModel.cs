using TRS_WebApi.Models;

namespace WebApi.Models
{
    public class BookingHistoryModel
    {
        public int TrainId { get; set; }
        public List<PNR_PassengerDetails> PassengerDetails { get; set; }

    }
}
