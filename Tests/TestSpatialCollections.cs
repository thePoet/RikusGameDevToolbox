using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

public class TestSpatialCollections 
{
    private class Item
    {
        public int Id;
        public Vector2 Position;
    }
    
    [Test]
    public void Inserting()
    {
        var items = CreateTestItems();
        var sc = CollectionWithItems(items);
        Assert.IsTrue(SameContents(items, sc.ToList()));
    }
    
    
    [Test]
    public void ItemsInArea()
    {
        var items = CreateTestItems();
        var sc = CollectionWithItems(items);
        var searchArea = new Rect(0, 0, 50, 50);
        var expected = items.Where(item => searchArea.Contains(item.Position)).ToList();
        var actual = sc.ItemsInRectangle(searchArea);
        Assert.IsTrue(actual.Count > 0);
        Assert.IsTrue(SameContents(expected, actual));
    }
    
    [Test]
    public void ItemsInCircle()
    {
        var items = CreateTestItems();
        var sc = CollectionWithItems(items);
      
        Vector2 center = new Vector2(15, -20);
        float radius = 30;
        var expected = items.Where(item => Vector2.Distance(item.Position, center) <= radius).ToList();
        var actual = sc.ItemsInCircle(center, radius);
        Assert.IsTrue(actual.Count > 0);
        Assert.IsTrue(SameContents(expected, actual));
    }
    
    [Test]
    public void ClosestItem()
    {
        var items = CreateTestItems();
        var sc = CollectionWithItems(items);

        for (int i = 0; i < 50; i++)
        {
            Vector2 randomPoint = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
            Item expected = items.OrderBy(item => Vector2.Distance(item.Position, randomPoint)).First();
            Item actual = sc.Closest(randomPoint);
            Assert.IsTrue(actual == expected);
        }
    }
    
    [Test]
    public void DeleteItems()
    {
        var items = CreateTestItems();
        var sc = CollectionWithItems(items);
     
        
        var searchArea = new Rect(0, 0, 30, 30);
        var expectedDeleted = items.Where(item => searchArea.Contains(item.Position)).ToList();
        var toBeDeleted = sc.ItemsInRectangle(searchArea);

        Assert.IsTrue(SameContents(expectedDeleted, toBeDeleted));
        Assert.IsTrue(toBeDeleted.Count > 0);
        
        foreach (var item in toBeDeleted)
        {
            sc.Remove(item.Position, item);
        }
        
        Assert.IsTrue(DifferentContents(sc.ToList(), toBeDeleted));
    }
    
    [Test]
    public void AddDeleteCyclesItems()
    {
     
        var sc = new SpatialCollection2d<Item>();
        List<Item> items = new List<Item>();

        for (int cycle = 0; cycle < 10; cycle++)
        {
            for (int i = 0; i < 100; i++)
            {
                Item item = new Item
                {
                    Id = i,
                    Position = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f))
                };
                sc.Add(item.Position, item);
                items.Add(item);
            }

            var searchArea = new Rect(0, 0, 30, 30);
            var expectedDeleted = items.Where(item => searchArea.Contains(item.Position)).ToList();
            var toBeDeleted = sc.ItemsInRectangle(searchArea);

            Assert.IsTrue(SameContents(expectedDeleted, toBeDeleted));
            Assert.IsTrue(toBeDeleted.Count > 0);

            foreach (var item in toBeDeleted)
            {
                sc.Remove(item.Position, item);
                items.RemoveAll(i => i == item);
            }

            Assert.IsTrue(SameContents(sc.ToList(), items));
        }
    }
    
    
    private SpatialCollection2d<Item> CollectionWithItems(List<Item> items)
    {
        var sc = new SpatialCollection2d<Item>();
        foreach (var item in items)
        {
            sc.Add(item.Position, item);
        }
        return sc;
    }
    
    private List<Item> CreateTestItems()
    {
        var items = new List<Item>();
        for (int i = 0; i < 100; i++)
        {
            Item item = new Item
            {
                Id = i,
                Position = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f))
            };
            items.Add(item);
        }

        return items;
    }
    
    private bool SameContents(List<Item> a, List<Item> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var item in a)
        {
            if (!b.Contains(item)) return false;
        }
        return true;
    }
    
    private bool DifferentContents(List<Item> a, List<Item> b)
    {
        foreach (var item in a)
        {
            if (b.Contains(item)) return false;
        }
        return true;
    }
}
