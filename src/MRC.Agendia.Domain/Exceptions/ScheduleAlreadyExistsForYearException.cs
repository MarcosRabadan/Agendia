namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// A schedule generation targets a year the business already has a schedule for
    /// and the caller did not confirm the replacement. Maps to HTTP 400. The caller
    /// should re-issue the request with ReplaceExisting set to rebuild the year.
    /// </summary>
    public class ScheduleAlreadyExistsForYearException : DomainException
    {
        public override string Code => "SCHEDULE_YEAR_ALREADY_EXISTS";

        public ScheduleAlreadyExistsForYearException(int year)
            : base($"The business already has a schedule configured for year {year}. " +
                   "Generate again confirming the replacement to redo it.")
        {
        }
    }
}
