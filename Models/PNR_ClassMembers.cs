namespace TRS_WebApi.Models
{
    public class PNR_ClassMembers
    {
        public string TrainName { get; set; }
        public int TrainId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public List<PNR_PassengerDetails> PassengerDetails { get; set; }
        public DateTime BookingDate { get; set; }
        public int TicketCount { get; set; }
        public int PNR { get; set; }

    }
}
