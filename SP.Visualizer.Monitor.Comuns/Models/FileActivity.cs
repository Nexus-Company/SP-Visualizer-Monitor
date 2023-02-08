using ActivityType = SP.Visualizer.Monitor.Dal.Models.Enums.ActivityType;

namespace SP.Visualizer.Monitor.Comuns.Helpers;
public class FileActivity
{
    public int Id { get; set; }
    public DateTime ActivityDateTime { get; set; }
    public ActivityType Type { get; set; }
    public UserActor Actor { get; set; }

    public class UserActor
    {
        public User User { get; set; }
    }
    public class User
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Id { get; set; }
    }
}