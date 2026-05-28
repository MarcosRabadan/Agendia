namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>No appointment belongs to the requested series id. Maps to HTTP 404.</summary>
    public class AppointmentSeriesNotFoundException : NotFoundException
    {
        public override string Code => "APPOINTMENT_SERIES_NOT_FOUND";

        public AppointmentSeriesNotFoundException(Guid seriesId)
            : base($"No existe ninguna serie de citas con el identificador {seriesId}.")
        {
        }
    }
}
