namespace JollySchoolTimetabler.Models
{
    public class Room : ModelBase
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public string FullDetails
        {
            get => $"{RoomId} - {Name} {Capacity}";
        }
    }
}
