using System.Collections.Generic;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal static class MintermGenerator<TSet> where TSet : IComparable<TSet>
{
	private sealed class PartitionTree
	{
		internal readonly TSet _set;

		private PartitionTree _left;

		private PartitionTree _right;

		internal PartitionTree(TSet set)
		{
			_set = set;
		}

		internal void Refine(ISolver<TSet> solver, TSet other)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				StackHelper.CallOnEmptyStack(Refine, solver, other);
				return;
			}
			TSet set = solver.And(_set, other);
			if (solver.IsEmpty(set))
			{
				return;
			}
			TSet set2 = solver.And(_set, solver.Not(other));
			if (!solver.IsEmpty(set2))
			{
				if (_left == null)
				{
					_left = new PartitionTree(set);
					_right = new PartitionTree(set2);
				}
				else
				{
					_left.Refine(solver, other);
					_right.Refine(solver, other);
				}
			}
		}

		internal List<TSet> GetLeafSets()
		{
			List<TSet> list = new List<TSet>();
			Stack<PartitionTree> stack = new Stack<PartitionTree>();
			stack.Push(this);
			PartitionTree result;
			while (stack.TryPop(out result))
			{
				if (result._left == null && result._right == null)
				{
					list.Add(result._set);
					continue;
				}
				stack.Push(result._left);
				stack.Push(result._right);
			}
			return list;
		}
	}

	public static List<TSet> GenerateMinterms(ISolver<TSet> solver, HashSet<TSet> sets)
	{
		PartitionTree partitionTree = new PartitionTree(solver.Full);
		foreach (TSet set in sets)
		{
			partitionTree.Refine(solver, set);
		}
		return partitionTree.GetLeafSets();
	}
}
