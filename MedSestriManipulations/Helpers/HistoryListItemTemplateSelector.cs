using MedSestriManipulations.Models;

namespace MedSestriManipulations.Helpers
{
    public class HistoryListItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? HeaderTemplate { get; set; }
        public DataTemplate? PatientTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            return item is HistoryListItem { IsHeader: true }
                ? HeaderTemplate
                : PatientTemplate;
        }
    }
}
