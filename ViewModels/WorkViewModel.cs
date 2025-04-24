namespace DepartmentLibrary.ViewModels
{
    public class WorkViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Annotation { get; set; }
        public DateTime PublishDate { get; set; }
        public List<string> AuthorNames { get; set; }
    }
}
