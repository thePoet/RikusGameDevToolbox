using NUnit.Framework;
using RikusGameDevToolbox.GeneralUse;

public class TestsUndirectedGraph
{
    [Test]
    public void Tests()
    {
        var a = new UndirectedGraph<int>();
        Assert.IsTrue(a.NumNodes == 0);
        
        a.AddNode(1);
        a.AddNode(2);
        a.AddNode(3);
        
        Assert.IsTrue(a.NumNodes == 3);
        a.AddLink(1, 2);
        a.AddLink(2, 3);

        Assert.IsTrue(a.AreLinked(3, 2));
        Assert.IsTrue(a.PathExist(1, 3));
        a.AddNode(4);
        Assert.IsFalse(a.PathExist(1, 4));
        Assert.IsFalse(a.IsConnected());
        a.AddLink(1,4);
        Assert.IsTrue(a.IsConnected());
        var aLinks = a.Links(1);
        Assert.IsTrue(aLinks.Count == 2);
        Assert.IsTrue(aLinks.Contains(2));
        Assert.IsTrue(aLinks.Contains(4));
        Assert.IsTrue(a.GraphsOfConnectedNodes().Count == 1);
        a.RemoveNode(2);
        Assert.IsTrue(a.NumNodes == 3);
        var groups = a.GraphsOfConnectedNodes();
        Assert.IsTrue(groups.Count == 2);
        Assert.IsTrue(a.NumNodes == 3);
        Assert.IsTrue(groups[0].NumNodes == 2);
        Assert.IsTrue(groups[1].NumNodes == 1);
        Assert.IsTrue(groups[0].ContainsNode(1));
        Assert.IsTrue(groups[0].ContainsNode(4));
        Assert.IsTrue(groups[0].AreLinked(1,4));
        Assert.IsTrue(groups[1].ContainsNode(3));
        Assert.IsFalse(groups[1].ContainsNode(1));

        var b = new UndirectedGraph<int>();
        b.AddNode(1);
        b.AddNode(2);
        b.AddLink(1,2);
        var c = new UndirectedGraph<int>();
        c.AddNode(3);
        c.AddNode(4);
        c.AddLink(3,4);
        b.Merge(c);
        Assert.IsTrue(b.NumNodes == 4);
        Assert.IsTrue(b.AreLinked(1,2));
        Assert.IsTrue(b.AreLinked(3,4));
        Assert.IsFalse(b.PathExist(1,3));
        Assert.IsFalse(b.IsConnected());
        b.AddLink(1,3);
        Assert.IsTrue(b.IsConnected());
    }


}
