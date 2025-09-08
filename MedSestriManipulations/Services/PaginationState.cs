namespace MedSestriManipulations.Services
{
    public class PaginationState
    {
        public int VisibleThreshold { get; set; } = 30;
        public int CurrentIndex { get; set; } = 0;
        public bool IsLoading { get; set; } = false;

        public void Reset()
        {
            CurrentIndex = 0;
            IsLoading = false;
        }
    }

}
