namespace WebApi.Models.DTO
{
    public class BookingHistoryModel
    {
        public int TrainId { get; set; }
        public List<PNR_PassengerDetails> PassengerDetails { get; set; }

    }
}
