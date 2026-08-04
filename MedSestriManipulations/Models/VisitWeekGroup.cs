namespace MedSestriManipulations.Models
{
    // A Monday-Sunday week of visits, for CollectionView's IsGrouped="True".
    public class VisitWeekGroup : List<Patient>
    {
        public string WeekRangeLabel { get; }

        public VisitWeekGroup(string weekRangeLabel, IEnumerable<Patient> visits) : base(visits)
        {
            WeekRangeLabel = weekRangeLabel;
        }
    }
}
