using System.Collections.Generic;

namespace RikusGameDevToolbox.RTree
{
	public interface ISpatialIndex<T>
	{
		IEnumerable<T> All();
		IEnumerable<T> Search(Envelope boundingBox);
	}
}