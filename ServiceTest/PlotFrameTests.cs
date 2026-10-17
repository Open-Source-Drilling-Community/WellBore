extern alias WellBoreWebPages;

using System.Collections;
using System.Reflection;
using Plot2D = WellBoreWebPages::OSDC.Drilling.WellBore.WebPages.ScatterPlot;
using Plot3D = WellBoreWebPages::OSDC.Drilling.WellBore.WebPages.Scatter3DPlot;

namespace OSDC.Drilling.WellBore.ServiceTest;

public class PlotFrameTests
{
    [TestCase(typeof(Plot2D))]
    [TestCase(typeof(Plot3D))]
    public void BuildsCompleteFrameWithoutCallingPlotlyAndClearsOldTraces(Type type)
    {
        object component = Activator.CreateInstance(type)!;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        // An unrendered chart cannot perform JS interop. Building a frame must not call it.
        FieldInfo chart = type.GetField("chart", flags)!;
        chart.SetValue(component, Activator.CreateInstance(chart.FieldType));
        type.GetField("_plotlyReady", flags)!.SetValue(component, true);
        PropertyInfo parent = type.GetProperty("Parent", flags)!;
        parent.SetValue(component, Activator.CreateInstance(parent.PropertyType));
        foreach (string axis in new[] { "X", "Y", "Z" })
            type.GetProperty(axis + "Unit")?.SetValue(component, string.Empty);
        List<List<object>> coordinates = Enumerable.Range(0, 200)
            .Select(i => new List<object> { (double)i, i + .5, (double)i }).ToList();
        foreach (string axis in new[] { "X", "Y", "Z" })
            type.GetProperty(axis + "ValuesList")?.SetValue(component, coordinates);
        var frame = (IList)type.GetField("data", flags)!.GetValue(component)!;
        MethodInfo build = type.GetMethod("ManageTraces", flags)!;
        build.Invoke(component, null);
        Assert.That(frame.Count, Is.EqualTo(200), "All trajectories and ellipses belong in one frame");
        object first = frame[0]!;
        Assert.That((IEnumerable)first.GetType().GetProperty("X")!.GetValue(first)!, Is.EqualTo(coordinates[0]));
        Assert.That((IEnumerable)first.GetType().GetProperty("Y")!.GetValue(first)!, Is.EqualTo(coordinates[0]));
        if (type == typeof(Plot3D))
            Assert.That((IEnumerable)first.GetType().GetProperty("Z")!.GetValue(first)!, Is.EqualTo(coordinates[0]));
        build.Invoke(component, null);
        Assert.That(frame.Count, Is.EqualTo(200), "Rebuilding replaces rather than duplicates traces");
        foreach (string axis in new[] { "X", "Y", "Z" })
            type.GetProperty(axis + "ValuesList")?.SetValue(component, new List<List<object>>());
        build.Invoke(component, null);
        Assert.That(frame, Is.Empty, "An empty selection must remove the previous geometry");
    }
}
