using Sicoob.Visualizer.Monitor.Dal.Models;

namespace Sicoob.Visualizer.Monitor.Comuns.Models;
public class ExportActivity
{
    public string Type { get => Enum.GetName(actv.Type); }
    public string Date { get => actv.Date.ToString("dd/MM/yyyy"); }
    public string Hour { get => actv.Date.ToString("HH:mm:ss"); }
    public string User { get => actv.Account.Name; }
    public string Email { get => actv.Account.Email; }
    public string Directory { get => actv.Item.Folder.Directory; }
    public string FileName { get => actv.Item.Name; }
    public string WebUrl { get => actv.Item.WebUrl; }

    private Activity actv;
    private ExportActivity(in Activity actv)
    {
        this.actv = actv;
    }
    internal static ExportActivity[] Convert(IEnumerable<Activity> activities)
    {
        ExportActivity[] result = new ExportActivity[activities.Count()];

        for (int i = 0; i < result.Length; i++)
        {
            Activity actv = activities.ElementAt(i);
            result[i] = new ExportActivity(actv);
        }

        return result;
    }

    public override string ToString()
    {
        return base.ToString();
    }
}