using ScottPlot;

namespace LinearCongruentialGenerator;
public class PlotCreator
{
    private Plot _plot;
    public void CreatePlot(double[] x_Data, string x_Title, double[] y_Data, string y_Title)
    {
        _plot = new Plot();
        _plot.AddScatter(x_Data, y_Data);
        _plot.XAxis.Label(x_Title);
        _plot.YAxis.Label(y_Title);
        _plot.SetAxisLimitsY(0, y_Data.Max() + 1);
    }
    public void Save(string path, string name)
    {
        var fullPath = Path.Combine(path, name);
        _plot.SaveFig(fullPath);
    }
}
